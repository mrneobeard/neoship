using System.Runtime.InteropServices;
#pragma warning disable S3903 // Types should be defined in named namespaces

internal static partial class FFI
{
    internal static partial class Libc
    {
        [LibraryImport("libc", EntryPoint = "mkdtemp", SetLastError = true)]
        internal static unsafe partial byte* mkdtemp(byte* tempFile);
    }
}