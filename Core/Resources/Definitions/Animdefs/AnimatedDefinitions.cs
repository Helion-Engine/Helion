using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs.Switches;
using Helion.Resources.Definitions.Animdefs.Textures;
using NLog;

namespace Helion.Resources.Definitions.Animdefs
{
    public class AnimatedDefinitions
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly List<AnimatedTexture> AnimatedTextures = new List<AnimatedTexture>();
        public readonly List<AnimatedSwitch> AnimatedSwitches = new List<AnimatedSwitch>();
        public readonly List<AnimatedWarpTexture> WarpTextures = new List<AnimatedWarpTexture>();
        public readonly List<AnimatedCameraTexture> CameraTextures = new List<AnimatedCameraTexture>();
            
        public void AddDefinitions(Entry entry)
        {
            AnimdefsParser parser = new AnimdefsParser();
            if (!parser.Parse(entry))
            {
                Log.Error("Unable to parse animdefs file, animations will be missing");
                return;
            }
            
            AnimatedTextures.AddRange(parser.AnimatedTextures);
            AnimatedSwitches.AddRange(parser.AnimatedSwitches);
            WarpTextures.AddRange(parser.WarpTextures);
            CameraTextures.AddRange(parser.CameraTextures);
        }
    }
}