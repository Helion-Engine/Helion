using Helion.Input;
using Helion.Maps;
using Helion.Projects;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.World.Geometry;
using System.Numerics;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        public Camera Camera { get; } = new Camera();

        private SinglePlayerWorld(Project project, Map map, BspTree bspTree) : base(project, map, bspTree)
        {
        }

        public static SinglePlayerWorld? Create(Project project, Map map, MapEntryCollection? mapEntryCollection)
        {
            BspTree? bspTree = BspTree.Create(map, mapEntryCollection);
            if (bspTree == null)
                return null;

            return new SinglePlayerWorld(project, map, bspTree);
        }

        public void HandleTickInput(ConsumableInput tickInput)
        {
            Camera.Tick();

            //=================================================================
            // TODO: Temporary!
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.W))
                Camera.MoveForward(12);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.A))
                Camera.MoveLeft(8);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.S))
                Camera.MoveBackward(12);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.D))
                Camera.MoveRight(8);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.Space))
                Camera.MoveUp(10);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.C))
                Camera.MoveDown(10);
            //=================================================================
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            Vec2I pixelsMoved = frameInput.ConsumeMouseDelta();
            Vector2 moveDelta = pixelsMoved.ToFloat() / 800.0f;

            Camera.AddToYaw(moveDelta.X);
            Camera.AddToPitch(moveDelta.Y);
        }

        public override void Tick()
        {
            base.Tick();
        }
    }
}
