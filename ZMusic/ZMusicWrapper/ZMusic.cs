namespace ZMusicWrapper.Generated
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    public unsafe partial class ZMusic
    {
#if LINUX
        internal const string LibraryName = "libzmusic.so";
#else
        internal const string LibraryName = "zmusic.dll";
#endif

#if !LINUX && !WINDOWS
        private static bool RegisteredResolver;
        private static IntPtr m_dllHandle = IntPtr.Zero;

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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.Is64BitProcess)
                return "runtimes\\win-x64\\native\\";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.Is64BitProcess)
                return "runtimes/linux-x64/native/";

            throw new NotSupportedException("This library does not support the current OS.");
#pragma warning restore IDE0046
        }

        private static string[] GetExpectedLibraryNames()
        {
#pragma warning disable IDE0046 // if/else collapsing produces very dense code here
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ["zmusic.dll", "libzmusic.dll"];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ["libzmusic.so", "zmusic.so"];

            throw new NotSupportedException("This library does not support the current OS.");
#pragma warning restore IDE0046
        }

        private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (libraryName == LibraryName)
            {
                if (m_dllHandle != IntPtr.Zero)
                {
                    return m_dllHandle;
                }

                string runtimePath = GetRuntimePath();
                string[] libraryNames = GetExpectedLibraryNames();

                foreach (string library in libraryNames)
                {
                    // e.g. appdir/runtimes/linux-x64/native/fluidsynth.so
                    if (NativeLibrary.TryLoad($"{baseDirectory}{runtimePath}{library}", out m_dllHandle))
                    {
                        return m_dllHandle;
                    }

                    // e.g. appdir/fluidsynth.so
                    if (NativeLibrary.TryLoad($"{baseDirectory}{library}", out m_dllHandle))
                    {
                        return m_dllHandle;
                    }
                }

                foreach (string primaryLibrary in libraryNames)
                {
                    // default runtime search paths
                    if (NativeLibrary.TryLoad(primaryLibrary, out m_dllHandle))
                    {
                        return m_dllHandle;
                    }
                }

                throw new DllNotFoundException($"Could not load a suitable substitute for DllImport {libraryName}.");
            }

            return IntPtr.Zero;
        }
#endif
    }
}
