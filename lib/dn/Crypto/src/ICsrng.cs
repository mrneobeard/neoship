using System;

namespace NeoBeard.Crypto;

/// <summary>
/// A contract for a cryptographically secure random
/// number generator.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var rng = new Csrng();
/// var bytes = rng.NextBytes(32);
/// Assert.Equal(32, bytes.Length);
/// </code>
/// </example>
/// </remarks>
public interface ICsrng
{
    void GetBytes(byte[] bytes);

    void GetBytes(Span<byte> bytes);

    byte[] NextBytes(int length);

    short NextInt16();

    int NextInt32();

    long NextInt64();
}