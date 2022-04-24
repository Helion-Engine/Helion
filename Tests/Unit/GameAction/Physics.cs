using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public partial class Physics : IDisposable
    {
        private static readonly string ResourceZip = "Resources/physics.zip";

        private static readonly string MapName = "MAP01";
        private static readonly string Zombieman = "ZOMBIEMAN";
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;

        private const int LiftLine1 = 10;
        private const int LiftLine2 = 16;
        private const int LiftLine3 = 23;
        private static readonly Vec2D LiftCenter1 = new(64, 416);
        private static readonly Vec2D LiftBlock1 = new(64, 448);
        private static readonly Vec2D LiftCenter2 = new(256, 416);
        private static readonly Vec2D LiftCenter3 = new(-128, 416);

        public Physics()
        {
            World = WorldAllocator.LoadMap(ResourceZip, "physics.wad", MapName, GetType().Name, WorldInit, IWadType.Doom2);
        }

        public void Dispose()
        {
            Player.Velocity = Vec3D.Zero;
            GameActions.DestroyCreatedEntities(World);
            GC.SuppressFinalize(this);
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            CheatManager.Instance.ActivateCheat(world.Player, CheatType.God);
        }
    }
}
