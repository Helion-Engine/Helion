using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Parser;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Resources.Definitions.Language
{
    public class LanguageDefinition
    {
        private static readonly HashSet<string> TypeNames = new HashSet<string>(new string[] { "Pickup", "Locks", "Cast call names", "Actor tag names", "Obituaries" });
        private static readonly HashSet<string> IWadMessageTypeNames = new HashSet<string>(new string[] { "Level names" });
        private static readonly HashSet<string> IWadTypeNames = new HashSet<string>(new string[] { "Doom 1", "Doom 2", "Plutonia", "TNT: Evilution" });

        private static readonly Dictionary<CIString, LanguageMessageType> MessageTypeLookup = new()
        {
            { "Pickup", LanguageMessageType.Pickup },
            { "Locks", LanguageMessageType.Lock },
            { "Obituaries", LanguageMessageType.Obituary }
        };
        private static readonly Dictionary<CIString, IWadLanguageMessageType> IWadMessageTypeLookup = new()
        {
            { "Level names", IWadLanguageMessageType.LevelName },
        };
        private static readonly Dictionary<CIString, IWadType> IWadTypeLookup = new()
        {
            { "Doom 1", IWadType.UltimateDoom },
            { "Doom 2", IWadType.Doom2 },
            { "Plutonia", IWadType.Plutonia },
            { "TNT: Evilution", IWadType.TNT }
        };

        private readonly Dictionary<CIString, string>[] m_lookups;
        private readonly Dictionary<IWadType, Dictionary<CIString, string>[]> m_iwadLookups = new();

        public LanguageDefinition()
        {
            m_lookups = new Dictionary<CIString, string>[Enum.GetValues(typeof(LanguageMessageType)).Length];
            for (int i = 0; i < m_lookups.Length; i++)
                m_lookups[i] = new();

            var iwads = Enum.GetValues(typeof(IWadType));
            foreach (IWadType iwad in iwads)
            {
                var lookup = new Dictionary<CIString, string>[Enum.GetValues(typeof(IWadLanguageMessageType)).Length];
                for (int i = 0; i < lookup.Length; i++)
                    lookup[i] = new();
                m_iwadLookups.Add(iwad, lookup);
            }
        }

        public void ParseInternal(string data)
        {
            Dictionary<CIString, string> currentLookup = new();
            Dictionary<CIString, string> currentIwadLookup = new();
            SimpleParser parser = new SimpleParser(ParseType.Csv);
            parser.Parse(data);

            LanguageMessageType parseType = LanguageMessageType.Pickup;
            IWadLanguageMessageType iwadParseType = IWadLanguageMessageType.None;
            IWadType iwadType;

            while (!parser.IsDone())
            {
                string item = parser.ConsumeString();
                if (TypeNames.Contains(item))
                {
                    iwadParseType = IWadLanguageMessageType.None;
                    parseType = GetMessageType(item);
                    currentLookup = GetLookup(parseType);
                    continue;
                }

                if (IWadMessageTypeNames.Contains(item))
                {
                    parseType = LanguageMessageType.None;
                    iwadParseType = GetIWadMessageType(item);
                    item = parser.ConsumeString();
                    if (!IWadTypeNames.Contains(item))
                        throw new ParserException(parser.GetCurrentLine(), parser.GetCurrentCharOffset(), 0, $"Invalid game type {item}");

                    iwadType = GetIWadType(item);
                    currentIwadLookup = GetIWadLookup(iwadType, iwadParseType);
                    continue;
                }

                if (iwadParseType != IWadLanguageMessageType.None && IWadTypeNames.Contains(item))
                {
                    iwadType = GetIWadType(item);
                    currentIwadLookup = GetIWadLookup(iwadType, iwadParseType);
                    continue;
                }

                if (parseType != LanguageMessageType.None)
                    currentLookup[item] = parser.ConsumeString();
                else if (iwadParseType != IWadLanguageMessageType.None)
                    currentIwadLookup[item] = parser.ConsumeString();
            }
        }

        public void Parse(string data)
        {
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);
            var lookup = GetIWadLookup(IWadType.None, IWadLanguageMessageType.None);
            bool consumeLanguage = false;
            StringBuilder sb = new StringBuilder();

            while (!parser.IsDone())
            {
                string item = parser.ConsumeString();

                if (item.StartsWith('['))
                {
                    consumeLanguage = item[1..].StartsWith("enu", StringComparison.OrdinalIgnoreCase);

                    item = parser.ConsumeString();
                    if (item.EndsWith(']'))
                        continue;
                }

                if (!consumeLanguage)
                    continue;

                string key = item;
                parser.ConsumeString("=");

                sb.Clear();
                do
                {
                    item = parser.ConsumeString();
                    sb.Append(item);
                } while (!parser.Peek(';'));

                parser.ConsumeString(";");

                lookup.Add(key, sb.ToString());
            }
        }

        public string GetString(string str)
        {
            if (GetLookup(LanguageMessageType.None).TryGetValue(str, out string? value))
                return value;

            return str;
        }

        public string GetMessage(Player player, Player? other, string message, LanguageMessageType type)
        {
            if (message.Length > 0 && message[0] == '$')
            {
                message = LookupMessage(message[1..], type);
                return AddMessageParams(player, other, message, type);
            }

            return message;
        }

        public string GetIWadMessage(string message, IWadType iwad, IWadLanguageMessageType type)
        {
            if (iwad == IWadType.DoomShareware)
                iwad = IWadType.UltimateDoom;

            if (message.Length > 0 && message[0] == '$')
                return LookupIWadMessage(message[1..], iwad, type);

            return message;
        }

        private static string AddMessageParams(Player player, Player? other, string message, LanguageMessageType type)
        {
            switch (type)
            {
                case LanguageMessageType.Obituary:
                    message = message.Replace("%o", player.GetPlayerName());
                    message = message.Replace("%g", "he");
                    if (other != null)
                        message = message.Replace("%k", other.GetPlayerName());
                    return message;

                default:
                    break;
            }

            return message;
        }

        private string LookupMessage(string message, LanguageMessageType type)
        {
            if (GetLookup(type).TryGetValue(message, out string? translatedMessage))
                return translatedMessage;

            return string.Empty;
        }

        private string LookupIWadMessage(string message, IWadType iwad, IWadLanguageMessageType type)
        {
            if (GetIWadLookup(iwad, type).TryGetValue(message, out string? translatedMessage))
                return translatedMessage;

            if (GetIWadLookup(IWadType.None, IWadLanguageMessageType.None).TryGetValue(message, out string? translatedMessage2))
                return translatedMessage2;

            return string.Empty;
        }

        private LanguageMessageType GetMessageType(string item)
        {
            if (MessageTypeLookup.TryGetValue(item, out LanguageMessageType type))
                return type;

            return LanguageMessageType.None;
        }

        private IWadLanguageMessageType GetIWadMessageType(string item)
        {
            if (IWadMessageTypeLookup.TryGetValue(item, out IWadLanguageMessageType type))
                return type;

            return IWadLanguageMessageType.None;
        }

        private IWadType GetIWadType(string item)
        {
            if (IWadTypeLookup.TryGetValue(item, out IWadType type))
                return type;

            return IWadType.None;
        }

        private Dictionary<CIString, string> GetLookup(LanguageMessageType type)
        {
            return m_lookups[(int)type];
        }

        private Dictionary<CIString, string> GetIWadLookup(IWadType iwadType, IWadLanguageMessageType type)
        {
            return m_iwadLookups[iwadType][(int)type];
        }
    }
}
