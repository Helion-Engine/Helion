namespace Helion.Models;

public struct ConfigValueModel(string key, object value)
{
    public string Key { get; set; } = key;
    public object Value { get; set; } = value;
}
