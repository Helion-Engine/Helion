using System.Numerics;
using Helion.Input;
using Helion.Maps;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.World.Entity.Player;
using Helion.World.Geometry;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        public Camera Camera { get; } = new Camera(new Vector3(-80, 748, 90), MathHelper.HalfPi);

        private SinglePlayerWorld(ArchiveCollection archiveCollection, Map map, BspTree bspTree) : 
            base(archiveCollection, map, bspTree)
        {
        }

        public static SinglePlayerWorld? Create(ArchiveCollection archiveCollection, Map map, 
            MapEntryCollection? mapEntryCollection)
        {
            BspTree? bspTree = BspTree.Create(map, mapEntryCollection);
            if (bspTree == null)
                return null;

            return new SinglePlayerWorld(archiveCollection, map, bspTree);
        }

        public void HandleTickCommand(TickCommand tickCommand)
        {
            Camera.Tick();

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
            Vector2 moveDelta = pixelsMoved.ToFloat() / 800.0f;

            Camera.AddToYaw(moveDelta.X);
            Camera.AddToPitch(moveDelta.Y);
        }
    }
}
