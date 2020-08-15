using System;
using System.Collections.Generic;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry.Vectors;
using Helion.Util.RandomGenerators;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;

namespace Helion.World
{
    public interface IWorld : IDisposable
    {
        int Gametick { get; }
        WorldState WorldState { get; }
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
        IRandom Random { get; }
        EntityManager EntityManager { get; }
        SoundManager SoundManager { get; }
        BlockmapTraverser BlockmapTraverser { get; }
        PhysicsManager PhysicsManager { get; }

        void Link(Entity entity);
        void Tick();
        IEnumerable<Sector> FindBySectorTag(int tag);
        IEnumerable<Entity> FindByTid(int tid);
        void ExitLevel(LevelChangeType type);
        List<Entity> GetBossTargets();
        int CurrentBossTarget { get; set; }
        void TelefragBlockingEntities(Entity entity);
    }
}