using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

// ReSharper disable ParameterHidesMember
namespace NeoBeard.Crypto.X509;

public class CertificateRequestBuilder : ICertificateRequestBuilder
{
    private readonly List<Oid> enhancedKeyUsage = new();

    private readonly List<X509Extension> extensions = new();

    private readonly List<(string MethodOid, Uri Location)> authorityInformationAccess = new();

    private SubjectAlternativeNameBuilder sanBuilder = new();

    private ECDsa? ecdsa;

    private bool hasPathConstraint;

    private bool isCa;

    private bool isBasicConstraintsCritical;

    private bool isEnhancedKeyUsageCritical;

    private bool isKeyUsageCritical;

    private bool isPathConstraintCritical;

    private X509KeyUsageFlags keyUsage;

    private DateTimeOffset? notAfter;

    private DateTimeOffset? notBefore;

    private int pathLengthConstraint;

    private CertificateRequest? request;

    private RSA? rsa;

    private RSASignaturePadding rsaSignaturePadding = RSASignaturePadding.Pkcs1;

    private byte[]? serialNumber;

    private X509Certificate2? signer;

    private HashAlgorithmName signingAlgo = HashAlgorithmName.SHA256;

    private string? subject;

    public ICertificateRequestBuilder AsCertificateAuthority(bool isCertificateAuthority = true, bool critical = true)
    {
        this.isCa = isCertificateAuthority;
        this.isBasicConstraintsCritical = critical;
        return this;
    }

    public CertificateRequest Build()
    {
        if (this.subject.IsNullOrWhiteSpace())
        {
            throw new InvalidOperationException("The subject for the certificate request must not be null.");
        }

        if (this.ecdsa is not null)
        {
            this.request = new CertificateRequest(this.subject, this.ecdsa, this.signingAlgo);
        }
        else
        {
            this.rsa ??= RSA.Create(2048);
            this.request = new CertificateRequest(
                this.subject,
                this.rsa,
                this.signingAlgo,
                this.rsaSignaturePadding);
        }

        CertificateRequest? req = this.request;
        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                this.isCa,
                this.hasPathConstraint,
                this.pathLengthConstraint,
                this.isBasicConstraintsCritical || this.isPathConstraintCritical));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(this.keyUsage, this.isKeyUsageCritical));

        if (this.signer != null)
        {
            // set the AuthorityKeyIdentifier. There is no built-in
            // support, so it needs to be copied from the Subject Key
            // Identifier of the signing certificate and massaged slightly.
            // AuthorityKeyIdentifier is "KeyID=<subject key identifier>"
            foreach (X509Extension? item in this.signer.Extensions)
            {
                // "Subject Key Identifier"
                if (item.Oid?.Value == "2.5.29.14")
                {
                    byte[] issuerSubjectKey = item.RawData;

                    // var issuerSubjectKey = signingCertificate.Extensions["Subject Key Identifier"].RawData;
                    ArraySegment<byte> segment = new(issuerSubjectKey, 2, issuerSubjectKey.Length - 2);
                    byte[] authorityKeyIdentifier = new byte[segment.Count + 4];

                    // "KeyID" bytes
                    authorityKeyIdentifier[0] = 0x30;
                    authorityKeyIdentifier[1] = 0x16;
                    authorityKeyIdentifier[2] = 0x80;
                    authorityKeyIdentifier[3] = 0x14;

#if NETLEGACY
                    var j = 4;
                    foreach (var bit in segment)
                    {
                        authorityKeyIdentifier[j] = bit;
                        j++;
                    }
#else
                    segment.CopyTo(authorityKeyIdentifier, 4);
#endif

                    this.request.CertificateExtensions.Add(
                        new X509Extension("2.5.29.35", authorityKeyIdentifier, false));
                    break;
                }
            }
        }

        var subjectAlternativeName = this.sanBuilder.Build();
        if (subjectAlternativeName.RawData.Length > 2)
        {
            req.CertificateExtensions.Add(subjectAlternativeName);
        }

        OidCollection oidCollection = new();

        foreach (Oid? id in this.enhancedKeyUsage)
        {
            oidCollection.Add(id);
        }

        if (oidCollection.Count > 0)
        {
            req.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(oidCollection, this.isEnhancedKeyUsageCritical));
        }

        req.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        foreach (var extension in this.extensions)
        {
            req.CertificateExtensions.Add(extension);
        }

        if (this.authorityInformationAccess.Count > 0)
        {
            req.CertificateExtensions.Add(BuildAuthorityInformationAccess(this.authorityInformationAccess));
        }

        return req;
    }

    public X509Certificate2 BuildCertificate()
    {
        this.request ??= this.Build();
        DateTimeOffset nb = (this.notBefore ?? DateTime.UtcNow).ToUniversalTime();
        DateTimeOffset na = (this.notAfter ?? DateTime.UtcNow.AddYears(5)).ToUniversalTime();

        if (this.signer is null)
        {
            return this.request.CreateSelfSigned(nb, na);
        }

        byte[] serial;
        if (this.serialNumber is not null && this.serialNumber.Length > 0)
        {
            serial = this.serialNumber;
        }
        else
        {
#if NET8_0_OR_GREATER
            var epoch = DateTime.UnixEpoch;
#else
            DateTime epoch = new(
                1970,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);
#endif
            ulong unixTime = Convert.ToUInt64((DateTime.UtcNow - epoch).TotalSeconds);
            Span<byte> serialByteSpan = new(new byte[sizeof(ulong)]);
            BinaryPrimitives.WriteUInt64BigEndian(serialByteSpan, unixTime);
            serial = serialByteSpan.ToArray();
        }

        var cert = this.request.Create(
            this.signer,
            nb,
            na,
            serial);

        // CertificateRequest.Create does not include the private key,
        // so we need to copy it to the generated certificate
        if (this.rsa is not null)
        {
            return cert.CopyWithPrivateKey(this.rsa);
        }

        if (this.ecdsa is not null)
        {
            return cert.CopyWithPrivateKey(this.ecdsa);
        }

        return cert;
    }

    public ICertificateRequestBuilder Reset()
    {
        this.enhancedKeyUsage.Clear();
        this.extensions.Clear();
        this.authorityInformationAccess.Clear();
        this.sanBuilder = new SubjectAlternativeNameBuilder();
        this.ecdsa = null;
        this.rsa = null;
        this.isCa = false;
        this.isBasicConstraintsCritical = false;
        this.isEnhancedKeyUsageCritical = false;
        this.isKeyUsageCritical = false;
        this.isPathConstraintCritical = false;
        this.keyUsage = X509KeyUsageFlags.None;
        this.notAfter = null;
        this.notBefore = null;
        this.pathLengthConstraint = 0;
        this.request = null;
        this.rsaSignaturePadding = RSASignaturePadding.Pkcs1;
        this.serialNumber = null;
        this.signingAlgo = HashAlgorithmName.SHA256;
        this.subject = null;
        return this;
    }

    public ICertificateRequestBuilder WithDnsNames(params string[]? dnsNames)
    {
        if (!(dnsNames?.Length > 0))
        {
            return this;
        }

        foreach (string dnsName in dnsNames)
        {
            this.sanBuilder.AddDnsName(dnsName);
        }

        return this;
    }

    public ICertificateRequestBuilder WithEmails(params string[]? emails)
    {
        if (!(emails?.Length > 0))
        {
            return this;
        }

        foreach (string email in emails)
        {
            this.sanBuilder.AddEmailAddress(email);
        }

        return this;
    }

    public ICertificateRequestBuilder WithIpAddresses(params IPAddress[]? ipAddresses)
    {
        if (!(ipAddresses?.Length > 0))
        {
            return this;
        }

        foreach (IPAddress ipAddress in ipAddresses)
        {
            this.sanBuilder.AddIpAddress(ipAddress);
        }

        return this;
    }

    public ICertificateRequestBuilder WithUserPrincipalNames(params string[]? userPrincipalNames)
    {
        if (userPrincipalNames?.Length > 0)
        {
            foreach (string upn in userPrincipalNames)
            {
                this.sanBuilder.AddUserPrincipalName(upn);
            }
        }

        return this;
    }

    public ICertificateRequestBuilder WithRsa(int keySize, RSASignaturePadding? padding = null, bool force = false)
    {
        if ((!force && keySize < 2048) &&
            keySize % 1024 == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(keySize),
                keySize,
                "keySize must be a multiple of 1024 and have a value of 2048 or greater");
        }

        this.ecdsa = null;
        this.rsa = RSA.Create(keySize);
        this.rsaSignaturePadding = padding ?? RSASignaturePadding.Pkcs1;
        return this;
    }

    public ICertificateRequestBuilder WithECDsa(int keySize)
    {
        this.rsa = null;
        this.ecdsa = ECDsa.Create();
        if (this.ecdsa is null)
        {
            throw new PlatformNotSupportedException("ECDsa is not supported for this version of .NET");
        }

        var allowedSizes = new[]
        {
            256, 384, 521,
        };

        if (Array.IndexOf(allowedSizes, keySize) == -1)
        {
            throw new ArgumentOutOfRangeException(nameof(keySize), keySize, "keySize must be 256, 384, or 521");
        }

        this.ecdsa.KeySize = keySize;

        return this;
    }

    public ICertificateRequestBuilder WithEnhancedKeyUsages(params Oid[] enhancedKeyUsages)
    {
        foreach (Oid id in enhancedKeyUsages)
        {
            if (!this.enhancedKeyUsage.Contains(id))
            {
                this.enhancedKeyUsage.Add(id);
            }
        }

        return this;
    }

    /// <summary>
    /// Sets the enhanced key usages for the certificate with critical flag.
    /// </summary>
    /// <param name="critical">Whether the extension is critical.</param>
    /// <param name="enhancedKeyUsages">The enhanced key usages.</param>
    /// <returns>This builder instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var cert = new CertificateRequestBuilder()
    ///     .WithSubject("CN=Test Server")
    ///     .WithEnhancedKeyUsages(critical: true, EnhancedKeyUsageOids.ServerAuthentication)
    ///     .WithRsa(2048)
    ///     .BuildCertificate();
    /// </code>
    /// </example>
    /// </remarks>
    public ICertificateRequestBuilder WithEnhancedKeyUsages(bool critical, params EnhancedKeyUsageOids[] enhancedKeyUsages)
    {
        this.isEnhancedKeyUsageCritical = critical;
        return this.WithEnhancedKeyUsages(enhancedKeyUsages);
    }

    public ICertificateRequestBuilder WithEnhancedKeyUsages(params EnhancedKeyUsageOids[] enhancedKeyUsages)
    {
        foreach (EnhancedKeyUsageOids enumeration in enhancedKeyUsages)
        {
            Oid id;
            switch (enumeration)
            {
                case EnhancedKeyUsageOids.ClientAuthentication:
                    id = new Oid(Oids.ClientAuthentication);
                    break;

                case EnhancedKeyUsageOids.CodeSigning:
                    id = new Oid(Oids.CodeSigning);
                    break;

                case EnhancedKeyUsageOids.SecureEmail:
                    id = new Oid(Oids.SecureEmail);
                    break;

                case EnhancedKeyUsageOids.ServerAuthentication:
                    id = new Oid(Oids.ServerAuthentication);
                    break;

                case EnhancedKeyUsageOids.TimestampSigning:
                    id = new Oid(Oids.TimeStamping);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (!this.enhancedKeyUsage.Contains(id))
            {
                this.enhancedKeyUsage.Add(id);
            }
        }

        return this;
    }

    public ICertificateRequestBuilder WithKeyUsage(X509KeyUsageFlags flags, bool critical = false)
    {
        this.keyUsage = flags;
        this.isKeyUsageCritical = critical;
        return this;
    }

    public ICertificateRequestBuilder WithSigningAlgorithm(HashAlgorithmName name)
    {
        this.signingAlgo = name;
        return this;
    }

    public ICertificateRequestBuilder WithExtensions(params X509Extension[] extensions)
    {
        foreach (X509Extension ext in extensions)
        {
            if (!this.extensions.Contains(ext))
            {
                this.extensions.Add(ext);
            }
        }

        return this;
    }

    public ICertificateRequestBuilder WithCertificatePolicies(params Oid[] policies)
    {
        if (policies.Length == 0)
            return this;

        var encodedPolicies = new List<byte>();
        foreach (var policy in policies)
        {
            if (policy.Value.IsNullOrWhiteSpace())
                throw new ArgumentException("Certificate policy OIDs must have a value.", nameof(policies));

            encodedPolicies.AddRange(EncodeSequence(EncodeOid(policy.Value!)));
        }

        this.extensions.Add(new X509Extension("2.5.29.32", EncodeSequence(encodedPolicies.ToArray()), critical: false));
        return this;
    }

    public ICertificateRequestBuilder WithPathLengthConstraint(int pathLengthConstraint, bool critical)
    {
        this.pathLengthConstraint = pathLengthConstraint;
        this.hasPathConstraint = true;
        this.isPathConstraintCritical = critical;

        return this;
    }

    public ICertificateRequestBuilder WithIssuer(X509Certificate2 issuer)
    {
        this.signer = issuer;
        return this;
    }

    public ICertificateRequestBuilder WithOcspUrls(params Uri[] urls)
    {
        foreach (var url in urls)
            this.authorityInformationAccess.Add(("1.3.6.1.5.5.7.48.1", url));

        return this;
    }

    public ICertificateRequestBuilder WithCaIssuerUrls(params Uri[] urls)
    {
        foreach (var url in urls)
            this.authorityInformationAccess.Add(("1.3.6.1.5.5.7.48.2", url));

        return this;
    }

    public ICertificateRequestBuilder WithSubject(string subject)
    {
        this.subject = subject;
        return this;
    }

    public ICertificateRequestBuilder WithStartDate(DateTimeOffset startAt)
    {
        this.notBefore = startAt;
        return this;
    }

    public ICertificateRequestBuilder WithEndDate(DateTimeOffset endAt)
    {
        this.notAfter = endAt;
        return this;
    }

    public ICertificateRequestBuilder WithDateRange(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        this.notBefore = startAt;
        this.notAfter = endAt;

        return this;
    }

    public ICertificateRequestBuilder WithSerialNumber(int serialNumber)
    {
        if (serialNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(serialNumber), serialNumber, "serialNumber must be greater than 0");

        this.serialNumber = BitConverter.GetBytes(serialNumber);
        return this;
    }

    public ICertificateRequestBuilder WithSerialNumber(long serialNumber)
    {
        if (serialNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(serialNumber), serialNumber, "serialNumber must be greater than 0");

        this.serialNumber = BitConverter.GetBytes(serialNumber);
        return this;
    }

    [CLSCompliant(false)]
    public ICertificateRequestBuilder WithSerialNumber(uint serialNumber)
    {
        this.serialNumber = BitConverter.GetBytes(serialNumber);
        return this;
    }

    [CLSCompliant(false)]
    public ICertificateRequestBuilder WithSerialNumber(ulong serialNumber)
    {
        this.serialNumber = BitConverter.GetBytes(serialNumber);
        return this;
    }

    public ICertificateRequestBuilder WithSerialNumber(byte[] serialNumber)
    {
        this.serialNumber = serialNumber;
        return this;
    }

    public ICertificateRequestBuilder WithUrls(params Uri[]? uris)
    {
        if (!(uris?.Length > 0))
        {
            return this;
        }

        foreach (Uri uri in uris)
        {
            this.sanBuilder.AddUri(uri);
        }

        return this;
    }

    private static X509Extension BuildAuthorityInformationAccess(IEnumerable<(string MethodOid, Uri Location)> descriptions)
    {
        return new X509Extension(
            "1.3.6.1.5.5.7.1.1",
            EncodeSequence(descriptions.SelectMany(static description => CreateAccessDescription(description.MethodOid, description.Location)).ToArray()),
            critical: false);
    }

    private static byte[] CreateAccessDescription(string methodOid, Uri location)
    {
        var method = EncodeOid(methodOid);
        var uri = EncodeContextSpecificPrimitive(6, System.Text.Encoding.ASCII.GetBytes(location.AbsoluteUri));
        return EncodeSequence(method.Concat(uri).ToArray());
    }

    private static byte[] EncodeOid(string oid)
    {
        var parts = oid.Split('.').Select(uint.Parse).ToArray();
        if (parts.Length < 2 || parts[0] > 2 || parts[1] > 39)
            throw new ArgumentException("The OID is invalid.", nameof(oid));

        var body = new List<byte> { (byte)((parts[0] * 40) + parts[1]) };
        foreach (var part in parts.Skip(2))
            body.AddRange(EncodeBase128(part));

        return EncodeTlv(0x06, body.ToArray());
    }

    private static byte[] EncodeSequence(byte[] value)
        => EncodeTlv(0x30, value);

    private static byte[] EncodeContextSpecificPrimitive(int tag, byte[] value)
        => EncodeTlv(0x80 | tag, value);

    private static byte[] EncodeTlv(int tag, byte[] value)
    {
        var length = EncodeLength(value.Length);
        return [(byte)tag, .. length, .. value];
    }

    private static byte[] EncodeLength(int length)
    {
        if (length < 128)
            return [(byte)length];

        var bytes = BitConverter.GetBytes(length).Reverse().SkipWhile(static b => b == 0).ToArray();
        return [(byte)(0x80 | bytes.Length), .. bytes];
    }

    private static IEnumerable<byte> EncodeBase128(uint value)
    {
        var parts = new Stack<byte>();
        parts.Push((byte)(value & 0x7f));
        value >>= 7;
        while (value > 0)
        {
            parts.Push((byte)(0x80 | (value & 0x7f)));
            value >>= 7;
        }

        return parts;
    }
}