// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
#pragma warning disable CS0649
#pragma warning disable S3903 // Types should be defined in named namespaces

internal static partial class FFI
{
    internal static partial class Sys
    {
        [LibraryImport(Libs.SystemNative, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "SystemNative_GetPwUidR", SetLastError = false)]
        internal static unsafe partial int GetPwUidR(uint uid, out Passwd pwd, byte* buf, int bufLen);

        [LibraryImport(Libs.SystemNative, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "SystemNative_GetPwNamR", SetLastError = false)]
        internal static unsafe partial int GetPwNamR(string name, out Passwd pwd, byte* buf, int bufLen);

        internal static unsafe uint? GetUserId(string name)
        {
            var size = Passwd.InitialBufferSize;
            var stackBuf = stackalloc byte[size];
            Passwd pwd;
            int result = GetPwNamR(name, out pwd, stackBuf, size);
            if (result == 0)
            {
                return pwd.UserId;
            }

            while (true)
            {
                size *= 2;
                var buf = Marshal.AllocHGlobal(size);
                try
                {
                    result = GetPwNamR(name, out pwd, (byte*)buf, size);
                    if (result == 0)
                    {
                        return pwd.UserId;
                    }

                    if (result == (int)Error.ERANGE)
                    {
                        // The buffer was too small, try again with a larger one.
                        continue;
                    }

                    return null; // An error occurred, return null.
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
        }

        internal static unsafe string? GetUserName(uint uid)
        {
            var size = Passwd.InitialBufferSize;
            var stackBuf = stackalloc byte[size];
            Passwd pwd;
            int result = GetPwUidR(uid, out pwd, stackBuf, size);
            if (result == 0)
            {
                return Marshal.PtrToStringUTF8((IntPtr)pwd.Name);
            }

            while (true)
            {
                size *= 2;
                var buf = Marshal.AllocHGlobal(size);
                try
                {
                    result = GetPwUidR(uid, out pwd, (byte*)buf, size);
                    if (result == 0)
                    {
                        return Marshal.PtrToStringUTF8((IntPtr)pwd.Name);
                    }

                    if (result == (int)Error.ERANGE)
                    {
                        // The buffer was too small, try again with a larger one.
                        continue;
                    }

                    return null; // An error occurred, return null.
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
        }

#pragma warning disable S2344
#pragma warning disable S2346
#pragma warning disable SA1025
#pragma warning disable SA1310

        internal unsafe struct Passwd
        {
            internal const int InitialBufferSize = 256;

            internal byte* Name;
            internal byte* Password;
            internal uint UserId;
            internal uint GroupId;
            internal byte* UserInfo;
            internal byte* HomeDirectory;
            internal byte* Shell;
        }
    }
}