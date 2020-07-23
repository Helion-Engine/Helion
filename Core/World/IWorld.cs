using System;
using System.Collections.Generic;
using System.Numerics;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry.Vectors;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World
{
    public interface IWorld : IDisposable
    {
        int Gametick { get; }
        IList<Line> Lines { get; }
        IList<Side> Sides { get; }
        IList<Wall> Walls { get; }
        IList<Sector> Sectors { get; }
        BspTree BspTree { get; }
        LinkableList<Entity> Entities { get; }
        Vec3D ListenerPosition { get; }
        double ListenerAngle { get; }
        double ListenerPitch { get; }
        Entity ListenerEntity { get; }
        
        void Link(Entity entity);
        void Tick();
        IEnumerable<Sector> FindBySectorTag(int tag);
        IEnumerable<Entity> FindByTid(int tid);
    }
}