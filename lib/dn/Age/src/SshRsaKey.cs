using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Age;

/// <summary>
/// Provides helpers for importing and exporting SSH RSA keys for age recipients.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var rsa = RSA.Create(2048);
/// var publicKey = SshRsaKey.ExportPublicKey(rsa);
/// var recipient = AgeRecipient.FromSshPublicKey(publicKey);
/// Console.WriteLine(recipient.Kind);
/// </code>
/// </example>
/// </remarks>
public static class SshRsaKey
{
    /// <summary>
    /// Exports an RSA key as an OpenSSH authorized_keys public key.
    /// </summary>
    /// <param name="rsa">The RSA key.</param>
    /// <returns>The OpenSSH public key text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var rsa = RSA.Create(2048);
    /// var publicKey = SshRsaKey.ExportPublicKey(rsa);
    /// Console.WriteLine(publicKey.StartsWith("ssh-rsa ", StringComparison.Ordinal));
    /// </code>
    /// </example>
    /// </remarks>
    public static string ExportPublicKey(RSA rsa)
    {
        ArgumentNullException.ThrowIfNull(rsa);
        var parameters = rsa.ExportParameters(false);
        var wire = BuildWireKey(parameters);
        return "ssh-rsa " + Convert.ToBase64String(wire);
    }

    internal static (RSA PublicKey, byte[] WireKey) ParsePublicKey(string authorizedKey)
    {
        var text = authorizedKey.AsSpan().Trim();
        var firstSpace = text.IndexOf(' ');
        if (firstSpace < 0)
            throw new FormatException("Only ssh-rsa public keys are supported.");

        if (!text[..firstSpace].SequenceEqual("ssh-rsa".AsSpan()))
            throw new FormatException("Only ssh-rsa public keys are supported.");

        var remainder = text[(firstSpace + 1)..];
        var secondSpace = remainder.IndexOf(' ');
        var wireValue = (secondSpace < 0 ? remainder : remainder[..secondSpace]).ToString();

        var wire = Convert.FromBase64String(wireValue);
        var reader = new SshWireReader(wire);
        var kind = Encoding.ASCII.GetString(reader.ReadString());
        if (!string.Equals(kind, "ssh-rsa", StringComparison.Ordinal))
            throw new FormatException("The SSH public key is not ssh-rsa.");

        var exponent = reader.ReadMpint();
        var modulus = reader.ReadMpint();
        if (!reader.IsComplete)
            throw new FormatException("The SSH public key contains trailing data.");

        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters { Exponent = exponent, Modulus = modulus });

        if (rsa.KeySize < 2048)
        {
            rsa.Dispose();
            throw new FormatException("SSH RSA keys must be at least 2048 bits.");
        }

        return (rsa, wire);
    }

    internal static byte[] BuildWireKey(RSAParameters parameters)
    {
        var exponent = parameters.Exponent ?? throw new ArgumentException("RSA exponent is missing.", nameof(parameters));
        var modulus = parameters.Modulus ?? throw new ArgumentException("RSA modulus is missing.", nameof(parameters));

        using var stream = new MemoryStream();
        WriteString(stream, Encoding.ASCII.GetBytes("ssh-rsa"));
        WriteMpint(stream, exponent);
        WriteMpint(stream, modulus);
        return stream.ToArray();
    }

    internal static void WriteString(Stream stream, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)value.Length);
        stream.Write(length);
        stream.Write(value);
    }

    private static void WriteMpint(Stream stream, ReadOnlySpan<byte> value)
    {
        var start = 0;
        while (start < value.Length - 1 && value[start] == 0)
            start++;

        var needsSignBitClear = (value[start] & 0x80) != 0;
        if (!needsSignBitClear)
        {
            WriteString(stream, value[start..]);
            return;
        }

        var valueLength = value.Length - start;
        var prefixed = new byte[valueLength + 1];
        prefixed[0] = 0;
        value.Slice(start).CopyTo(prefixed.AsSpan(1));
        WriteString(stream, prefixed);
    }

    internal ref struct SshWireReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> data = data;
        private int offset;

        public bool IsComplete => this.offset == this.data.Length;

        public byte[] ReadString()
        {
            if (this.data.Length - this.offset < 4)
                throw new FormatException("The SSH key is truncated.");

            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(this.data.Slice(this.offset, 4)));
            this.offset += 4;
            if (length < 0 || this.data.Length - this.offset < length)
                throw new FormatException("The SSH key is truncated.");

            var value = this.data.Slice(this.offset, length).ToArray();
            this.offset += length;
            return value;
        }

        public byte[] ReadMpint()
        {
            var value = this.ReadString();
            if (value.Length > 1 && value[0] == 0)
                value = value[1..];

            return value;
        }
    }
}
