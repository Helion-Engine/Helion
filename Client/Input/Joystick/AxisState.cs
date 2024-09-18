namespace Helion.Client.Input.Joystick
{
    public record struct AxisState(float position, float positionCorrected, float delta)
    {
        public static implicit operator (float position, float positionCorrected, float delta)(AxisState value)
        {
            return (value.position, value.positionCorrected, value.delta);
        }

        public static implicit operator AxisState((float position, float positionCorrected, float delta) value)
        {
            return new AxisState(value.position, value.positionCorrected, value.delta);
        }
    }
}
