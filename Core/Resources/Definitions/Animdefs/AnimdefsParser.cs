using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Animdefs;

public class AnimdefsParser
{
    public readonly IList<AnimatedTexture> AnimatedTextures = new List<AnimatedTexture>();
    public readonly IList<AnimatedSwitch> AnimatedSwitches = new List<AnimatedSwitch>();
    public readonly IList<AnimatedWarpTexture> WarpTextures = new List<AnimatedWarpTexture>();
    public readonly IList<AnimatedCameraTexture> CameraTextures = new List<AnimatedCameraTexture>();
    public readonly IList<AnimatedRange> AnimatedRanges = new List<AnimatedRange>();

    public void Parse(string text)
    {
        SimpleParser parser = new();
        parser.Parse(text);

        while (!parser.IsDone())
            ConsumeDefinition(parser);
    }

    private void ConsumeAnimatedDoor(SimpleParser parser)
    {
        throw parser.MakeException("TODO: Animated doors are not supported in animdefs currently");
    }

    private void ConsumeCameraTexture(SimpleParser parser)
    {
        string name = parser.ConsumeString();
        int width = parser.ConsumeInteger();
        int height = parser.ConsumeInteger();
        int? fitWidth = null;
        int? fitHeight = null;
        bool worldPanning = false;

        if (parser.ConsumeIf("FIT"))
        {
            fitWidth = parser.ConsumeInteger();
            fitHeight = parser.ConsumeInteger();
            worldPanning = parser.ConsumeIf("WORLDPANNING");
        }

        CameraTextures.Add(new AnimatedCameraTexture(name, width, height, fitWidth, fitHeight, worldPanning));
    }

    private void ConsumeWarp(SimpleParser parser, bool waterEffect)
    {
        string warpNamespace = parser.ConsumeString();

        ResourceNamespace resourceNamespace;
        if (warpNamespace.Equals("TEXTURE", StringComparison.OrdinalIgnoreCase))
            resourceNamespace = ResourceNamespace.Textures;
        else if (warpNamespace.Equals("FLAT", StringComparison.OrdinalIgnoreCase))
            resourceNamespace = ResourceNamespace.Flats;
        else
            throw parser.MakeException($"Warp animated texture needs to be 'TEXTURE' or 'FLAT', got '{warpNamespace}' instead");

        string name = parser.ConsumeString();
        int? speed = parser.ConsumeIfInt();
        bool allowDecals = parser.ConsumeIf("ALLOWDECALS");

        WarpTextures.Add(new AnimatedWarpTexture(name, resourceNamespace, speed, allowDecals, waterEffect));
    }

    private static void ConsumePic(SimpleParser parser, AnimatedTexture texture)
    {
        if (parser.PeekInteger(out _))
        {
            ParseAnimatedIndex(parser, texture);
            return;
        }

        string name = parser.ConsumeString();

        // Apparently it is possible for these to be floating point values
        // instead of integers. I don't know if anyone does this though...
        int minTicks;
        int maxTicks;
        if (parser.ConsumeIf("tics"))
        {
            minTicks = parser.ConsumeInteger();
            maxTicks = minTicks;
        }
        else
        {
            parser.ConsumeString("rand");
            minTicks = parser.ConsumeInteger();
            maxTicks = parser.ConsumeInteger();
        }

        if (minTicks <= 0)
            throw parser.MakeException($"Texture '{name}' has a zero or negative tick duration, which is not allowed");
        if (minTicks > maxTicks)
            throw parser.MakeException($"Texture '{name}' has badly ordered min/max range (min is greater than max)");

        texture.Components.Add(new AnimatedTextureComponent(name, minTicks, maxTicks));
    }

    private static void ParseAnimatedIndex(SimpleParser parser, AnimatedTexture texture)
    {
        int index = parser.ConsumeInteger();
        parser.ConsumeString("tics");
        int tics = parser.ConsumeInteger();
        texture.Components.Add(new AnimatedTextureComponent(texture.Name, tics, tics, index));
    }

    private void ConsumeGraphicAnimation(SimpleParser parser, ResourceNamespace resourceNamespace)
    {
        bool optional = parser.ConsumeIf("OPTIONAL");
        string name = parser.ConsumeString();
        bool addTexture = true;
        AnimatedTexture texture = new(name, optional, resourceNamespace);

        while (true)
        {
            if (parser.ConsumeIf("ALLOWDECALS"))
                texture.AllowDecals = true;
            else if (parser.ConsumeIf("OSCILLATE"))
                texture.Oscillate = true;
            else if (parser.ConsumeIf("PIC"))
                ConsumePic(parser, texture);
            else if (parser.ConsumeIf("RANDOM"))
                texture.Random = true;
            else if (parser.ConsumeIf("RANGE"))
            {
                ConsumeRangeDefinition(parser, texture);
                addTexture = false;
            }
            else
                break;
        }

        if (!addTexture)
            return;

        if (texture.Components.Empty())
            throw parser.MakeException($"Animated definition for '{name}' has no animation components");

        AnimatedTextures.Add(texture);
    }

    private void ConsumeRangeDefinition(SimpleParser parser, AnimatedTexture texture)
    {
        string endTexture = parser.ConsumeString();
        int minTics = -1;
        int maxTics = -1;

        if (parser.ConsumeIf("tics"))
        {
            minTics = maxTics = parser.ConsumeInteger();
        }
        else if (parser.ConsumeIf("rand"))
        {
            minTics = parser.ConsumeInteger();
            maxTics = parser.ConsumeInteger();
        }

        bool oscillate = parser.ConsumeIf("Oscillate");

        AnimatedRange range = new()
        {
            StartTexture = texture.Name,
            EndTexture = endTexture,
            MinTics = minTics,
            MaxTics = maxTics,
            Namespace = texture.Namespace,
            Optional = texture.Optional,
            Oscillate = oscillate
        };

        AnimatedRanges.Add(range);
    }

    private static void ConsumeSwitchPic(SimpleParser parser, AnimatedSwitch animatedSwitch, bool on)
    {
        string name = parser.ConsumeString();

        // I don't know if this is like the texture/flat combo whereby any
        // floating point numbers are allowed or not.
        int minTicks;
        int maxTicks;
        if (parser.ConsumeIf("tics"))
        {
            minTicks = parser.ConsumeInteger();
            maxTicks = minTicks;
        }
        else
        {
            parser.ConsumeString("rand");
            minTicks = parser.ConsumeInteger();
            maxTicks = parser.ConsumeInteger();
        }

        if (minTicks > maxTicks)
            throw parser.MakeException($"Switch '{animatedSwitch.Texture}' (pic '{name}') has badly ordered min/max range (min is greater than max)");

        AnimatedTextureComponent component = new(name, minTicks, maxTicks);
        if (on)
            animatedSwitch.On.Add(component);
        else
            animatedSwitch.Off.Add(component);
    }

    private static void ConsumeAllSwitchPicAndSounds(SimpleParser parser, AnimatedSwitch animatedSwitch, bool on)
    {
        while (true)
        {
            if (parser.IsDone())
                return;

            string item = parser.PeekString();
            if (item.Equals("PIC", StringComparison.OrdinalIgnoreCase))
            {
                parser.ConsumeString();
                ConsumeSwitchPic(parser, animatedSwitch, on);
            }
            else if (item.Equals("SOUND", StringComparison.OrdinalIgnoreCase))
            {
                parser.ConsumeString();
                animatedSwitch.Sound = parser.ConsumeString();
            }
            else
            {
                return;
            }
        }
    }

    private void ConsumeSwitchAnimation(SimpleParser parser)
    {
        // TODO: This needs to be looped, because we could have an ON
        // definition followed by an OFF.
        string upperSwitchName = parser.ConsumeString();
        if (CheckGame(parser, upperSwitchName, out IWadBaseType iwad))
            upperSwitchName = parser.ConsumeString();

        bool on = true;
        if (!parser.ConsumeIf("ON"))
        {
            parser.ConsumeString("OFF");
            on = false;
        }

        AnimatedSwitch animatedSwitch = new(upperSwitchName, iwad);
        ConsumeAllSwitchPicAndSounds(parser, animatedSwitch, on);

        if (animatedSwitch.On.Empty())
            throw parser.MakeException($"Found no animated definitions for switch {upperSwitchName}");

        AnimatedSwitches.Add(animatedSwitch);
    }

    private static bool CheckGame(SimpleParser parser, string name, out IWadBaseType type)
    {
        type = IWadBaseType.None;
        if (name.Equals("Doom", StringComparison.OrdinalIgnoreCase))
        {
            type = IWadBaseType.Doom2;
            int? number = parser.ConsumeIfInt();
            if (number.HasValue && number.Value == 1)
                type = IWadBaseType.Doom1;
            return true;
        }
        else if (name.Equals("Chex", StringComparison.OrdinalIgnoreCase) || name.Equals("Chex Quest", StringComparison.OrdinalIgnoreCase))
        {
            type = IWadBaseType.ChexQuest;
            parser.ConsumeIfInt();
            return true;
        }
        else if (name.Equals("Heretic", StringComparison.OrdinalIgnoreCase) || name.Equals("Hexen", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Strife", StringComparison.OrdinalIgnoreCase))
        {
            // we will probably never support these, just set to Doom2
            type = IWadBaseType.Doom2;
            return true;
        }
        else if (name.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private void ConsumeDefinition(SimpleParser parser)
    {
        string text = parser.ConsumeString();
        if (text.Equals("ANIMATEDDOOR", StringComparison.OrdinalIgnoreCase))
            ConsumeAnimatedDoor(parser);
        else if (text.Equals("CAMERATEXTURE", StringComparison.OrdinalIgnoreCase))
            ConsumeCameraTexture(parser);
        else if (text.Equals("FLAT", StringComparison.OrdinalIgnoreCase))
            ConsumeGraphicAnimation(parser, ResourceNamespace.Flats);
        else if (text.Equals("SWITCH", StringComparison.OrdinalIgnoreCase))
            ConsumeSwitchAnimation(parser);
        else if (text.Equals("TEXTURE", StringComparison.OrdinalIgnoreCase))
            ConsumeGraphicAnimation(parser, ResourceNamespace.Textures);
        else if (text.Equals("WARP", StringComparison.OrdinalIgnoreCase))
            ConsumeWarp(parser, false);
        else if (text.Equals("WARP2", StringComparison.OrdinalIgnoreCase))
            ConsumeWarp(parser, true);
        else
            throw parser.MakeException($"Unknown animdefs type {text}");
    }
}
