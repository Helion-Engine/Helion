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
        public Camera Camera { get; } = new Camera(new Vector3(-80, 748, 90), MathHelper.HalfPi);

        private SinglePlayerWorld(Project project, Map map, BspTree bspTree) : base(project, map, bspTree)
        {
        }

        public static SinglePlayerWorld? Create(Project project, Map map)
        {
            BspTree? bspTree = BspTree.Create(map);
            if (bspTree == null)
                return null;

            return new SinglePlayerWorld(project, map, bspTree);
        }

        public void HandleTickInput(ConsumableInput tickInput)
        {
            //=================================================================
            // TODO: Temporary!
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.W))
                Camera.MoveForward(4);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.A))
                Camera.MoveLeft(4);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.S))
                Camera.MoveBackward(4);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.D))
                Camera.MoveRight(4);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.Space))
                Camera.MoveUp(4);
            if (tickInput.ConsumeKeyPressedOrDown(InputKey.C))
                Camera.MoveDown(4);
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
            Camera.Tick();
            base.Tick();
        }
    }
}
