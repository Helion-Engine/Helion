using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Animdefs
{
    public class AnimdefsParser : ParserBase
    {
        protected override void PerformParsing()
        {
            while (!Done)
                ConsumeDefinition();
        }
        
        private void ConsumeAnimatedDoor()
        {
            // TODO
        }

        private void ConsumeCameraTexture()
        {
            // TODO
        }

        private void ConsumeWarp()
        {
            // TODO
        }

        private void CreateComponentsFromRange(string name, int min, int max, bool oscillate)
        {
            // TODO
        }

        private void ConsumePicOrRangeDefinition(AnimatedTexture texture, bool isRange)
        {
            if (PeekInteger())
                throw MakeException("Animdefs texture/flat pic index type not supported currently");

            string name = ConsumeString();

            // Apparently it is possible for these to be floating point values
            // instead of integers. I don't know if anyone does this though...
            int min;
            int max;
            if (ConsumeIf("tics"))
            {
                min = ConsumeInteger();
                max = min;
            }
            else
            {
                Consume("rand");
                min = ConsumeInteger();
                max = ConsumeInteger();
            }

            if (min <= 0)
                throw MakeException($"Texture '{name}' has a zero or negative tick duration, which is not allowed");
            if (min > max)
                throw MakeException($"Texture '{name}' has badly ordered min/max range (min is greater than max)");

            if (isRange)
            {
                bool oscillate = ConsumeIf("oscillate");
                CreateComponentsFromRange(name, min, max, oscillate);
            }
            else
                texture.Components.Add(new AnimatedTextureComponent(name, min, max));
        }

        private void ConsumeGraphicAnimation(ResourceNamespace resourceNamespace)
        {
            bool optional = ConsumeIf("OPTIONAL");
            string name = ConsumeString();

            AnimatedTexture texture = new AnimatedTexture(name, optional);

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
        }

        private void ConsumeSwitchAnimation()
        {
            // TODO
        }

        private void ConsumeDefinition()
        {
            if (ConsumeIf("ANIMATEDDOOR"))
                ConsumeAnimatedDoor();
            else if (ConsumeIf("CAMERATEXTURE"))
                ConsumeCameraTexture();
            else if (ConsumeIf("FLAT"))
                ConsumeGraphicAnimation(ResourceNamespace.Flats);
            else if (ConsumeIf("SWITCH"))
                ConsumeSwitchAnimation();
            else if (ConsumeIf("TEXTURE"))
                ConsumeGraphicAnimation(ResourceNamespace.Textures);
            else if (ConsumeIf("WARP"))
                ConsumeWarp();
        }
    }
}