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

    public static string GetOutFragColor(bool clipPlaneZ)
    {
        if (clipPlaneZ)
            return "";

        return "out vec4 fragColor;";
    }

    public static string GetOutPlaneZ(bool clipPlaneZ)
    {
        if (clipPlaneZ)
            return "layout (location = 0) out float planeZ;";

        return "";
    }

    public static string GetSetPlaneZ(bool clipPlaneZ)
    {
        if (clipPlaneZ)
            return "planeZ = zPos;";

        return "";
    }
}
