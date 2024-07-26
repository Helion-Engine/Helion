namespace Helion.Util.Configs.Values;

public class ConfigValueHeader(string headerText) : ConfigValue<string>("", (s) => "")
{
    public string HeaderText { get; set; } = headerText;
}
