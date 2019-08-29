using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Decorate.Parser
{
    public class DecorateParser : ParserBase
    {
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
            return frame == '#' || frame == '_' || frame == '\\' ||
                   (frame >= '[' && frame <= ']') ||
                   (frame >= '0' && frame <= '9') ||
                   (frame >= 'A' && frame <= 'Z') ||
                   (frame >= 'a' && frame <= 'z');
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
                ConsumeActorProperty();
        }

        private void ConsumeActorFlag()
        {
            bool setFlag = false;
            if (ConsumeIf('+'))
                setFlag = true;
            else
                Consume('-');

            string flagName = ConsumeIdentifier();
            // TODO: Set the flag.
        }

        private void ConsumeActorProperty()
        {
            // TODO
            ThrowException("Actor property parser not supported");
        }

        private void CreateActorStateLabel(string label)
        {
            m_currentDefinition.States.Labels[label] = m_frameIndex;
        }

        private int ConsumeActorFrameTicks()
        {
            if (ConsumeIf("random"))
            {
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
                
            return ConsumeInteger();
        }

        private ActorFrameProperties ConsumeActorFrameKeywordsIfAny()
        {
            ActorFrameProperties properties = new ActorFrameProperties();
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

        private void ConsumeActorStateElement()
        {
            string text = ConsumeString();
            if (ConsumeIf(':'))
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