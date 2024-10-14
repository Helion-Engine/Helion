using System;
using System.Collections.Generic;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Compatibility;

public class OptionsModel
{
    public Dictionary<string, string> Items { get; }

    public OptionsModel()
    {
        Items = new(StringComparer.OrdinalIgnoreCase);
    }

    public OptionsModel(string? input)
    {
        Items = new(StringComparer.OrdinalIgnoreCase);
        if (input != null)
        {
            SimpleParser parser = new();
            parser.Parse(input);
            while (!parser.IsDone())
            {
                int lineNumber = parser.GetCurrentLine();
                try
                {
                    if (!parser.Peek("#"))
                    {
                        string key = parser.ConsumeString();
                        string value = parser.ConsumeString();
                        Items[key] = value;
                    }
                }
                catch { }
                if (parser.GetCurrentLine() == lineNumber)
                    parser.ConsumeLine();
            }
        }
    }

    public void Set(string option, string value)
    {
        Items[option] = value;
    }

    public bool OptionEnabled(string option, CompLevel level) => (
        Items.TryGetValue(option, out string? val) == true &&
        OptionValidForCompLevel(option, level) &&
        val == "1"
    );

    // https://doomwiki.org/wiki/OPTIONS - most are not supported
    private static bool OptionValidForCompLevel(string option, CompLevel level)
    {
        bool atLeastBoom = level >= CompLevel.Boom;
        return option switch
        {
            OptionsConstants.Comp.Pain when atLeastBoom => true,
            OptionsConstants.Comp.Stairs when atLeastBoom => true,
            OptionsConstants.Comp.Vile when atLeastBoom => true,
            _ => false
        };
    }
}

public static class OptionsConstants
{
    public static class Comp
    {
        public const string Pain = "comp_pain";
        public const string Stairs = "comp_stairs";
        public const string Vile = "comp_vile";
    }
}
