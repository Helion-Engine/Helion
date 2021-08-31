using System;
using Helion.Window.Input;

namespace Helion.Layer
{
    public interface IGameLayer : IDisposable
    {
        void HandleInput(InputEvent input);
        void RunLogic();
    }
}
