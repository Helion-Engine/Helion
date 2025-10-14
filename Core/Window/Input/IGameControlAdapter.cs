namespace Helion.Window.Input;

using Helion.Audio.Sounds;
using Helion.Geometry.Vectors;
using System;

public enum GyroAxis
{
    Pitch,
    Yaw,
    Roll
}

public enum GyroOrAccelAxis
{
    X,
    Y,
    Z,
    Pitch,
    Yaw,
    Roll
}

/// <summary>
/// Interface for game controller input.  Any implementation of this interface is responsible for deciding which
/// controller is the current "active" one.
/// </summary>
public interface IGameControlAdapter
{
    /// <summary>
    /// 1.  Poll the underlying input source
    /// 2.  Convert any button presses to key presses and send those via input manager, etc.
    /// 3.  Update any reported values for analog axis/gyro states
    /// </summary>
    void Poll();

    /// <summary>
    /// Try to get an analog value in the range [0...1] for the specified key.
    /// </summary>
    /// <param name="key">Key for which to retrieve analog values</param>
    /// <param name="axisAnalogValue">Output: Analog value for the specified key.</param>
    /// <returns>True if the key corresponds to an analog axis, false otherwise</returns>
    bool TryGetAnalogValueForAxis(Key key, out float axisAnalogValue);

    /// <summary>
    /// Ask the controller to vibrate.  This will replace any previous vibration effect, and will fail silently if the
    /// controller does not support vibration.
    /// </summary>
    /// <param name="lowFrequency">Intensity for low-frequency rumble</param>
    /// <param name="highFrequency">Intensity for high-frequency rumble</param>
    /// <param name="durationms">Effect duration in milliseconds</param>
    void Rumble(ushort lowFrequency, ushort highFrequency, uint durationms);

    /// <summary>
    /// Event handler for when the sound system plays a sound.  The controller adapter should decide whether 
    /// to apply a rumble effect.
    /// </summary>
    /// <param name="sender"><see cref="SoundManager"/> that sent the sound effect</param>
    /// <param name="evt">Details about the sound that was created</param>
    void RumbleForSoundCreated(object sender, SoundCreatedEventArgs evt);

    /// <summary>
    /// Get the most recent reported values from the controller's onboard gyroscope/accelerometer, if supported
    /// </summary>
    /// <param name="axis">Gyro/accelerometer axis</param>
    /// <param name="value">Output: last reported value for the specified axis</param>
    /// <returns>True if the controller has a gyro and has reported a value for that axis, false otherwise</returns>
    bool TryGetGyroAxis(GyroOrAccelAxis axis, out float value);

    /// <summary>
    /// Get the estimated absolute position from the controller's onboard gyroscope
    /// </summary>
    /// <param name="axis">Gyro axis</param>
    /// <param name="absoluteValue">Output: last estimated absolute position for the specified axis</param>
    /// <returns>True if the controller has a gyro and has reported a value for that axis, false otherwise</returns>
    bool TryGetGyroAbsolute(GyroAxis axis, out double absoluteValue);

    /// <summary>
    /// Reset estimated absolute orientations for the controller's onboard gyroscope
    /// </summary>
    void ZeroGyroAbsolute();

    /// <summary>
    /// Gets or sets whether the controller should _try_ to report gyro and accelerometer values (if it has a gyro)
    /// Toggling this on and off should also re-zero any absolute orientation values reported by the gyro,
    /// as these have likely become out-of-date.
    /// </summary>
    bool GyroEnabled { get; set; }

    /// <summary>
    /// Whether the controller has a gyro
    /// </summary>
    bool HasGyro { get; }

    /// <summary>
    /// Calibrate the "drift" and "noise" parameters for the gyro
    /// </summary>
    /// <param name="durationMilliseconds">Duration for which to calibrate the gyro</param>
    /// <param name="callback">Callback function to execute when calibration finishes.  Input parameters will
    /// include the "noise" and "drift" calibration values</param>
    /// <returns>True if calibration was started, false otherwise</returns>
    bool CalibrateGyro(int durationMilliseconds, Action<Vec3F, Vec3F> callback);
}
