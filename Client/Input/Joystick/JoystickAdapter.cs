namespace Helion.Client.Input.Joystick
{
    using Helion.Window.Input;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using OpenTKJoystick = OpenTK.Windowing.GraphicsLibraryFramework.JoystickState;

    public class JoystickAdapter : IAnalogAdapter
    {
        private const int RezeroDelay = 1000;

        private readonly IReadOnlyList<OpenTKJoystick> m_windowJoystickStates;
        private OpenTKJoystick? m_activeJoystick;

        private readonly JoystickState[] m_joystickState;
        private JoystickState m_initialJoystickState;

        private int m_statePointer = 0;
        private int m_prevStatePointer = 1;
        private float m_deadZone;
        private bool m_zeroed;

        private InputManager m_inputManager;
        private Timer m_zeroTimer;

        /// <summary>
        /// Get or set the dead zone for the controllers, in the range of 0-.5 (the range for an axis is [-1..+1])
        /// </summary>
        public float DeadZone
        {
            get => m_deadZone;
            set
            {
                m_deadZone = Math.Clamp(value, .1f, .5f);
            }
        }


        /// <summary>
        /// Selects the active joystick ID; this should only be needed if we allow the user to plug in multiple joysticks
        /// and select one.
        /// </summary>
        public int ActiveJoystick
        {
            get => m_activeJoystick?.Id ?? -1;
            set
            {
                m_activeJoystick = m_windowJoystickStates.FirstOrDefault(j => j.Id == value) ?? m_activeJoystick;
            }
        }

        /// <summary>
        /// Get data on the positions of each axis on the current controller
        /// </summary>
        public AxisState[] AxisStates { get; private set; }

        public JoystickAdapter(IReadOnlyList<OpenTKJoystick> joystickInputs, float axisDeadzone, InputManager inputManager)
        {
            m_windowJoystickStates = joystickInputs;
            m_inputManager = inputManager;
            m_inputManager.AnalogAdapter = this;
            AxisStates = [];
            m_joystickState = new JoystickState[2];
            m_deadZone = axisDeadzone;
            m_zeroTimer = new Timer(RezeroDelay);

            RedetectJoysticks();
        }

        /// <summary>
        /// Redetect active joysticks after a configuration change
        /// </summary>
        public void RedetectJoysticks()
        {
            OpenTKJoystick[] activeSticks = m_windowJoystickStates
                .Where(stick => stick != null)
                .ToArray();

            if (activeSticks.Length == 0)
            {
                AxisStates = [];
                m_activeJoystick = null;
                return;
            }

            AxisStates = new AxisState[activeSticks.Length];
            m_activeJoystick = activeSticks.FirstOrDefault(joystick => joystick.Id == m_activeJoystick?.Id) ??
                activeSticks.First();

            m_joystickState[0] = new JoystickState(m_activeJoystick);
            m_joystickState[1] = new JoystickState(m_activeJoystick);
            m_initialJoystickState = new JoystickState(m_activeJoystick);
            AxisStates = new AxisState[m_activeJoystick.AxisCount];

            // Initial zeros when a joystick is plugged in are not reliable, particularly for "trigger" axes.
            // Schedule a re-zero after some time has passed.
            m_zeroed = false;
            m_zeroTimer.Elapsed += RezeroTimerElapsed;
            m_zeroTimer.Start();
        }

        private void RezeroTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            m_zeroTimer.Stop();
            m_zeroTimer.Elapsed -= RezeroTimerElapsed;
            Zero();
        }

        /// <summary>
        /// Reset the "zero" position on all active joysticks to whatever position they are now in
        /// This should be called at some point after launch, since some trigger buttons have a nonzero resting position.
        /// </summary>
        public void Zero()
        {
            if (m_activeJoystick == null)
            {
                return;
            }

            m_initialJoystickState.Update(m_activeJoystick);
            m_zeroed = true;
        }

        /// <summary>
        /// Get the positions and button states of all active joysticks
        /// </summary>
        public void SampleJoystickStates()
        {
            if (m_activeJoystick == null)
            {
                // Do minimal amount of work if controller input is enabled but no controllers are available
                return;
            }

            // Sample current input state
            m_joystickState[m_statePointer].Update(m_activeJoystick);

            // Diff against input state since the last time we sampled
            // Note that we're using this, instead of OpenTK's built-in "Previous" states, because we may or may not want
            // to sample at the same rate as the parent window.
            ref JoystickState currentState = ref m_joystickState[m_statePointer];
            ref JoystickState prevState = ref m_joystickState[m_prevStatePointer];
            ref JoystickState initialState = ref m_initialJoystickState;

            CheckAxes(currentState, prevState, initialState);
            CheckButtons(currentState, prevState);

            m_statePointer = (m_statePointer + 1) % 2;
            m_prevStatePointer = (m_prevStatePointer + 1) % 2;
        }

        private void CheckButtons(in JoystickState currentState, in JoystickState prevState)
        {
            // Hats/D-pads are aliased as the last buttons on the controller, at least with XBox and DualShock controllers.
            int totalButtonCount = currentState.ButtonStates.Length;
            int hatButtonCount = currentState.HatPositions.Length * 4;
            int normalButtonCount = totalButtonCount - hatButtonCount;

            // Regular buttons
            for (int button = 0; button < normalButtonCount; button++)
            {
                bool currentlyPressed = currentState.ButtonStates[button];
                bool previouslyPressed = prevState.ButtonStates[button];

                if (button < JoystickStatic.ButtonsToKeys.Length)
                {
                    if (currentlyPressed && !previouslyPressed)
                    {
                        m_inputManager.SetKeyDown(JoystickStatic.ButtonsToKeys[button]);
                    }

                    if (!currentlyPressed && previouslyPressed)
                    {
                        m_inputManager.SetKeyUp(JoystickStatic.ButtonsToKeys[button]);
                    }
                }
            }

            // Hat (D-pad) buttons
            for (int hatButton = 0; hatButton < hatButtonCount; hatButton++)
            {
                bool currentlyPressed = currentState.ButtonStates[normalButtonCount + hatButton];
                bool previouslyPressed = prevState.ButtonStates[normalButtonCount + hatButton];

                if (hatButton < JoystickStatic.PadToKeys.Length)
                {
                    if (currentlyPressed && !previouslyPressed)
                    {
                        m_inputManager.SetKeyDown(JoystickStatic.PadToKeys[hatButton]);
                    }

                    if (!currentlyPressed && previouslyPressed)
                    {
                        m_inputManager.SetKeyUp(JoystickStatic.PadToKeys[hatButton]);
                    }
                }
            }
        }

        private void CheckAxes(in JoystickState currentState, in JoystickState prevState, in JoystickState initialState)
        {
            for (int axis = 0; axis < currentState.AxisValues.Length; axis++)
            {
                float axisValue = currentState.AxisValues[axis];
                float prevAxisValue = prevState.AxisValues[axis];

                ref AxisState axisState = ref AxisStates[axis];

                axisState.Position = axisValue;
                axisState.Delta = axisValue - prevAxisValue;

                float axisMovementFromNeutral = axisValue - initialState.AxisValues[axis];

                axisState.PositionCorrected = Math.Clamp(Math.Sign(axisMovementFromNeutral)
                    * Math.Clamp(Math.Abs(axisMovementFromNeutral) - m_deadZone, 0, 1)
                    / (1 - m_deadZone), -1, 1);

                bool pressedPositive = axisMovementFromNeutral > m_deadZone;
                bool pressedNegative = axisMovementFromNeutral < -m_deadZone;

                if (m_zeroed && axis < JoystickStatic.AxisToKeys.Length / 2)
                {
                    if (!axisState.PressedPositive && pressedPositive)
                    {
                        m_inputManager.SetKeyDown(JoystickStatic.AxisToKeys[axis * 2]);
                    }
                    if (axisState.PressedPositive && !pressedPositive)
                    {
                        m_inputManager.SetKeyUp(JoystickStatic.AxisToKeys[axis * 2]);
                    }
                    if (!axisState.PressedNegative && pressedNegative)
                    {
                        m_inputManager.SetKeyDown(JoystickStatic.AxisToKeys[(axis * 2) + 1]);
                    }
                    if (axisState.PressedNegative && !pressedNegative)
                    {
                        m_inputManager.SetKeyUp(JoystickStatic.AxisToKeys[(axis * 2) + 1]);
                    }
                }

                axisState.PressedPositive = pressedPositive;
                axisState.PressedNegative = pressedNegative;
            }
        }

        /// <summary>
        /// Gets value in the range [0..1] for the specified "key", if it is actually an analog axis.
        /// This is a reverse lookup from "pressed key" to an analog axis.
        /// </summary>
        /// <param name="key">Key for which to get analog values</param>
        /// <param name="axisAnalogValue">Scaled analog value for the axis, in the range [0..1]</param>
        /// <returns>True (and a floating point value) if the axis exists on a currently active controller, false (and zero) otherwise</returns>
        public bool TryGetAnalogValueForAxis(Key key, out float axisAnalogValue)
        {
            if ((m_activeJoystick == null)
                || !JoystickStatic.KeysToAxis.TryGetValue(key, out (int axisId, bool isPositive) axisLookup)
                || m_activeJoystick.AxisCount < axisLookup.axisId)
            {
                axisAnalogValue = 0f;
                return false;
            }

            float correctedAxisValue = AxisStates[axisLookup.axisId].PositionCorrected;
            axisAnalogValue = Math.Abs(axisLookup.isPositive
                ? Math.Clamp(correctedAxisValue, 0, 1)
                : Math.Clamp(correctedAxisValue, -1, 0));
            return true;
        }
    }
}
