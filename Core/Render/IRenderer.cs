using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Window;

namespace Helion.Render;

public interface IRenderer : IDisposable
{
    IRendererTextureManager Textures { get; }
    IWindow Window { get; }
    IRenderableSurface DefaultSurface { get; }
    IRenderableSurface GetOrCreateSurface(string name, Dimension dimension);
    void PerformThrowableErrorChecks();
    void FlushPipeline();
}
