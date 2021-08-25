using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Input;
using Helion.Util.Extensions;

namespace Helion.Util.ConfigsNew
{
    /// <summary>
    /// A case insensitive two-way lookup.
    /// </summary>
    public class ConfigKeyMapping : IEnumerable<(Key Key, IEnumerable<string> Commands)>
    {
        private static readonly IReadOnlySet<Key> EmptyKeySet = new HashSet<Key>();
        private static readonly IReadOnlySet<string> EmptyStringSet = new HashSet<string>();

        public bool Changed { get; internal set; }
        private readonly Dictionary<Key, HashSet<string>> m_keyToCommands = new();
        private readonly Dictionary<string, HashSet<Key>> m_commandsToKey = new(StringComparer.OrdinalIgnoreCase);

        public ConfigKeyMapping()
        {
            Add(Key.W, "Forward");
            Add(Key.A, "Left");
            Add(Key.S, "Backward");
            Add(Key.D, "Right");
            Add(Key.E, "Use");
            Add(Key.ShiftLeft, "Run");
            Add(Key.ShiftRight, "RunAlt");
            Add(Key.AltLeft, "Strafe");
            Add(Key.Left, "TurnLeft");
            Add(Key.Right, "TurnRight");
            Add(Key.O, "LookUp");
            Add(Key.L, "LookDown");
            Add(Key.Space, "Jump");
            Add(Key.C, "Crouch");
            Add(Key.Backtick, "Console");
            Add(Key.MouseLeft, "Attack");
            Add(Key.ControlRight, "AttackAlt");
            Add(Key.Up, "NextWeapon");
            Add(Key.Down, "PreviousWeapon");
            Add(Key.One, "WeaponSlot1");
            Add(Key.Two, "WeaponSlot2");
            Add(Key.Three, "WeaponSlot3");
            Add(Key.Four, "WeaponSlot4");
            Add(Key.Five, "WeaponSlot5");
            Add(Key.Six, "WeaponSlot6");
            Add(Key.Seven, "WeaponSlot7");
            Add(Key.PrintScreen, "Screenshot");
            Add(Key.Equals, "HudIncrease");
            Add(Key.Minus, "HudDecrease");
            Add(Key.Equals, "AutoMapIncrease");
            Add(Key.Minus, "AutoMapDecrease");
            Add(Key.Up, "AutoMapUp");
            Add(Key.Down, "AutoMapDown");
            Add(Key.Left, "AutoMapLeft");
            Add(Key.Right, "AutoMapRight");
            Add(Key.F2, "Save");
            Add(Key.F3, "Load");
            Add(Key.Tab, "Automap");
        }
        
        public IReadOnlySet<string> this[Key key] =>
            m_keyToCommands.TryGetValue(key, out HashSet<string>? commands) ? 
                commands : 
                EmptyStringSet;

        public IReadOnlySet<Key> this[string command] => 
            m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys) ? 
                keys : 
                EmptyKeySet;

        /// <summary>
        /// Adds a two way mapping.
        /// </summary>
        /// <param name="key">The key to map to the command.</param>
        /// <param name="command">The command to map to the key.</param>
        public void Add(Key key, string command)
        {
            if (m_keyToCommands.TryGetValue(key, out HashSet<string>? commands))
            {
                Changed |= !commands.Contains(command);
                commands.Add(command);
            }
            else
            {
                Changed = true;
                m_keyToCommands[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { command };
            }

            if (m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys))
            {
                Changed |= !keys.Contains(key);
                keys.Add(key);
            }
            else
            {
                Changed = true;
                m_commandsToKey[command] = new HashSet<Key> { key };
            }
        }

        /// <summary>
        /// Removes all bindings from a key to anything, and if there are any
        /// commands for this key, they are all unbound in the other direction.
        /// </summary>
        /// <param name="key">The key to unbind.</param>
        public void UnbindAll(Key key)
        {
            if (!m_keyToCommands.TryGetValue(key, out HashSet<string>? commands))
                return;
            
            foreach (string command in commands)
            {
                if (!m_commandsToKey.TryGetValue(command, out HashSet<Key>? keys)) 
                    continue;
                
                keys.Remove(key);
                if (keys.Empty())
                    m_commandsToKey.Remove(command);
            }

            Changed |= m_keyToCommands.ContainsKey(key);
            m_keyToCommands.Remove(key);
        }

        public IEnumerator<(Key Key, IEnumerable<string> Commands)> GetEnumerator()
        {
            foreach ((Key key, HashSet<string> commands) in m_keyToCommands)
                yield return (key, commands);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
