using Helion.Geometry;
using Helion.Render;
using Helion.Window;
using System;

namespace Helion.Tests.Unit.GameLayer;

public class MockWindow : IWindow
{
    private readonly IInputManager m_inputManager;

    public MockWindow(IInputManager inputManager)
    {
        m_inputManager = inputManager;
    }

    public IInputManager InputManager => m_inputManager;

    public Renderer Renderer => throw new NotImplementedException();

    public Dimension Dimension => new(0, 0);

    public Dimension FramebufferDimension => new(0, 0);

    public void Dispose()
    {

    }
}
