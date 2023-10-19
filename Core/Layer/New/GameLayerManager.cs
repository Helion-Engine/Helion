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

    private void ClearDisposedLayers()
    {
        // Avoid GC and LINQ unless we are disposing.
        for (int i = 0; i < m_layers.Count; i++)
        {
            if (!m_layers[i].IsDisposed) 
                continue;
            
            m_layers.RemoveAll(l => l.IsDisposed);
            break;
        }
    }

    public bool ShouldFocus()
    {
        for (int i = 0; i < m_layers.Count; i++)
        {
            bool? focus = m_layers[i].ShouldFocus();
            if (focus.HasValue)
                return focus.Value;
        }

        return false;
    }

    public void HandleInput(IConsumableInput input)
    {
        for (int i = 0; i < m_layers.Count; i++)
            m_layers[i].HandleInput(input);

        ClearDisposedLayers();
    }

    public void RunLogic()
    {
        for (int i = 0; i < m_layers.Count; i++)
            m_layers[i].RunLogic();

        ClearDisposedLayers();
    }

    public void Render(IHudRenderContext ctx)
    {
        for (int i = m_layers.Count - 1; i >= 0; i--)
            m_layers[i].Render(ctx);
    }
}