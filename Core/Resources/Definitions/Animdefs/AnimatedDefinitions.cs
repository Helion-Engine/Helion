using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs.Textures;

namespace Helion.Resources.Definitions.Animdefs;

public class AnimatedDefinitions
{
    public readonly List<AnimatedTexture> AnimatedTextures = new();
    public readonly List<AnimatedSwitch> AnimatedSwitches = new();
    public readonly List<AnimatedWarpTexture> WarpTextures = new();
    public readonly List<AnimatedCameraTexture> CameraTextures = new();

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
