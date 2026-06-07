using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE1006, SA1025, SA1310, CS8981, SA1300

[assembly: SuppressMessage("", "CS8981", Justification = "cause")]

internal static partial class FFI
{
    internal static partial class Libs
    {
        internal const string LibcName = "libc";

        // Shims
        internal const string SystemNative = "System.Native";
        internal const string HttpNative = "System.Net.Http.Native";
        internal const string CryptoNative = "System.Security.Cryptography.Native";
    }
}