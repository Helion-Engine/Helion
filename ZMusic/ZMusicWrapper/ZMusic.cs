namespace ZMusicWrapper.Generated
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    public unsafe partial class ZMusic
    {
        private static bool RegisteredResolver;

        static ZMusic()
        {
            RegisteredResolver = false;
            RegisterDllResolver();
        }

        internal static void RegisterDllResolver()
        {
            if (!RegisteredResolver)
            {
                NativeLibrary.SetDllImportResolver(typeof(ZMusic).Assembly, ImportResolver);
                RegisteredResolver = true;
            }
        }

        private static string[] GetExpectedLibraryNames()
        {
#pragma warning disable IDE0046 // if/else collapsing produces very dense code here
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ["zmusic.dll"];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ["libzmusic.so.1", "libzmusic.so"];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return ["libzmusic.dylib"];

            throw new NotSupportedException("This library does not support the current machine OS.");
#pragma warning restore IDE0046
        }

        private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == ZMusic.LibraryName)
            {
                string[] libraryNames = GetExpectedLibraryNames();

                foreach (string possibleName in libraryNames)
                {
                    if (NativeLibrary.TryLoad(possibleName, out nint handle))
                    {
                        return handle;
                    }
                }

                throw new DllNotFoundException($"Could not load a suitable substitute for DllImport {libraryName}.  Tried searching for {string.Join(',', libraryNames)}");
            }
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }
    }
}
