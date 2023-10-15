using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Helion.Render.Common.Renderers;
using Helion.Window;

namespace Helion.Layer.New;

public class GameLayerManager
{
    private readonly List<GameLayer> m_layers = new();

    public void Add<TLayer>(TLayer layer) where TLayer : GameLayer
    {
        foreach (GameLayer existingLayer in m_layers.Where(l => l is TLayer))
            existingLayer.Dispose();
        
        m_layers.Add(layer);
        m_layers.Sort();

        ClearDisposedLayers();
    }

    public bool TryGet<TLayer>([NotNullWhen(true)] out TLayer? layer) where TLayer : GameLayer
    {
        layer = m_layers.FirstOrDefault(l => l is TLayer) as TLayer;
        return layer != null;
    }

    public bool HasLayer<TLayer>() where TLayer : GameLayer
    {
        return TryGet<TLayer>(out _);
    }

    public void Clear()
    {
        foreach (GameLayer layer in m_layers)
            layer.Dispose();

        m_layers.Clear();
    }

    private void ClearDisposedLayers()
    {
        m_layers.RemoveAll(l => l.IsDisposed);
    }

    public bool ShouldFocus()
    {
        foreach (GameLayer layer in m_layers)
        {
            bool? focus = layer.ShouldFocus();
            if (focus.HasValue)
                return focus.Value;
        }

        return false;
    }

    public void HandleInput(IConsumableInput input)
    {
        foreach (GameLayer layer in m_layers)
            layer.HandleInput(input);
    }

    public void RunLogic()
    {
        foreach (GameLayer layer in m_layers)
            layer.RunLogic();
        
        ClearDisposedLayers();
    }

    public void Render(IHudRenderContext ctx)
    {
        foreach (GameLayer layer in m_layers)
            layer.Render(ctx);
    }
}