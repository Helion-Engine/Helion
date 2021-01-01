using System;
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

namespace Helion.Util.Terminals
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
    public class Terminal : Target
    {
        private const string TargetName = "HelionConsole";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly Color DebugColor = Color.FromArgb(255, 128, 255, 255);
        private static readonly Color TraceColor = Color.FromArgb(255, 200, 255, 255);

        /// <summary>
        /// All the messages that have been received thus far.
        /// </summary>
        /// <remarks>
        /// This will never exceed <see cref="m_capacity"/>. Any messages at the
        /// end of the list will be removed once this grows past the capacity
        /// value.
        /// </remarks>
        public readonly LinkedList<ConsoleMessage> Messages = new();

        /// <summary>
        /// A list of all the input that has been submitted. This allows us to
        /// get the commands we've sent in the past. The front of the list is
        /// the most recent command.
        /// </summary>
        /// <remarks>
        /// This will never grow beyond <see cref="m_capacity"/> in length.
        /// </remarks>
        public readonly LinkedList<string> SubmittedInput = new();

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
        public string Input => m_input.ToString();

        /// <summary>
        /// The event handler that emits console commands on user input.
        /// </summary>
        public event EventHandler<ConsoleCommandEventArgs>? OnConsoleCommandEvent;

        private readonly Config m_config;
        private readonly StringBuilder m_input = new();
        private int m_capacity;
        private bool m_disposed;

        public Terminal(Config cfg)
        {
            Name = TargetName;
            m_config = cfg;
            m_capacity = m_config.Engine.Console.MaxMessages;

            m_config.Engine.Console.MaxMessages.OnChanged += OnMaxMessagesChanged;

            AddToLogger();
        }

        ~Terminal()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        /// <summary>
        /// Removes an input character, if any.
        /// </summary>
        public void RemoveInputCharacter()
        {
            if (m_input.Length > 0)
                m_input.Remove(m_input.Length - 1, 1);
        }

        /// <summary>
        /// Clears the input text.
        /// </summary>
        public void ClearInputText()
        {
            m_input.Clear();
        }

        /// <summary>
        /// Submits the current input text by firing an event and clears the
        /// input.
        /// </summary>
        public void SubmitInputText()
        {
            string inputText = m_input.ToString();
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
                m_input.Append(c);
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
        /// Tries to autocomplete, printing out help messages as needed.
        /// </summary>
        public void ApplyAutocomplete()
        {
            string lowerInput = Input.Empty() ? "*" : Input.ToLower();

            // TODO: use m_config.
            AddMessage("Autocomplete TODO!");
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
            m_capacity = Math.Max(1, maxMsgEvent.NewValue);
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
            while (Messages.Count > m_capacity)
                Messages.RemoveLast();
        }

        private void CacheSubmittedInput(string inputText)
        {
            RemoveExcessSubmittedInputIfAny();
            SubmittedInput.AddFirst(inputText);
        }

        private void RemoveExcessSubmittedInputIfAny()
        {
            while (SubmittedInput.Count > m_capacity)
                SubmittedInput.RemoveLast();
        }

        public new void Dispose()
        {
            GC.SuppressFinalize(this);
            base.Dispose();
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            m_config.Engine.Console.MaxMessages.OnChanged -= OnMaxMessagesChanged;

            // TODO: Investigate whether this is correct or not, the logger
            // documentation isn't clear and stack overflow has some unusual
            // results for how to properly remove the logger.
            // The logger stops logging to this target after we dispose of
            // this object, but I'd like to make sure that it's foolproof.
            RemoveLogger();

            m_disposed = true;
        }
    }
}
