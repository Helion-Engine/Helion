using System.Collections.Generic;
using Helion.Resource.Archives;
using Helion.Resource.Definitions.Animations.Switches;
using Helion.Resource.Definitions.Animations.Textures;
using NLog;

namespace Helion.Resource.Definitions.Animations
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
            AnimdefsParser parser = new();
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