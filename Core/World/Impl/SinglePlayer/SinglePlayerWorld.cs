using System;
using System.Numerics;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Players;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        public readonly Player Player;
        
        private SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, Map map, BspTree bspTree) : 
            base(config, archiveCollection, map, bspTree)
        {
            Player = EntityManager.CreatePlayer(1);
        }

        public static SinglePlayerWorld? Create(Config config, ArchiveCollection archiveCollection, Map map, 
            MapEntryCollection? mapEntryCollection)
        {
            BspTree? bspTree = BspTree.Create(map, mapEntryCollection);
            return bspTree != null ? new SinglePlayerWorld(config, archiveCollection, map, bspTree) : null;
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            Vec2I pixelsMoved = frameInput.ConsumeMouseDelta();
            Vector2 moveDelta = pixelsMoved.ToFloat() / (float)Config.Engine.Mouse.PixelDivisor;
            moveDelta.X *= (float)(Config.Engine.Mouse.Sensitivity * Config.Engine.Mouse.Yaw);
            moveDelta.Y *= (float)(Config.Engine.Mouse.Sensitivity * Config.Engine.Mouse.Pitch);

            Player.AddToYaw(moveDelta.X);
            Player.AddToPitch(moveDelta.Y);
        }

        public void HandleTickCommand(TickCommand tickCommand)
        {
            Entity entity = Player.Entity;
            
            if (tickCommand.Has(TickCommands.Forward))
            {
                double x = Math.Cos(entity.Angle) * Player.ForwardMovementSpeed;
                double y = Math.Sin(entity.Angle) * Player.ForwardMovementSpeed;
                entity.Velocity.X += x;
                entity.Velocity.Y += y;
            }

            if (tickCommand.Has(TickCommands.Jump) && Player.AbleToJump())
                entity.Velocity.Z += Player.JumpZ;
        }
    }
}