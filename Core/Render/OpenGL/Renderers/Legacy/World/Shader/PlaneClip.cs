namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class PlaneClip
{
    public static string WritePlaneFragFunction() =>
         @"
            #version 330

            uniform int planeType;

            flat in float zPos;
            in float depthFrag;

            layout (location = 0) out vec3 outPlane;

            void main() {
                outPlane = vec3(zPos, depthFrag, planeType);
            }";

    public static string WriteWallFragFunction() =>
         @"
            #version 330

            uniform int planeType;

            flat in float mapIdFrag;
            in float depthFrag;

            layout (location = 0) out vec3 outPlane;

            void main() {
                outPlane = vec3(mapIdFrag, depthFrag, 0);
            }";
}