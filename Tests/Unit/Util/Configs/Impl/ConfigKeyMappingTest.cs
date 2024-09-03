using System.Collections.Generic;
using FluentAssertions;
using Helion.Util;
using Helion.Util.Configs.Impl;
using Helion.Window;
using Helion.Window.Input;
using NSubstitute;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl;

public class ConfigKeyMappingTest
{
    // This is a convenience mapping that is designed to catch human error.
    // If someone breaks a binding, this test will fail.
    private static readonly List<(Key key, string command)> ExpectedMappings = new()
    {
        (Key.W, Constants.Input.Forward),
        (Key.A, Constants.Input.Left),
        (Key.S, Constants.Input.Backward),
        (Key.D, Constants.Input.Right),
        (Key.E, Constants.Input.Use),
        (Key.ShiftLeft, Constants.Input.Run),
        (Key.ShiftRight, Constants.Input.Run),
        (Key.AltLeft, Constants.Input.Strafe),
        (Key.AltRight, Constants.Input.Strafe),
        (Key.Left, Constants.Input.TurnLeft),
        (Key.Right, Constants.Input.TurnRight),
        (Key.Up, Constants.Input.LookUp),
        (Key.Down, Constants.Input.LookDown),
        (Key.Space, Constants.Input.Jump),
        (Key.C, Constants.Input.Crouch),
        (Key.Backtick, Constants.Input.Console),
        (Key.MouseLeft, Constants.Input.Attack),
        (Key.ControlLeft, Constants.Input.Attack),
        (Key.ControlRight, Constants.Input.Attack),
        (Key.MouseWheelUp, Constants.Input.NextWeapon),
        (Key.MouseWheelDown, Constants.Input.PreviousWeapon),
        (Key.MouseWheelUp, Constants.Input.AutoMapIncrease),
        (Key.MouseWheelDown, Constants.Input.AutoMapDecrease),
        (Key.One, Constants.Input.WeaponSlot1),
        (Key.Two, Constants.Input.WeaponSlot2),
        (Key.Three, Constants.Input.WeaponSlot3),
        (Key.Four, Constants.Input.WeaponSlot4),
        (Key.Five, Constants.Input.WeaponSlot5),
        (Key.Six, Constants.Input.WeaponSlot6),
        (Key.Seven, Constants.Input.WeaponSlot7),
        (Key.PrintScreen, Constants.Input.Screenshot),
        (Key.Equals, Constants.Input.HudIncrease),
        (Key.Minus, Constants.Input.HudDecrease),
        (Key.Equals, Constants.Input.AutoMapIncrease),
        (Key.Minus, Constants.Input.AutoMapDecrease),
        (Key.Up, Constants.Input.AutoMapUp),
        (Key.Down, Constants.Input.AutoMapDown),
        (Key.Left, Constants.Input.AutoMapLeft),
        (Key.Right, Constants.Input.AutoMapRight),
        (Key.F2, Constants.Input.Save),
        (Key.F3, Constants.Input.Load),
        (Key.F4, Constants.Input.OptionsMenu),
        (Key.Tab, Constants.Input.Automap),
        (Key.F6, Constants.Input.QuickSave),
    };

    [Fact(DisplayName = "Can add defaults")]
    public void CanAddDefaults()
    {
        ConfigKeyMapping keys = new();

        // No keys are added yet.
        foreach ((Key key, string command) in ExpectedMappings)
        {
            GetCommandKeys(keys, command).Should().BeEmpty();
            GetKeyCommands(keys, key).Should().BeEmpty();
        }

        keys.SetInitialDefaultKeyBindings();

        // Now with the defaults applied, let's make sure they are in fact added.
        foreach ((Key key, string command) in ExpectedMappings)
        {
            GetKeyCommands(keys, key).Should().Contain(command);
            GetCommandKeys(keys, command).Should().Contain(key);
        }
    }

    [Fact(DisplayName = "Look up by key")]
    public void LookUpByKey()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.A, "first");
        keys.Add(Key.A, "second");
        keys.Add(Key.B, "third");

        GetKeyCommands(keys, Key.A).Should().Equal("first", "second");
        GetKeyCommands(keys, Key.B).Should().Equal("third");
        GetKeyCommands(keys, Key.C).Should().BeEmpty();
    }

    [Fact(DisplayName = "Look up by command")]
    public void LookUpByCommand()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.A, "first");
        keys.Add(Key.A, "second");
        keys.Add(Key.B, "first");

        GetCommandKeys(keys, "first").Should().Equal(Key.A, Key.B);
        GetCommandKeys(keys, "second").Should().Equal(Key.A);
        GetCommandKeys(keys, "no such command").Should().BeEmpty();
    }

    [Fact(DisplayName = "Can add new key/command mapping")]
    public void AddNewMapping()
    {
        ConfigKeyMapping keys = new();
        GetKeyCommands(keys, Key.A).Should().BeEmpty();
        GetCommandKeys(keys, "a").Should().BeEmpty();

        keys.Add(Key.A, "a");
        GetKeyCommands(keys, Key.A).Should().Equal("a");
        GetCommandKeys(keys, "a").Should().Equal(Key.A);

        // Adding again should do nothing.
        keys.Add(Key.A, "a");
        GetKeyCommands(keys, Key.A).Should().Equal("a");
        GetCommandKeys(keys, "a").Should().Equal(Key.A);
    }

    [Fact(DisplayName = "Can add existing key mapping to a new command")]
    public void AddExistingMappingNewCommand()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.A, "a");

        GetKeyCommands(keys, Key.A).Should().Equal("a");
        GetCommandKeys(keys, "a").Should().Equal(Key.A);

        keys.Add(Key.A, "b");
        GetKeyCommands(keys, Key.A).Should().Equal("a", "b");
        GetCommandKeys(keys, "a").Should().Equal(Key.A);
        GetCommandKeys(keys, "b").Should().Equal(Key.A);
    }

    [Fact(DisplayName = "Can add an existing command to a new key")]
    public void AddExistingMappingNewKey()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.A, "a");

        GetKeyCommands(keys, Key.A).Should().Equal("a");
        GetCommandKeys(keys, "a").Should().Equal(Key.A);

        keys.Add(Key.B, "a");
        GetKeyCommands(keys, Key.A).Should().Equal("a");
        GetKeyCommands(keys, Key.B).Should().Equal("a");
        GetCommandKeys(keys, "a").Should().Equal(Key.A, Key.B);
    }

    [Fact(DisplayName = "Can consume a key press for a command")]
    public void CanConsumeKeyCommandPress()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.C, "something");

        var input = Substitute.For<IConsumableInput>();
        input.ConsumeKeyPressed(Arg.Is(Key.C)).Returns(true);

        keys.ConsumeCommandKeyPress("something", input, out _).Should().BeTrue();
        input.Received(1).ConsumeKeyPressed(Key.C);
    }

    [Fact(DisplayName = "Can consume key down for a command")]
    public void CanConsumeKeyDownCommand()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.C, "something");

        var input = Substitute.For<IConsumableInput>();
        input.ConsumeKeyDown(Arg.Is(Key.C)).Returns(true);

        keys.ConsumeCommandKeyDown("something", input, out _, out _).Should().BeTrue();
        input.Received(1).ConsumeKeyDown(Key.C);
    }

    [Fact(DisplayName = "Can unbind all")]
    public void CanUnbindAll()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.C, "something");
        keys.Add(Key.C, "another");
        keys.Add(Key.F, "something");

        // Nothing changes if there's no binding.
        keys.UnbindAll(Key.A);
        GetKeyCommands(keys, Key.C).Should().Equal("something", "another");
        GetKeyCommands(keys, Key.F).Should().Equal("something");

        // A binding will get removed.
        keys.UnbindAll(Key.C);
        GetKeyCommands(keys, Key.C).Should().BeEmpty();
        GetCommandKeys(keys, "something").Should().Equal(Key.F);
        GetCommandKeys(keys, "another").Should().BeEmpty();
    }

    [Fact(DisplayName = "Unbind all marks change if key was bound")]
    public void UnbindAllMarksChangedIfKeyBound()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.C, "something");
        keys.Add(Key.C, "another");
        keys.Add(Key.F, "something");

        keys.ClearChanged();

        keys.UnbindAll(Key.A);
        keys.Changed.Should().BeFalse();

        keys.UnbindAll(Key.F);
        keys.Changed.Should().BeTrue();
    }

    [Fact(DisplayName = "Removing key marks change if key was bound")]
    public void RemovingKeyMarksChangedIfKeyBound()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.B, "something");
        keys.Add(Key.C, "other");

        keys.ClearChanged();

        keys.Remove(Key.A);
        keys.Changed.Should().BeFalse();

        keys.Remove(Key.B, "something");
        keys.Changed.Should().BeTrue();

        keys.ClearChanged();

        keys.Remove(Key.C, "notbound");
        keys.Changed.Should().BeFalse();

        keys.Remove(Key.C);
        keys.Changed.Should().BeTrue();
    }

    [Fact(DisplayName = "Adding a new key value marks a change")]
    public void AddNewKeyMarksChanged()
    {
        ConfigKeyMapping keys = new();
        keys.Changed.Should().BeFalse();

        keys.Add(Key.A, "yes");
        keys.Changed.Should().BeTrue();
    }

    [Fact(DisplayName = "Adding a new command marks a change")]
    public void AddNewCommandMarksChanged()
    {
        ConfigKeyMapping keys = new();
        keys.Add(Key.A, "yes");
        keys.ClearChanged();
        keys.Changed.Should().BeFalse();

        keys.Add(Key.A, "hi");
        keys.Changed.Should().BeTrue();
    }

    [Fact(DisplayName = "Iterate over all keys")]
    public void CanIterateOverKeys()
    {
        Dictionary<Key, List<string>> expected = new()
        {
            [Key.C] = new List<string>{ "something", "another" },
            [Key.F] = new List<string>{ "something" }
        };

        ConfigKeyMapping keys = new();
        foreach ((Key key, List<string> values) in expected)
            foreach (string value in values)
                keys.Add(key, value);

        var mapping = keys.GetKeyMapping();
        Dictionary<Key, List<string>> actual = new();
        foreach (var cmd in mapping)
        {
            if (actual.TryGetValue(cmd.Key, out var list))
            {
                list.Add(cmd.Command);
                continue;
            }

            list = new() { cmd.Command };
            actual[cmd.Key] = list;
        }
        actual.Count.Should().Be(expected.Count);

        foreach ((Key key, List<string> values) in expected)
            actual[key].Should().Equal(values);
    }

    private static IList<string> GetKeyCommands(ConfigKeyMapping keys, Key key)
    {
        List<string> commands = new();
        foreach (var item in keys.GetKeyMapping())
        {
            if (item.Key == key)
                commands.Add(item.Command);
        }

        return commands;
    }

    private static IList<Key> GetCommandKeys(ConfigKeyMapping keys, string command)
    {
        List<Key> key = new();
        foreach (var item in keys.GetKeyMapping())
        {
            if (item.Command.Equals(command, System.StringComparison.OrdinalIgnoreCase))
                key.Add(item.Key);
        }

        return key;
    }
}

