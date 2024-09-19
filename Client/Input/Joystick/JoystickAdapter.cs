namespace Helion.Client.Input.Joystick
{
    using Helion.Window.Input;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using OpenTKJoystick = OpenTK.Windowing.GraphicsLibraryFramework.JoystickState;

    public class JoystickAdapter
    {
        private const int RezeroDelay = 1000;
        private readonly IReadOnlyList<OpenTKJoystick> m_windowJoystickStates;

        private readonly JoystickState[][] m_joystickStates;
        private JoystickState[] m_initialJoystickStates;
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
        /// Get data on the positions of each axis on each controller
        /// </summary>
        public AxisState[][] AxisStates { get; private set; }

        public JoystickAdapter(IReadOnlyList<OpenTKJoystick> joystickInputs, float axisDeadzone, InputManager inputManager)
        {
            m_windowJoystickStates = joystickInputs;
            m_inputManager = inputManager;
            AxisStates = [];
            m_joystickStates = new JoystickState[2][];
            m_initialJoystickStates = [];
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

            m_joystickStates[0] = new JoystickState[activeSticks.Length];
            m_joystickStates[1] = new JoystickState[activeSticks.Length];
            m_initialJoystickStates = new JoystickState[activeSticks.Length];

            AxisStates = new AxisState[activeSticks.Length][];

            for (int i = 0; i < activeSticks.Length; i++)
            {
                m_joystickStates[0][i] = new JoystickState(activeSticks[i]);
                m_joystickStates[1][i] = new JoystickState(activeSticks[i]);
                m_initialJoystickStates[i] = new JoystickState(activeSticks[i]);
                AxisStates[i] = new AxisState[activeSticks[i].AxisCount];
            }

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
            foreach (JoystickState joystick in m_initialJoystickStates)
            {
                joystick.Update(m_windowJoystickStates[joystick.JoystickId]);
            }

            m_zeroed = true;
        }

        /// <summary>
        /// Get the positions and button states of all active joysticks
        /// </summary>
        public void SampleJoystickStates()
        {
            if (m_initialJoystickStates.Length == 0)
            {
                // Do minimal amount of work if controller input is enabled but no controllers are available
                return;
            }

            // Sample current input state
            foreach (JoystickState joystick in m_joystickStates[m_statePointer])
            {
                joystick.Update(m_windowJoystickStates[joystick.JoystickId]);
            }

            // Diff against input state since the last time we sampled
            // Note that we're using this, instead of OpenTK's built-in "Previous" states, because we may or may not want
            // to sample at the same rate as the parent window.
            for (int joystick = 0; joystick < m_joystickStates[0].Length; joystick++)
            {
                ref JoystickState currentState = ref m_joystickStates[m_statePointer][joystick];
                ref JoystickState prevState = ref m_joystickStates[m_prevStatePointer][joystick];
                ref JoystickState initialState = ref m_initialJoystickStates[joystick];

                CheckAxes(currentState, prevState, initialState, joystick);
                CheckButtons(currentState, prevState);
            }

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

        private void CheckAxes(in JoystickState currentState, in JoystickState prevState, in JoystickState initialState, int joystick)
        {
            for (int axis = 0; axis < currentState.AxisValues.Length; axis++)
            {
                float axisValue = currentState.AxisValues[axis];
                float prevAxisValue = prevState.AxisValues[axis];

                ref AxisState axisState = ref AxisStates[joystick][axis];

                axisState.Position = axisValue;
                axisState.Delta = axisValue - prevAxisValue;

                float axisMovementFromNeutral = axisValue - initialState.AxisValues[axis];
                axisState.PositionCorrected = Math.Sign(axisMovementFromNeutral)
                    * Math.Clamp(Math.Abs(axisMovementFromNeutral) - m_deadZone, 0, 1)
                    / (1 - m_deadZone);

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
    }
}
