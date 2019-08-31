using System;
using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Parser;
using NLog;

namespace Helion.Resources.Definitions.Decorate.Parser
{
    /// <summary>
    /// Parses decorate text data into usable definition.
    /// </summary>
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

        private void ConsumeActorStates()
        {
            m_frameIndex = 0;
            
            Consume('{');
            InvokeUntilAndConsume('}', ConsumeActorStateElement);
        }
    }
}