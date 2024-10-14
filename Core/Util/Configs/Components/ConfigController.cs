namespace Helion.Util.Configs.Components;

using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

public class ConfigController: ConfigElement<ConfigController>
{
    // Controller

    [ConfigInfo("Enable game controller support.")]
    [OptionMenu(OptionSectionType.Controller, "Enable Game Controller", spacer: true)]
    public readonly ConfigValue<bool> EnableGameController = new(true);

    [ConfigInfo("Dead zone for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Dead Zone")]
    public readonly ConfigValue<double> GameControllerDeadZone = new(0.2, Clamp(0.1, 0.9));

    [ConfigInfo("Turn speed scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Turn Sensitivity")]
    public readonly ConfigValue<double> GameControllerTurnScale = new(1.0, Clamp(0.1, 3.0));

    [ConfigInfo("Pitch speed scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Pitch Sensitivity")]
    public readonly ConfigValue<double> GameControllerPitchScale = new(0.5, Clamp(0.1, 3.0));

    [ConfigInfo("Run input scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Run Sensitivity")]
    public readonly ConfigValue<double> GameControllerRunScale = new(1.0, Clamp(0.1, 3.0));

    [ConfigInfo("Strafe input scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Strafe Sensitivity")]
    public readonly ConfigValue<double> GameControllerStrafeScale = new(1.0, Clamp(0.1, 3.0));
}

