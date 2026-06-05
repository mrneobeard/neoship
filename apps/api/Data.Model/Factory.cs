using System.Runtime.CompilerServices;

namespace NeoShip.Data.Model;


public class Factory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid NewGuid() => Guid.CreateVersion7();
}