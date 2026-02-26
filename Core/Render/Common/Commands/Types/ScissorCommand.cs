using Helion.Geometry.Boxes;

namespace Helion.Render.OpenGL.Commands.Types;

public enum ScissorEnable
{
    KeepState,
    Disable,
    Enable,
}


public record struct ScissorCommand(Box2I Box, ScissorEnable Enable);
