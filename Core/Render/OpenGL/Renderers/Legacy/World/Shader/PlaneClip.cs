namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class PlaneClip
{
    public static string WritePlaneFragFunction() =>
         @"
            #version 330

            flat in float zPos;
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;

            layout (location = 0) out vec3 outPlane;

            void main() {
                outPlane = vec3(zPos, depthFrag, lowerFrag + (upperFrag * 2));
            }";

    public static string WriteWallFragFunction() =>
         @"
            #version 330

            flat in float mapIdFrag;
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;

            layout (location = 0) out vec3 outPlane;

            void main() {
                // This is required for flood fill rendering that doesn't separate planes(floor/ceiling) from walls
                // Planes are written with -1 and need to be discarded
                if (mapIdFrag < 0)
                    discard;
                outPlane = vec3(mapIdFrag, depthFrag, lowerFrag + (upperFrag * 2));
            }";
}