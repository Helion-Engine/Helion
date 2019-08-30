using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using NLog;

namespace Helion.Resources.Definitions.Decorate.Parser
{
    public partial class DecorateParser : ParserBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly IList<ActorDefinition> ActorDefinitions = new List<ActorDefinition>();
        private ActorDefinition m_currentDefinition = new ActorDefinition("none", null, null, null);
        private int m_frameIndex;
        
        protected override void PerformParsing()
        {
            while (!Done)
            {
                if (Peek('#'))
                    ConsumeInclude();
                else if (Peek("const"))
                    ConsumeVariable();
                else if (Peek("enum"))
                    ConsumeEnum();
                else
                    ConsumeActorDefinition();
            }
        }
        
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
        
        private void ConsumeVariable()
        {
            ThrowException("Variables not supported in decorate currently");
        }

        private void ConsumeEnum()
        {
            ThrowException("Enums not supported in decorate currently");
        }

        private void ConsumeInclude()
        {
            // TODO: We need to do preprocessing instead
            ThrowException("Enums not supported in decorate currently");
        }

        private void ConsumeActorDefinition()
        {
            Consume("actor");
            ConsumeActorHeader();
            Consume('{');
            InvokeUntilAndConsume('}', ConsumeActorBodyComponent);
            
            ActorDefinitions.Add(m_currentDefinition);
        }

        private void ConsumeActorHeader()
        {
            string name = ConsumeString().ToUpper();
            
            CIString? parent = null;
            if (ConsumeIf(':'))
                parent = ConsumeString().ToUpper();

            CIString? replacesName = null;
            if (ConsumeIf("replaces"))
                replacesName = ConsumeString().ToUpper();
            
            int? editorId = ConsumeIfInt();
            
            m_currentDefinition = new ActorDefinition(name, parent, replacesName, editorId);
        }

        private void ConsumeActorBodyComponent()
        {
            if (Peek('+') || Peek('-'))
                ConsumeActorFlag();
            else if (ConsumeIf("states"))
                ConsumeActorStates();
            else
                ConsumeActorPropertyOrCombo();
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
                ThrowException("No negative tick durations allowed (unless it is -1)");
            return tickAmount;
        }

        private ActorFrameProperties ConsumeActorFrameKeywordsIfAny()
        {
            ActorFrameProperties properties = new ActorFrameProperties();
            
            // These can probably come in any order, so we'll need a looping
            // dictionary. Apparently we have to watch out for new lines when
            // dealing with `fast` and `slow`.
            // TODO: Bright
            // TODO: CanRaise
            // TODO: Fast
            // TODO: Light("")
            // TODO: NoDelay
            // TODO: Offset(x, y)
            // TODO: Slow
            
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
                    ThrowException($"Invalid actor frame letter: {frame} (ascii ordinal {(int)frame})");
                
                ActorFrame actorFrame = new ActorFrame(sprite, frame, ticks, properties, actionFunction);
                m_currentDefinition.States.Frames.Add(actorFrame);
                m_frameIndex++;
            }
        }

        private ActionFunction? ConsumeActionFunctionIfAny()
        {
            string? text = PeekNextText();
            if (text == null)
                return null;
            
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
                ThrowException("Cannot have a flow control label when no frames were defined");

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

        private void ConsumeActorStates()
        {
            m_frameIndex = 0;
            
            Consume('{');
            InvokeUntilAndConsume('}', ConsumeActorStateElement);
        }
    }
}