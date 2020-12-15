using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Util.Parser;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;

namespace Helion.Resources.Definitions.SoundInfo
{
    public class SoundInfoDefinition
    {
        private readonly Dictionary<string, SoundInfo> m_lookup = new Dictionary<string, SoundInfo>();
        private readonly Dictionary<string, List<string>> m_randomLookup = new Dictionary<string, List<string>>();
        private readonly SimpleParser m_parser = new SimpleParser();

        private int m_pitchShiftRange = 0;

        public SoundInfoDefinition()
        {
        }

        public static string GetPlayerSound(Player player, string sound)
        {
            if (sound.Length > 0 && sound[0] == '*')
                return $"player/{player.GetGenderString()}/{sound}";

            return sound;
        }

        public void Parse(Entry entry)
        {
            m_parser.Parse(System.Text.Encoding.UTF8.GetString(entry.ReadData()));          
            PerformParsing();
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

            if (m_lookup.TryGetValue(name, out SoundInfo? sndInfo))
                return sndInfo;
            return null;
        }

        protected void PerformParsing()
        {
            while (!m_parser.IsDone())
            {
                if (m_parser.Peek('$'))
                    ParseCommand();
                else
                    ParseSound();
            }
        }

        private void ParseSound()
        {
            AddSound(m_parser.ConsumeString(), m_parser.ConsumeString());
        }

        private void ParseCommand()
        {
            string type = m_parser.ConsumeString();

            if (type == "$playercompat")
                ParsePlayerCompat();
            else if (type == "$playersound")
                ParsePlayerSound();
            else if (type == "$playersounddup")
                ParsePlayerSoundDup();
            else if (type == "$pitchshift")
                ParsePitchShift();
            else if (type == "$pitchshiftrange")
                m_pitchShiftRange = m_parser.ConsumeInteger();
            else if (type == "$alias")
                ParseAlias();
            else if (type == "$limit")
                ParseLimit();
            else if (type == "$random")
                ParseRandom();
            else
                throw new ParserException(m_parser.GetCurrentLine(), 0, 0, "Bad command.");
        }

        private void ParsePitchShift()
        {
            string key = m_parser.ConsumeString();
            int pitch = m_parser.ConsumeInteger();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                soundInfo.PitchShift = pitch;
        }

        private void ParseLimit()
        {
            string key = m_parser.ConsumeString();
            int limit = m_parser.ConsumeInteger();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                soundInfo.Limit = limit;
        }

        private void ParseAlias()
        {
            string alias = m_parser.ConsumeString();
            string key = m_parser.ConsumeString();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                m_lookup[alias] = soundInfo;
        }

        private void ParsePlayerCompat()
        {
            m_parser.ConsumeString();
            m_parser.ConsumeString();
            m_parser.ConsumeString();
            m_parser.ConsumeString();
        }

        private void ParsePlayerSoundDup()
        {
            string player = m_parser.ConsumeString();
            string gender = m_parser.ConsumeString();
            string name = m_parser.ConsumeString();
            string entryName = m_parser.ConsumeString();
            string key = $"{player}/{gender}/{entryName}";

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            {
                key = $"{player}/{gender}/{name}";
                AddSound(key, soundInfo.EntryName, true);
            }
        }

        private void ParsePlayerSound()
        {
            string key = $"{m_parser.ConsumeString()}/{m_parser.ConsumeString()}/{m_parser.ConsumeString()}";
            AddSound(key, m_parser.ConsumeString(), true);
        }

        private void ParseRandom()
        {
            List<string> sounds = new List<string>();
            string key = m_parser.ConsumeString();
            m_parser.Consume('{');

            while (!m_parser.Peek('}'))
                sounds.Add(m_parser.ConsumeString());
            m_parser.Consume('}');

            m_randomLookup[key] = sounds;
        }

        private void AddSound(string key, string entryName, bool playerEntry = false)
        {
            m_lookup[key] = new SoundInfo(key, entryName, m_pitchShiftRange, playerEntry);
        }
    }
}
