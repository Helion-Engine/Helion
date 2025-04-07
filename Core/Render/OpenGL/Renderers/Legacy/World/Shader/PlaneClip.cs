namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class PlaneClip
{
    public static string WritePlaneFragFunction() =>
         @"
            #version 330

            flat in float zPos;
            in float depthFrag;

            layout (location = 0) out vec2 outPlaneZ;

            void main() {
                outPlaneZ = vec2(zPos, depthFrag);
            }";
}