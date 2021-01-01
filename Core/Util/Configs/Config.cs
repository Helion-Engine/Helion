using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helion.Input;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Tree;
using Helion.Util.Configs.Values;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Configs
{
    /// <summary>
    /// A container for all of the configuration data.
    /// </summary>
    public partial class Config : IDisposable
    {
        private const string EngineSectionName = "engine";
        private const string KeysSectionName = "keys";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // If you want to add new serializable fields into the engine section,
        // they should be here. Order technically doesn't matter, but it is
        // clearer for developers adding new fields to add them here.
        public readonly ConfigAudio Audio = new();
        public readonly ConfigConsole Console = new();
        public readonly ConfigDeveloper Developer = new();
        public readonly ConfigFiles Files = new();
        public readonly ConfigGame Game = new();
        public readonly ConfigHud Hud = new();
        public readonly ConfigMouse Mouse = new();
        public readonly ConfigRender Render = new();
        public readonly ConfigWindow Window = new();

        // Anything below this is not serialized into the engine section of the
        // config file.
        public readonly Dictionary<InputKey, string> Keys = MakeDefaultKeyMapping();
        public readonly ConfigTree Tree;
        private readonly string m_path;
        private bool m_disposed;
        private bool m_newConfig;

        public Config(string path = "config.ini")
        {
            m_path = path;
            ReadConfig(path);
            Tree = new(this);
        }

        ~Config()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private static Dictionary<InputKey, string> MakeDefaultKeyMapping()
        {
            return new()
            {
                [InputKey.W] = "+forward",
                [InputKey.A] = "+left",
                [InputKey.S] = "+back",
                [InputKey.D] = "+right",
                [InputKey.E] = "+use",
                [InputKey.Space] = "+jump",
                [InputKey.C] = "+crouch",
                [InputKey.MouseLeft] = "+attack",
                [InputKey.Up] = "+nextweapon",
                [InputKey.Down] = "+prevweapon",
                [InputKey.One] = "+slot1",
                [InputKey.Two] = "+slot2",
                [InputKey.Three] = "+slot3",
                [InputKey.Four] = "+slot4",
                [InputKey.Five] = "+slot5",
                [InputKey.Six] = "+slot6",
                [InputKey.Seven] = "+slot7",
                [InputKey.Backtick] = "console"
            };
        }

        internal static bool HasConfigAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDefined(typeof(ConfigInfoAttribute), true);
        }

        internal static bool IsConfigValue(FieldInfo fieldInfo) => IsConfigValueType(fieldInfo.FieldType);

        internal static bool IsConfigValueType(Type type)
        {
            Type? interfaceType = type.GetInterfaces().FirstOrDefault();
            return interfaceType != null &&
                   interfaceType.IsGenericType &&
                   interfaceType.GetGenericTypeDefinition().IsAssignableFrom(typeof(IConfigValue<>));
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            WriteConfig();
            m_disposed = true;
        }
    }
}
