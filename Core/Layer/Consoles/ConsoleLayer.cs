using System;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Timing;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.Consoles;

public partial class ConsoleLayer : IGameLayer, IAnimationLayer
{
    public InterpolationAnimation<IAnimationLayer> Animation { get; }

    private readonly IConfig m_config;
    private readonly HelionConsole m_console;
    private readonly ConsoleCommands m_consoleCommands;
    private readonly string m_backingImage;
    private int m_messageRenderOffset;
    private bool m_disposed;

    public ConsoleLayer(string backingImage, IConfig config, HelionConsole console, ConsoleCommands consoleCommands)
    {
        m_config = config;
        m_console = console;
        m_consoleCommands = consoleCommands;
        m_backingImage = backingImage;
        Animation = new(TimeSpan.FromMilliseconds(200), this);

        console.ClearInputText();
    }

    public bool ShouldRemove()
    {
        return Animation.State == InterpolationAnimationState.OutComplete;
    }

    ~ConsoleLayer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void ClearInputText()
    {
        m_console.ClearInputText();
    }

    public void RunLogic(TickerInfo tickerInfo)
    {
        // Not used.
    }

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        // TODO

        m_console.ClearInputText();
        m_console.LastClosedNanos = Ticker.NanoTime();

        m_disposed = true;
    }
}
