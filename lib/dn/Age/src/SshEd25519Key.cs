using System.Security.Cryptography;
using NeoBeard.Ssh;
using System.Text;

namespace NeoBeard.Age;

/// <summary>
/// Provides helpers for importing and exporting SSH Ed25519 keys for age recipients.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var seed = RandomNumberGenerator.GetBytes(32);
/// var publicKey = SshEd25519Key.ExportPublicKey(seed);
/// var recipient = AgeRecipient.FromSshPublicKey(publicKey);
/// Console.WriteLine(recipient.Kind);
/// </code>
/// </example>
/// </remarks>
public static class SshEd25519Key
{
    /// <summary>
    /// Exports an Ed25519 seed as an OpenSSH authorized_keys public key.
    /// </summary>
    /// <param name="seed">The 32-byte Ed25519 private key seed.</param>
    /// <returns>The OpenSSH public key text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var seed = new byte[32];
    /// var publicKey = SshEd25519Key.ExportPublicKey(seed);
    /// Console.WriteLine(publicKey.StartsWith("ssh-ed25519 ", StringComparison.Ordinal));
    /// </code>
    /// </example>
    /// </remarks>
    public static string ExportPublicKey(ReadOnlySpan<byte> seed)
    {
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seeds must be 32 bytes.", nameof(seed));

        var publicKeyArray = new byte[32];
        Ed25519.GeneratePublicKey(seed, publicKeyArray);
        return "ssh-ed25519 " + Convert.ToBase64String(BuildWireKey(publicKeyArray));
    }

    internal static (byte[] Ed25519PublicKey, byte[] Curve25519PublicKey, byte[] WireKey) ParsePublicKey(string authorizedKey)
    {
        var text = authorizedKey.AsSpan().Trim();
        var firstSpace = text.IndexOf(' ');
        if (firstSpace < 0)
            throw new FormatException("The SSH public key is not ssh-ed25519.");

        if (!text[..firstSpace].SequenceEqual("ssh-ed25519"))
            throw new FormatException("The SSH public key is not ssh-ed25519.");

        var remainder = text[(firstSpace + 1)..];
        var secondSpace = remainder.IndexOf(' ');
        var key = (secondSpace < 0 ? remainder : remainder[..secondSpace]).ToString();

        var wire = Convert.FromBase64String(key);
        var reader = new SshRsaKey.SshWireReader(wire);
        var kind = Encoding.ASCII.GetString(reader.ReadString());
        if (!string.Equals(kind, "ssh-ed25519", StringComparison.Ordinal))
            throw new FormatException("The SSH public key is not ssh-ed25519.");

        var publicKey = reader.ReadString();
        if (publicKey.Length != 32 || !reader.IsComplete)
            throw new FormatException("The SSH Ed25519 public key is invalid.");

        return (publicKey, Ed25519Conversion.PublicKeyToX25519(publicKey), wire);
    }

    internal static (byte[] Seed, byte[] PublicKey, byte[] WireKey) ParsePrivateKey(string pem)
    {
        var keyBytes = Pem.Decode(pem, "OPENSSH PRIVATE KEY");
        var reader = new OpenSshReader(keyBytes);
        var magic = reader.ReadCString();
        if (!string.Equals(magic, "openssh-key-v1", StringComparison.Ordinal))
            throw new FormatException("The OpenSSH private key header is invalid.");

        var cipherName = reader.ReadUtf8String();
        var kdfName = reader.ReadUtf8String();
        _ = reader.ReadString();
        var keyCount = reader.ReadUInt32();
        if (!string.Equals(cipherName, "none", StringComparison.Ordinal) || !string.Equals(kdfName, "none", StringComparison.Ordinal) || keyCount != 1)
            throw new NotSupportedException("Only unencrypted single-key OpenSSH private keys are supported.");

        _ = reader.ReadString();
        var privateBlob = reader.ReadString();
        if (!reader.IsComplete)
            throw new FormatException("The OpenSSH private key contains trailing data.");

        var privateReader = new OpenSshReader(privateBlob);
        var check1 = privateReader.ReadUInt32();
        var check2 = privateReader.ReadUInt32();
        if (check1 != check2)
            throw new FormatException("The OpenSSH private key check values do not match.");

        var kind = privateReader.ReadUtf8String();
        if (!string.Equals(kind, "ssh-ed25519", StringComparison.Ordinal))
            throw new FormatException("The OpenSSH private key is not ssh-ed25519.");

        var publicKey = privateReader.ReadString();
        var privateKey = privateReader.ReadString();
        _ = privateReader.ReadUtf8String();
        ValidatePadding(privateReader.Remaining);
        if (publicKey.Length != 32 || privateKey.Length != 64)
            throw new FormatException("The OpenSSH Ed25519 private key is invalid.");

        var seed = privateKey.AsSpan(0, 32).ToArray();
        if (!CryptographicOperations.FixedTimeEquals(publicKey, privateKey.AsSpan(32, 32)))
            throw new FormatException("The OpenSSH Ed25519 private key public component is invalid.");

        return (seed, publicKey, BuildWireKey(publicKey));
    }

    internal static byte[] BuildWireKey(ReadOnlySpan<byte> publicKey)
    {
        using var stream = new MemoryStream();
        SshRsaKey.WriteString(stream, Encoding.ASCII.GetBytes("ssh-ed25519"));
        SshRsaKey.WriteString(stream, publicKey);
        return stream.ToArray();
    }

    private static void ValidatePadding(ReadOnlySpan<byte> padding)
    {
        for (var i = 0; i < padding.Length; i++)
        {
            if (padding[i] != (byte)(i + 1))
                throw new FormatException("The OpenSSH private key padding is invalid.");
        }
    }

    private ref struct OpenSshReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> data = data;
        private int offset;

        public bool IsComplete => this.offset == this.data.Length;

        public ReadOnlySpan<byte> Remaining => this.data[this.offset..];

        public string ReadCString()
        {
            var start = this.offset;
            while (this.offset < this.data.Length && this.data[this.offset] != 0)
                this.offset++;

            if (this.offset >= this.data.Length)
                throw new FormatException("The OpenSSH private key magic is truncated.");

            var value = Encoding.ASCII.GetString(this.data[start..this.offset]);
            this.offset++;
            return value;
        }

        public uint ReadUInt32()
        {
            if (this.data.Length - this.offset < 4)
                throw new FormatException("The OpenSSH private key is truncated.");

            var value = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(this.data.Slice(this.offset, 4));
            this.offset += 4;
            return value;
        }

        public byte[] ReadString()
        {
            var length = checked((int)this.ReadUInt32());
            if (this.data.Length - this.offset < length)
                throw new FormatException("The OpenSSH private key is truncated.");

            var value = this.data.Slice(this.offset, length).ToArray();
            this.offset += length;
            return value;
        }

        public string ReadUtf8String()
        {
            return Encoding.UTF8.GetString(this.ReadString());
        }
    }
}
