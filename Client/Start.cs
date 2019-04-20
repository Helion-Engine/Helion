using Helion.Project.Impl.Local;
using Helion.Util;
using NLog;

namespace Helion.Client
{
    public class Start
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        
        private static void PrintHeader()
        {
            string bars = "======================================================================";
            log.Info(bars);
            log.Info($"| {Constants.APPLICATION_NAME} v{Constants.APPLICATION_VERSION}".PadRight(bars.Length - 1) + "|");
            log.Info(bars);
        }

        public static void Main(string[] args)
        {
            CommandLineArgs commandLineArgs = CommandLineArgs.Parse(args);
            Logging.Initialize(commandLineArgs);
            PrintHeader();

            if (commandLineArgs.Files.Count > 0)
            {
                LocalProject project = new LocalProject();
                project.Load(commandLineArgs.Files);
                log.Info("Finished loading files");
            }
            else
            {
                log.Warn("No files to load, exiting.");
                log.Warn("If you want to load files, use the -f (or --file) command line option.");
                log.Warn("   Ex: \"-f C:\\MyPath\\file.pk3 C:\\MyPath\\SOMETHING.WAD\"");
            }

            LogManager.Shutdown();
        }
    }
}
