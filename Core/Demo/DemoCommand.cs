using System.Runtime.InteropServices;

namespace Helion.Demo;

[StructLayout(LayoutKind.Sequential)]
public struct DemoCommand
{
    public int Buttons;
    public double AngleTurn;
    public double PitchTurn;
    public double MouseAngle;
    public double MousePitch;
    public double ForwardMoveSpeed;
    public double SideMoveSpeed;
}
