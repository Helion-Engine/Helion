using System;
using System.Collections.Generic;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Compatibility;

public class Options
{
    public Dictionary<string, string> Items { get; }

    public Options()
    {
        Items = new(StringComparer.OrdinalIgnoreCase);
    }

    public Options(string? input)
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

    // https://doomwiki.org/wiki/OPTIONS
    // most of these aren't supported
    private static bool OptionValidForCompLevel(string option, CompLevel level)
    {
        // bool atLeastBugFixed = level >= ExecutableValues[GameConfConstants.Executable.BugFixed];
        bool atLeastBoom = level >= CompLevel.Boom;
        // bool atLeastComplevel9 = level >= ExecutableValues[GameConfConstants.Executable.Complevel9];
        // bool atLeastMbf = level >= CompLevel.Mbf;
        // bool atLeastMbf21 = level >= CompLevel.Mbf21;
        return option switch
        {
            // vanilla
            // "comp_soul" => true,
            // Boom
            // "comp_blazing" when atLeastBoom => true,
            // "comp_doorlight" when atLeastBoom => true,
            // "comp_doorstuck" when atLeastBoom => true,
            // "comp_floors" when atLeastBoom => true,
            // "comp_god" when atLeastBoom => true,
            // "comp_model" when atLeastBoom => true,
            OptionsConstants.Comp.Pain when atLeastBoom => true,
            // "comp_skull" when atLeastBoom => true,
            OptionsConstants.Comp.Stairs when atLeastBoom => true,
            OptionsConstants.Comp.Vile when atLeastBoom => true,
            // "comp_zerotags" when atLeastBoom => true,
            // PrBoom
            // "comp_zombie" when atLeastComplevel9 => true, // TODO: verify
            // MBF
            // "comp_dropoff" when atLeastMbf => true,
            // "comp_falloff" when atLeastMbf => true,
            // "comp_infcheat" when atLeastMbf => true,
            // "comp_pursuit" when atLeastMbf => true,
            // "comp_respawn" when atLeastMbf => true,
            // "comp_skymap" when atLeastMbf => true,
            // "comp_staylift" when atLeastMbf => true,
            // "comp_telefrag" when atLeastMbf => true,
            // "dog_jumping" when atLeastMbf => true,
            // "friend_distance" when atLeastMbf => true,
            // "help_friends" when atLeastMbf => true,
            // "monkeys" when atLeastMbf => true,
            // "monster_avoid_hazards" when atLeastMbf => true,
            // "monster_backing" when atLeastMbf => true,
            // "monster_friction" when atLeastMbf => true,
            // "monster_infighting" when atLeastMbf => true,
            // "monsters_remember" when atLeastMbf => true,
            // "player_helpers" when atLeastMbf => true,
            // "weapon_recoil" when atLeastMbf => true,
            // MBF21
            // "comp_friendlyspawn" when atLeastMbf21 => true,
            // "comp_ledgeblock" when atLeastMbf21 => true,
            // "comp_reservedlineflag" when atLeastMbf21 => true,
            // "comp_voodooscroller" when atLeastMbf21 => true,

            // forced off in MBF21
            // vanilla
            // "comp_666" when !atLeastMbf21 => true,
            // "comp_maskedanim" when !atLeastMbf21 => true,
            // LxDoom
            // "comp_moveblock" when atLeastBugFixed && !atLeastMbf21 => true, // TODO: verify
            // Boom
            // "comp_maxhealth" when atLeastBoom && !atLeastMbf21 => true,
            // "comp_sound" when atLeastBoom && !atLeastMbf21 => true,
            // PrBoom+
            // "comp_ouchface" when atLeastComplevel9 && !atLeastMbf21 => true, // TODO: verify

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