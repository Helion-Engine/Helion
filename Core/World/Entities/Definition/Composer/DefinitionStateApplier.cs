using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition.States;
using MoreLinq.Extensions;
using NLog;
using static Helion.World.Entities.Definition.States.EntityActionFunctions;

namespace Helion.World.Entities.Definition.Composer
{
    public static class DefinitionStateApplier
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Apply(EntityDefinition definition, LinkedList<ActorDefinition> actorDefinitions)
        {
            if (actorDefinitions.Count < 2 || actorDefinitions.First == null)
            {
                Log.Error("Missing actor base class and/or actor definition for {0} (report to a developer!)", definition.Name);
                return;
            }

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
            Dictionary<string, int> masterLabelTable = new(StringComparer.OrdinalIgnoreCase);
            
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
            int startingFrameOffset, IList<UnresolvedGotoFrame> unresolvedGotoFrames)
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
                EntityFrameProperties properties = new EntityFrameProperties(frame.Properties);
                ActionFunction? actionFunction = Find(frame.ActionFunction?.FunctionName);
                EntityFrame entityFrame = new EntityFrame(frame.Sprite, frame.Frame, frame.Ticks, properties,
                    actionFunction, absoluteNextFrameIndex);

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

        private static void AddLabelsToMasterTable(ActorDefinition definition, IDictionary<string, int> masterLabelTable,
            string upperActorName, int startingFrameOffset)
        {
            definition.States.Labels.ForEach(pair =>
            {
                masterLabelTable[pair.Key] = startingFrameOffset + pair.Value;
                
                string upperFullLabel = $"{upperActorName}::{pair.Key}";
                masterLabelTable[upperFullLabel] = startingFrameOffset + pair.Value;
            });
        }

        private static void PurgeAnyControlFlowStopOverride(ActorDefinition current, IDictionary<string, int> masterLabelTable)
        {
            current.States.FlowOverrides.ForEach(pair =>
            {
                if (pair.Value.BranchType == ActorStateBranch.Stop)
                    RemoveAllEndingMatchKeys(pair.Value.Label);
            });

            void RemoveAllEndingMatchKeys(string? suffix)
            {
                if (suffix == null)
                    return;

                HashSet<string> keysToRemove = new(StringComparer.OrdinalIgnoreCase);               
                masterLabelTable.ForEach(pair =>
                {
                    if (pair.Key.EndsWith(suffix))
                        keysToRemove.Add(pair.Key);
                });
                
                keysToRemove.ForEach(key => masterLabelTable.Remove(key));
            }
        }

        private static int FindGotoOverrideOffset(IDictionary<string, int> masterLabelTable, ActorFlowOverride flowOverride, 
            string upperImmediateParentName)
        {
            if (flowOverride.Label == null)
            {
                Log.Error("Malformed flow override offset label (report this to a developer!)");
                return 0;
            }

            int offset = flowOverride.Offset ?? 0;

            if (flowOverride.Parent == null) 
                return masterLabelTable[$"{flowOverride.Label}"] + offset;
            
            string label = $"{flowOverride.Parent}::{flowOverride.Label}";
            if (flowOverride.Parent.Equals("SUPER", StringComparison.OrdinalIgnoreCase))
                label = $"{upperImmediateParentName}::{flowOverride.Label}";
            
            return masterLabelTable[label] + offset;
        }

        private static void HandleGotoFlowOverrides(ActorDefinition current, string upperImmediateParentName,
            IDictionary<string, int> masterLabelTable)
        {
            foreach ((string label, ActorFlowOverride flowOverride) in current.States.FlowOverrides)
            {
                if (flowOverride.BranchType != ActorStateBranch.Goto)
                    continue;

                int overrideOffset = FindGotoOverrideOffset(masterLabelTable, flowOverride, upperImmediateParentName);
                masterLabelTable[label] = overrideOffset;
                masterLabelTable[$"{current.Name}::{label}"] = overrideOffset;
            }
        }
        
        private static void ApplyGotoOffsets(IEnumerable<UnresolvedGotoFrame> unresolvedGotoFrames, 
            Dictionary<string, int> masterLabelTable, string upperImmediateParentName, EntityDefinition definition)
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
                
                if (flowControl.Parent.Empty())
                {
                    if (masterLabelTable.TryGetValue(flowControl.Label, out int offset))
                    {
                        entityFrame.NextFrameIndex = offset + flowControl.Offset;
                        if (entityFrame.NextFrameIndex < 0 || entityFrame.NextFrameIndex >= definition.States.Frames.Count)
                            Log.Error($"Invalid goto offset '{flowControl.Label}' in actor '{definition.Name}'");
                    }
                    else
                    {
                        Log.Error("Unable to resolve goto label '{0}' in actor '{1}', actor is likely malformed", flowControl.Label, definition.Name);
                    }

                    continue;
                }
                
                string targetLabel = $"{flowControl.Parent}::{flowControl.Label}";
                if (flowControl.Parent.Equals("SUPER", StringComparison.OrdinalIgnoreCase))
                    targetLabel = $"{upperImmediateParentName}::{flowControl.Label}";
                
                if (masterLabelTable.TryGetValue(targetLabel, out int parentOffset))
                    entityFrame.NextFrameIndex = parentOffset;
                else
                    Log.Error("Unable to resolve inheritance goto label '{0}' in actor '{1}', actor is likely malformed", targetLabel, definition.Name);
            }
        }

        private static void ApplyActorDefinition(EntityDefinition definition, ActorDefinition current,
            ActorDefinition parent, Dictionary<string, int> masterLabelTable)
        {
            int startingFrameOffset = definition.States.Frames.Count;
            List<UnresolvedGotoFrame> unresolvedGotoFrames = new List<UnresolvedGotoFrame>();
            
            AddFrameAndNonGotoFlowControl(definition, current, startingFrameOffset, unresolvedGotoFrames);
            AddLabelsToMasterTable(current, masterLabelTable, current.Name, startingFrameOffset);
            PurgeAnyControlFlowStopOverride(current, masterLabelTable);
            HandleGotoFlowOverrides(current, parent.Name, masterLabelTable);
            ApplyGotoOffsets(unresolvedGotoFrames, masterLabelTable, parent.Name, definition);
        }
    }
}