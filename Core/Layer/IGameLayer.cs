using System;
using Helion.Util.Timing;
using Helion.Window;

namespace Helion.Layer;

public interface IGameLayer : IDisposable
{
    void OnShow() { }
    void HandleInput(IConsumableInput input);
    void RunLogic(TickerInfo tickerInfo);
}
