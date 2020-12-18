using System.Collections.Generic;
using System.Linq;
using Helion.Resource.Definitions.Animations.Switches;
using Helion.Resource.Definitions.Animations.Textures;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using MoreLinq.Extensions;

namespace Helion.Resource.Definitions.Animations
{
    public class AnimdefsParser : ParserBase
    {
        public readonly IList<AnimatedTexture> AnimatedTextures = new List<AnimatedTexture>();
        public readonly IList<AnimatedSwitch> AnimatedSwitches = new List<AnimatedSwitch>();
        public readonly IList<AnimatedWarpTexture> WarpTextures = new List<AnimatedWarpTexture>();
        public readonly IList<AnimatedCameraTexture> CameraTextures = new List<AnimatedCameraTexture>();

        protected override void PerformParsing()
        {
            while (!Done)
                ConsumeDefinition();
        }

        private void ConsumeAnimatedDoor()
        {
            throw MakeException("TODO: Animated doors are not supported in animdefs currently");
        }

        private void ConsumeCameraTexture()
        {
            string name = ConsumeString().ToUpper();
            int width = ConsumeInteger();
            int height = ConsumeInteger();
            int? fitWidth = null;
            int? fitHeight = null;
            bool worldPanning = false;

            if (ConsumeIf("FIT"))
            {
                fitWidth = ConsumeInteger();
                fitHeight = ConsumeInteger();
                worldPanning = ConsumeIf("WORLDPANNING");
            }

            AnimatedCameraTexture cameraTexture = new(name, width, height, fitWidth, fitHeight, worldPanning);
            CameraTextures.Add(cameraTexture);
        }

        private void ConsumeWarp(bool waterEffect)
        {
            string warpNamespace = ConsumeString().ToUpper();

            Namespace resourceNamespace = warpNamespace.ToUpper() switch
            {
                "TEXTURE" => Namespace.Textures,
                "FLAT" => Namespace.Flats,
                _ => throw MakeException(
                    $"Warp animated texture needs to be 'TEXTURE' or 'FLAT', got '{warpNamespace}' instead")
            };

            string upperName = ConsumeString().ToUpper();
            int? speed = ConsumeIfInt();
            bool allowDecals = ConsumeIf("ALLOWDECALS");

            WarpTextures.Add(new AnimatedWarpTexture(upperName, resourceNamespace, speed, allowDecals, waterEffect));
        }

        private (string baseText, int endingNumberIndex) FindTextureRangeFrom(CIString textureName)
        {
            string upperName = textureName.ToString().ToUpper();
            int rightmostNumberChar = upperName.Length - 1;

            for (int i = upperName.Length - 1; i >= 0; i--)
            {
                if (char.IsNumber(upperName[i]))
                    rightmostNumberChar = i;
                else
                    break;
            }

            string baseStr = upperName.Substring(0, rightmostNumberChar);
            string numStr = upperName.Substring(rightmostNumberChar);

            if (!int.TryParse(numStr, out int value))
                throw MakeException($"Could not find ending numbers for texture {upperName} to make animation range from");

            return (baseStr, value);
        }

        private void GenerateComponentsFrom(string textureBase, int startIndex, int endIndex, int padding,
            int minTicks, int maxTicks, bool oscillate, AnimatedTexture texture)
        {
            List<AnimatedTextureComponent> components = new();

            for (int i = startIndex; i <= endIndex; i++)
            {
                string textureName = textureBase + i.ToString().PadLeft(padding, '0').ToUpper();
                AnimatedTextureComponent component = new(textureName, minTicks, maxTicks);
                components.Add(component);
            }

            // If we have [A, B, C, D], we want [A, B, C, D, C, B] if there is
            // oscillation. Otherwise we can just add it directly.
            if (oscillate)
                components.AsEnumerable().Reverse().Skip(1).Take(components.Count - 2).ForEach(texture.Components.Add);
            else
                components.ForEach(texture.Components.Add);
        }

        private void CreateComponentsFromRange(AnimatedTexture texture, string endName, int minTicks, int maxTicks,
            bool oscillate)
        {
            if (texture.Name.Length != endName.Length)
                throw MakeException($"Cannot create animation range for {texture.Name} to {endName} due to mismatched text lengths");

            (string textureBaseText, int startIndex) = FindTextureRangeFrom(texture.Name);
            (string endBaseText, int endIndex) = FindTextureRangeFrom(endName);

            if (textureBaseText != endBaseText)
                throw MakeException($"Range animdefs texture mismatch: {textureBaseText} (from {texture.Name}) and {endBaseText} (from {endName}) should match");

            int padding = texture.Name.Length - textureBaseText.Length;
            GenerateComponentsFrom(textureBaseText, startIndex, endIndex, padding, minTicks, maxTicks, oscillate, texture);
        }

        private void ConsumePicOrRangeDefinition(AnimatedTexture texture, bool isRange)
        {
            if (PeekInteger())
                throw MakeException("Animdefs texture/flat pic index type not supported currently");

            string name = ConsumeString().ToUpper();

            // Apparently it is possible for these to be floating point values
            // instead of integers. I don't know if anyone does this though...
            int minTicks;
            int maxTicks;
            if (ConsumeIf("tics"))
            {
                minTicks = ConsumeInteger();
                maxTicks = minTicks;
            }
            else
            {
                Consume("rand");
                minTicks = ConsumeInteger();
                maxTicks = ConsumeInteger();
            }

            if (minTicks <= 0)
                throw MakeException($"Texture '{name}' has a zero or negative tick duration, which is not allowed");
            if (minTicks > maxTicks)
                throw MakeException($"Texture '{name}' has badly ordered min/max range (min is greater than max)");

            if (isRange)
            {
                bool oscillate = ConsumeIf("oscillate");
                CreateComponentsFromRange(texture, name, minTicks, maxTicks, oscillate);
            }
            else
            {
                AnimatedTextureComponent component = new(name, minTicks, maxTicks);
                texture.Components.Add(component);
            }
        }

        private void ConsumeGraphicAnimation(Namespace resourceNamespace)
        {
            bool optional = ConsumeIf("OPTIONAL");
            string name = ConsumeString().ToUpper();

            AnimatedTexture texture = new(name, optional, resourceNamespace);

            while (true)
            {
                if (ConsumeIf("ALLOWDECALS"))
                    texture.AllowDecals = true;
                else if (ConsumeIf("OSCILLATE"))
                    texture.Oscillate = true;
                else if (ConsumeIf("PIC"))
                    ConsumePicOrRangeDefinition(texture, false);
                else if (ConsumeIf("RANDOM"))
                    texture.Random = true;
                else if (ConsumeIf("RANGE"))
                    ConsumePicOrRangeDefinition(texture, true);
                else
                    break;
            }

            if (texture.Components.Empty())
                throw MakeException($"Animated definition for '{name}' has no animation components");

            AnimatedTextures.Add(texture);
        }

        private void ConsumeSwitchPic(AnimatedSwitch animatedSwitch)
        {
            string name = ConsumeString().ToUpper();

            // I don't know if this is like the texture/flat combo whereby any
            // floating point numbers are allowed or not.
            int minTicks;
            int maxTicks;
            if (ConsumeIf("tics"))
            {
                minTicks = ConsumeInteger();
                maxTicks = minTicks;
            }
            else
            {
                Consume("rand");
                minTicks = ConsumeInteger();
                maxTicks = ConsumeInteger();
            }

            if (minTicks > maxTicks)
                throw MakeException($"Switch '{animatedSwitch.StartTexture}' (pic '{name}') has badly ordered min/max range (min is greater than max)");

            AnimatedTextureComponent component = new(name.ToUpper(), minTicks, maxTicks);
            animatedSwitch.Components.Add(component);
        }

        private void ConsumeAllSwitchPicAndSounds(AnimatedSwitch animatedSwitch)
        {
            while (true)
            {
                string? value = PeekCurrentText();
                if (value == null)
                    throw MakeException($"Ran out of tokens when parsing switch definition for {animatedSwitch.StartTexture}");

                switch (value.ToUpper())
                {
                case "PIC":
                    Consume();
                    ConsumeSwitchPic(animatedSwitch);
                    break;
                case "SOUND":
                    Consume();
                    animatedSwitch.Sound = ConsumeString().ToUpper();
                    break;
                default:
                    return;
                }
            }
        }

        private void ConsumeSwitchAnimation()
        {
            string upperSwitchName = ConsumeString().ToUpper();
            SwitchType switchType = SwitchType.On;

            if (!ConsumeIf("ON"))
            {
                Consume("OFF");
                switchType = SwitchType.Off;
            }

            AnimatedSwitch animatedSwitch = new(upperSwitchName, switchType);
            ConsumeAllSwitchPicAndSounds(animatedSwitch);

            if (animatedSwitch.Components.Empty())
                throw MakeException($"Found no animated definitions for switch {upperSwitchName}");

            AnimatedSwitches.Add(animatedSwitch);
        }

        private void ConsumeDefinition()
        {
            string text = ConsumeString();
            switch (text.ToUpper())
            {
            case "ANIMATEDDOOR":
                ConsumeAnimatedDoor();
                break;
            case "CAMERATEXTURE":
                ConsumeCameraTexture();
                break;
            case "FLAT":
                ConsumeGraphicAnimation(Namespace.Flats);
                break;
            case "SWITCH":
                ConsumeSwitchAnimation();
                break;
            case "TEXTURE":
                ConsumeGraphicAnimation(Namespace.Textures);
                break;
            case "WARP":
                ConsumeWarp(false);
                break;
            case "WARP2":
                ConsumeWarp(true);
                break;
            default:
                throw MakeException($"Unknown animdefs type {text}");
            }
        }
    }
}