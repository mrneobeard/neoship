using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Secrets;

/// <summary>
/// Stores sensitive byte or character data in an encrypted pinned buffer. Based on the spec in a thread
/// in the .NET designs repo: https://github.com/dotnet/designs/pull/147#issuecomment-825326105
///
/// The primary use case is for storing sensitive data in memory, such as passwords, API keys, and other sensitive information
/// and avoid logging it, making additional copies of the data in memory.
/// </summary>
/// <typeparam name="T">The element type. Only <see cref="byte"/> and <see cref="char"/> are supported.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var secret = new Secret&lt;char&gt;("password".AsSpan());
/// Span&lt;char&gt; buffer = stackalloc char[secret.Length];
/// secret.UnshroudInto(buffer);
/// </code>
/// </example>
/// </remarks>
public sealed partial class Secret<T> : ICloneable, IDisposable
    where T : unmanaged
{
    private const string MaskedSecret = "*******";

    private int length;
    private byte[] protectedBytes;
    private byte[] nonce;
    private byte[] tag;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Secret{T}"/> class.
    /// </summary>
    /// <param name="buffer">The plaintext buffer to protect.</param>
    /// <exception cref="NotSupportedException">Thrown when <typeparamref name="T"/> is not <see cref="byte"/> or <see cref="char"/>.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// byte[] key = [1, 2, 3];
    /// using var secret = new Secret&lt;byte&gt;(key);
    /// </code>
    /// </example>
    /// </remarks>
    public Secret(ReadOnlySpan<T> buffer)
    {
        ThrowIfUnsupportedElementType();

        this.length = buffer.Length;
        this.protectedBytes = GC.AllocateArray<byte>(buffer.Length * Unsafe.SizeOf<T>(), pinned: true);
        this.nonce = GC.AllocateArray<byte>(12, pinned: true);
        this.tag = GC.AllocateArray<byte>(16, pinned: true);
        RandomNumberGenerator.Fill(this.nonce);

        var plaintext = GC.AllocateArray<byte>(this.protectedBytes.Length, pinned: true);
        try
        {
            MemoryMarshal.AsBytes(buffer).CopyTo(plaintext);
            SecretMemoryProtection.Encrypt(plaintext, this.protectedBytes, this.nonce, this.tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>
    /// Converts a span to a protected secret.
    /// </summary>
    /// <param name="buffer">The span to protect.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Span&lt;byte&gt; bytes = [1, 2, 3];
    /// using Secret&lt;byte&gt; secret = bytes;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Secret<T>(Span<T> buffer)
        => new(buffer);

    /// <summary>
    /// Converts a read-only span to a protected secret.
    /// </summary>
    /// <param name="buffer">The read-only span to protect.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ReadOnlySpan&lt;char&gt; chars = "token".AsSpan();
    /// using Secret&lt;char&gt; secret = chars;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Secret<T>(ReadOnlySpan<T> buffer)
        => new(buffer);

    /// <summary>
    /// Converts a value secret to a protected secret.
    /// </summary>
    /// <param name="secret">The value secret to copy.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ValueSecret&lt;byte&gt; valueSecret = new([1, 2, 3]);
    /// using Secret&lt;byte&gt; secret = valueSecret;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Secret<T>(ValueSecret<T> secret)
        => secret.ToSecret();

    /// <summary>
    /// Gets the number of protected elements.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// Console.WriteLine(secret.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public int Length
    {
        get
        {
            this.ThrowIfDisposed();
            return this.length;
        }
    }

    /// <summary>
    /// Creates a new secret containing the same protected data.
    /// </summary>
    /// <returns>A cloned secret.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// using var clone = secret.Clone();
    /// </code>
    /// </example>
    /// </remarks>
    public Secret<T> Clone()
    {
        this.ThrowIfDisposed();

        var plaintext = GC.AllocateArray<byte>(this.protectedBytes.Length, pinned: true);
        try
        {
            SecretMemoryProtection.Decrypt(this.protectedBytes, this.tag, plaintext, this.nonce);
            return new Secret<T>(MemoryMarshal.Cast<byte, T>(plaintext));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>
    /// Appends one element to the protected contents.
    /// </summary>
    /// <param name="value">The element to append.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;char&gt;("key".AsSpan());
    /// secret.Append('!');
    /// </code>
    /// </example>
    /// </remarks>
    public void Append(T value)
    {
        Span<T> buffer = stackalloc T[1];
        buffer[0] = value;
        this.Append(buffer);
    }

    /// <summary>
    /// Appends elements to the protected contents.
    /// </summary>
    /// <param name="buffer">The elements to append.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2]);
    /// secret.Append([3, 4]);
    /// </code>
    /// </example>
    /// </remarks>
    public void Append(ReadOnlySpan<T> buffer)
    {
        this.ThrowIfDisposed();

        if (buffer.IsEmpty)
        {
            return;
        }

        var oldByteLength = this.protectedBytes.Length;
        var newLength = checked(this.length + buffer.Length);
        var newByteLength = checked(newLength * Unsafe.SizeOf<T>());
        var oldPlaintext = GC.AllocateArray<byte>(oldByteLength, pinned: true);
        var newPlaintext = GC.AllocateArray<byte>(newByteLength, pinned: true);
        var newProtectedBytes = GC.AllocateArray<byte>(newByteLength, pinned: true);
        var newNonce = GC.AllocateArray<byte>(12, pinned: true);
        var newTag = GC.AllocateArray<byte>(16, pinned: true);

        try
        {
            SecretMemoryProtection.Decrypt(this.protectedBytes, this.tag, oldPlaintext, this.nonce);
            oldPlaintext.CopyTo(newPlaintext);
            MemoryMarshal.AsBytes(buffer).CopyTo(newPlaintext.AsSpan(oldByteLength));
            RandomNumberGenerator.Fill(newNonce);
            SecretMemoryProtection.Encrypt(newPlaintext, newProtectedBytes, newNonce, newTag);

            CryptographicOperations.ZeroMemory(this.protectedBytes);
            CryptographicOperations.ZeroMemory(this.nonce);
            CryptographicOperations.ZeroMemory(this.tag);

            this.length = newLength;
            this.protectedBytes = newProtectedBytes;
            this.nonce = newNonce;
            this.tag = newTag;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(oldPlaintext);
            CryptographicOperations.ZeroMemory(newPlaintext);
        }
    }

    /// <summary>
    /// Copies the unprotected contents to a caller-provided destination.
    /// </summary>
    /// <param name="destination">The destination that receives the unprotected contents.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;char&gt;("token".AsSpan());
    /// Span&lt;char&gt; buffer = stackalloc char[secret.Length];
    /// secret.CopyTo(buffer);
    /// </code>
    /// </example>
    /// </remarks>
    public void CopyTo(Span<T> destination)
        => this.UnshroudInto(destination);

    /// <summary>
    /// Copies the unprotected contents to a caller-provided destination.
    /// </summary>
    /// <param name="destination">The destination that receives the unprotected contents.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// Span&lt;byte&gt; buffer = stackalloc byte[secret.Length];
    /// secret.UnshroudInto(buffer);
    /// </code>
    /// </example>
    /// </remarks>
    public void UnshroudInto(Span<T> destination)
    {
        this.ThrowIfDisposed();

        if (destination.Length < this.length)
        {
            throw new ArgumentException("The destination is too small.", nameof(destination));
        }

        var plaintext = GC.AllocateArray<byte>(this.protectedBytes.Length, pinned: true);
        try
        {
            SecretMemoryProtection.Decrypt(this.protectedBytes, this.tag, plaintext, this.nonce);
            MemoryMarshal.Cast<byte, T>(plaintext).CopyTo(destination);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>
    /// Copies the unprotected contents to a new array.
    /// </summary>
    /// <returns>A new array containing the unprotected contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// byte[] bytes = secret.UnshroudAsArray();
    /// </code>
    /// </example>
    /// </remarks>
    public T[] UnshroudAsArray()
    {
        this.ThrowIfDisposed();

        var array = new T[this.length];
        this.UnshroudInto(array);
        return array;
    }

    /// <summary>
    /// Copies the unprotected contents to a new array segment.
    /// </summary>
    /// <returns>A new array segment containing the unprotected contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;char&gt;("token".AsSpan());
    /// ArraySegment&lt;char&gt; segment = secret.UnshroudArraySegment();
    /// </code>
    /// </example>
    /// </remarks>
    public ArraySegment<T> UnshroudArraySegment()
    {
        var array = this.UnshroudAsArray();
        return new ArraySegment<T>(array, 0, array.Length);
    }

    /// <summary>
    /// Copies the unprotected contents to a new string.
    /// </summary>
    /// <returns>A new string containing the unprotected contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;char&gt;("token".AsSpan());
    /// string value = secret.UnshroudAsString();
    /// </code>
    /// </example>
    /// </remarks>
    public string UnshroudAsString()
    {
        this.ThrowIfDisposed();

        if (typeof(T) == typeof(char))
        {
            return string.Create(this.length, this, static (destination, state) => state.UnshroudInto(MemoryMarshal.Cast<char, T>(destination)));
        }

        var bytes = this.UnshroudAsArray();
        try
        {
            return Encoding.UTF8.GetString(MemoryMarshal.AsBytes(bytes.AsSpan()));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(bytes.AsSpan()));
        }
    }

    /// <summary>
    /// Releases and clears the protected contents.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// secret.Dispose();
    /// </code>
    /// </example>
    /// </remarks>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(this.protectedBytes);
        CryptographicOperations.ZeroMemory(this.nonce);
        CryptographicOperations.ZeroMemory(this.tag);
        this.disposed = true;
    }

    /// <summary>
    /// Returns a non-sensitive description of this secret.
    /// </summary>
    /// <returns>A non-sensitive description.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;char&gt;("hidden".AsSpan());
    /// Console.WriteLine(secret.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString()
        => MaskedSecret;

    object ICloneable.Clone()
        => this.Clone();

    internal static void ThrowIfUnsupportedElementType()
    {
        if (typeof(T) != typeof(byte) && typeof(T) != typeof(char))
        {
            throw new NotSupportedException("Secret<T> supports only byte and char element types.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(nameof(Secret<T>));
        }
    }
}

internal static class SecretMemoryProtection
{
    private static readonly byte[] ProcessKey = CreateProcessKey();

    public static void Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, ReadOnlySpan<byte> nonce, Span<byte> tag)
    {
        using var algorithm = new ChaCha20Poly1305(ProcessKey);
        algorithm.Encrypt(nonce, plaintext, ciphertext, tag);
    }

    public static void Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext, ReadOnlySpan<byte> nonce)
    {
        using var algorithm = new ChaCha20Poly1305(ProcessKey);
        algorithm.Decrypt(nonce, ciphertext, tag, plaintext);
    }

    private static byte[] CreateProcessKey()
    {
        var key = GC.AllocateArray<byte>(32, pinned: true);
        RandomNumberGenerator.Fill(key);
        return key;
    }
}

/// <summary>
/// Stores sensitive byte or character data in an encrypted pinned value container.
/// </summary>
/// <typeparam name="T">The element type. Only <see cref="byte"/> and <see cref="char"/> are supported.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var secret = new ValueSecret&lt;char&gt;("token".AsSpan());
/// var appended = secret.Append('!');
/// </code>
/// </example>
/// </remarks>
public readonly struct ValueSecret<T> : IDisposable
    where T : unmanaged
{
    private const string MaskedSecret = "*******";

    private readonly int length;
    private readonly byte[] protectedBytes;
    private readonly byte[] nonce;
    private readonly byte[] tag;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueSecret{T}"/> struct.
    /// </summary>
    /// <param name="buffer">The plaintext buffer to protect.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;byte&gt;([1, 2, 3]);
    /// </code>
    /// </example>
    /// </remarks>
    public ValueSecret(ReadOnlySpan<T> buffer)
    {
        Secret<T>.ThrowIfUnsupportedElementType();

        this.length = buffer.Length;
        this.protectedBytes = GC.AllocateArray<byte>(buffer.Length * Unsafe.SizeOf<T>(), pinned: true);
        this.nonce = GC.AllocateArray<byte>(12, pinned: true);
        this.tag = GC.AllocateArray<byte>(16, pinned: true);
        RandomNumberGenerator.Fill(this.nonce);

        var plaintext = GC.AllocateArray<byte>(this.protectedBytes.Length, pinned: true);
        try
        {
            MemoryMarshal.AsBytes(buffer).CopyTo(plaintext);
            SecretMemoryProtection.Encrypt(plaintext, this.protectedBytes, this.nonce, this.tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>
    /// Gets the number of protected elements.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;byte&gt;([1, 2, 3]);
    /// Console.WriteLine(secret.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public int Length => this.length;

    /// <summary>
    /// Converts a span to a protected value secret.
    /// </summary>
    /// <param name="buffer">The span to protect.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Span&lt;byte&gt; bytes = [1, 2, 3];
    /// ValueSecret&lt;byte&gt; secret = bytes;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator ValueSecret<T>(Span<T> buffer)
        => new(buffer);

    /// <summary>
    /// Converts a read-only span to a protected value secret.
    /// </summary>
    /// <param name="buffer">The read-only span to protect.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ReadOnlySpan&lt;char&gt; chars = "token".AsSpan();
    /// ValueSecret&lt;char&gt; secret = chars;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator ValueSecret<T>(ReadOnlySpan<T> buffer)
        => new(buffer);

    /// <summary>
    /// Converts a protected secret to a protected value secret.
    /// </summary>
    /// <param name="secret">The secret to copy.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// ValueSecret&lt;byte&gt; valueSecret = secret;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator ValueSecret<T>(Secret<T> secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var plaintext = GC.AllocateArray<T>(secret.Length, pinned: true);
        try
        {
            secret.UnshroudInto(plaintext);
            return new ValueSecret<T>(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(plaintext.AsSpan()));
        }
    }

    /// <summary>
    /// Appends one element and returns a new value secret.
    /// </summary>
    /// <param name="value">The element to append.</param>
    /// <returns>A new value secret containing the appended element.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;char&gt;("key".AsSpan());
    /// var appended = secret.Append('!');
    /// </code>
    /// </example>
    /// </remarks>
    public ValueSecret<T> Append(T value)
    {
        Span<T> buffer = stackalloc T[1];
        buffer[0] = value;
        return this.Append(buffer);
    }

    /// <summary>
    /// Appends elements and returns a new value secret.
    /// </summary>
    /// <param name="buffer">The elements to append.</param>
    /// <returns>A new value secret containing the appended elements.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;byte&gt;([1, 2]);
    /// var appended = secret.Append([3, 4]);
    /// </code>
    /// </example>
    /// </remarks>
    public ValueSecret<T> Append(ReadOnlySpan<T> buffer)
    {
        if (buffer.IsEmpty)
        {
            return this;
        }

        var newLength = checked(this.length + buffer.Length);
        var plaintext = GC.AllocateArray<T>(newLength, pinned: true);
        try
        {
            this.UnshroudInto(plaintext);
            buffer.CopyTo(plaintext.AsSpan(this.length));
            return new ValueSecret<T>(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(plaintext.AsSpan()));
        }
    }

    /// <summary>
    /// Converts this value secret to a protected secret.
    /// </summary>
    /// <returns>A protected secret containing the same contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var valueSecret = new ValueSecret&lt;byte&gt;([1, 2, 3]);
    /// using var secret = valueSecret.ToSecret();
    /// </code>
    /// </example>
    /// </remarks>
    public Secret<T> ToSecret()
    {
        var plaintext = GC.AllocateArray<T>(this.length, pinned: true);
        try
        {
            this.UnshroudInto(plaintext);
            return new Secret<T>(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(plaintext.AsSpan()));
        }
    }

    /// <summary>
    /// Copies the unprotected contents to a caller-provided destination.
    /// </summary>
    /// <param name="destination">The destination that receives the unprotected contents.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;byte&gt;([1, 2, 3]);
    /// Span&lt;byte&gt; buffer = stackalloc byte[secret.Length];
    /// secret.CopyTo(buffer);
    /// </code>
    /// </example>
    /// </remarks>
    public void CopyTo(Span<T> destination)
        => this.UnshroudInto(destination);

    /// <summary>
    /// Copies the unprotected contents to a caller-provided destination.
    /// </summary>
    /// <param name="destination">The destination that receives the unprotected contents.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;char&gt;("token".AsSpan());
    /// Span&lt;char&gt; buffer = stackalloc char[secret.Length];
    /// secret.UnshroudInto(buffer);
    /// </code>
    /// </example>
    /// </remarks>
    public void UnshroudInto(Span<T> destination)
    {
        if (destination.Length < this.length)
        {
            throw new ArgumentException("The destination is too small.", nameof(destination));
        }

        var plaintext = GC.AllocateArray<byte>(this.protectedBytes.Length, pinned: true);
        try
        {
            SecretMemoryProtection.Decrypt(this.protectedBytes, this.tag, plaintext, this.nonce);
            MemoryMarshal.Cast<byte, T>(plaintext).CopyTo(destination);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>
    /// Copies the unprotected contents to a new array.
    /// </summary>
    /// <returns>A new array containing the unprotected contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;byte&gt;([1, 2, 3]);
    /// byte[] bytes = secret.UnshroudAsArray();
    /// </code>
    /// </example>
    /// </remarks>
    public T[] UnshroudAsArray()
    {
        var array = new T[this.length];
        this.UnshroudInto(array);
        return array;
    }

    /// <summary>
    /// Copies the unprotected contents to a new array segment.
    /// </summary>
    /// <returns>A new array segment containing the unprotected contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;char&gt;("token".AsSpan());
    /// ArraySegment&lt;char&gt; segment = secret.UnshroudArraySegment();
    /// </code>
    /// </example>
    /// </remarks>
    public ArraySegment<T> UnshroudArraySegment()
    {
        var array = this.UnshroudAsArray();
        return new ArraySegment<T>(array, 0, array.Length);
    }

    /// <summary>
    /// Copies the unprotected contents to a new string.
    /// </summary>
    /// <returns>A new string containing the unprotected contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;char&gt;("token".AsSpan());
    /// string value = secret.UnshroudAsString();
    /// </code>
    /// </example>
    /// </remarks>
    public string UnshroudAsString()
    {
        if (typeof(T) == typeof(char))
        {
            return string.Create(this.length, this, static (destination, state) => state.UnshroudInto(MemoryMarshal.Cast<char, T>(destination)));
        }

        var bytes = this.UnshroudAsArray();
        try
        {
            return Encoding.UTF8.GetString(MemoryMarshal.AsBytes(bytes.AsSpan()));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(bytes.AsSpan()));
        }
    }

    /// <summary>
    /// Releases and clears the protected contents.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;byte&gt;([1, 2, 3]);
    /// secret.Dispose();
    /// </code>
    /// </example>
    /// </remarks>
    public void Dispose()
    {
        if (this.protectedBytes is not null)
        {
            CryptographicOperations.ZeroMemory(this.protectedBytes);
        }

        if (this.nonce is not null)
        {
            CryptographicOperations.ZeroMemory(this.nonce);
        }

        if (this.tag is not null)
        {
            CryptographicOperations.ZeroMemory(this.tag);
        }
    }

    /// <summary>
    /// Returns a masked representation of this secret.
    /// </summary>
    /// <returns>A masked representation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = new ValueSecret&lt;char&gt;("hidden".AsSpan());
    /// Console.WriteLine(secret.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString()
        => MaskedSecret;
}