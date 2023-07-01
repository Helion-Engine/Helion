using Helion.Window;

namespace Helion.Layer;

public interface IGameLayerManager : IGameLayerParent
{
    void HandleInput(IInputManager inputManager);
}
