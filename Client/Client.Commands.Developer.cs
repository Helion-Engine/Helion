using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;

namespace Helion.Client;

public partial class Client
{
    [ConsoleCommand("dumpbuffers", "Writes OpenGL TBO buffer data")]
    private void DumpBuffers(ConsoleCommandEventArgs args)
    {
        m_window.Renderer.DumpBuffers();
    }
}
