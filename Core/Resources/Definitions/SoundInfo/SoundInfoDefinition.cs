using System;
using System.Collections.Generic;
using Helion.Util.Parser;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;

namespace Helion.Resources.Definitions.SoundInfo
{
    public class SoundInfoDefinition
    {
        private readonly Dictionary<string, SoundInfo> m_lookup = new Dictionary<string, SoundInfo>();
        private readonly Dictionary<string, List<string>> m_randomLookup = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, string> m_playerCompatLookup = new Dictionary<string, string>();

        private int m_pitchShiftRange = 0;

        public static string GetPlayerSound(Player player, string sound)
        {
            if (sound.Length > 0 && sound[0] == '*')
                return $"player/{player.GetGenderString()}/{sound}";

            return sound;
        }

        public SoundInfo? Lookup(string name, IRandom random)
        {
            if (m_randomLookup.TryGetValue(name, out List<string>? sounds))
            {
                if (sounds.Count > 0)
                    name = sounds[random.NextByte() % sounds.Count];
                else
                    return null;
            }

            if (name.StartsWith("player/", System.StringComparison.OrdinalIgnoreCase) && 
                m_playerCompatLookup.TryGetValue(name, out string? playerCompat) && playerCompat != null)
                name = playerCompat;

            if (m_lookup.TryGetValue(name, out SoundInfo? sndInfo))
                return sndInfo;
            return null;
        }

        public void Parse(string data)
        {
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            while (!parser.IsDone())
            {
                if (parser.Peek('$'))
                    ParseCommand(parser);
                else
                    ParseSound(parser);
            }
        }

        private void ParseSound(SimpleParser parser)
        {
            AddSound(parser.ConsumeString(), parser.ConsumeString());
        }

        private void ParseCommand(SimpleParser parser)
        {
            string type = parser.ConsumeString();

            if (type.Equals("$playercompat", StringComparison.OrdinalIgnoreCase))
                ParsePlayerCompat(parser);
            else if (type.Equals("$playersound", StringComparison.OrdinalIgnoreCase))
                ParsePlayerSound(parser);
            else if (type.Equals("$playersounddup", StringComparison.OrdinalIgnoreCase))
                ParsePlayerSoundDup(parser);
            else if (type.Equals("$pitchshift", StringComparison.OrdinalIgnoreCase))
                ParsePitchShift(parser);
            else if (type.Equals("$pitchshiftrange", StringComparison.OrdinalIgnoreCase))
                m_pitchShiftRange = parser.ConsumeInteger();
            else if (type.Equals("$alias", StringComparison.OrdinalIgnoreCase))
                ParseAlias(parser);
            else if (type.Equals("$limit", StringComparison.OrdinalIgnoreCase))
                ParseLimit(parser);
            else if (type.Equals("$random", StringComparison.OrdinalIgnoreCase))
                ParseRandom(parser);
            else
                throw new ParserException(parser.GetCurrentLine(), 0, 0, "Bad command.");
        }

        private void ParsePitchShift(SimpleParser parser)
        {
            string key = parser.ConsumeString();
            int pitch = parser.ConsumeInteger();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                soundInfo.PitchShift = pitch;
        }

        private void ParseLimit(SimpleParser parser)
        {
            string key = parser.ConsumeString();
            int limit = parser.ConsumeInteger();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                soundInfo.Limit = limit;
        }

        private void ParseAlias(SimpleParser parser)
        {
            string alias = parser.ConsumeString();
            string key = parser.ConsumeString();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                m_lookup[alias] = soundInfo;
        }

        private void ParsePlayerCompat(SimpleParser parser)
        {
            string player = parser.ConsumeString();
            string gender = parser.ConsumeString();
            string name = parser.ConsumeString();
            string compat = parser.ConsumeString();

            m_playerCompatLookup[compat] = $"{player}/{gender}/{name}";
        }

        private void ParsePlayerSoundDup(SimpleParser parser)
        {
            string player = parser.ConsumeString();
            string gender = parser.ConsumeString();
            string name = parser.ConsumeString();
            string entryName = parser.ConsumeString();
            string key = $"{player}/{gender}/{entryName}";

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            {
                key = $"{player}/{gender}/{name}";
                AddSound(key, soundInfo.EntryName, true);
            }
        }

        private void ParsePlayerSound(SimpleParser parser)
        {
            string key = $"{parser.ConsumeString()}/{parser.ConsumeString()}/{parser.ConsumeString()}";
            AddSound(key, parser.ConsumeString(), true);
        }

        private void ParseRandom(SimpleParser parser)
        {
            List<string> sounds = new List<string>();
            string key = parser.ConsumeString();
            parser.Consume('{');

            while (!parser.Peek('}'))
                sounds.Add(parser.ConsumeString());
            parser.Consume('}');

            m_randomLookup[key] = sounds;
        }

        private void AddSound(string key, string entryName, bool playerEntry = false)
        {
            if (playerEntry && m_playerCompatLookup.TryGetValue(key, out string? playerCompat))
                key = playerCompat;

            m_lookup[key] = new SoundInfo(key, entryName, m_pitchShiftRange, playerEntry);
        }
    }
}
