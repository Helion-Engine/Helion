using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Input;
using Helion.Util.Extensions;
using NLog;

namespace Helion.Util.Configs
{
    /// <summary>
    /// A case insensitive two-way lookup.
    /// </summary>
    public class ConfigKeyMapping : IEnumerable<(Key Key, IEnumerable<string> Commands)>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly IReadOnlySet<Key> EmptyKeySet = new HashSet<Key>();
        private static readonly IReadOnlySet<string> EmptyStringSet = new HashSet<string>();
        
        public bool Changed { get; internal set; }
        private readonly Dictionary<Key, HashSet<string>> m_keyToCommands = new();
        private readonly Dictionary<string, HashSet<Key>> m_commandsToKey = new(StringComparer.OrdinalIgnoreCase);

        public void AddDefaults()
        {
            Log.Trace("Adding default key commands to config keys");
            
            Add(Key.W, Constants.Input.Forward);
            Add(Key.A, Constants.Input.Left);
            Add(Key.S, Constants.Input.Backward);
            Add(Key.D, Constants.Input.Right);
            Add(Key.E, Constants.Input.Use);
            Add(Key.ShiftLeft, Constants.Input.Run);
            Add(Key.ShiftRight, Constants.Input.RunAlt);
            Add(Key.AltLeft, Constants.Input.Strafe);
            Add(Key.Left, Constants.Input.TurnLeft);
            Add(Key.Right, Constants.Input.TurnRight);
            Add(Key.O, Constants.Input.LookUp);
            Add(Key.L, Constants.Input.LookDown);
            Add(Key.Space, Constants.Input.Jump);
            Add(Key.C, Constants.Input.Crouch);
            Add(Key.Backtick, Constants.Input.Console);
            Add(Key.MouseLeft, Constants.Input.Attack);
            Add(Key.ControlRight, Constants.Input.AttackAlt);
            Add(Key.Up, Constants.Input.NextWeapon);
            Add(Key.Down, Constants.Input.PreviousWeapon);
            Add(Key.One, Constants.Input.WeaponSlot1);
            Add(Key.Two, Constants.Input.WeaponSlot2);
            Add(Key.Three, Constants.Input.WeaponSlot3);
            Add(Key.Four, Constants.Input.WeaponSlot4);
            Add(Key.Five, Constants.Input.WeaponSlot5);
            Add(Key.Six, Constants.Input.WeaponSlot6);
            Add(Key.Seven, Constants.Input.WeaponSlot7);
            Add(Key.PrintScreen, Constants.Input.Screenshot);
            Add(Key.Equals, Constants.Input.HudIncrease);
            Add(Key.Minus, Constants.Input.HudDecrease);
            Add(Key.Equals, Constants.Input.AutoMapIncrease);
            Add(Key.Minus, Constants.Input.AutoMapDecrease);
            Add(Key.Up, Constants.Input.AutoMapUp);
            Add(Key.Down, Constants.Input.AutoMapDown);
            Add(Key.Left, Constants.Input.AutoMapLeft);
            Add(Key.Right, Constants.Input.AutoMapRight);
            Add(Key.F2, Constants.Input.Save);
            Add(Key.F3, Constants.Input.Load);
            Add(Key.Tab, Constants.Input.Automap);
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
            if (key == Key.Unknown || command == "")
                return;
            
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
        /// Will search through all the possible keys for the command. If found,
        /// it will consume the key and exit. If multiple are pressed, it will
        /// only consume the first one it finds.
        /// </summary>
        /// <param name="command">The case-insensitive command.</param>
        /// <param name="inputEvent">The input event to consume from.</param>
        /// <returns>True if it consumed the key (meaning it was pressed), false
        /// if no keys were pressed for that command.</returns>
        public bool ConsumeCommandKeyPress(string command, InputEvent inputEvent)
        {
            foreach (Key key in this[command])
                if (inputEvent.ConsumeKeyPressed(key))
                    return true;
            return false;
        }
        
        /// <summary>
        /// Will search through all the possible keys for the command. If found,
        /// it will consume the key and exit. If multiple are pressed, it will
        /// only consume the first one it finds.
        /// </summary>
        /// <param name="command">The case-insensitive command.</param>
        /// <param name="inputEvent">The input event to consume from.</param>
        /// <returns>True if it consumed the key (meaning it was pressed), false
        /// if no keys were pressed for that command.</returns>
        public bool ConsumeCommandKeyPressedOrDown(string command, InputEvent inputEvent)
        {
            foreach (Key key in this[command])
                if (inputEvent.ConsumeKeyPressedOrDown(key))
                    return true;
            return false;
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
