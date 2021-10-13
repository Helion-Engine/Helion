using FluentAssertions;
using Helion.Util.Consoles;
using Xunit;

namespace Helion.Tests.Unit.Util.Consoles;

public class HelionConsoleTest
{
    [Fact(DisplayName = "Can add messages to the console")]
    public void AddMessages()
    {
        using HelionConsole console = new();

        console.AddMessage("hello!");

        console.Messages.Count.Should().Be(1);
        console.Messages.First!.Value.Message.ToString().Should().Be("hello!");
    }

    [Fact(DisplayName = "Add input to console")]
    public void AddInput()
    {
        using HelionConsole console = new();

        console.AddInput("yes hi");

        console.Input.Should().Be("yes hi");
    }

    [Fact(DisplayName = "Submit console input with new lines")]
    public void SubmitInputWithNewlines()
    {
        using HelionConsole console = new();
        using var monitor = console.Monitor();

        console.AddInput("stuff a b\n");

        console.Input.Should().BeEmpty();

        monitor.Should().Raise("OnConsoleCommandEvent");
        var eventObject = monitor.OccurredEvents[0];
        eventObject.Parameters[0].Should().BeSameAs(console);
        eventObject.Parameters[1].Should().BeOfType<ConsoleCommandEventArgs>();
        ConsoleCommandEventArgs args = (ConsoleCommandEventArgs)eventObject.Parameters[1];
        args.Command.Should().Be("stuff");
        args.Args.Should().Equal("a", "b");
    }

    [Fact(DisplayName = "Submit console input manually")]
    public void SubmitInput()
    {
        using HelionConsole console = new();
        using var monitor = console.Monitor();

        console.AddInput("stuff a b");
        console.SubmitInputText();

        console.Input.Should().BeEmpty();

        monitor.Should().Raise("OnConsoleCommandEvent");
        var eventObject = monitor.OccurredEvents[0];
        eventObject.Parameters[0].Should().BeSameAs(console);
        eventObject.Parameters[1].Should().BeOfType<ConsoleCommandEventArgs>();
        ConsoleCommandEventArgs args = (ConsoleCommandEventArgs)eventObject.Parameters[1];
        args.Command.Should().Be("stuff");
        args.Args.Should().Equal("a", "b");
    }

    [Fact(DisplayName = "Clear console input")]
    public void ClearInput()
    {
        using HelionConsole console = new();
        using var monitor = console.Monitor();

        console.AddInput("stuff a b");
        console.Input.Should().NotBeEmpty();

        console.ClearInputText();
        console.Input.Should().BeEmpty();
    }
}
