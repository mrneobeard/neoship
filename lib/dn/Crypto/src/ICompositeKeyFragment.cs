using System;

namespace NeoBeard.Crypto
{
    public interface ICompositeKeyFragment : IDisposable
    {
        ReadOnlySpan<byte> ToReadOnlySpan();
    }
}