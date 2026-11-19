using NLog;
using System;
using System.IO;
using System.Text.Json;

namespace Helion.Util;

public class AppState<T>(IAppStateFactory factory, string filePath)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly T Data = ReadFile(factory, filePath);
    public readonly string FilePath = filePath;

    private readonly IAppStateFactory m_factory = factory;

    private static T ReadFile(IAppStateFactory factory, string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                using var stream = new FileStream(filePath, FileMode.Open);
                return (T?)JsonSerializer.Deserialize(stream, factory.JsonInfo) ?? (T)factory.Create();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed read app state for {filePath}: {ex.Message}");
        }

        return (T)factory.Create();
    }

    public bool Write(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            JsonSerializer.Serialize(stream, Data, m_factory.JsonInfo);
        }
        catch
        {
            return false;
        }

        return true;
    }
}
