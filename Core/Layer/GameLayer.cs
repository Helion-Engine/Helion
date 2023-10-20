using System;
using Helion.Render.Common.Renderers;
using Helion.Window;

namespace Helion.Layer;

// Disposable is not for resources, but rather signals cleanup for the GameLayerManager.
public abstract class GameLayer : IComparable<GameLayer>, IDisposable
{
    public bool IsDisposed { get; private set; }
    public abstract double Priority { get; }

    // True means it should focus, false means it should not focus, null means this
    // layer doesn't care and the caller should continue evaluating other layers.
    public abstract bool? ShouldFocus();
    public abstract void HandleInput(IConsumableInput input);
    public abstract void RunLogic();
    public abstract void Render(IRenderableSurfaceContext surface, IHudRenderContext ctx);
    
    public int CompareTo(GameLayer? other)
    {
        return Priority.CompareTo(other?.Priority ?? int.MaxValue);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        IsDisposed = true;
    }
}