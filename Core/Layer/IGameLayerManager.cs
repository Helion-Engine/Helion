using System;

namespace Helion.Layer;

public interface IGameLayerManager : IGameLayerParent
{
    event EventHandler<IGameLayer> GameLayerAdded;
}
