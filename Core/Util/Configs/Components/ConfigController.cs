namespace Helion.Util.Configs.Components;

using Helion.Geometry.Vectors;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

public enum GyroTurnAxis
{
    Yaw = 1,
    Roll = 2
}

public class ConfigController : ConfigElement<ConfigController>
{
    // Controller

    [ConfigInfo("Enable game controller support.")]
    [OptionMenu(OptionSectionType.Controller, "Enable Game Controller", spacer: true)]
    public readonly ConfigValue<bool> EnableGameController = new(true);

    [ConfigInfo("Enable rumble feedback effects.")]
    [OptionMenu(OptionSectionType.Controller, "Enable Rumble")]
    public readonly ConfigValue<bool> EnableRumble = new(true);

    [ConfigInfo("Dead zone for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Dead Zone", sliderMin: 0.1, sliderMax: 0.9, sliderStep: .05)]
    public readonly ConfigValue<double> GameControllerDeadZone = new(0.2, Clamp(0.1, 0.9));

    [ConfigInfo("Turn speed scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Turn Sensitivity", sliderMin: 0.1, sliderMax: 3.0, sliderStep: .05)]
    public readonly ConfigValue<double> GameControllerTurnScale = new(1.0, Clamp(0.1, 3.0));

    [ConfigInfo("Pitch speed scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Pitch Sensitivity", sliderMin: 0.1, sliderMax: 3.0, sliderStep: .05)]
    public readonly ConfigValue<double> GameControllerPitchScale = new(0.5, Clamp(0.1, 3.0));

    [ConfigInfo("Run input scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Run Sensitivity", sliderMin: 0.1, sliderMax: 3.0, sliderStep: .05)]
    public readonly ConfigValue<double> GameControllerRunScale = new(1.0, Clamp(0.1, 3.0));

    [ConfigInfo("Strafe input scaling factor for analog inputs.")]
    [OptionMenu(OptionSectionType.Controller, "Strafe Sensitivity", sliderMin: 0.1, sliderMax: 3.0, sliderStep: .05)]
    public readonly ConfigValue<double> GameControllerStrafeScale = new(1.0, Clamp(0.1, 3.0));

    // Gyro aiming

    [ConfigInfo("Gyro axis to use for turning left and right.")]
    [OptionMenu(OptionSectionType.Controller, "Gyro Aim Turn Axis", spacer: true)]
    public readonly ConfigValue<GyroTurnAxis> GyroAimTurnAxis = new(GyroTurnAxis.Yaw);

    [ConfigInfo("Vertical aiming sensitivity for gyro input.")]
    [OptionMenu(OptionSectionType.Controller, "Gyro Aim Vertical Sensitivity", sliderMin: 0, sliderMax: 10, sliderStep: .1)]
    public readonly ConfigValue<double> GyroAimVerticalSensitivity = new(3.0, Clamp(0, 10.0));

    [ConfigInfo("Horizontal aiming sensitivity for gyro input.")]
    [OptionMenu(OptionSectionType.Controller, "Gyro Aim Turn Sensitivity", sliderMin: 0, sliderMax: 10, sliderStep: .1)]
    public readonly ConfigValue<double> GyroAimHorizontalSensitivity = new(3.0, Clamp(0, 10.0));

    [ConfigInfo("Whether gyro aiming is on or off by default.  Holding the gyro button on the controller will temporarily switch this.")]
    [OptionMenu(OptionSectionType.Controller, "Gyro On By Default")]
    public readonly ConfigValue<bool> GyroAimOnByDefault = new(true);

    [ConfigInfo("How much to add to sensitivity at the upper gyro threshold. Set to 0 to disable gyro acceleration")]
    [OptionMenu(OptionSectionType.Controller, "Gyro Acceleration")]
    public readonly ConfigValue<double> GyroAcceleration = new(2.0);

    [ConfigInfo("Lower threshold for gyro acceleration. If the speed of the controller falls below this, no acceleration will be applied.")]
    [OptionMenu(OptionSectionType.Controller, "Lower Gyro Threshold")]
    public readonly ConfigValue<double> LowerGyroThreshold = new(0.0);

    [ConfigInfo("Upper threshold for gyro acceleration. Beyond this point, no more acceleration will be applied.")]
    [OptionMenu(OptionSectionType.Controller, "Upper Gyro Threshold")]
    public readonly ConfigValue<double> UpperGyroThreshold = new(75.0);

    [ConfigInfo("Whether gyro smoothing should be enabled to reduce twitchiness.")]
    [OptionMenu(OptionSectionType.Controller, "Gyro Smoothing")]
    public readonly ConfigValue<bool> GyroSmoothingEnabled = new(false);

    [ConfigInfo("Gyro smoothing threshold (degrees/s), beyond which smoothing is not applied.")]
    [OptionMenu(OptionSectionType.Controller, "Gyro Smoothing Threshold", sliderMin: 0, sliderMax: 50, sliderStep: .1)]
    public readonly ConfigValue<double> GyroSmoothingThreshold = new(5f, Clamp(0f, 50.0f));

    [ConfigInfo("Perform gyro calibration, if controller has a gyro.")]
    [OptionMenu(OptionSectionType.Controller, "Gyro Calibration", dialogType: DialogType.GyroCalibrationDialog)]
    public readonly ConfigValue<string> GyroCalibrationDummy = new("Calibrate");

    [ConfigInfo("Learned value for gyro drift per sample")]
    public readonly ConfigValue<Vec3F> GyroDrift = new((0, 0, 0));

    [ConfigInfo("Learned value for gyro noise threshold")]
    public readonly ConfigValue<Vec3F> GyroNoise = new((0, 0, 0));
}

