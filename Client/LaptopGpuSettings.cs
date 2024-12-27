using Helion.Util.Configs.Components;
using Microsoft.Win32;
using System;
using System.IO;

namespace Helion.Client;

public static class LaptopGpuSettings
{
    public static LaptopGpuMode GetGpuMode(AppInfo appInfo)
    {
        try
        {
            using var key = GetRegKey(false);
            if (key != null)
                return GetModeFromKey(appInfo, key);
        }
        catch { }
        return LaptopGpuMode.Auto;
    }

    public static bool SetGpuMode(AppInfo appInfo, LaptopGpuMode mode)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return false;

            using var key = GetRegKey(true);
            if (key == null)
                return false;

            key.SetValue(GetSubKeyName(appInfo), $"GpuPreference={(int)mode};");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static LaptopGpuMode GetModeFromKey(AppInfo appInfo, RegistryKey key)
    {
        if (!OperatingSystem.IsWindows())
            return LaptopGpuMode.Auto;

        var lookupValue = key.GetValue(GetSubKeyName(appInfo))?.ToString();
        if (lookupValue == null)
            return LaptopGpuMode.Auto;

        var regString = lookupValue.Replace("GpuPreference=", "").Replace(";", "");
        if (int.TryParse(regString, out var value))
            return (LaptopGpuMode)value;

        return LaptopGpuMode.Auto;
    }

    private static string GetSubKeyName(AppInfo appInfo) => Path.Combine(appInfo.ApplicationDirectory, appInfo.ApplicationExe);

    private static RegistryKey? GetRegKey(bool writeable)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        return Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences", writeable);
    }
}
