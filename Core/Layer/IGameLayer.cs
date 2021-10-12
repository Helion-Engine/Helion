using System;
using Helion.Window;

namespace Helion.Layer;

public interface IGameLayer : IDisposable
{
    void HandleInput(IConsumableInput input);
    void RunLogic();
}

