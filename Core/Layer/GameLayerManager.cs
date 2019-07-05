using System;
using System.Linq;

namespace Helion.Layer
{
    public class GameLayerManager : GameLayer
    {
        protected override double GetPriority() => 0.25;
    }
}