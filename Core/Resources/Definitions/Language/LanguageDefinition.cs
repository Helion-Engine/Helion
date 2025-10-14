using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Helion.Resources.Definitions.Language;

public class LanguageDefinition
{
    public CultureInfo CultureInfo { get; set; } = CultureInfo.CurrentCulture;

    private readonly Dictionary<string, string> m_lookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> m_compatLookup = new(StringComparer.OrdinalIgnoreCase);

    // Used for compatibility when modifying language strings. E.g. BEX defines 'gotredskull'
    // But zdoom uses 'gotredskul' with a single l
    public void ParseCompatibility(string data)
    {
        SimpleParser parser = new();
        parser.Parse(data);

        while (!parser.IsDone())
            m_compatLookup.Add(parser.ConsumeString(), parser.ConsumeString());
    }

    public void Parse(string data, IWadInfo iwadInfo)
    {
        data = GetCurrentLanguageSection(data);

        SimpleParser parser = new();
        parser.Parse(data);
        StringBuilder sb = new();

        while (!parser.IsDone())
        {
            var key = parser.ConsumeString();
            if (key.StartsWithIgnoreCase("$ifgame"))
            {
                var type = GetGameType(GetGameName(key));
                if (!CompareIWadTypes(type, iwadInfo.IWadBaseType))
                {
                    parser.ConsumeLineSpan();
                    continue;
                }

                key = parser.ConsumeString();
            }

            parser.ConsumeString("=");

            sb.Clear();
            do
            {
                sb.Append(parser.ConsumeString().Replace("\\n", "\n"));
            } while (!parser.Peek(';'));

            parser.ConsumeString(";");

            m_lookup[key] = sb.ToString();
        }
    }

    private static bool CompareIWadTypes(IWadBaseType x, IWadBaseType y)
    {
        if (x == IWadBaseType.None || y == IWadBaseType.None)
            return false;

        if (x == IWadBaseType.Doom1)
            x = IWadBaseType.Doom2;
        if (y == IWadBaseType.Doom2)
            y = IWadBaseType.Doom2;

        return x == y;
    }

    private static ReadOnlySpan<char> GetGameName(string key)
    {
        const string IfGame = "$ifgame(";
        var start = key.IndexOf(IfGame, StringComparison.OrdinalIgnoreCase);
        var end = key.IndexOf(")", StringComparison.OrdinalIgnoreCase);

        if (start != -1 && end != -1)
            return key.AsSpan(start + IfGame.Length, end - start - IfGame.Length);

        return new ReadOnlySpan<char>();
    }

    private static IWadBaseType GetGameType(ReadOnlySpan<char> name)
    {
        Span<char> lower = stackalloc char[name.Length];
        name.ToLowerInvariant(lower);

        return lower switch
        {
            "doom" => IWadBaseType.Doom2,
            "heretic" => IWadBaseType.Heretic,
            "hexen" => IWadBaseType.Hexen,
            "chex" => IWadBaseType.ChexQuest,
            _ => IWadBaseType.None,
        };
    }

    public bool SetValue(string key, string value)
    {
        if (m_compatLookup.TryGetValue(key, out var compatValue))
            key = compatValue;

        if (!m_lookup.ContainsKey(key))
            return false;

        m_lookup[key] = value;
        return true;
    }

    public void Add(string key, string value)
    {
        m_lookup[key] = value;
    }

    private string GetCurrentLanguageSection(string data)
    {
        Regex currentLanguage = new(string.Format(CultureInfo.InvariantCulture, "\\[{0}\\w?(\\s+default)?]", CultureInfo.TwoLetterISOLanguageName));
        Regex defaultLanguage = new("\\[\\w+\\s+default]");
        Regex anyLanguage = new("\\[\\w+(\\s+default)?]");

        Match m = currentLanguage.Match(data);
        if (m.Success)
            return GetLanguageSection();

        m = defaultLanguage.Match(data);
        if (m.Success)
            return GetLanguageSection();

        string GetLanguageSection()
        {
            int startIndex = m.Index + m.Length;
            int endIndex = data.Length;
            m = anyLanguage.Match(data, startIndex);

            if (m.Success)
                endIndex = m.Index;

            return data[startIndex..endIndex];
        }

        return data;
    }

    private static readonly string[] NewLineSplit = new string[] { "\n", "\r\n" };

    public static string[] SplitMessageByNewLines(string text) => text.Split(NewLineSplit, StringSplitOptions.None);

    public bool TryGetMessages(string message, [NotNullWhen(true)] out string[]? messages)
    {
        if (message.Length == 0 || message[0] != '$')
        {
            messages = null;
            return false;
        }

        if (!m_lookup.TryGetValue(message[1..], out string? translatedMessage))
        {
            messages = null;
            return false;
        }

        messages = SplitMessageByNewLines(translatedMessage);
        return true;
    }

    public string[] GetMessages(string message)
    {
        if (message.Length > 0 && message[0] == '$')
            return SplitMessageByNewLines(LookupMessage(message[1..]));

        return SplitMessageByNewLines(message);
    }

    public string GetMessage(string message)
    {
        if (message.Length > 0 && message[0] == '$')
            return LookupMessage(message[1..]);

        return message;
    }

    private readonly Dictionary<string, string> m_messageTranslation = new(StringComparer.OrdinalIgnoreCase);

    public string GetMessage(Player? player, Player? other, string message)
    {
        if (message.Length > 0 && message[0] == '$')
        {
            // Until dictionary supports ReadOnlySpan<char>...
            if (!m_messageTranslation.TryGetValue(message, out var withoutMarker))
            {
                withoutMarker = message[1..];
                m_messageTranslation[message] = withoutMarker;
            }

            message = LookupMessage(withoutMarker);
            if (player == null)
                return message;
            return AddMessageParams(player, other, message);
        }

        return message;
    }

    public bool GetKeyByValue(string text, [NotNullWhen(true)] out string? key)
    {
        const int Length = 32;
        key = null;
        if (text.Length > Length)
            text = text.Substring(0, Length);

        foreach (var data in m_lookup)
        {
            if (data.Value.Length < text.Length)
                continue;

            if (data.Value.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                key = data.Key;
                return true;
            }
        }

        return false;
    }

    private static string AddMessageParams(Player player, Player? other, string message)
    {
        message = message.Replace("%o", player.Info.Name, StringComparison.OrdinalIgnoreCase);
        message = message.Replace("%g", player.Info.GetGenderSubject(), StringComparison.OrdinalIgnoreCase);
        message = message.Replace("%h", player.Info.GetGenderObject(), StringComparison.OrdinalIgnoreCase);
        if (other != null)
            message = message.Replace("%k", player.Info.Name, StringComparison.OrdinalIgnoreCase);
        return message;
    }

    private string LookupMessage(string message)
    {
        if (m_lookup.TryGetValue(message, out string? translatedMessage))
            return translatedMessage;

        return string.Empty;
    }
}
