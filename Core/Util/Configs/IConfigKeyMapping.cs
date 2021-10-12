using System.Collections.Generic;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Util.Configs;

/// <summary>
/// A bi-directional mapping of keys to commands.
/// </summary>
public interface IConfigKeyMapping : IEnumerable<(Key Key, IEnumerable<string> Commands)>
{
    /// <summary>
    /// Whether any keys have been set. This is intended to be a marker for
    /// the choice to save or not when something has changed.
    /// </summary>
    public bool Changed { get; }

    IReadOnlySet<string> this[Key key] { get; }
    IReadOnlySet<Key> this[string command] { get; }

    /// <summary>
    /// Adds a key to be mapped. This is a bi-directional mapping.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="command">The command for the key.</param>
    void Add(Key key, string command);

    /// <summary>
    /// Consumes the key for the mapped command if it is currently pressed.
    /// </summary>
    /// <param name="command">The command, which is not case sensitive.</param>
    /// <param name="input">The consumable input.</param>
    /// <returns>True if it was found to be pressed and was consumed, false if
    /// not or if something else consumed it.</returns>
    bool ConsumeCommandKeyPress(string command, IConsumableInput input);

    /// <summary>
    /// Consumes the key for the mapped command if it is currently down.
    /// </summary>
    /// <param name="command">The command, which is not case sensitive.</param>
    /// <param name="input">The consumable input.</param>
    /// <returns>True if it was found to be down and was consumed, false if
    /// not or if something else consumed it.</returns>
    bool ConsumeCommandKeyDown(string command, IConsumableInput input);

    /// <summary>
    /// Unbinds all commands and keys that match this key. This means the
    /// key will be removed, and any alias that would point onto that key
    /// would be removed. This does not remove bindings to other keys.
    /// </summary>
    /// <remarks>
    /// For example, suppose we had { A -> ["cmdA", "cmdB"], B -> ["cmdA", "cmdB"] }.
    /// If we UnbindAll(A), then the map would look like { B -> ["cmdA", "cmdB"] }.
    /// Likewise, any lookups for "cmdA" would return B only after this is invoked.
    /// </remarks>
    /// <param name="key">The key to unbind.</param>
    void UnbindAll(Key key);
}

