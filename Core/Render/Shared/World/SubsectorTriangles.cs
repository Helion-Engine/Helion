using System.Collections.Generic;
using Helion.Maps.Geometry;

namespace Helion.Render.Shared.World
{
    public class SubsectorFlatFan
    {
        public readonly Vertex Root;
        public readonly List<Vertex> Fan;
        public readonly string Texture;
        public readonly Sector Sector;

        public SubsectorFlatFan(Vertex root, List<Vertex> fan, string texture, Sector sector)
        {
            Root = root;
            Fan = fan;
            Texture = texture;
            Sector = sector;
        }
    }
    
    public class SubsectorTriangles
    {
        public readonly SubsectorFlatFan Floor;
        public readonly SubsectorFlatFan Ceiling;

        public SubsectorTriangles(SubsectorFlatFan floor, SubsectorFlatFan ceiling)
        {
            Floor = floor;
            Ceiling = ceiling;
        }
    }
}