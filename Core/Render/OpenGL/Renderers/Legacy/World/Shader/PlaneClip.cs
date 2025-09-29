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

            layout (location = 0) out vec4 outPlane;

            void main() {
                // This is required for flood fill rendering that doesn't separate planes(floor/ceiling) from walls
                // Planes are written with -1 and need to be discarded
                if (mapIdFrag < 0)
                    discard;
                
                int lineId = int(mapIdFrag);
                int byte0 = lineId & 0xFF;
                int byte1 = (lineId >> 8) & 0xFF;
                int byte2 = ((lineId >> 16) & 0x3F) << 2 | int(lowerFrag + (upperFrag * 2));
                outPlane = vec4(byte0, depthFrag, byte2, byte1);
            }";
}