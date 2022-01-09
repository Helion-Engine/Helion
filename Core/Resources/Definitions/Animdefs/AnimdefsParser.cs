using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using MoreLinq.Extensions;

namespace Helion.Resources.Definitions.Animdefs;

public class AnimdefsParser
{
    public readonly IList<AnimatedTexture> AnimatedTextures = new List<AnimatedTexture>();
    public readonly IList<AnimatedSwitch> AnimatedSwitches = new List<AnimatedSwitch>();
    public readonly IList<AnimatedWarpTexture> WarpTextures = new List<AnimatedWarpTexture>();
    public readonly IList<AnimatedCameraTexture> CameraTextures = new List<AnimatedCameraTexture>();

    public void Parse(Entry entry)
    {
        SimpleParser parser = new SimpleParser();
        parser.Parse(entry.ReadDataAsString());

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

    private (string baseText, int endingNumberIndex) FindTextureRangeFrom(SimpleParser parser, string textureName)
    {
        int rightmostNumberChar = textureName.Length - 1;

        for (int i = textureName.Length - 1; i >= 0; i--)
        {
            if (char.IsNumber(textureName[i]))
                rightmostNumberChar = i;
            else
                break;
        }

        string baseStr = textureName.Substring(0, rightmostNumberChar);
        string numStr = textureName.Substring(rightmostNumberChar);

        if (!int.TryParse(numStr, out int value))
            throw parser.MakeException($"Could not find ending numbers for texture {textureName} to make animation range from");

        return (baseStr, value);
    }

    private void GenerateComponentsFrom(string textureBase, int startIndex, int endIndex, int padding,
        int minTicks, int maxTicks, bool oscillate, AnimatedTexture texture)
    {
        List<AnimatedTextureComponent> components = new List<AnimatedTextureComponent>();

        for (int i = startIndex; i <= endIndex; i++)
        {
            string textureName = textureBase + i.ToString().PadLeft(padding, '0');
            AnimatedTextureComponent component = new AnimatedTextureComponent(textureName, minTicks, maxTicks);
            components.Add(component);
        }

        // If we have [A, B, C, D], we want [A, B, C, D, C, B] if there is
        // oscillation. Otherwise we can just add it directly.
        if (oscillate)
            components.AsEnumerable().Reverse().Skip(1).Take(components.Count - 2).ForEach(texture.Components.Add);
        else
            components.ForEach(texture.Components.Add);
    }

    private void CreateComponentsFromRange(SimpleParser parser, AnimatedTexture texture, string endName, int minTicks, int maxTicks,
        bool oscillate)
    {
        if (texture.Name.Length != endName.Length)
            throw parser.MakeException($"Cannot create animation range for {texture.Name} to {endName} due to mismatched text lengths");

        (string textureBaseText, int startIndex) = FindTextureRangeFrom(parser, texture.Name);
        (string endBaseText, int endIndex) = FindTextureRangeFrom(parser, endName);

        if (textureBaseText != endBaseText)
            throw parser.MakeException($"Range animdefs texture mismatch: {textureBaseText} (from {texture.Name}) and {endBaseText} (from {endName}) should match");

        int padding = texture.Name.Length - textureBaseText.Length;
        GenerateComponentsFrom(textureBaseText, startIndex, endIndex, padding, minTicks, maxTicks, oscillate, texture);
    }

    private void ConsumePicOrRangeDefinition(SimpleParser parser, AnimatedTexture texture, bool isRange)
    {
        if (parser.PeekInteger(out _))
            throw parser.MakeException("Animdefs texture/flat pic index type not supported currently");

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

        if (isRange)
        {
            bool oscillate = parser.ConsumeIf("oscillate");
            CreateComponentsFromRange(parser, texture, name, minTicks, maxTicks, oscillate);
        }
        else
            texture.Components.Add(new AnimatedTextureComponent(name, minTicks, maxTicks));
    }

    private void ConsumeGraphicAnimation(SimpleParser parser, ResourceNamespace resourceNamespace)
    {
        bool optional = parser.ConsumeIf("OPTIONAL");
        string name = parser.ConsumeString();

        AnimatedTexture texture = new AnimatedTexture(name, optional, resourceNamespace);

        while (true)
        {
            if (parser.ConsumeIf("ALLOWDECALS"))
                texture.AllowDecals = true;
            else if (parser.ConsumeIf("OSCILLATE"))
                texture.Oscillate = true;
            else if (parser.ConsumeIf("PIC"))
                ConsumePicOrRangeDefinition(parser, texture, false);
            else if (parser.ConsumeIf("RANDOM"))
                texture.Random = true;
            else if (parser.ConsumeIf("RANGE"))
                ConsumePicOrRangeDefinition(parser, texture, true);
            else
                break;
        }

        if (texture.Components.Empty())
            throw parser.MakeException($"Animated definition for '{name}' has no animation components");

        AnimatedTextures.Add(texture);
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
