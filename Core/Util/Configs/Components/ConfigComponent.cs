using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public record ConfigComponent(string Path, ConfigInfoAttribute Attribute, IConfigValue Value);
