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
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;

            layout (location = 0) out vec3 outPlane;

            void main() {
                outPlane = vec3(mapIdFrag, depthFrag, lowerFrag + (upperFrag * 2));
            }";
}