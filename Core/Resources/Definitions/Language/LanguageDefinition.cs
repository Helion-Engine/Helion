using Helion.Resources.IWad;
using Helion.Util.Parser;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Resources.Definitions.Language
{
    public class LanguageDefinition
    {
        private static readonly HashSet<string> TypeNames = new HashSet<string>(new string[] { "Default", "Pickup", "Locks", "Cast call names", "Actor tag names", "Obituaries" },
            StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> IWadMessageTypeNames = new HashSet<string>(new string[] { "Level names" },
            StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> IWadTypeNames = new HashSet<string>(new string[] { "Doom 1", "Doom 2", "Plutonia", "TNT: Evilution", "Chex" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, LanguageMessageType> MessageTypeLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Default", LanguageMessageType.Default },
            { "Pickup", LanguageMessageType.Pickup },
            { "Locks", LanguageMessageType.Lock },
            { "Obituaries", LanguageMessageType.Obituary }
        };
        private static readonly Dictionary<string, IWadLanguageMessageType> IWadMessageTypeLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Level names", IWadLanguageMessageType.LevelName },
        };
        private static readonly Dictionary<string, IWadBaseType> IWadTypeLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Doom 1", IWadBaseType.Doom1 },
            { "Doom 2", IWadBaseType.Doom2 },
            { "Plutonia", IWadBaseType.Plutonia },
            { "TNT: Evilution", IWadBaseType.TNT },
            { "Chex", IWadBaseType.ChexQuest }
        };

        private readonly Dictionary<string, string>[] m_lookups;
        private readonly Dictionary<IWadBaseType, Dictionary<string, string>[]> m_iwadLookups = new();

        public LanguageDefinition()
        {
            m_lookups = new Dictionary<string, string>[Enum.GetValues(typeof(LanguageMessageType)).Length];
            for (int i = 0; i < m_lookups.Length; i++)
                m_lookups[i] = new(StringComparer.OrdinalIgnoreCase);

            var iwads = Enum.GetValues(typeof(IWadBaseType));
            foreach (IWadBaseType iwad in iwads)
            {
                var lookup = new Dictionary<string, string>[Enum.GetValues(typeof(IWadLanguageMessageType)).Length];
                for (int i = 0; i < lookup.Length; i++)
                    lookup[i] = new(StringComparer.OrdinalIgnoreCase);
                m_iwadLookups.Add(iwad, lookup);
            }
        }

        public void ParseInternal(string data)
        {
            Dictionary<string, string> currentLookup = new();
            Dictionary<string, string> currentIwadLookup = new();
            SimpleParser parser = new SimpleParser(ParseType.Csv);
            parser.Parse(data);

            LanguageMessageType parseType = LanguageMessageType.Pickup;
            IWadLanguageMessageType iwadParseType = IWadLanguageMessageType.None;
            IWadBaseType iwadType;

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
            var lookup = GetIWadLookup(IWadBaseType.None, IWadLanguageMessageType.None);
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

        public string GetDefaultMessage(string message)
        {
            if (message.Length > 0 && message[0] == '$')
                return LookupMessage(message[1..], LanguageMessageType.Default);

            return message;
        }

        public string[] GetDefaultMessages(string message)
        {
            if (message.Length > 0 && message[0] == '$')
                return LookupMessage(message[1..], LanguageMessageType.Default).Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);

            return new string[] { message };
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

        public string GetIWadMessage(string message, IWadBaseType iwad, IWadLanguageMessageType type)
        {
            if (message.Length > 0 && message[0] == '$')
                return LookupIWadMessage(message[1..], iwad, type);

            return message;
        }

        private static string AddMessageParams(Player player, Player? other, string message, LanguageMessageType type)
        {
            switch (type)
            {
                case LanguageMessageType.Obituary:
                    message = message.Replace("%o", player.Info.Name);
                    message = message.Replace("%g", player.Info.GetGenderSubject());
                    message = message.Replace("%h", player.Info.GetGenderObject());
                    if (other != null)
                        message = message.Replace("%k", player.Info.Name);
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

        private string LookupIWadMessage(string message, IWadBaseType iwad, IWadLanguageMessageType type)
        {
            if (GetIWadLookup(iwad, type).TryGetValue(message, out string? translatedMessage))
                return translatedMessage;

            if (GetIWadLookup(IWadBaseType.None, IWadLanguageMessageType.None).TryGetValue(message, out string? translatedMessage2))
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

        private IWadBaseType GetIWadType(string item)
        {
            if (IWadTypeLookup.TryGetValue(item, out IWadBaseType type))
                return type;

            return IWadBaseType.None;
        }

        private Dictionary<string, string> GetLookup(LanguageMessageType type)
        {
            return m_lookups[(int)type];
        }

        private Dictionary<string, string> GetIWadLookup(IWadBaseType iwadType, IWadLanguageMessageType type)
        {
            return m_iwadLookups[iwadType][(int)type];
        }
    }
}
