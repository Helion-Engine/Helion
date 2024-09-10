namespace ZMusicWrapper
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using ZMusicWrapper.Generated;

    internal static class ZMusicLoader
    {
        private static bool RegisteredResolver;

        static ZMusicLoader()
        {
            RegisteredResolver = false;
            RegisterDllResolver();
        }

        internal static void RegisterDllResolver()
        {
            if (!RegisteredResolver)
            {
                NativeLibrary.SetDllImportResolver(typeof(ZMusicLoader).Assembly, ImportResolver);
                RegisteredResolver = true;
            }
        }

        private static string GetExpectedLibraryName()
        {
#pragma warning disable IDE0046 // if/else collapsing produces very dense code here
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "zmusic.dll";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "libzmusic.so";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "libzmusic.dylib";

            throw new NotSupportedException("This library does not support the current machine OS.");
#pragma warning restore IDE0046
        }

        private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == ZMusic.LibraryName)
            {
                string libraryName2 = GetExpectedLibraryName();
                return !NativeLibrary.TryLoad(libraryName2, assembly, searchPath, out nint handle)
                    ? throw new DllNotFoundException("Could not load the dll '" + libraryName2 + "' (this load is intercepted, specified in DllImport as '" + libraryName + "').")
                    : handle;
            }
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }
    }
}
