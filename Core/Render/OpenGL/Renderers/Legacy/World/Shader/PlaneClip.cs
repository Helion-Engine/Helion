namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class PlaneClip
{
    public static string WritePlaneFragFunction() =>
         @"
            #version 330

            flat in float zPos;
            in float depthFrag;

            layout (location = 0) out vec3 outPlaneZ;

            void main() {
                outPlaneZ = vec3(zPos, depthFrag, 0);
            }";

    public static string WriteWallFragFunction() =>
         @"
            #version 330

            flat in float mapIdFrag;
            in float depthFrag;

            layout (location = 0) out vec3 outPlaneZ;

            void main() {
                outPlaneZ = vec3(mapIdFrag, depthFrag, 1);
            }";
}