using Helion.Graphics;
using Helion.World.Geometry.Sectors;

namespace Helion.World;

public record struct SectorFogEvent(Sector Sector, Color PreviousColor);
