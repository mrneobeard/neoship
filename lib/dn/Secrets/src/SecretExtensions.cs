using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace NeoBeard.Secrets;

/// <summary>
/// Provides explicit unshrouding helpers for <see cref="Secret{T}"/>.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
/// byte[] value = secret.ToUnshroudedArray();
/// </code>
/// </example>
/// </remarks>
public static partial class SecretExtensions
{
    /// <summary>
    /// Copies a secret to a new unprotected array.
    /// </summary>
    /// <typeparam name="T">The secret element type.</typeparam>
    /// <param name="secret">The secret to copy.</param>
    /// <returns>A new unprotected array containing the secret contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// byte[] value = secret.ToUnshroudedArray();
    /// </code>
    /// </example>
    /// </remarks>
    public static T[] ToUnshroudedArray<T>(this Secret<T> secret)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(secret);

        var array = new T[secret.Length];
        secret.UnshroudInto(array);
        return array;
    }

    /// <summary>
    /// Copies a character secret to a new unprotected string.
    /// </summary>
    /// <param name="secret">The character secret to copy.</param>
    /// <returns>A new unprotected string containing the secret contents.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;char&gt;("token".AsSpan());
    /// string value = secret.ToUnshroudedString();
    /// </code>
    /// </example>
    /// </remarks>
    public static string ToUnshroudedString(this Secret<char> secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        return string.Create(secret.Length, secret, static (destination, state) => state.UnshroudInto(destination));
    }

    /// <summary>
    /// Unshrouds a secret into a temporary pinned buffer and invokes an action with that buffer.
    /// </summary>
    /// <typeparam name="T">The secret element type.</typeparam>
    /// <typeparam name="TArg">The action argument type.</typeparam>
    /// <param name="secret">The secret to use.</param>
    /// <param name="arg">The argument passed to <paramref name="spanAction"/>.</param>
    /// <param name="spanAction">The action invoked with the temporary unprotected span.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secret = new Secret&lt;byte&gt;([1, 2, 3]);
    /// secret.Use(0, static (span, _) => Console.WriteLine(span.Length));
    /// </code>
    /// </example>
    /// </remarks>
    public static void Use<T, TArg>(this Secret<T> secret, TArg arg, ReadOnlySpanAction<T, TArg> spanAction)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(spanAction);

        var buffer = GC.AllocateArray<T>(secret.Length, pinned: true);
        try
        {
            secret.UnshroudInto(buffer);
            spanAction(buffer, arg);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(buffer.AsSpan()));
        }
    }
}