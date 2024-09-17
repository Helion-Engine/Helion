namespace Helion.Client.Input.Joystick
{
    using OpenTK.Windowing.Common.Input;

    public delegate void HatEventHandler(object source, HatEventArgs args);
    public delegate void ButtonEventHandler(object source, ButtonEventArgs args);
    public delegate void AxisEventHandler(object source, AxisEventArgs args);

    public struct HatEventArgs(int joystickId, int hatId, Hat hatDirection)
    {
        public readonly int JoystickId = joystickId;
        public readonly int HatId = hatId;
        public readonly Hat HatDirection = hatDirection;
    }

    public struct ButtonEventArgs(int joystickId, int buttonId, bool isPressed)
    {
        public readonly int JoystickId = joystickId;
        public readonly int ButtonId = buttonId;
        public readonly bool IsPressed = isPressed;
    }

    public struct AxisEventArgs(int joystickId, int axisId, float deltaPosition, float absolutePosition, float correctedPosition)
    {
        public readonly int JoystickId = joystickId;
        public readonly int AxisId = axisId;
        public readonly float DeltaPosition = deltaPosition;
        public readonly float AbsolutePosition = absolutePosition;
        public readonly float CorrectedPosition = correctedPosition;
    }
}
