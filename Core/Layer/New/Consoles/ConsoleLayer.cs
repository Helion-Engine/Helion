using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using NLog;

namespace Helion.Layer.New.Consoles;

public partial class ConsoleLayer : GameLayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override double Priority => 0.1;
    private readonly IConfig m_config;
    private readonly HelionConsole m_console;
    private readonly ConsoleCommands m_consoleCommands;
    private readonly string m_backingImage;

    public ConsoleLayer(IConfig config, HelionConsole console, ConsoleCommands consoleCommands, string backingImage)
    {
        m_config = config;
        m_console = console;
        m_consoleCommands = consoleCommands;
        m_backingImage = backingImage;
    }
    
    public override bool? ShouldFocus()
    {
        return false;
    }
    
    public override void RunLogic()
    {
        // Nothing to run.
    }
}