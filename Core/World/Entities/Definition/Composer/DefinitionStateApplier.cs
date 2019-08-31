using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition.States;
using MoreLinq.Extensions;
using NLog;

namespace Helion.World.Entities.Definition.Composer
{
    public static class DefinitionStateApplier
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Apply(EntityDefinition definition, ActorStates states)
        {
            AddActorLabelsToDefinition(states, definition);

            // The following are used for knowing where to jump back to if we
            // encounter the `loop` control flow.
            int lastLabelIndex = 0;
            HashSet<int> indicesWithLabels = definition.States.Labels.Values.ToHashSet();

            for (int frameIndex = 0; frameIndex < states.Frames.Count; frameIndex++)
            {
                ActorFrame frame = states.Frames[frameIndex];
                if (indicesWithLabels.Contains(frameIndex))
                    lastLabelIndex = frameIndex;
                
                Action<Entity>? actionFunction = EntityActionFunctions.Find(frame.ActionFunction?.FunctionName);
                EntityFrame entityFrame = new EntityFrame(frame.Sprite, frame.Frame, frame.Ticks, actionFunction, frameIndex + 1);
                SetOffsetsAndFlowControl(definition, frame, entityFrame, frameIndex, lastLabelIndex);
                
                definition.States.Frames.Add(entityFrame);
            }
            
            // TODO: Do sanity check on offsets and labels.
        }

        private static void AddActorLabelsToDefinition(ActorStates states, EntityDefinition definition)
        {
            states.Labels.ForEach(pair => definition.States.Labels[pair.Key] = pair.Value);
        }

        private static void HandleMissingGotoLabel(EntityFrame frame, EntityDefinition definition)
        {
            Log.Error($"Actor '{definition.Name}' missing label for goto branch, rerouting to base frame (will likely break the entity!)");
            frame.NextFrameIndex = 0;
        }

        private static void SetOffsetsAndFlowControl(EntityDefinition definition, ActorFrame frame, 
            EntityFrame entityFrame, int currentFrameIndex, int lastLabelIndex)
        {
            if (frame.FlowControl == null) 
                return;
            
            ActorFlowControl flowControl = frame.FlowControl.Value;
            entityFrame.BranchType = flowControl.FlowType;

            switch (frame.FlowControl.Value.FlowType)
            {
            case ActorStateBranch.Goto:
                if (!flowControl.Parent.Empty())
                    throw new NotImplementedException($"Flow control from actor '{definition.Name}' to parent via '{flowControl.Parent}' not supported yet");
                if (definition.States.Labels.TryGetValue(flowControl.Label, out int offset))
                    entityFrame.NextFrameIndex = offset + flowControl.Offset;
                else
                    HandleMissingGotoLabel(entityFrame, definition);
                break;
            
            case ActorStateBranch.Loop:
                entityFrame.NextFrameIndex = lastLabelIndex;
                break;
                
            case ActorStateBranch.Fail:
            case ActorStateBranch.Stop:
            case ActorStateBranch.Wait:
                entityFrame.NextFrameIndex = currentFrameIndex;
                break;
            }
        }
    }
}