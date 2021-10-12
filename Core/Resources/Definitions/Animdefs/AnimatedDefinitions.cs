using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs.Textures;
using NLog;

namespace Helion.Resources.Definitions.Animdefs;

public class AnimatedDefinitions
{
    public readonly List<AnimatedTexture> AnimatedTextures = new List<AnimatedTexture>();
    public readonly List<AnimatedSwitch> AnimatedSwitches = new List<AnimatedSwitch>();
    public readonly List<AnimatedWarpTexture> WarpTextures = new List<AnimatedWarpTexture>();
    public readonly List<AnimatedCameraTexture> CameraTextures = new List<AnimatedCameraTexture>();

    public void AddDefinitions(Entry entry)
    {
        AnimdefsParser parser = new AnimdefsParser();
        parser.Parse(entry);

        AnimatedTextures.AddRange(parser.AnimatedTextures);
        AnimatedSwitches.AddRange(parser.AnimatedSwitches);
        WarpTextures.AddRange(parser.WarpTextures);
        CameraTextures.AddRange(parser.CameraTextures);
    }
}
