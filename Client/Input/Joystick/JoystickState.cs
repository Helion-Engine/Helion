namespace Helion.Client.Input.Joystick
{
    using OpenTK.Windowing.Common.Input;
    using OpenTKJoystick = OpenTK.Windowing.GraphicsLibraryFramework.JoystickState;

    public struct JoystickState
    {
        public readonly int JoystickId;
        public readonly float[] AxisValues;
        public readonly bool[] ButtonStates;
        public readonly Hat[] HatPositions;

        public JoystickState(OpenTKJoystick openTKState)
        {
            JoystickId = openTKState.Id;
            AxisValues = new float[openTKState.AxisCount];
            ButtonStates = new bool[openTKState.ButtonCount];
            HatPositions = new Hat[openTKState.HatCount];

            Update(openTKState);
        }

        public void Update(OpenTKJoystick openTKState)
        {
            for (int axisId = 0; axisId < AxisValues.Length; axisId++)
            {
                AxisValues[axisId] = openTKState.GetAxis(axisId);
            }
            for (int buttonId = 0; buttonId < ButtonStates.Length; buttonId++)
            {
                ButtonStates[buttonId] = openTKState.IsButtonDown(buttonId);
            }
            for (int hatId = 0; hatId < HatPositions.Length; hatId++)
            {
                HatPositions[hatId] = openTKState.GetHat(hatId);
            }
        }
    }
}
