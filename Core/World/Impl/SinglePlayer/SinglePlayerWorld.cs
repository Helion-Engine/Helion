using System;
using System.Numerics;
using Helion.Input;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Things;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.World.Entity.Player;
using Helion.World.Geometry;
using NLog;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Camera Camera { get; private set; } = new Camera();

        private SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, Map map, BspTree bspTree) : 
            base(config, archiveCollection, map, bspTree)
        {
            SetCameraToSpawn(map);
        }

        public static SinglePlayerWorld? Create(Config config, ArchiveCollection archiveCollection, Map map, 
            MapEntryCollection? mapEntryCollection)
        {
            BspTree? bspTree = BspTree.Create(map, mapEntryCollection);
            return bspTree != null ? new SinglePlayerWorld(config, archiveCollection, map, bspTree) : null;
        }

        public void HandleTickCommand(TickCommand tickCommand)
        {
            Camera.StartNewTick();

            if (tickCommand.Has(TickCommands.Forward))
                Camera.MoveForward(12);
            if (tickCommand.Has(TickCommands.Backward))
                Camera.MoveBackward(12);
            if (tickCommand.Has(TickCommands.Left))
                Camera.MoveLeft(8);
            if (tickCommand.Has(TickCommands.Right))
                Camera.MoveRight(8);
            if (tickCommand.Has(TickCommands.Jump))
                Camera.MoveUp(10);
            if (tickCommand.Has(TickCommands.Crouch))
                Camera.MoveDown(10);
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            Vec2I pixelsMoved = frameInput.ConsumeMouseDelta();
            Vector2 moveDelta = pixelsMoved.ToFloat() / (float)Config.Engine.Mouse.PixelDivisor;
            moveDelta.X *= (float)(Config.Engine.Mouse.Sensitivity * Config.Engine.Mouse.Yaw);
            moveDelta.Y *= (float)(Config.Engine.Mouse.Sensitivity * Config.Engine.Mouse.Pitch);

            Camera.AddToYaw(moveDelta.X);
            Camera.AddToPitch(moveDelta.Y);
        }

        private void SetCameraToSpawn(Map map)
        {
            // This is a hack; we're doing temporarily until we properly make
            // entities in a level. Because we will remove this, there won't be
            // any slope support for this.
            foreach (Thing thing in map.Things)
            {
                if (thing.EditorNumber != 1) 
                    continue;

                Vector3 position = thing.Position.ToFloat();
                Sector sector = BspTree.ToSector(thing.Position.To2D());
                position.Z = Math.Max(position.Z, (float)sector.Floor.Plane.FlatHeight + 50.0f);
                position.Z = Math.Min(position.Z, (float)sector.Ceiling.Plane.FlatHeight);

                Camera = new Camera(position, thing.angleRadians);
                return;
            }
            
            Log.Warn("No player 1 spawns detected in map, camera set to origin");
        }
    }
}