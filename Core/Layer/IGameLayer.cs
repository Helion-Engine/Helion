using System;
using Helion.Input;

namespace Helion.Layer
{
    public interface IGameLayer : IDisposable
    {
        void HandleInput(InputEvent input);
        void RunLogic();
    }
}
