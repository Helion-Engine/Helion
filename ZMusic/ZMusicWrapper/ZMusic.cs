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

        private static string GetRuntimePath()
        {
#pragma warning disable IDE0046 // if/else collapsing produces very dense code here
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.Is64BitProcess
                    ? "runtimes\\win-x64\\native\\"
                    : "runtimes\\win-x86\\native\\";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.Is64BitProcess)
            {
                return "runtimes/linux-x64/native/";
            }

            throw new NotSupportedException("This library does not support the current OS.");
#pragma warning restore IDE0046
        }

        private static string[] GetExpectedLibraryNames()
        {
#pragma warning disable IDE0046 // if/else collapsing produces very dense code here
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.Is64BitProcess
                    ? ["zmusic.dll"]
                    : ["zmusic.dll"];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ["libzmusic.so"];

            throw new NotSupportedException("This library does not support the current machine OS.");
#pragma warning restore IDE0046
        }

        private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == LibraryName)
            {
                string runtimePath = GetRuntimePath();
                string[] libraryNames = GetExpectedLibraryNames();

                string primaryLibrary = libraryNames[0];
                if (NativeLibrary.TryLoad($"{runtimePath}{primaryLibrary}", out nint handle))
                {
                    foreach (string secondaryLibrary in libraryNames[1..])
                    {
                        _ = NativeLibrary.TryLoad($"{runtimePath}{secondaryLibrary}", out _);
                    }

                    return handle;
                }

                if (NativeLibrary.TryLoad(primaryLibrary, out handle))
                {
                    foreach (string secondaryLibrary in libraryNames[1..])
                    {
                        _= NativeLibrary.TryLoad($"{secondaryLibrary}", out _);
                    }

                    return handle;
                }

                throw new DllNotFoundException($"Could not load a suitable substitute for DllImport {libraryName}.");
            }

            return IntPtr.Zero;
        }
    }
}
