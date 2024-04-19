using System;
using System.Collections.Generic;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.Util.RandomGenerators;

namespace Helion.Resources.Definitions.SoundInfo;

public class SoundInfoDefinition
{
    private readonly Dictionary<string, SoundInfo> m_lookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> m_randomLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> m_playerCompatLookup = new(StringComparer.OrdinalIgnoreCase);

    private int m_pitchShiftRange = 0;

    public static string GetPlayerSound(string gender, string sound)
    {
        if (sound.Length > 0 && sound[0] == '*')
            return $"player/{gender}/{sound}";

        return sound;
    }

    public void GetSounds(List<SoundInfo> list)
    {
        foreach (var item in m_lookup)
            list.Add(item.Value);
    }

    public void Add(string name, SoundInfo soundInfo) =>
        m_lookup[name] = soundInfo;

    public SoundInfo? Lookup(string name, IRandom random)
    {
        if (m_randomLookup.TryGetValue(name, out List<string>? sounds))
        {
            if (sounds.Count > 0)
                name = sounds[random.NextByte() % sounds.Count];
            else
                return null;
        }

        if (name.StartsWith("player/", StringComparison.OrdinalIgnoreCase) &&
            m_playerCompatLookup.TryGetValue(name, out string? playerCompat) && playerCompat != null)
            name = playerCompat;

        if (m_lookup.TryGetValue(name, out SoundInfo? sndInfo))
            return sndInfo;
        return null;
    }

    public bool GetSound(string name, out SoundInfo? soundInfo) => m_lookup.TryGetValue(name, out soundInfo);

    public void Parse(string data)
    {
        SimpleParser parser = new();
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

        if (type.EqualsIgnoreCase("$playercompat"))
            ParsePlayerCompat(parser);
        else if (type.EqualsIgnoreCase("$playersound"))
            ParsePlayerSound(parser);
        else if (type.EqualsIgnoreCase("$playersounddup"))
            ParsePlayerSoundDup(parser);
        else if (type.EqualsIgnoreCase("$pitchshift"))
            ParsePitchShift(parser);
        else if (type.EqualsIgnoreCase("$pitchshiftrange"))
            m_pitchShiftRange = parser.ConsumeInteger();
        else if (type.EqualsIgnoreCase("$pitchset"))
            ParsePitchSet(parser);
        else if (type.EqualsIgnoreCase("$alias"))
            ParseAlias(parser);
        else if (type.Equals("$limit"))
            ParseLimit(parser);
        else if (type.EqualsIgnoreCase("$random"))
            ParseRandom(parser);
        else if (type.EqualsIgnoreCase("$rolloff"))
            ParseRolloff(parser);
        else if (type.EqualsIgnoreCase("$playeralias"))
            ParsePlayerAlias(parser);
        else if (type.EqualsIgnoreCase("$ambient"))
            ParseAmbient(parser);
        else if (type.EqualsIgnoreCase("$archivepath"))
            ParseArchivePath(parser);
        else if (type.EqualsIgnoreCase("$attenuation"))
            ParseAttenuation(parser);
        else if (type.EqualsIgnoreCase("$attenuation"))
            ParseAttenuation(parser);
        else if (type.EqualsIgnoreCase("$edfoverride"))
            ParseIgnore(parser);
        else if (type.EqualsIgnoreCase("$ifdoom"))
            ParseIgnore(parser);
        else if (type.EqualsIgnoreCase("$ifheretic"))
            ParseIgnore(parser);
        else if (type.EqualsIgnoreCase("$ifhexen"))
            ParseIgnore(parser);
        else if (type.EqualsIgnoreCase("$ifstrife"))
            ParseIgnore(parser);
        else if (type.EqualsIgnoreCase("$map"))
            ParseIgnore(parser, 2);
        else if (type.EqualsIgnoreCase("$mididevice"))
            ParseIgnore(parser, 2);
        else if (type.EqualsIgnoreCase("$musicalias"))
            ParseIgnore(parser, 2);
        else if (type.EqualsIgnoreCase("$musicvolume"))
            ParseIgnore(parser, 2);
        else if (type.EqualsIgnoreCase("$registered"))
            ParseIgnore(parser, 0);
        else if (type.EqualsIgnoreCase("$singular"))
            ParseIgnore(parser, 1);
        else if (type.EqualsIgnoreCase("$volume"))
            ParseIgnore(parser, 2);
        else
            throw new ParserException(parser.GetCurrentLine(), 0, 0, $"SoundInfo - Bad command. {type}");
    }

    private void ParseIgnore(SimpleParser parser, int argCount = 0)
    {
        for (int i = 0; i < argCount; i++)
            parser.ConsumeString();
    }

    private void ParseAttenuation(SimpleParser parser)
    {
        parser.ConsumeDouble();
    }

    private void ParseArchivePath(SimpleParser parser)
    {
        parser.ConsumeString();
    }

    private void ParseAmbient(SimpleParser parser)
    {
        // Not supported
        int index = parser.ConsumeInteger();
        string logicalSound = parser.ConsumeString();
        string type = parser.ConsumeString();
        string mode = parser.ConsumeString();
        double volume = parser.ConsumeDouble();
    }

    private void ParsePitchSet(SimpleParser parser)
    {
        string key = parser.ConsumeString();
        double pitch = parser.ConsumeDouble();
        if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            soundInfo.PitchSet = (float)pitch;
    }

    private void ParsePlayerAlias(SimpleParser parser)
    {
        string playerClass = parser.ConsumeString();
        string gender = parser.ConsumeString();
        string logicalName = parser.ConsumeString();
        string otherLogicalSound = parser.ConsumeString();
    }

    private void ParseRolloff(SimpleParser parser)
    {
        string sound = parser.ConsumeString();

        if (parser.PeekInteger(out int i))
        {
            parser.ConsumeInteger();
            return;
        }

        parser.ConsumeString();
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
