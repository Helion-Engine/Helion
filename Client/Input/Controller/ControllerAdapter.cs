namespace Helion.Client.Input.Controller
{
    using System;
    using System.Linq;
    using Helion.Audio.Sounds;
    using Helion.Geometry.Vectors;
    using Helion.Window.Input;
    using SDLControllerWrapper;

    public class ControllerAdapter : IGameControlAdapter, IDisposable
    {
        public float AnalogDeadZone;
        private bool m_enabled;
        private readonly InputManager m_inputManager;
        private SDLControllerWrapper m_controllerWrapper;
        private bool m_disposedValue;
        private bool m_gyroEnabled;
        private float m_smoothingThreshold;
        private bool m_smoothingEnabled;
        private Vec3F m_gyroNoise;
        private Vec3F m_gyroDrift;

        private Controller? m_activeController;
        public bool Enabled
        {
            get
            {
                return m_enabled;
            }
            set
            {
                if (value)
                {
                    m_controllerWrapper.DetectControllers();
                    m_activeController = m_controllerWrapper.Controllers.FirstOrDefault();
                }
                else
                {
                    // Ensure no buttons are "stuck" when we disable the controller.
                    for (Key k = Key.LeftYPlus; k <= Key.DPadRight; k++)
                    {
                        m_inputManager.SetKeyUp(k);
                    }
                }
                m_enabled = value;
            }
        }

        public bool RumbleEnabled { get; set; }

        public bool GyroEnabled
        {
            get
            {
                return m_gyroEnabled;
            }
            set
            {
                if (!m_gyroEnabled)
                {
                    ZeroGyroAbsolute();
                }
                m_gyroEnabled = value;
            }
        }

        public bool SmoothingEnabled
        {
            get
            {
                return m_smoothingEnabled;
            }
            set
            {
                m_smoothingEnabled = value;
                SetGyroSmoothing(m_smoothingEnabled, m_smoothingThreshold);
            }
        }

        public float SmoothingThreshold
        {
            get
            {
                return m_smoothingThreshold;
            }
            set
            {
                m_smoothingThreshold = value;
                SetGyroSmoothing(m_smoothingEnabled, m_smoothingThreshold);
            }
        }

        public bool HasGyro => m_activeController?.HasGyro ?? false;

        public bool CalibrateGyro(int durationMilliseconds, Action<Vec3F, Vec3F> callback)
        {
            return m_activeController?.CalibrateGyro(durationMilliseconds, () => GetCalibrationCallback(callback)) ?? false;
        }

        private void GetCalibrationCallback(Action<Vec3F, Vec3F> externalCallback)
        {
            if (m_activeController != null)
            {
                m_gyroNoise = new Vec3F(m_activeController.GyroNoise[0], m_activeController.GyroNoise[1], m_activeController.GyroNoise[2]);
                m_gyroDrift = new Vec3F(m_activeController.GyroDrift[0], m_activeController.GyroDrift[1], m_activeController.GyroDrift[2]);
            }

            externalCallback(m_gyroNoise, m_gyroDrift);
        }

        public ControllerAdapter(
            float analogDeadZone,
            bool enabled,
            bool rumbleEnabled,
            bool gyroSmoothingEnabled,
            float gyroSmoothingThresholdDegrees,
            Vec3F gyroNoise,
            Vec3F gyroDrift,
            InputManager inputManager)
        {
            AnalogDeadZone = analogDeadZone;
            m_enabled = enabled;
            RumbleEnabled = rumbleEnabled;
            m_inputManager = inputManager;
            inputManager.AnalogAdapter = this;

            m_controllerWrapper = new SDLControllerWrapper(HandleConfigChange);
            m_activeController = m_controllerWrapper.Controllers.FirstOrDefault();

            SetGyroCalibration(gyroNoise, gyroDrift);

            m_smoothingEnabled = gyroSmoothingEnabled;
            m_smoothingThreshold = gyroSmoothingThresholdDegrees;

            SetGyroSmoothing(m_smoothingEnabled, m_smoothingThreshold);
        }

        private static float DegreesToRads(float degrees) => (float)(degrees / 360 * (2 * Math.PI));

        private void HandleConfigChange(object? sender, ConfigurationEvent configEvent)
        {
            if (configEvent.ChangeType == ConfigurationChange.Removed
                && configEvent.JoystickIndex == m_activeController?.JoystickIndex)
            {
                m_activeController = null;
            }

            if (configEvent.ChangeType == ConfigurationChange.Added
                && m_activeController == null)
            {
                m_activeController = m_controllerWrapper.Controllers.First();
                ApplyGyroCalibration();
            }
        }

        public void Poll()
        {
            if (!m_enabled)
            {
                return;
            }

            // We must always poll, because this is also how we will detect if a controller is connected.
            m_controllerWrapper.Poll();
            if (m_activeController == null)
            {
                return;
            }

            // Check button states, send button updates
            for (int i = 0; i < m_activeController.CurrentButtonValues.Length; i++)
            {
                bool currentlyPressed = m_activeController.CurrentButtonValues[i];
                bool previouslyPressed = m_activeController.PreviousButtonValues[i];

                if (currentlyPressed && !previouslyPressed)
                {
                    m_inputManager.SetKeyDown(ControllerStatic.ButtonsToKeys[i]);
                }
                else if (previouslyPressed && !currentlyPressed)
                {
                    m_inputManager.SetKeyUp(ControllerStatic.ButtonsToKeys[i]);
                }
            }

            // Check axis states, send axis-as-button updates
            for (int i = 0; i < m_activeController.CurrentAxisValues.Length; i++)
            {
                float currentValue = m_activeController.CurrentAxisValues[i];
                float previousValue = m_activeController.PreviousAxisValues[i];

                bool isPositive = currentValue > AnalogDeadZone;
                bool isNegative = currentValue < -AnalogDeadZone;
                bool wasPositive = previousValue > AnalogDeadZone;
                bool wasNegative = previousValue < -AnalogDeadZone;

                (Key? axisNegative, Key axisPositive) = ControllerStatic.AxisToKeys[i];

                if (isPositive && !wasPositive)
                {
                    m_inputManager.SetKeyDown(axisPositive);
                }
                if (isNegative && !wasNegative)
                {
                    if (axisNegative != null)
                    {
                        m_inputManager.SetKeyDown(axisNegative.Value);
                    }
                }
                if (!isPositive && wasPositive)
                {
                    m_inputManager.SetKeyUp(axisPositive);
                }
                if (!isNegative && wasNegative)
                {
                    if (axisNegative != null)
                    {
                        m_inputManager.SetKeyUp(axisNegative.Value);
                    }
                }
            }
        }

        public void SetGyroCalibration(Vec3F gyroNoise, Vec3F gyroDrift)
        {
            m_gyroNoise = gyroNoise;
            m_gyroDrift = gyroDrift;

            ApplyGyroCalibration();
        }

        private void ApplyGyroCalibration()
        {
            if (m_activeController == null)
            {
                return;
            }

            m_activeController.GyroNoise[0] = m_gyroNoise.X;
            m_activeController.GyroNoise[1] = m_gyroNoise.Y;
            m_activeController.GyroNoise[2] = m_gyroNoise.Z;

            m_activeController.GyroDrift[0] = m_gyroDrift.X;
            m_activeController.GyroDrift[1] = m_gyroDrift.Y;
            m_activeController.GyroDrift[2] = m_gyroDrift.Z;
        }

        private void SetGyroSmoothing(bool enable, float thresholdDegrees)
        {
            if (m_activeController==null)
            {
                return;
            }

            m_activeController.PerformSmoothing = enable;
            m_activeController.SmoothingThreshold = DegreesToRads(thresholdDegrees);
        }

        public bool TryGetAnalogValueForAxis(Key key, out float axisAnalogValue)
        {
            if (!m_enabled || m_activeController == null || !ControllerStatic.KeysToAxis.TryGetValue(key, out (int axisId, bool isPositive) axis))
            {
                axisAnalogValue = 0;
                return false;
            }

            axisAnalogValue = m_activeController.CurrentAxisValues[axis.axisId];
            axisAnalogValue = Math.Abs(axis.isPositive
                ? Math.Clamp(axisAnalogValue, 0, 1)
                : Math.Clamp(axisAnalogValue, -1, 0));
            axisAnalogValue = (axisAnalogValue - AnalogDeadZone) / (1 - AnalogDeadZone);

            return true;
        }

        public bool TryGetGyroAxis(GyroOrAccelAxis axis, out float value)
        {
            if (!m_enabled || !m_gyroEnabled || m_activeController == null || !m_activeController.HasGyro)
            {
                value = 0;
                return false;
            }

            switch (axis)
            {
                case GyroOrAccelAxis.X:
                    value = m_activeController.CurrentAccelValues[0];
                    return true;
                case GyroOrAccelAxis.Y:
                    value = m_activeController.CurrentAccelValues[1];
                    return true;
                case GyroOrAccelAxis.Z:
                    value = m_activeController.CurrentAccelValues[2];
                    return true;
                case GyroOrAccelAxis.Pitch:
                    value = m_activeController.CurrentGyroValues[0];
                    return true;
                case GyroOrAccelAxis.Yaw:
                    value = m_activeController.CurrentGyroValues[1];
                    return true;
                case GyroOrAccelAxis.Roll:
                    value = m_activeController.CurrentGyroValues[2];
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        public bool TryGetGyroAbsolute(Helion.Window.Input.GyroAxis axis, out double value)
        {
            if (!m_enabled || !m_gyroEnabled || m_activeController == null || !m_activeController.HasGyro)
            {
                value = 0;
                return false;
            }

            value = m_activeController.CurrentGyroAbsolutePosition[(int)axis];
            return true;
        }

        public void ZeroGyroAbsolute()
        {
            m_activeController?.ZeroGyroAbsolute();
        }

        public void Rumble(ushort lowFrequency, ushort highFrequency, uint durationms)
        {
            if (m_enabled && RumbleEnabled && m_activeController?.HasRumble == true)
            {
                m_activeController.Rumble(lowFrequency, highFrequency, durationms);
            }
        }

        public void RumbleForSoundCreated(object? sender, SoundCreatedEventArgs evt)
        {
            if (!m_enabled || !RumbleEnabled || m_activeController?.HasRumble != true)
            {
                return;
            }

            if (evt.SoundParams.Context.EventType != Audio.SoundEventType.Default)
            {
                Audio.SoundContext ctx = evt.SoundParams.Context;
                m_activeController.Rumble(ctx.LowFrequencyIntensity, ctx.HighFrequencyIntensity, ctx.DurationMilliseconds);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                m_controllerWrapper.Dispose();
                m_disposedValue = true;
            }
        }

        ~ControllerAdapter()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
