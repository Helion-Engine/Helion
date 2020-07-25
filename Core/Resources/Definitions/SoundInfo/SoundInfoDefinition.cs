using System;
using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;

namespace Helion.Resources.Definitions.SoundInfo
{
    public class SoundInfoDefinition
    {
        private readonly Dictionary<string, SoundInfo> m_lookup = new Dictionary<string, SoundInfo>();
        private readonly Dictionary<string, List<string>> m_randomLookup = new Dictionary<string, List<string>>();

        private int m_pitchShiftRange = 0;

        private readonly List<string> m_tokens = new List<string>();
        private bool m_multiLineComment;
        private int m_index = 0;

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
            string text = System.Text.Encoding.UTF8.GetString(entry.ReadData()).Replace("\r\n", "\n");
            string[] lines = text.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string parseLine = StripComments(line);
                m_tokens.AddRange(parseLine.Split(new char[] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
            }

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

        private bool Done => m_index >= m_tokens.Count;

        private bool Peek(char c)
        {
            if (m_tokens[m_index][0] == c)
                return true;

            return false;
        }

        protected void PerformParsing()
        {
            while (!Done)
            {
                if (Peek('$'))
                    ParseCommand();
                else
                    ParseSound();
            }
        }

        private string StripComments(string line)
        {
            int start = 0;
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (m_multiLineComment)
                {
                    if (line[i] == '*' && line[i + 1] == '/')
                    {
                        m_multiLineComment = false;
                        start = i + 1;
                    }
                }
                else
                {
                    if (line[i] == '/')
                    {
                        if (line[i + 1] == '/')
                        {
                            return line.Substring(start, i);
                        }
                        else if (line[i] == '*')
                        {
                            m_multiLineComment = true;
                            return line.Substring(start, i);
                        }
                    }
                }
            }

            return line;
        }

        private string ConsumeString()
        {
            return m_tokens[m_index++];
        }

        private int ConsumeInteger()
        {
            return Convert.ToInt32(m_tokens[m_index++]);
        }

        private void Consume(char c)
        {
            if (m_tokens[m_index].Length == 1 && m_tokens[m_index][0] == c)
                m_index++;
            else
                throw new Exception("UHOH");
        }

        private void ParseSound()
        {
            AddSound(ConsumeString(), ConsumeString());
        }

        private void ParseCommand()
        {
            string type = ConsumeString();

            if (type == "$playercompat")
                ParsePlayerCompat();
            else if (type == "$playersound")
                ParsePlayerSound();
            else if (type == "$playersounddup")
                ParsePlayerSoundDup();
            else if (type == "$pitchshift")
                ParsePitchShift();
            else if (type == "$pitchshiftrange")
                m_pitchShiftRange = ConsumeInteger();
            else if (type == "$alias")
                ParseAlias();
            else if (type == "$limit")
                ParseLimit();
            else if (type == "$random")
                ParseRandom();
            else
                throw new Exception("bad command");
        }

        private void ParsePitchShift()
        {
            string key = ConsumeString();
            int pitch = ConsumeInteger();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                soundInfo.PitchShift = pitch;
        }

        private void ParseLimit()
        {
            string key = ConsumeString();
            int limit = ConsumeInteger();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                soundInfo.Limit = limit;
        }

        private void ParseAlias()
        {
            string alias = ConsumeString();
            string key = ConsumeString();

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
                m_lookup[alias] = soundInfo;
        }

        private void ParsePlayerCompat()
        {
            ConsumeString();
            ConsumeString();
            ConsumeString();
            ConsumeString();
        }

        private void ParsePlayerSoundDup()
        {
            string player = ConsumeString();
            string gender = ConsumeString();
            string name = ConsumeString();
            string entryName = ConsumeString();
            string key = $"{player}/{gender}/{entryName}";

            if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            {
                key = $"{player}/{gender}/{name}";
                AddSound(key, soundInfo.EntryName, true);
            }
        }

        private void ParsePlayerSound()
        {
            string key = $"{ConsumeString()}/{ConsumeString()}/{ConsumeString()}";
            AddSound(key, ConsumeString(), true);
        }

        private void ParseRandom()
        {
            List<string> sounds = new List<string>();
            string key = ConsumeString();
            Consume('{');

            while (!Peek('}'))
                sounds.Add(ConsumeString());
            Consume('}');

            m_randomLookup[key] = sounds;
        }

        private void AddSound(string key, string entryName, bool playerEntry = false)
        {
            m_lookup[key] = new SoundInfo(key, entryName, m_pitchShiftRange, playerEntry);
        }
    }
}
