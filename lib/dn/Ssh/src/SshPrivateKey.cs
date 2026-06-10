using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Ssh;

internal abstract class SshPrivateKey
{
    public static SshPrivateKey Parse(string text, string? passphrase = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var block = PemBlock.Parse(text);
        if (block.label1 == "RSA PRIVATE KEY")
            return new RsaSshPrivateKey(ParseRsaPrivateKey(block, passphrase));

        if (block.label1 == "PRIVATE KEY")
            return new RsaSshPrivateKey(ParsePkcs8PrivateKey(block, passphrase));

        if (block.label1 == "OPENSSH PRIVATE KEY")
            return ParseOpenSshPrivateKey(block.Body, passphrase);

        throw new FormatException($"Unsupported SSH private key type: {block.label1}");
    }

    public static string PublicKeyLine(string privateKeyText, string comment = "key", string? passphrase = null)
    {
        var key = Parse(privateKeyText, passphrase);
        var prefix = key.PublicKeyBlob.AsSpan(4, key.PublicKeyBlob[3]).ToArray();
        return Encoding.ASCII.GetString(prefix) + " " + Convert.ToBase64String(key.PublicKeyBlob) + " " + comment;
    }

    public abstract string Algorithm { get; }

    public abstract byte[] PublicKeyBlob { get; }

    public abstract byte[] Sign(byte[] data);

    private static RSA ParseRsaPrivateKey(PemBlock block, string? passphrase)
    {
        var der = block.IsEncrypted ? DecryptLegacyPem(block, passphrase) : block.Body;
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(der, out _);
        return rsa;
    }

    private static RSA ParsePkcs8PrivateKey(PemBlock block, string? passphrase)
    {
        var rsa = RSA.Create();
        if (passphrase is null)
            rsa.ImportPkcs8PrivateKey(block.Body, out _);
        else
            rsa.ImportEncryptedPkcs8PrivateKey(passphrase, block.Body, out _);

        return rsa;
    }

    private static SshPrivateKey ParseOpenSshPrivateKey(byte[] body, string? passphrase)
    {
        var magic = Encoding.ASCII.GetBytes("openssh-key-v1\0");
        if (!body.AsSpan().StartsWith(magic))
            throw new FormatException("Invalid OpenSSH private key header.");

        var reader = new SshBinaryReader(body.AsSpan(magic.Length).ToArray());
        var cipherName = reader.ReadString();
        var kdfName = reader.ReadString();
        var kdfOptions = reader.ReadStringBytes();
        var keyCount = reader.ReadUInt32();
        if (keyCount != 1)
            throw new NotSupportedException("Only single-key OpenSSH private keys are supported.");

        _ = reader.ReadStringBytes();
        var privateBlob = reader.ReadStringBytes();
        if (cipherName != "none" || kdfName != "none")
            privateBlob = DecryptOpenSshPrivateKey(cipherName, kdfName, kdfOptions, privateBlob, passphrase);
        else if (kdfOptions.Length != 0)
            throw new FormatException("Invalid unencrypted OpenSSH private key KDF options.");

        var privateReader = new SshBinaryReader(privateBlob);
        var check1 = privateReader.ReadUInt32();
        var check2 = privateReader.ReadUInt32();
        if (check1 != check2)
            throw new CryptographicException("OpenSSH private key check values do not match.");

        var kind = privateReader.ReadString();
        if (kind == "ssh-rsa")
        {
            var n = privateReader.ReadMpintBytes();
            var e = privateReader.ReadMpintBytes();
            var d = privateReader.ReadMpintBytes();
            _ = privateReader.ReadMpintBytes();
            var p = privateReader.ReadMpintBytes();
            var q = privateReader.ReadMpintBytes();
            _ = privateReader.ReadString();
            ValidateOpenSshPadding(privateReader.Remaining);

            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = n,
                Exponent = e,
                D = d,
                P = p,
                Q = q,
                DP = BigInteger.ModPow(new BigInteger(d, isUnsigned: true, isBigEndian: true), BigInteger.One, new BigInteger(p, isUnsigned: true, isBigEndian: true) - BigInteger.One).ToByteArray(isUnsigned: true, isBigEndian: true),
                DQ = BigInteger.ModPow(new BigInteger(d, isUnsigned: true, isBigEndian: true), BigInteger.One, new BigInteger(q, isUnsigned: true, isBigEndian: true) - BigInteger.One).ToByteArray(isUnsigned: true, isBigEndian: true),
                InverseQ = ModInverse(new BigInteger(q, isUnsigned: true, isBigEndian: true), new BigInteger(p, isUnsigned: true, isBigEndian: true)).ToByteArray(isUnsigned: true, isBigEndian: true),
            });
            return new RsaSshPrivateKey(rsa);
        }

        if (kind == "ssh-ed25519")
        {
            var publicKey = privateReader.ReadStringBytes();
            var privateKey = privateReader.ReadStringBytes();
            _ = privateReader.ReadString();
            ValidateOpenSshPadding(privateReader.Remaining);
            if (publicKey.Length != 32 || privateKey.Length != 64)
                throw new FormatException("Invalid OpenSSH Ed25519 key length.");

            return new Ed25519SshPrivateKey(privateKey[..32], publicKey);
        }

        throw new NotSupportedException($"Unsupported OpenSSH private key algorithm: {kind}");
    }

    private static byte[] DecryptOpenSshPrivateKey(
        string cipherName,
        string kdfName,
        byte[] kdfOptions,
        byte[] privateBlob,
        string? passphrase)
    {
        if (passphrase is null)
            throw new CryptographicException("OpenSSH private key passphrase is required.");

        if (kdfName != "bcrypt")
            throw new NotSupportedException($"Unsupported OpenSSH private key KDF: {kdfName}");

        var cipher = cipherName switch
        {
            "aes128-ctr" => (KeyLength: 16, IvLength: 16),
            "aes192-ctr" => (KeyLength: 24, IvLength: 16),
            "aes256-ctr" => (KeyLength: 32, IvLength: 16),
            _ => throw new NotSupportedException($"Unsupported OpenSSH private key cipher: {cipherName}"),
        };

        var optionsReader = new SshBinaryReader(kdfOptions);
        var salt = optionsReader.ReadStringBytes();
        var rounds = checked((int)optionsReader.ReadUInt32());
        if (!optionsReader.Remaining.IsEmpty)
            throw new FormatException("Invalid OpenSSH private key KDF options.");

        var material = OpenSshBcryptPbkdf.Key(Encoding.UTF8.GetBytes(passphrase), salt, rounds, cipher.KeyLength + cipher.IvLength);
        var output = privateBlob.ToArray();
        AesCtrTransform(material[..cipher.KeyLength], material[cipher.KeyLength..], output);
        return output;
    }

    private static byte[] DecryptLegacyPem(PemBlock block, string? passphrase)
    {
        if (passphrase is null)
            throw new CryptographicException("Private key passphrase is required.");

        if (!block.headers1.TryGetValue("DEK-Info", out var dekInfo))
            throw new FormatException("Encrypted PEM block is missing DEK-Info.");

        var parts = dekInfo.Split(',', 2);
        if (parts.Length != 2)
            throw new FormatException("Invalid encrypted PEM DEK-Info.");

        var iv = Convert.FromHexString(parts[1]);
        var keyLength = parts[0] switch
        {
            "AES-128-CBC" => 16,
            "AES-192-CBC" => 24,
            "AES-256-CBC" => 32,
            _ => throw new NotSupportedException($"Unsupported encrypted PEM cipher: {parts[0]}"),
        };

        var key = OpenSslBytesToKey(Encoding.UTF8.GetBytes(passphrase), iv[..8], keyLength);
        using var aes = Aes.Create();
#pragma warning disable SCS0013 // Legacy encrypted PEM requires the cipher mode declared in DEK-Info.
        aes.Mode = CipherMode.CBC;
#pragma warning restore SCS0013
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(block.Body, 0, block.Body.Length);
    }

    private static byte[] OpenSslBytesToKey(byte[] password, byte[] salt, int length)
    {
        var output = new List<byte>();
        var previous = Array.Empty<byte>();
        while (output.Count < length)
        {
            previous = MD5.HashData(Concat(previous, password, salt));
            output.AddRange(previous);
        }

        return output.Take(length).ToArray();
    }

    private static void AesCtrTransform(byte[] key, byte[] iv, byte[] data)
    {
        using var aes = Aes.Create();
#pragma warning disable SCS0013 // SSH AES-CTR uses the block cipher primitive directly.
        aes.Mode = CipherMode.ECB;
#pragma warning restore SCS0013
        aes.Padding = PaddingMode.None;
        aes.Key = key;
        using var encryptor = aes.CreateEncryptor();
        var counter = iv.ToArray();
        var block = new byte[16];
        for (var offset = 0; offset < data.Length; offset += block.Length)
        {
            encryptor.TransformBlock(counter, 0, counter.Length, block, 0);
            var count = Math.Min(block.Length, data.Length - offset);
            for (var i = 0; i < count; i++)
                data[offset + i] ^= block[i];

            IncrementCounter(counter);
        }
    }

    private static void IncrementCounter(byte[] counter)
    {
        for (var i = counter.Length - 1; i >= 0; i--)
        {
            counter[i]++;
            if (counter[i] != 0)
                return;
        }
    }

    private static void ValidateOpenSshPadding(ReadOnlySpan<byte> padding)
    {
        for (var i = 0; i < padding.Length; i++)
        {
            if (padding[i] != (byte)(i + 1))
                throw new FormatException("Invalid OpenSSH private key padding.");
        }
    }

    private static BigInteger ModInverse(BigInteger value, BigInteger modulus)
    {
        var a = value;
        var m = modulus;
        var x0 = BigInteger.Zero;
        var x1 = BigInteger.One;
        while (a > BigInteger.One)
        {
            var q = a / m;
            (a, m) = (m, a % m);
            (x0, x1) = (x1 - (q * x0), x0);
        }

        return x1 < BigInteger.Zero ? x1 + modulus : x1;
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        var length = arrays.Sum(static a => a.Length);
        var output = new byte[length];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, output, offset, array.Length);
            offset += array.Length;
        }

        return output;
    }

    private sealed class RsaSshPrivateKey : SshPrivateKey
    {
        private readonly RSA rsa;
        private readonly byte[] publicKeyBlob;

        public RsaSshPrivateKey(RSA rsa)
        {
            this.rsa = rsa;
            var parameters = rsa.ExportParameters(false);
            this.publicKeyBlob = SshBinaryWriter.Concat(
                SshBinaryWriter.String("ssh-rsa"),
                SshBinaryWriter.Mpint(parameters.Exponent ?? throw new CryptographicException("RSA exponent is missing.")),
                SshBinaryWriter.Mpint(parameters.Modulus ?? throw new CryptographicException("RSA modulus is missing.")));
        }

        public override string Algorithm => "rsa-sha2-256";

        public override byte[] PublicKeyBlob => this.publicKeyBlob;

        public override byte[] Sign(byte[] data)
        {
            var signature = this.rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return SshBinaryWriter.Concat(SshBinaryWriter.String(this.Algorithm), SshBinaryWriter.String(signature));
        }
    }

    private sealed class Ed25519SshPrivateKey : SshPrivateKey
    {
        private readonly byte[] seed;
        private readonly byte[] publicKey;
        private readonly byte[] publicKeyBlob;

        public Ed25519SshPrivateKey(byte[] seed, byte[] publicKey)
        {
            this.seed = seed;
            this.publicKey = publicKey.Length == 32 ? publicKey : Ed25519.PublicKeyFromSeed(seed);
            this.publicKeyBlob = SshBinaryWriter.Concat(SshBinaryWriter.String("ssh-ed25519"), SshBinaryWriter.String(this.publicKey));
        }

        public override string Algorithm => "ssh-ed25519";

        public override byte[] PublicKeyBlob => this.publicKeyBlob;

        public override byte[] Sign(byte[] data)
        {
            var signature = Ed25519.Sign(data, this.seed, this.publicKey);
            return SshBinaryWriter.Concat(SshBinaryWriter.String(this.Algorithm), SshBinaryWriter.String(signature));
        }
    }

    private sealed record PemBlock(string label1, Dictionary<string, string> headers1, byte[] body)
    {
        public byte[] Body { get; } = body;

        public bool IsEncrypted => this.headers1.TryGetValue("Proc-Type", out var procType) && procType.Contains("ENCRYPTED", StringComparison.OrdinalIgnoreCase);

        public static PemBlock Parse(string text)
        {
            var begin = text.IndexOf("-----BEGIN ", StringComparison.Ordinal);
            if (begin < 0)
                throw new FormatException("PEM block is missing.");

            var beginEnd = text.IndexOf("-----", begin + 11, StringComparison.Ordinal);
            if (beginEnd < 0)
                throw new FormatException("PEM begin marker is invalid.");

            var label = text[(begin + 11)..beginEnd];
            var endMarker = "-----END " + label + "-----";
            var end = text.IndexOf(endMarker, beginEnd, StringComparison.Ordinal);
            if (end < 0)
                throw new FormatException("PEM end marker is missing.");

            var lines = text[(beginEnd + 5)..end].Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var headers = new Dictionary<string, string>(StringComparer.Ordinal);
            var base64 = new StringBuilder();
            var inBody = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                var colon = line.IndexOf(':');
                if (!inBody && colon > 0)
                {
                    headers[line[..colon]] = line[(colon + 1)..].Trim();
                    continue;
                }

                inBody = true;
                base64.Append(line);
            }

            return new PemBlock(label, headers, Convert.FromBase64String(base64.ToString()));
        }
    }
}