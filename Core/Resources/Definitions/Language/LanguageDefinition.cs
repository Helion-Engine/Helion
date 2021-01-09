using Helion.Util;
using Helion.Util.Parser;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Language
{
    public class LanguageDefinition
    {
        private static readonly HashSet<string> TypeNames = new HashSet<string>(new string[] { "Pickup", "Locks", "Cast call names", "Actor tag names", "Obituaries" });
        private static readonly Dictionary<CIString, LanguageMessageType> MessageTypeLookup = new()
        {
            { "Pickup", LanguageMessageType.Pickup },
            { "Locks", LanguageMessageType.Lock },
            { "Obituaries", LanguageMessageType.Obituary }
        };

        private readonly Dictionary<CIString, string>[] m_lookups;

        private LanguageMessageType m_parseType = LanguageMessageType.Pickup;

        public LanguageDefinition()
        {
            m_lookups = new Dictionary<CIString, string>[Enum.GetValues(typeof(LanguageMessageType)).Length];
            for (int i = 0; i < m_lookups.Length; i++)
                m_lookups[i] = new();
        }

        public void Parse(string data)
        {
            Dictionary<CIString, string> currentLookup = new();
            SimpleParser parser = new SimpleParser(ParseType.Csv);
            parser.Parse(data);

            while (!parser.IsDone())
            {
                string item = parser.ConsumeString();
                if (TypeNames.Contains(item))
                {
                    m_parseType = GetMessageType(item);
                    currentLookup = GetLookup(m_parseType);
                    continue;
                }

                if (m_parseType == LanguageMessageType.None)
                    continue;

                currentLookup[item] = parser.ConsumeString();
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

        private LanguageMessageType GetMessageType(string item)
        {
            if (MessageTypeLookup.TryGetValue(item, out LanguageMessageType type))
                return type;

            return LanguageMessageType.None;
        }

        private Dictionary<CIString, string> GetLookup(LanguageMessageType type)
        {
            return m_lookups[(int)type];
        }
    }
}
