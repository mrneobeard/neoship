using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Age;

/// <summary>
/// Encrypts and decrypts age v1 files.
/// </summary>
/// <remarks>
/// <code lang="csharp">
/// var key = AgeKey.Create();
/// using var plain = new MemoryStream("hello"u8.ToArray());
/// using var encrypted = new MemoryStream();
/// AgeFile.Encrypt(plain, encrypted, [AgeRecipient.FromPublicKey(key.PublicKey)]);
/// </code>
/// </remarks>
public static class AgeFile
{
    private const int ChunkSize = 64 * 1024;
    private const int MaxHeaderLineLength = 16 * 1024;

    /// <summary>
    /// Encrypts a file with the supplied recipients.
    /// </summary>
    public static void EncryptFile(string inputPath, string outputPath, IReadOnlyList<AgeRecipient> recipients)
    {
        using var input = File.OpenRead(inputPath);
        using var output = File.Create(outputPath);
        Encrypt(input, output, recipients);
    }

    /// <summary>
    /// Decrypts a file with the supplied identities.
    /// </summary>
    public static void DecryptFile(string inputPath, string outputPath, IReadOnlyList<AgeIdentity> identities)
    {
        using var input = File.OpenRead(inputPath);
        using var output = File.Create(outputPath);
        Decrypt(input, output, identities);
    }

    /// <summary>
    /// Encrypts plaintext from a stream into an age v1 stream.
    /// </summary>
    public static void Encrypt(Stream input, Stream output, IReadOnlyList<AgeRecipient> recipients)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(recipients);
        if (recipients.Count == 0)
            throw new ArgumentException("At least one recipient is required.", nameof(recipients));

        var fileKey = RandomNumberGenerator.GetBytes(16);
        var stanzas = new Stanza[recipients.Count];
        for (var i = 0; i < recipients.Count; i++)
            stanzas[i] = recipients[i].Wrap(fileKey);

        var header = BuildHeader(stanzas, fileKey);
        output.Write(Encoding.ASCII.GetBytes(header));
        EncryptPayload(input, output, fileKey);
    }

    /// <summary>
    /// Decrypts an age v1 stream into plaintext.
    /// </summary>
    public static void Decrypt(Stream input, Stream output, IReadOnlyList<AgeIdentity> identities)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(identities);
        if (identities.Count == 0)
            throw new ArgumentException("At least one identity is required.", nameof(identities));

        var header = ParseHeader(input);
        var fileKey = FindMatchingFileKey(identities, header.Stanzas);
        if (fileKey is null)
            throw new AgeException("No identity matched the age file recipients.");

        VerifyHeaderMac(header.HeaderPrefix, header.Mac, fileKey);
        DecryptPayload(input, output, fileKey);
    }

    private static byte[]? FindMatchingFileKey(IReadOnlyList<AgeIdentity> identities, IReadOnlyList<Stanza> stanzas)
    {
        for (var i = 0; i < identities.Count; i++)
        {
            var fileKey = identities[i].Unwrap(stanzas);
            if (fileKey is not null)
                return fileKey;
        }

        return null;
    }

    private static string BuildHeader(IReadOnlyList<Stanza> stanzas, byte[] fileKey)
    {
        var builder = new StringBuilder();
        builder.Append("age-encryption.org/v1\n");
        foreach (var stanza in stanzas)
        {
            builder.Append("-> ").Append(stanza.Type);
            for (var i = 0; i < stanza.Args.Length; i++)
                builder.Append(' ').Append(stanza.Args[i]);

            builder.Append('\n');
            AppendWrappedBase64(builder, stanza.Body);
        }

        builder.Append("---");
        var prefix = builder.ToString();
        var mac = ComputeHeaderMac(prefix, fileKey);
        return prefix + " " + AgeBase64.Encode(mac) + "\n";
    }

    private static void AppendWrappedBase64(StringBuilder builder, byte[] body)
    {
        var encoded = AgeBase64.Encode(body);
        for (var offset = 0; offset < encoded.Length; offset += 64)
            builder.Append(encoded.AsSpan(offset, Math.Min(64, encoded.Length - offset))).Append('\n');

        if (encoded.Length % 64 == 0)
            builder.Append('\n');
    }

    private static byte[] ComputeHeaderMac(string headerPrefix, byte[] fileKey)
    {
        var key = CryptoPrimitives.Hkdf(fileKey, [], "header");
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.ASCII.GetBytes(headerPrefix));
    }

    private static void VerifyHeaderMac(string headerPrefix, byte[] mac, byte[] fileKey)
    {
        var expected = ComputeHeaderMac(headerPrefix, fileKey);
        if (!CryptographicOperations.FixedTimeEquals(expected, mac))
            throw new AgeException("The age header MAC is invalid.");
    }

    private static void EncryptPayload(Stream input, Stream output, byte[] fileKey)
    {
        var nonce = new byte[16];
        var plaintext = new byte[ChunkSize];
        var ciphertext = new byte[ChunkSize + 16];
        var chunkNonce = new byte[12];

        RandomNumberGenerator.Fill(nonce);
        output.Write(nonce);
        var payloadKey = CryptoPrimitives.Hkdf(fileKey, nonce, "payload");
        using var aead = new ChaCha20Poly1305(payloadKey);
        ulong counter = 0;

        while (true)
        {
            var read = ReadChunk(input, plaintext);
            var isFinal = read < ChunkSize;
            var ciphertextLength = read + 16;
            BuildChunkNonce(counter++, isFinal, chunkNonce);
            aead.Encrypt(
                chunkNonce,
                plaintext.AsSpan(0, read),
                ciphertext.AsSpan(0, read),
                ciphertext.AsSpan(read, 16));

            output.Write(ciphertext, 0, ciphertextLength);
            if (isFinal)
                return;
        }
    }

    private static void DecryptPayload(Stream input, Stream output, byte[] fileKey)
    {
        var nonce = new byte[16];
        ReadExactly(input, nonce);
        var payloadKey = CryptoPrimitives.Hkdf(fileKey, nonce, "payload");
        using var aead = new ChaCha20Poly1305(payloadKey);
        ulong counter = 0;
        var encryptedChunk = new byte[ChunkSize + 16];
        var plaintext = new byte[ChunkSize];
        var chunkNonce = new byte[12];

        while (true)
        {
            var read = ReadAtMost(input, encryptedChunk, encryptedChunk.Length);
            if (read == 0)
                throw new AgeException("The age payload ended before the final chunk.");

            var isFinal = read < encryptedChunk.Length;
            if (read < 16)
                throw new AgeException("The age payload chunk is truncated.");

            var plaintextLength = read - 16;
            BuildChunkNonce(counter++, isFinal, chunkNonce);
            try
            {
                aead.Decrypt(
                    chunkNonce,
                    encryptedChunk.AsSpan(0, plaintextLength),
                    encryptedChunk.AsSpan(plaintextLength, 16),
                    plaintext.AsSpan(0, plaintextLength));
            }
            catch (CryptographicException ex)
            {
                throw new AgeException("The age payload authentication failed.", ex);
            }

            output.Write(plaintext, 0, plaintextLength);
            if (isFinal)
                return;
        }
    }

    private static void BuildChunkNonce(ulong counter, bool isFinal, Span<byte> nonce)
    {
        for (var i = 10; i >= 0; i--)
        {
            nonce[i] = (byte)counter;
            counter >>= 8;
        }

        nonce[11] = isFinal ? (byte)1 : (byte)0;
    }

    private static int ReadChunk(Stream input, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = input.Read(buffer.AsSpan(offset, buffer.Length - offset));
            if (read == 0)
                break;

            offset += read;
        }

        return offset;
    }

    private static int ReadAtMost(Stream input, byte[] buffer, int count)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = input.Read(buffer.AsSpan(offset, count - offset));
            if (read == 0)
                break;

            offset += read;
        }

        return offset;
    }

    private static void ReadExactly(Stream input, byte[] buffer)
    {
        if (ReadAtMost(input, buffer, buffer.Length) != buffer.Length)
            throw new AgeException("The age file is truncated.");
    }

    private static ParsedHeader ParseHeader(Stream input)
    {
        var prefix = new StringBuilder();
        var version = ReadAsciiLine(input) ?? throw new AgeException("The age header is empty.");
        if (!string.Equals(version, "age-encryption.org/v1", StringComparison.Ordinal))
            throw new AgeException("The age file version is unsupported.");

        prefix.Append(version).Append('\n');
        var stanzas = new List<Stanza>();
        while (true)
        {
            var line = ReadAsciiLine(input) ?? throw new AgeException("The age header is truncated.");
            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                prefix.Append("---");
                return new ParsedHeader(stanzas, prefix.ToString(), AgeBase64.Decode(line[4..]));
            }

            if (!line.StartsWith("-> ", StringComparison.Ordinal))
                throw new AgeException("The age recipient stanza is invalid.");

            prefix.Append(line).Append('\n');
            var args = line[3..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0)
                throw new AgeException("The age recipient stanza is invalid.");

            var body = new StringBuilder();
            while (true)
            {
                var bodyLine = ReadAsciiLine(input) ?? throw new AgeException("The age recipient body is truncated.");
                prefix.Append(bodyLine).Append('\n');
                body.Append(bodyLine);
                if (bodyLine.Length < 64)
                    break;
            }

            stanzas.Add(new Stanza(args[0], args[1..], AgeBase64.Decode(body.ToString())));
        }
    }

    private static string? ReadAsciiLine(Stream input)
    {
        var buffer = new byte[256];
        var length = 0;
        while (true)
        {
            var value = input.ReadByte();
            if (value < 0)
                return length == 0 ? null : throw new AgeException("The age header line is truncated.");

            if (value == '\n')
                return Encoding.ASCII.GetString(buffer, 0, length);

            if (value == '\r')
                throw new AgeException("The age header must use LF line endings.");

            if (length >= MaxHeaderLineLength)
                throw new AgeException("The age header line is too large.");

            if (length == buffer.Length)
                buffer = GrowLineBuffer(buffer);

            buffer[length++] = (byte)value;
        }
    }

    private static byte[] GrowLineBuffer(byte[] buffer)
    {
        var nextLength = Math.Min(MaxHeaderLineLength, buffer.Length * 2);
        var next = new byte[nextLength];
        buffer.AsSpan().CopyTo(next);
        return next;
    }

    private sealed record ParsedHeader
    {
        public ParsedHeader(IReadOnlyList<Stanza> stanzas, string headerPrefix, byte[] mac)
        {
            this.Stanzas = stanzas;
            this.HeaderPrefix = headerPrefix;
            this.Mac = mac;
        }

        public IReadOnlyList<Stanza> Stanzas { get; }

        public string HeaderPrefix { get; }

        public byte[] Mac { get; }
    }
}
