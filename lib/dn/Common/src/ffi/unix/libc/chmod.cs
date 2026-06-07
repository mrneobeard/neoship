using System.Runtime.InteropServices;
#pragma warning disable S3903 // Types should be defined in named namespaces

internal static partial class FFI
{
    internal static partial class Libc
    {
        [LibraryImport(Libs.LibcName, EntryPoint = "chmod", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int chmod(string path, int mode);
    }
}