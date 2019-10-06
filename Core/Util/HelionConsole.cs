﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Helion.Graphics.String;
using Helion.Util.Configuration;
using Helion.Util.Extensions;
using Helion.Util.Time;
using NLog;
using NLog.Targets;
using static Helion.Util.Assertion.Assert;

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
    public class HelionConsole : Target
    {
        private const string TargetName = "HelionConsole";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly Color DebugColor = Color.FromArgb(255, 128, 255, 255);
        private static readonly Color TraceColor = Color.FromArgb(255, 200, 255, 255);

        /// <summary>
        /// How many console messages wil be logged. Any more than this will
        /// cause older messages to be removed. This also applies to the input
        /// message submission list.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// The current location of the input caret. This will be between the
        /// range of [0, length]. Note that the upper bound is inclusive.
        /// </summary>
        public int InputCaretPosition { get; private set; }

        /// <summary>
        /// All the messages that have been received thus far.
        /// </summary>
        /// <remarks>
        /// This will never exceed <see cref="Capacity"/>. Any messages at the
        /// end of the list will be removed once this grows past the capacity
        /// value.
        /// </remarks>
        public readonly LinkedList<ConsoleMessage> Messages = new LinkedList<ConsoleMessage>();

        /// <summary>
        /// A list of all the input that has been submitted. This allows us to
        /// get the commands we've sent in the past. The front of the list is
        /// the most recent command.
        /// </summary>
        /// <remarks>
        /// This will never grow beyond <see cref="Capacity"/> in length.
        /// </remarks>
        public readonly LinkedList<string> SubmittedInput = new LinkedList<string>();
        
        /// <summary>
        /// The clock epoch in nanoseconds when this was last closed.
        /// </summary>
        /// <remarks>
        /// This is set by other viewers as a marker when it was last closed.
        /// We need this because we don't want console messages that were just
        /// viewed in a console renderer to appear in the messages area on the
        /// screen. This should be set with something like Ticker.NanoTime()
        /// when the console is closed from a viewer.
        /// </remarks>
        public long LastClosedNanos;

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
        public event EventHandler<ConsoleCommandEventArgs>? OnConsoleCommandEvent;

        private readonly Config config;
        private readonly StringBuilder input = new StringBuilder();

        public HelionConsole(Config cfg)
        {
            Name = TargetName;
            config = cfg;
            
            Capacity = config.Engine.Console.MaxMessages;
            config.Engine.Console.MaxMessages.OnChanged += OnMaxMessagesChanged;

            AddToLogger();
        }

        ~HelionConsole()
        {
            FailedToDispose(this);
        }
        
        /// <summary>
        /// Removes an input character, if any.
        /// </summary>
        public void RemoveInputCharacter()
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

            if (inputText.Empty())
                return;

            Log.Info(inputText);
            CacheSubmittedInput(inputText);
            OnConsoleCommandEvent?.Invoke(this, new ConsoleCommandEventArgs(inputText));
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
            if (message.Empty())
                return;
            
            AddMessage(RGBColoredStringDecoder.Decode(message));
        }
        
        /// <summary>
        /// Adds a new message to the console.
        /// </summary>
        /// <remarks>
        /// If this message causes the console to exceed the capacity, then it
        /// will remove the older messages to make space for this message.
        /// </remarks>
        /// <param name="message">The message to add.</param>
        public void AddMessage(ColoredString message)
        {
            if (message.Empty)
                return;
            
            Messages.AddFirst(new ConsoleMessage(message, Ticker.NanoTime()));
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

        public new void Dispose()
        {
            config.Engine.Console.MaxMessages.OnChanged -= OnMaxMessagesChanged;
            
            // TODO: Investigate whether this is correct or not, the logger
            // documentation isn't clear and stack overflow has some unusual
            // results for how to properly remove the logger.
            // The logger stops logging to this target after we dispose of
            // this object, but I'd like to make sure that it's foolproof.
            RemoveLogger();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            // We can't switch on this because the values are not a constant.
            // Therefore we'll provide the most common levels first to avoid
            // branching evaluations.
            switch (logEvent.Level.Ordinal)
            {
            case 0:
                AddMessage(ColoredStringBuilder.From(TraceColor, logEvent.FormattedMessage));
                break;
            case 1:
                AddMessage(ColoredStringBuilder.From(DebugColor, logEvent.FormattedMessage));
                break;
            case 2:
                AddMessage(logEvent.FormattedMessage);
                break;
            case 3:
                AddMessage(ColoredStringBuilder.From(Color.Yellow, logEvent.FormattedMessage));
                break;
            default:
                if (logEvent.Level.Ordinal < 0 || logEvent.Level.Ordinal > 6)
                    Fail("Unexpected log level detected, outside of NLog ordinal range");
                AddMessage(ColoredStringBuilder.From(Color.Red, logEvent.FormattedMessage));
                break;
            }
        }

        private static bool IsTextCharacter(char c) => c >= 32 && c < 127;
        
        private static bool IsBackspaceCharacter(char c) => c == 8;
        
        private static bool IsInputSubmissionCharacter(char c) => c == '\n' || c == '\r';

        private void OnMaxMessagesChanged(object? sender, ConfigValueEvent<int> maxMsgEvent)
        {
            Capacity = Math.Max(1, maxMsgEvent.NewValue);
            RemoveExcessMessagesIfAny();
        }
        
        private void AddToLogger()
        {
            var rule = new NLog.Config.LoggingRule("*", LogLevel.Trace, this);
            LogManager.Configuration.AddTarget(TargetName, this);
            LogManager.Configuration.LoggingRules.Add(rule);
            LogManager.ReconfigExistingLoggers();
        }

        private void RemoveLogger()
        {
            LogManager.Configuration.RemoveTarget(TargetName);
        }

        private void RemoveExcessMessagesIfAny()
        {
            while (Messages.Count > Capacity)
                Messages.RemoveLast();
        }
        
        private void CacheSubmittedInput(string inputText)
        {
            RemoveExcessSubmittedInputIfAny();
            SubmittedInput.AddFirst(inputText);
        }

        private void RemoveExcessSubmittedInputIfAny()
        {
            while (SubmittedInput.Count > Capacity)
                SubmittedInput.RemoveLast();
        }
    }

    /// <summary>
    /// An event fired by a console when the user submits an 'enter' character.
    /// </summary>
    public class ConsoleCommandEventArgs : EventArgs
    {
        /// <summary>
        /// The upper case command this event is.
        /// </summary>
        /// <remarks>
        /// This is always the first string in the command. For example, if the
        /// console was firing out "map map01" then the command would be "MAP".
        /// </remarks>
        public readonly string Command = "";

        /// <summary>
        /// The arguments (if any) that came with the command.
        /// </summary>
        public readonly IList<string> Args = new List<string>();

        /// <summary>
        /// Parses the text provided into a console command event.
        /// </summary>
        /// <param name="text">The input to parse. This should not be empty.
        /// </param>
        public ConsoleCommandEventArgs(string text)
        {
            Precondition(!text.Empty(), "Should not be getting an empty console command");

            string[] tokens = text.Split(' ');
            if (tokens.Length == 0)
                return;

            Command = tokens[0];
            for (int i = 1; i < tokens.Length; i++)
                Args.Add(tokens[i]);
        }

        public override string ToString() => $"{Command} [{string.Join(", ", Args)}]";
    }

    // TODO: Move this out of the class.
    public readonly struct ConsoleMessage
    {
        public readonly ColoredString Message;
        public readonly long TimeNanos;

        public ConsoleMessage(ColoredString message, long timeNanos)
        {
            Message = message;
            TimeNanos = timeNanos;
        }
    }
}
