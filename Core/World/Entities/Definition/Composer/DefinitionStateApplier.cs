using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition.States;
using MoreLinq.Extensions;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Definition.Composer
{
    public static class DefinitionStateApplier
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Apply(EntityDefinition definition, LinkedList<ActorDefinition> actorDefinitions)
        {
            Precondition(actorDefinitions.Count >= 2, $"Expected state applier to have the base actor definition and the new definition in {definition.Name}");

            // We need a table that remembers both the current and previous
            // label locations. We will store everything in the name of:
            //
            //    ACTORNAME::LABEL
            //
            // and we will also store the labels as well like:
            //
            //    LABEL
            //
            // which are eligible to be overwritten.
            Dictionary<string, int> masterLabelTable = new Dictionary<string, int>();
            
            // We always have to apply the first definition to it, which should
            // be the Actor class. However to reduce code duplication, we'll be
            // passing itself as the parent. There should be no recursion nor
            // should there be any Super::Label goto's, so this is okay unless
            // the user has done something critically wrong.
            ActorDefinition baseActor = actorDefinitions.First.Value;
            ApplyActorDefinition(definition, baseActor, baseActor, masterLabelTable);
            
            actorDefinitions.Window(2).ForEach(list =>
            {
                ActorDefinition parent = list[0];
                ActorDefinition current = list[1];
                ApplyActorDefinition(definition, current, parent, masterLabelTable);
            });

            // Now that all the labels have been handled/added/pruned/linked,
            // we can add them in safely at the end.
            ApplyAllLabels(definition, masterLabelTable);
        }

        private static void ApplyAllLabels(EntityDefinition definition, Dictionary<string, int> masterLabelTable)
        {
            masterLabelTable.ForEach(pair => definition.States.Labels[pair.Key] = pair.Value);
        }

        private static void AddFrameAndNonGotoFlowControl(EntityDefinition definition, ActorDefinition current,
            Dictionary<string, int> masterLabelTable, int startingFrameOffset, List<UnresolvedGotoFrame> unresolvedGotoFrames)
        {
            // The following are used for knowing where to jump back to if we
            // encounter the `loop` control flow.
            int lastLabelIndex = 0;
            HashSet<int> indicesWithLabels = new HashSet<int>();
            current.States.Labels.Values.ForEach(index => indicesWithLabels.Add(index));

            for (int localFrameOffset = 0; localFrameOffset < current.States.Frames.Count; localFrameOffset++)
            {
                int absoluteFrameOffset = startingFrameOffset + localFrameOffset;
                
                ActorFrame frame = current.States.Frames[localFrameOffset];
                if (indicesWithLabels.Contains(localFrameOffset))
                    lastLabelIndex = absoluteFrameOffset;

                int absoluteNextFrameIndex = absoluteFrameOffset + 1;
                Action<Entity>? actionFunction = EntityActionFunctions.Find(frame.ActionFunction?.FunctionName);
                EntityFrame entityFrame = new EntityFrame(frame.Sprite, frame.Frame, frame.Ticks, actionFunction, absoluteNextFrameIndex);

                HandleNonGotoFlowControl(frame, entityFrame, absoluteFrameOffset, lastLabelIndex, unresolvedGotoFrames);
                
                definition.States.Frames.Add(entityFrame);
            }
        }

        private static void HandleNonGotoFlowControl(ActorFrame frame, EntityFrame entityFrame,
            int absoluteFrameOffset, int lastLabelIndex, IList<UnresolvedGotoFrame> unresolvedGotoFrames)
        {
            if (frame.FlowControl == null) 
                return;
            
            ActorFlowControl flowControl = frame.FlowControl.Value;
            if (flowControl.FlowType == ActorStateBranch.Goto)
            {
                unresolvedGotoFrames.Add(new UnresolvedGotoFrame(entityFrame, frame));
                return;
            }
            
            entityFrame.BranchType = flowControl.FlowType;

            switch (flowControl.FlowType)
            {
            case ActorStateBranch.Loop:
                entityFrame.NextFrameIndex = lastLabelIndex;
                break;
            case ActorStateBranch.Fail:
            case ActorStateBranch.Stop:
            case ActorStateBranch.Wait:
                entityFrame.NextFrameIndex = absoluteFrameOffset;
                break;
            }
        }

        private static void AddLabelsToMasterTable(ActorDefinition definition, Dictionary<string, int> masterLabelTable,
            string upperActorName, int startingFrameOffset)
        {
            definition.States.Labels.ForEach(pair =>
            {
                string upperLabel = pair.Key.ToString().ToUpper();
                masterLabelTable[upperLabel] = startingFrameOffset + pair.Value;
                
                string upperFullLabel = $"{upperActorName}::{upperLabel}";
                masterLabelTable[upperFullLabel] = startingFrameOffset + pair.Value;
            });
        }

        private static void PurgeAnyControlFlowStopOverride(ActorDefinition current, 
            Dictionary<string, int> masterLabelTable)
        {
            current.States.FlowOverrides.ForEach(pair =>
            {
                if (pair.Value.BranchType == ActorStateBranch.Stop)
                    RemoveAllEndingMatchKeys(pair.Value.Label.ToUpper());
            });

            void RemoveAllEndingMatchKeys(string upperSuffix)
            {
                HashSet<string> keysToRemove = new HashSet<string>();
                
                masterLabelTable.ForEach(pair =>
                {
                    if (pair.Key.EndsWith(upperSuffix))
                        keysToRemove.Add(pair.Key);
                });
                
                keysToRemove.ForEach(key => masterLabelTable.Remove(key));
            }
        }

        private static void ApplyGotoOffsets(IEnumerable<UnresolvedGotoFrame> unresolvedGotoFrames, 
            Dictionary<string, int> masterLabelTable, string upperParentName, EntityDefinition definition)
        {
            foreach (UnresolvedGotoFrame unresolved in unresolvedGotoFrames)
            {
                ActorFrame actorFrame = unresolved.ActorFrame;
                if (actorFrame.FlowControl == null)
                {
                    Log.Error("Unexpected 'unresolved' flow control handling (contact a developer with the current files!)");
                    continue;
                }
                
                EntityFrame entityFrame = unresolved.EntityFrame;
                ActorFlowControl flowControl = actorFrame.FlowControl.Value;
                string upperLabel = flowControl.Label.ToUpper();
                string upperParent = flowControl.Parent;
                
                if (upperParent.Empty())
                {
                    if (masterLabelTable.TryGetValue(upperLabel, out int offset))
                        entityFrame.NextFrameIndex = offset;
                    else
                        Log.Error("Unable to resolve goto label '{0}' in actor '{1}', actor is likely malformed", upperLabel, definition.Name);
                    continue;
                }
                
                string targetLabel = $"{upperParent}::{upperLabel}";
                if (upperParent == "SUPER")
                    targetLabel = $"{upperParentName}::{upperLabel}";
                
                if (masterLabelTable.TryGetValue(targetLabel, out int parentOffset))
                    entityFrame.NextFrameIndex = parentOffset;
                else
                    Log.Error("Unable to resolve inheritance goto label '{0}' in actor '{1}', actor is likely malformed", targetLabel, definition.Name);
            }
        }

        private static void ApplyActorDefinition(EntityDefinition definition, ActorDefinition current,
            ActorDefinition parent, Dictionary<string, int> masterLabelTable)
        {
            string upperParentName = parent.Name.ToString().ToUpper();
            string upperCurrentName = current.Name.ToString().ToUpper();
            int startingFrameOffset = definition.States.Frames.Count;
            List<UnresolvedGotoFrame> unresolvedGotoFrames = new List<UnresolvedGotoFrame>();
            
            AddFrameAndNonGotoFlowControl(definition, current, masterLabelTable, startingFrameOffset, unresolvedGotoFrames);
            AddLabelsToMasterTable(current, masterLabelTable, upperCurrentName, startingFrameOffset);
            PurgeAnyControlFlowStopOverride(current, masterLabelTable);
            ApplyGotoOffsets(unresolvedGotoFrames, masterLabelTable, upperParentName, definition);
        }
    }
}