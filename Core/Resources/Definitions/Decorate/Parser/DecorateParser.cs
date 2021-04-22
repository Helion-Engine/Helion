using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Parser;
using MoreLinq;
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
        public readonly Dictionary<string, double> Variables = new(StringComparer.OrdinalIgnoreCase);
        protected readonly string Path;
        protected readonly Func<string, string?> IncludeResolver;
        private ActorDefinition m_currentDefinition = new ActorDefinition("none", null, null, null);
        private int m_frameIndex;
        private string? m_immediatelySeenLabel;

        public DecorateParser(string path, Func<string, string?> includeResolver)
        {
            Path = path;
            IncludeResolver = includeResolver;
        }
        
        protected override void PerformParsing()
        {
            while (!Done)
            {
                if (ConsumeIf('#'))
                    ConsumeInclude();
                else if (Peek("const"))
                    ConsumeVariable();
                else if (Peek("enum"))
                    ConsumeEnum();
                else
                    ConsumeActorDefinition();
            }
            
            // TODO: Should remove duplicate definitions (same name).
        }

        private void ConsumeVariable()
        {
            throw MakeException("Variables not supported in decorate currently");
        }

        private void ConsumeEnum()
        {
            throw MakeException("Enums not supported in decorate currently");
        }

        private void MergeWithParsedData(DecorateParser parser)
        {
            parser.ActorDefinitions.ForEach(ActorDefinitions.Add);
            parser.Variables.ForEach(pair => Variables[pair.Key] = pair.Value);
        }

        private void ConsumeInclude()
        {
            Token? token = GetCurrentToken();
            if (token == null)
                throw MakeException("Expected value to follow preprocessor include symbol");
            
            Consume("include");
            string includePath = ConsumeString();

            string? includeText = IncludeResolver.Invoke(includePath);
            if (includeText == null)
                throw new ParserException(token.Value, $"Could not locate include at {includePath}");
            
            DecorateParser parser = new DecorateParser(includePath, IncludeResolver);
            if (!parser.Parse(includeText))
                throw new ParserException(token.Value, $"Failed to parse include path at {includePath}");

            MergeWithParsedData(parser);
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
            
            string? parent = null;
            if (ConsumeIf(':'))
                parent = ConsumeString().ToUpper();

            string? replacesName = null;
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
            m_immediatelySeenLabel = null;
            
            Consume('{');
            InvokeUntilAndConsume('}', ConsumeActorStateElement);

            // For whatever reason, default crush state isn't in decorate (which would probably make more sense...)
            // Check for monster / player definition and add the default crush state here
            if (!m_currentDefinition.States.Labels.ContainsKey(Constants.ActorCrushState) &&
                ((m_currentDefinition.Flags.Monster.HasValue && m_currentDefinition.Flags.Monster.Value) || m_currentDefinition.Name.Equals(Constants.PlayerClass, StringComparison.OrdinalIgnoreCase)))
            {
                ActorFrame actorFrame = new ActorFrame("POL5", 0, -1, new ActorFrameProperties(), null)
                {
                    FlowControl = new ActorFlowControl(ActorStateBranch.Stop)
                };
                m_currentDefinition.States.Frames.Add(actorFrame);
                m_frameIndex++;

                m_currentDefinition.States.Labels.Add(Constants.ActorCrushState, m_frameIndex - 1);
            }
        }
    }
}