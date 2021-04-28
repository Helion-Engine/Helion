using Helion.Util.Parser;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Resources.Definitions.Language
{
    public class LanguageDefinition
    {
        private readonly Dictionary<string, string> m_lookup = new(StringComparer.OrdinalIgnoreCase);

        public void Parse(string data)
        {
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);
            StringBuilder sb = new StringBuilder();

            while (!parser.IsDone())
            {
                if (parser.ConsumeIf("["))
                {
                    string language = parser.ConsumeString();
                    bool isDefault = parser.ConsumeIf("default");
                    parser.ConsumeString("]");
                    continue;
                }

                string key = parser.ConsumeString();
                parser.ConsumeString("=");

                sb.Clear();
                do
                {
                    sb.Append(parser.ConsumeString());
                } while (!parser.Peek(';'));

                parser.ConsumeString(";");

                m_lookup.Add(key, sb.ToString());
            }
        }

        public string[] GetMessages(string message)
        {
            if (message.Length > 0 && message[0] == '$')
                return LookupMessage(message[1..]).Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);

            return new string[] { message };
        }

        public string GetMessage(string message)
        {
            if (message.Length > 0 && message[0] == '$')
                return LookupMessage(message[1..]);

            return message;
        }

        public string GetMessage(Player player, Player? other, string message)
        {
            if (message.Length > 0 && message[0] == '$')
            {
                message = LookupMessage(message[1..]);
                return AddMessageParams(player, other, message);
            }

            return message;
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
}
