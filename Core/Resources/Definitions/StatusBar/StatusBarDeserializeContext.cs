namespace Helion.Resources.Definitions.StatusBar;

public static class StatusBarDeserializeContext
{
    public static string Prefix = string.Empty;
    public static string GetName(string name) => $"[{Prefix}].{name}";
}
