using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using NLog;
using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Texture
{
    public class TexturesDefinition
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public List<TextureDefinition> Textures = new();
        private TextureOptions m_options = new();
        private TextureComponentOptions m_componentOptions = new();

        private static readonly Dictionary<string, ResourceNamespace> NamespaceLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "texture", ResourceNamespace.Textures },
            { "sprite", ResourceNamespace.Sprites },
            { "walltexture", ResourceNamespace.Textures },
            { "flat", ResourceNamespace.Flats },
        };

        public void Parse(string text)
        {
            SimpleParser parser = new();
            parser.Parse(text);

            while (!parser.IsDone())
            {
                string item = parser.ConsumeString();
                if (NamespaceLookup.TryGetValue(item, out ResourceNamespace ns))
                    ParseTexture(parser, ns);
                else
                    throw new ParserException(parser.GetCurrentLine(), parser.GetCurrentCharOffset(), 0, $"Invalid texture item: {item}");
            }
        }

        private void ParseTexture(SimpleParser parser, ResourceNamespace ns)
        {
            string name = parser.ConsumeString();
            Dimension dimension = new(GetInt(parser), GetInt(parser));

            List<TextureDefinitionComponent> components = new();
            bool useOptions = false;
            ConsumeBrace(parser, true);

            while (!parser.Peek('}'))
            {
                string item = parser.ConsumeString();
                if (item.EqualsIgnoreCase("Patch"))
                {
                    components.Add(ParsePatch(parser));
                }
                else if (item.EqualsIgnoreCase("XScale"))
                {
                    CheckSetUseTextureOptions(ref useOptions, ref m_options);
                    double value = parser.ConsumeDouble();
                    m_options.Scale = new(value, m_options.Scale.Y);
                }
                else if (item.EqualsIgnoreCase("YScale"))
                {
                    CheckSetUseTextureOptions(ref useOptions, ref m_options);
                    double value = parser.ConsumeDouble();
                    m_options.Scale = new(m_options.Scale.X, value);
                }
                else if (item.Equals("Offset"))
                {
                    ParseOffset(parser, out double x, out double y);
                    m_options.Offset = new(x, y);
                }
                else if (item.Equals("WorldPanning"))
                {
                    CheckSetUseTextureOptions(ref useOptions, ref m_options);
                    m_options.Flags |= TextureOptionFlags.WorldPanning;
                }
                else if (item.Equals("NoDecals"))
                {
                    CheckSetUseTextureOptions(ref useOptions, ref m_options);
                    m_options.Flags |= TextureOptionFlags.NoDecals;
                }
                else if (item.Equals("NullTexture"))
                {
                    CheckSetUseTextureOptions(ref useOptions, ref m_options);
                    m_options.Flags |= TextureOptionFlags.NullTexture;
                }
                else if (item.Equals("NoTrim"))
                {
                    CheckSetUseTextureOptions(ref useOptions, ref m_options);
                    m_options.Flags |= TextureOptionFlags.NoTrim;
                }
                else
                {
                    throw new ParserException(parser.GetCurrentLine(), parser.GetCurrentCharOffset(), 0, $"Invalid texture property: {item}");
                }
            }

            ConsumeBrace(parser, false);

            if (useOptions)
                Textures.Add(new(name, dimension, ns, components, m_options));
            else
                Textures.Add(new(name, dimension, ns, components));
        }

        private static void ParseOffset(SimpleParser parser, out double xOffset, out double yOffset)
        {
            xOffset = GetDouble(parser);
            yOffset = GetDouble(parser);
        }

        private TextureDefinitionComponent ParsePatch(SimpleParser parser)
        {
            string patchName = parser.ConsumeString();
            Vec2I patchOffset = new(GetInt(parser), GetInt(parser));
            bool useOptions = false;

            if (parser.Peek('{'))
            {
                ConsumeBrace(parser, true);
                ParsePatchProperties(parser, out useOptions);
            }

            if (useOptions)
                return new TextureDefinitionComponent(patchName, patchOffset, m_componentOptions);
            else
                return new TextureDefinitionComponent(patchName, patchOffset);
        }

        private void ParsePatchProperties(SimpleParser parser, out bool useOptions)
        {
            useOptions = false;
            while (!parser.Peek('}'))
            {
                string item = parser.ConsumeString();
                if (item.EqualsIgnoreCase("Rotate"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    m_componentOptions.Rotate = parser.ConsumeDouble();
                }
                else if (item.EqualsIgnoreCase("FlipX"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    m_componentOptions.Flags |= TextureComponentFlags.FlipX;
                }
                else if (item.EqualsIgnoreCase("FlipY"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    m_componentOptions.Flags |= TextureComponentFlags.FlipY;
                }
                else if (item.EqualsIgnoreCase("UseOffsets"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    m_componentOptions.Flags |= TextureComponentFlags.UseOffets;
                }
                else if (item.EqualsIgnoreCase("Translation"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    ParseTranslation(parser);
                }
                else if (item.EqualsIgnoreCase("Alpha"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    m_componentOptions.Alpha = parser.ConsumeDouble();
                }
                else if (item.EqualsIgnoreCase("Style"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    ParseStyle(parser);
                }
                else if (item.EqualsIgnoreCase("Blend"))
                {
                    CheckSetUseComponentOptions(ref useOptions, ref m_componentOptions);
                    ParseBlend(parser);
                }
                else
                {
                    throw new ParserException(parser.GetCurrentLine(), parser.GetCurrentCharOffset(), 0, $"Invalid patch property: {item}");
                }
            }

            ConsumeBrace(parser, false);
        }

        private void ParseTranslation(SimpleParser parser)
        {
            string item = parser.ConsumeString();
            if (item.EqualsIgnoreCase("Desaturate"))
            {
                m_componentOptions.Translation = TextureComponentTranslation.Desaturate;
                m_componentOptions.TranslationAmount = GetDouble(parser);
            }
            else if (item.EqualsIgnoreCase("Blue"))
                m_componentOptions.Translation = TextureComponentTranslation.Blue;
            else if (item.EqualsIgnoreCase("Gold"))
                m_componentOptions.Translation = TextureComponentTranslation.Gold;
            else if (item.EqualsIgnoreCase("Green"))
                m_componentOptions.Translation = TextureComponentTranslation.Green;
            else if (item.EqualsIgnoreCase("Ice"))
                m_componentOptions.Translation = TextureComponentTranslation.Ice;
            else if (item.EqualsIgnoreCase("Inverse"))
                m_componentOptions.Translation = TextureComponentTranslation.Inverse;
            else if (item.EqualsIgnoreCase("Red"))
                m_componentOptions.Translation = TextureComponentTranslation.Red;
            else
                Log.Warn($"Invalid texture translation: {item}");
        }

        private void ParseStyle(SimpleParser parser)
        {
            string item = parser.ConsumeString();
            if (item.EqualsIgnoreCase("Add"))
                m_componentOptions.Style = TextureComponentStyle.Add;
            else if (item.EqualsIgnoreCase("Copy"))
                m_componentOptions.Style = TextureComponentStyle.Copy;
            else if (item.EqualsIgnoreCase("CopyAlpha"))
                m_componentOptions.Style = TextureComponentStyle.CopyAlpha;
            else if (item.EqualsIgnoreCase("CopyNewAlpha"))
                m_componentOptions.Style = TextureComponentStyle.CopyNewAlpha;
            else if (item.EqualsIgnoreCase("Modulate"))
                m_componentOptions.Style = TextureComponentStyle.Modulate;
            else if (item.EqualsIgnoreCase("Overlay"))
                m_componentOptions.Style = TextureComponentStyle.Overlay;
            else if (item.EqualsIgnoreCase("ReverseSubtract"))
                m_componentOptions.Style = TextureComponentStyle.ReverseSubtract;
            else if (item.EqualsIgnoreCase("Subtract"))
                m_componentOptions.Style = TextureComponentStyle.Subtract;
            else if (item.EqualsIgnoreCase("Translucent"))
                m_componentOptions.Style = TextureComponentStyle.Translucent;
            else
                Log.Warn($"Invalid texture style: {item}");
        }

        private void ParseBlend(SimpleParser parser)
        {
            int line = parser.GetCurrentLine();
            if (parser.PeekInteger(out int _))
                m_componentOptions.BlendColor = Color.FromInts(255, GetInt(parser), GetInt(parser), GetInt(parser));
            else
                m_componentOptions.BlendColor = Color.FromName(parser.ConsumeString());

            if (parser.GetCurrentLine() == line)
                m_componentOptions.BlendAlpha = GetDouble(parser);
        }

        private static int GetInt(SimpleParser parser)
        {
            if (parser.Peek(','))
                parser.Consume(',');

            return parser.ConsumeInteger();
        }

        private static double GetDouble(SimpleParser parser)
        {
            if (parser.Peek(','))
                parser.Consume(',');

            return parser.ConsumeDouble();
        }

        private static void ConsumeBrace(SimpleParser parser, bool start)
        {
            parser.ConsumeString(start ? "{" : "}");
        }

        private static void CheckSetUseTextureOptions(ref bool useOptions, ref TextureOptions options)
        {
            if (useOptions)
                return;

            useOptions = true;
            options = new TextureOptions();
        }

        private static void CheckSetUseComponentOptions(ref bool useOptions, ref TextureComponentOptions options)
        {
            if (useOptions)
                return;

            useOptions = true;
            options = new TextureComponentOptions();
        }
    }
}
