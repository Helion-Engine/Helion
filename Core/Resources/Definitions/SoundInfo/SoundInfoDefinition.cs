using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.Util.RandomGenerators;

namespace Helion.Resources.Definitions.SoundInfo;

public class SoundInfoDefinition
{
    private readonly Dictionary<string, SoundInfo> m_lookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> m_randomLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> m_playerCompatLookup = new(StringComparer.OrdinalIgnoreCase);

    private int m_pitchShiftRange;

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
        if (!LookupInternal(name, out var sndInfo))
            return null;

        if (sndInfo.Random && m_randomLookup.TryGetValue(name, out var sounds) && sounds.Count > 0)
        {
            name = sounds[random.NextByte() % sounds.Count];
            if (LookupInternal(name, out sndInfo))
                return sndInfo;
        }

        return sndInfo;
    }

    private bool LookupInternal(string name, [NotNullWhen(true)] out SoundInfo? sndInfo)
    {
        if (name.StartsWith("player/", StringComparison.OrdinalIgnoreCase) &&
            m_playerCompatLookup.TryGetValue(name, out string? playerCompat) && playerCompat != null)
            name = playerCompat;

        return m_lookup.TryGetValue(name, out sndInfo);
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
        var type = parser.ConsumeStringSpan();
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
        else if (type.EqualsIgnoreCase("$limit"))
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

    private static void ParseIgnore(SimpleParser parser, int argCount = 0)
    {
        for (int i = 0; i < argCount; i++)
            parser.ConsumeString();
    }

    private static void ParseAttenuation(SimpleParser parser)
    {
        parser.ConsumeDouble();
    }

    private static void ParseArchivePath(SimpleParser parser)
    {
        parser.ConsumeStringSpan();
    }

    private static void ParseAmbient(SimpleParser parser)
    {
        // Not supported
        var index = parser.ConsumeInteger();
        var logicalSound = parser.ConsumeStringSpan();
        var type = parser.ConsumeStringSpan();
        var mode = parser.ConsumeStringSpan();
        var volume = parser.ConsumeDouble();
    }

    private void ParsePitchSet(SimpleParser parser)
    {
        string key = parser.ConsumeString();
        double pitch = parser.ConsumeDouble();
        if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            soundInfo.PitchSet = (float)pitch;
    }

    private static void ParsePlayerAlias(SimpleParser parser)
    {
        var playerClass = parser.ConsumeStringSpan();
        var gender = parser.ConsumeStringSpan();
        var logicalName = parser.ConsumeStringSpan();
        var otherLogicalSound = parser.ConsumeStringSpan();
    }

    private static void ParseRolloff(SimpleParser parser)
    {
        var sound = parser.ConsumeStringSpan();
        if (parser.PeekInteger(out int i))
        {
            parser.ConsumeInteger();
            return;
        }

        parser.ConsumeStringSpan();
    }

    private void ParsePitchShift(SimpleParser parser)
    {
        var key = parser.ConsumeString();
        var pitch = parser.ConsumeInteger();

        if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            soundInfo.PitchShift = pitch;
    }

    private void ParseLimit(SimpleParser parser)
    {
        var key = parser.ConsumeString();
        var limit = parser.ConsumeInteger();

        if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            soundInfo.Limit = limit;
    }

    private void ParseAlias(SimpleParser parser)
    {
        var alias = parser.ConsumeString();
        var key = parser.ConsumeString();

        if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
            m_lookup[alias] = soundInfo;
    }

    private void ParsePlayerCompat(SimpleParser parser)
    {
        var player = parser.ConsumeStringSpan();
        var gender = parser.ConsumeStringSpan();
        var name = parser.ConsumeStringSpan();
        var compat = parser.ConsumeString();

        m_playerCompatLookup[compat] = $"{player}/{gender}/{name}";
    }

    private void ParsePlayerSoundDup(SimpleParser parser)
    {
        var player = parser.ConsumeStringSpan();
        var gender = parser.ConsumeStringSpan();
        var name = parser.ConsumeStringSpan();
        var entryName = parser.ConsumeStringSpan();
        var key = $"{player}/{gender}/{entryName}";

        if (m_lookup.TryGetValue(key, out SoundInfo? soundInfo))
        {
            key = $"{player}/{gender}/{name}";
            AddSound(key, soundInfo.EntryName, true);
        }
    }

    private void ParsePlayerSound(SimpleParser parser)
    {
        var key = $"{parser.ConsumeStringSpan()}/{parser.ConsumeStringSpan()}/{parser.ConsumeStringSpan()}";
        AddSound(key, parser.ConsumeString(), true);
    }

    private void ParseRandom(SimpleParser parser)
    {
        List<string> sounds = [];
        var key = parser.ConsumeString();
        parser.Consume('{');

        while (!parser.Peek('}'))
            sounds.Add(parser.ConsumeString());
        parser.Consume('}');

        m_lookup[key] = new SoundInfo(key, string.Empty, 0, random: true);        
        m_randomLookup[key] = sounds;
    }

    private void AddSound(string key, string entryName, bool playerEntry = false)
    {
        if (playerEntry && m_playerCompatLookup.TryGetValue(key, out string? playerCompat))
            key = playerCompat;

        m_lookup[key] = new SoundInfo(key, entryName, m_pitchShiftRange, playerEntry);
    }
}
