using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry;

namespace Helion.Resources.Definitions.Decorate.Parser
{
    /// <summary>
    /// Handles parsing of the states section of a definition.
    /// </summary>
    public partial class DecorateParser
    {
        private static readonly HashSet<string> FramePropertyKeywords = new HashSet<string>
        {
            "BRIGHT", "CANRAISE", "FAST", "LIGHT", "NODELAY", "OFFSET", "SLOW",
        };

        private static bool IsValidFrameLetter(char frame)
        {
            return frame == '#' || frame == '-' || frame == '_' || frame == '\\' ||
                   (frame >= '[' && frame <= ']') ||
                   (frame >= '0' && frame <= '9') ||
                   (frame >= 'A' && frame <= 'Z') ||
                   (frame >= 'a' && frame <= 'z');
        }

        private static bool TryGetStateBranch(string text, out ActorStateBranch branchType)
        {
            switch (text.ToUpper())
            {
            case "FAIL":
                branchType = ActorStateBranch.Fail;
                return true;
            case "GOTO":
                branchType = ActorStateBranch.Goto;
                return true;
            case "LOOP":
                branchType = ActorStateBranch.Loop;
                return true;
            case "STOP":
                branchType = ActorStateBranch.Stop;
                return true;
            case "WAIT":
                branchType = ActorStateBranch.Wait;
                return true;
            }

            branchType = ActorStateBranch.None;
            return false;
        }
        
        private void CreateActorStateLabel(string label)
        {
            m_currentDefinition.States.Labels[label] = m_frameIndex;
        }

        private int ConsumeActorFrameTicks()
        {
            if (ConsumeIf("random"))
            {
                // We don't check for negative numbers because it's probably
                // not allowed.
                Consume('(');
                int low = ConsumeInteger();
                Consume(',');
                int high = ConsumeInteger();
                Consume(')');
                
                // Right now we don't support random. I don't know if we ever
                // want to until a lot of stuff is implemented because it would
                // be a pain to do prediction with. Therefore we'll just take
                // the average of it.
                (int min, int max) = MathHelper.MinMax(low, high);
                return (min + max) / 2;
            }
                
            int tickAmount = ConsumeSignedInteger();
            if (tickAmount < -1)
                throw MakeException($"No negative tick durations allowed (unless it is -1) on actor '{m_currentDefinition.Name}'");
            return tickAmount;
        }

        private ActorFrameProperties ConsumeActorFrameKeywordsIfAny()
        {
            ActorFrameProperties properties = new ActorFrameProperties();
            
            // These can probably come in any order, so we'll need a looping
            // dictionary. Apparently we have to watch out for new lines when
            // dealing with `fast` and `slow`.
            while (true)
            {
                string? value = PeekCurrentText();
                if (value == null)
                    throw MakeException($"Ran out of tokens when reading actor frame properties on actor '{m_currentDefinition.Name}'");

                string upperKeyword = value.ToUpper();
                if (!FramePropertyKeywords.Contains(upperKeyword))
                    break;

                // Note: In the future we will need to possibly *not* consume
                // if its 'fast' or 'slow' and on another line since that may
                // be a valid sprite frame. This is why we moved the string
                // consumer into each block because some of them might not do
                // consumption (whenever we get around to implementing that).
                switch (upperKeyword)
                {
                case "BRIGHT":
                    ConsumeString();
                    properties.Bright = true;
                    break; 
                case "CANRAISE":
                    ConsumeString();
                    properties.CanRaise = true;
                    break; 
                case "FAST":
                    // TODO: Make sure it's on the same line (ex: if it's a sprite).
                    ConsumeString();
                    properties.Fast = true;
                    break; 
                case "LIGHT":
                    ConsumeString();
                    Consume('(');
                    properties.Light = ConsumeString();
                    Consume(')');
                    break; 
                case "NODELAY":
                    ConsumeString();
                    properties.NoDelay = true;
                    break; 
                case "OFFSET":
                    ConsumeString();
                    Consume('(');
                    int x = ConsumeSignedInteger();
                    Consume(',');
                    int y = ConsumeSignedInteger();
                    Consume(')');
                    properties.Offset = new Vec2I(x, y);
                    break; 
                case "SLOW":
                    ConsumeString();
                    // TODO: Make sure it's on the same line (ex: if it's a sprite).
                    properties.Slow = true;
                    break;
                }
            }

            return properties;
        }

        private void ConsumeActorStateFrames(string sprite)
        {
            string frames = ConsumeString();
            int ticks = ConsumeActorFrameTicks();
            ActorFrameProperties properties = ConsumeActorFrameKeywordsIfAny();
            ActionFunction? actionFunction = ConsumeActionFunctionIfAny();

            foreach (char frame in frames)
            {
                if (!IsValidFrameLetter(frame))
                    throw MakeException($"Invalid actor frame letter: {frame} (ascii ordinal {(int)frame})");
                
                ActorFrame actorFrame = new ActorFrame(sprite, frame, ticks, properties, actionFunction);
                m_currentDefinition.States.Frames.Add(actorFrame);
                m_frameIndex++;
            }
        }

        private ActionFunction? ConsumeActionFunctionIfAny()
        {
            string? text = PeekCurrentText();
            if (text == null)
                throw MakeException($"Ran out of tokens when reading an actor frame action function on actor '{m_currentDefinition.Name}'");
            
            // It is possible that no such action function exists and we would
            // be reading a label or frame.
            if (!text.StartsWith("A_", StringComparison.OrdinalIgnoreCase))
                return null;

            string functionName = ConsumeIdentifier();
            // TODO: Support reading/processing arguments.
            
            return new ActionFunction(functionName);
        }

        private void ApplyStateBranch(ActorStateBranch branchType)
        {
            if (m_currentDefinition.States.Frames.Empty())
                throw MakeException("Cannot have a flow control label when no frames were defined");

            ActorFrame frame = m_currentDefinition.States.Frames.Last();
            
            if (branchType != ActorStateBranch.Goto)
            {
                frame.FlowControl = new ActorFlowControl(branchType);
                return;
            }

            string parent = "";
            string label = ConsumeIdentifier();
            int offset = 0;

            if (ConsumeIf(':'))
            {
                Consume(':');
                parent = label;
                label = ConsumeString();
            }

            if (ConsumeIf('+'))
                offset = ConsumeInteger();

            frame.FlowControl = new ActorFlowControl(branchType, parent, label, offset);
        }

        private void ConsumeActorStateElement()
        {
            string text = ConsumeString();
            if (TryGetStateBranch(text, out ActorStateBranch branchType))
                ApplyStateBranch(branchType);
            else if (ConsumeIf(':'))
                CreateActorStateLabel(text);
            else
                ConsumeActorStateFrames(text);
        }
    }
}