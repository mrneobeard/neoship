using System.Runtime.InteropServices;
#pragma warning disable S3903 // Types should be defined in named namespaces

internal static partial class FFI
{
    internal static partial class Libc
    {
        [LibraryImport(Libs.LibcName, EntryPoint = "chown", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int chown(string path, uint owner, uint group);

        [LibraryImport(Libs.LibcName, EntryPoint = "lchown", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int lchown(string path, uint owner, uint group);
    }
}