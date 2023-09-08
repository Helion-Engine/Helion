using System.Runtime.InteropServices;
using Helion.RenderNew.Interfaces.World;

namespace Helion.RenderNew.Renderers.World.Geometry;

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public record struct SectorPlaneRenderData(
    float Z, 
    float PrevZ, 
    float LightLevel, 
    int TextureIdx);

[StructLayout(LayoutKind.Sequential, Pack = 32)]
public record struct WallRenderData(
    int TextureIdx,
    int LightLevel,
    int FacingSectorIdx,
    int OppositeSectorIdx,
    float ScrollX,
    float ScrollY,
    int Flags,
    int Padding = 0)
{
    public static int MakeFlags(bool onFront, WallSection section)
    {
        // Bits 0 and 1
        int sectionBits = section switch
        {
            WallSection.Lower => 0b00,
            WallSection.Middle => 0b01,
            WallSection.Upper => 0b10,
            _ => throw new($"Unexpected section type {section}")
        };
        
        // Bit 2
        int frontBits = onFront ? 0b100 : 0b000;

        return sectionBits | frontBits;
    }
}
