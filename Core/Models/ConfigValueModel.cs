namespace Helion.Models
{
    public class ConfigValueModel
    {
        public ConfigValueModel(string key, object value)
        {
            Key = key;
            Value= value;
        }

        public string Key { get; set; } = string.Empty;
        public object Value { get; set; }
    }
}
