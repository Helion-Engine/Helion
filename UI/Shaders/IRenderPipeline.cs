using OpenTK.Mathematics;

namespace Helion.UI.Shaders;

public interface IRenderPipeline
{
    void Restart();
    void Render(Vector2i windowSize);
}