using Helion.World;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TransitionCommand(TransitionType type, float progress, bool start)
{
    public readonly TransitionType Type = type;
    public readonly float Progress = progress;
    public readonly bool Start = start;
}