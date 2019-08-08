using System.Runtime.InteropServices;

namespace Helion.Render.OldOpenGL.Renderers.World.Geometry
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct SectorFlatData
    {
        public readonly float PlaneStartA;
        public readonly float PlaneStartB;
        public readonly float PlaneStartC;
        public readonly float PlaneStartD;
        public readonly float PlaneEndA;
        public readonly float PlaneEndB;
        public readonly float PlaneEndC;
        public readonly float PlaneEndD;
        public readonly float LightLevelStart;
        public readonly float LightLevelEnd;
        public readonly float TextureTableIndex;

        public SectorFlatData(float planeStartA, float planeStartB, float planeStartC, float planeStartD, 
            float planeEndA, float planeEndB, float planeEndC, float planeEndD, 
            float lightLevelStart, float lightLevelEnd, float textureTableIndex)
        {
            PlaneStartA = planeStartA;
            PlaneStartB = planeStartB;
            PlaneStartC = planeStartC;
            PlaneStartD = planeStartD;
            PlaneEndA = planeEndA;
            PlaneEndB = planeEndB;
            PlaneEndC = planeEndC;
            PlaneEndD = planeEndD;
            LightLevelStart = lightLevelStart;
            LightLevelEnd = lightLevelEnd;
            TextureTableIndex = textureTableIndex;
        }
    }
}