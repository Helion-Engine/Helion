using Helion.Graphics.String;
using Helion.Util.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Util
{
    /// <summary>
    /// A console object that accepts input, emits console commands, and will
    /// be able to register for log messages to track.
    /// </summary>
    /// <remarks>
    /// This class is not intended to handle any rendering. Its only job is to
    /// be a medium for user pressed characters and messages from a variety of
    /// message emitters (ex: loggers).
    /// </remarks>
    public class Console
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// How many console messages wil be logged. Any more than this will
        /// cause older messages to be removed.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// The current location of the input caret. This will be between the
        /// range of [0, length]. Note that the upper bound is inclusive!
        /// </summary>
        /// <remarks>
        /// Because the upper bound is inclusive, it is unsafe to use this as a
        /// direct index into the 
        /// </remarks>
        public int InputCaretPosition { get; private set; } = 0;

        /// <summary>
        /// All the messages that have been received thus far.
        /// </summary>
        /// <remarks>
        /// This will never exceed <see cref="Capacity"/>. Any messages at the
        /// end of the list will be removed once this grows past the capcity
        /// value.
        /// </remarks>
        public readonly LinkedList<ColoredString> Messages = new LinkedList<ColoredString>();

        /// <summary>
        /// Gets the current input.
        /// </summary>
        /// <remarks>
        /// This causes a copy allocation of the current input text.
        /// </remarks>
        public string Input => input.ToString();

        /// <summary>
        /// The event handler that emits console commands on user input.
        /// </summary>
        public event EventHandler<ConsoleCommandEventArgs> ConsoleCommandEmitter;

        private readonly StringBuilder input = new StringBuilder();

        private static bool IsTextCharacter(char c) => c >= 32 && c < 127;
        private static bool IsBackspaceCharacter(char c) => c == 8;
        private static bool IsInputSubmissionCharacter(char c) => c == '\n' || c == '\r';

        private void RemoveExcessMessagesIfAny()
        {
            while (Messages.Count > Capacity)
                Messages.RemoveLast();
        }

        private void RemoveInputCharacter()
        {
            if (input.Length > 0)
                input.Remove(input.Length - 1, 1);
        }

        /// <summary>
        /// Clears the input text.
        /// </summary>
        public void ClearInputText()
        {
            input.Clear();
            InputCaretPosition = 0;
        }

        /// <summary>
        /// Submits the current input text by firing an event and clears the 
        /// input.
        /// </summary>
        public void SubmitInputText()
        {
            string inputText = input.ToString();
            ClearInputText();

            if (inputText.NotEmpty())
            {
                log.Info(inputText);
                ConsoleCommandEmitter?.Invoke(this, new ConsoleCommandEventArgs(inputText));
            }
        }

        /// <summary>
        /// Adds a new message to the console.
        /// </summary>
        /// <remarks>
        /// If this message causes the console to exceed the capacity, then it
        /// will remove the older messages to make space for this message.
        /// </remarks>
        /// <param name="message">The message to add.</param>
        public void AddMessage(string message)
        {
            ColoredString coloredStr = RGBColoredStringDecoder.Decode(message);
            Messages.AddFirst(coloredStr);
            RemoveExcessMessagesIfAny();
        }

        /// <summary>
        /// Adds a single character to the input.
        /// </summary>
        /// <remarks>
        /// Invalid characters are not supported. For example, adding a null
        /// terminator will cause nothing to happen.
        /// </remarks>
        /// <param name="c">The character to add.</param>
        public void AddInput(char c)
        {
            if (IsInputSubmissionCharacter(c))
                SubmitInputText();
            else if (IsBackspaceCharacter(c))
                RemoveInputCharacter();
            else if (IsTextCharacter(c))
                input.Append(c);
        }

        /// <summary>
        /// Adds the provided string to the input.
        /// </summary>
        /// <remarks>
        /// See <see cref="AddInput(char)"/> for further remarks.
        /// </remarks>
        /// <param name="text">The text to add.</param>
        public void AddInput(string text)
        {
            Array.ForEach(text.ToCharArray(), AddInput);
        }

        /// <summary>
        /// Sets the capacity. If it is smaller than the number of existing
        /// strings, it will prune the older ones.
        /// </summary>
        /// <remarks>
        /// This will not let you enter a negative or zero
        /// </remarks>
        /// <param name="capacity"></param>
        public void SetCapacity(int capacity)
        {
            Assert.Precondition(capacity > 0, "Should never be trying to set a non-positive capcity to the console");

            Capacity = Math.Max(1, capacity);
            RemoveExcessMessagesIfAny();
        }
    }

    /// <summary>
    /// An event fired by a console when the user submits an 'enter' character.
    /// </summary>
    public class ConsoleCommandEventArgs : EventArgs
    {
        /// <summary>
        /// The case insensitive command this event is.
        /// </summary>
        /// <remarks>
        /// This is always the first string in the command. For example, if the
        /// console was firing out "map map01" then the command would be "MAP".
        /// </remarks>
        public readonly UpperString Command = "";

        /// <summary>
        /// The arguments (if any) that came with the command.
        /// </summary>
        public readonly List<string> Args = new List<string>();

        public ConsoleCommandEventArgs(string text)
        {
            Assert.Precondition(text.NotEmpty(), "Should not be getting an empty console command");

            string[] tokens = text.Split(' ');
            if (tokens.Length == 0)
                return;

            Command = tokens[0];
            for (int i = 1; i < tokens.Length; i++)
                Args.Add(tokens[i]);
        }

        public override string ToString() => $"{Args} [{string.Join(", ", Args.ToArray())}]";
    }
}
