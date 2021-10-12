using System;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Timing;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.Consoles;

public partial class ConsoleLayer : IGameLayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IConfig m_config;
    private readonly HelionConsole m_console;
    private readonly ConsoleCommands m_consoleCommands;
    private int m_messageRenderOffset;
    private bool m_disposed;

    public ConsoleLayer(IConfig config, HelionConsole console, ConsoleCommands consoleCommands)
    {
        m_config = config;
        m_console = console;
        m_consoleCommands = consoleCommands;

        console.ClearInputText();
    }

    ~ConsoleLayer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void RunLogic()
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
