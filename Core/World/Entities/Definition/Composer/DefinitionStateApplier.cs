using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Definitions.Decorate;
using Helion.Resources.Definitions.Decorate.States;
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

        private static readonly Dictionary<string, EntityFrame> ProcessedFrames = new();
        private static readonly List<UnresolvedGotoFrame> UnresolvedGotoFrames = new();
        private static readonly List<string> ModifiedLabels = new();

        public static void Apply(EntityFrameTable entityFrameTable, EntityDefinition definition, LinkedList<ActorDefinition> actorDefinitions)
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

            string vanillaActorName;
            if (actorDefinitions.Count > 1)
                vanillaActorName = actorDefinitions.Skip(1).First().Name;
            else
                vanillaActorName = actorDefinitions.First.Value.Name;

            // We always have to apply the first definition to it, which should
            // be the Actor class. However to reduce code duplication, we'll be
            // passing itself as the parent. There should be no recursion nor
            // should there be any Super::Label goto's, so this is okay unless
            // the user has done something critically wrong.
            int offset = 0;
            ActorDefinition baseActor = actorDefinitions.First.Value;
            ApplyActorDefinition(entityFrameTable, definition, baseActor, baseActor, masterLabelTable, offset, vanillaActorName);
            offset += baseActor.States.Frames.Count;

            actorDefinitions.Window(2).ForEach(list =>
            {
                ActorDefinition parent = list[0];
                ActorDefinition current = list[1];
                ApplyActorDefinition(entityFrameTable, definition, current, parent, masterLabelTable, offset, vanillaActorName);
                offset += current.States.Frames.Count;
            });

            // Now that all the labels have been handled/added/pruned/linked,
            // we can add them in safely at the end.
            ApplyAllLabels(definition, masterLabelTable);
        }

        private static void ApplyAllLabels(EntityDefinition definition, Dictionary<string, int> masterLabelTable)
        {
            masterLabelTable.ForEach(pair => definition.States.Labels[pair.Key] = pair.Value);
        }

        private static void AddFrameAndNonGotoFlowControl(EntityFrameTable entityFrameTable, ActorDefinition current, EntityDefinition definition,
            IList<UnresolvedGotoFrame> unresolvedGotoFrames, Dictionary<string, int> masterLabelTable, int offset,
            string vanillaActorName)
        {
            // The following are used for knowing where to jump back to if we
            // encounter the `loop` control flow.
            int lastLabelIndex = 0;
            HashSet<int> indicesWithLabels = new HashSet<int>();
            current.States.Labels.Values.ForEach(index => indicesWithLabels.Add(index + offset));

            int startingFrameOffset = entityFrameTable.Frames.Count;
            FrameSet? currentFrameSet = null;

            for (int localFrameOffset = 0; localFrameOffset < current.States.Frames.Count; localFrameOffset++)
            {
                int currentFrameOffset = localFrameOffset + offset;
                string key = $"{current.Name}::{currentFrameOffset}";
                if (ProcessedFrames.TryGetValue(key, out EntityFrame? existingFrame))
                {
                    if (indicesWithLabels.Contains(currentFrameOffset))
                        UpdateMasterLabelTable(currentFrameOffset, existingFrame, masterLabelTable, out _);
                    continue;
                }
                
                ActorFrame frame = current.States.Frames[localFrameOffset];
                if (indicesWithLabels.Contains(currentFrameOffset))
                    lastLabelIndex = startingFrameOffset + localFrameOffset;

                EntityFrameProperties properties = new EntityFrameProperties(frame.Properties);
                ActionFunction? actionFunction = Find(frame.ActionFunction?.FunctionName);
                EntityFrame entityFrame = new EntityFrame(frame.Sprite, frame.Frame, frame.Ticks, properties,
                    actionFunction, entityFrameTable.Frames.Count + 1, vanillaActorName);

                HandleNonGotoFlowControl(frame, entityFrame, startingFrameOffset, lastLabelIndex, unresolvedGotoFrames);

                entityFrame.MasterFrameIndex = entityFrameTable.Frames.Count;
                ProcessedFrames[key] = entityFrame;
                entityFrameTable.Frames.Add(entityFrame);
                definition.States.FrameCount++;

                if (indicesWithLabels.Contains(currentFrameOffset))
                {
                    UpdateMasterLabelTable(currentFrameOffset, entityFrame, masterLabelTable, out List<string> modifiedLabels);
                    foreach (var label in modifiedLabels)
                    {
                        currentFrameSet = new FrameSet() { VanillActorName = vanillaActorName, StartFrameIndex = entityFrame.MasterFrameIndex, Count = 1 };
                        entityFrameTable.FrameSets[label] = currentFrameSet;
                    }
                }
                else if (currentFrameSet != null)
                {
                    currentFrameSet.Count++;
                }
            }
        }

        private static void UpdateMasterLabelTable(int frameOffset, EntityFrame frame, Dictionary<string, int> masterLabelTable,
            out List<string> modifiedLabels)
        {
            ModifiedLabels.Clear();
            modifiedLabels = ModifiedLabels;

            // TODO this sucks
            foreach (var pair in masterLabelTable)
            {
                if (pair.Value != frameOffset)
                    continue;

                masterLabelTable[pair.Key] = frame.MasterFrameIndex;
                modifiedLabels.Add(pair.Key);
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
            string upperActorName, int offset)
        {
            foreach (var pair in definition.States.Labels)
            {
                masterLabelTable[pair.Key] = pair.Value + offset;
                masterLabelTable[$"{upperActorName}::{pair.Key}"] = pair.Value + offset;
            }
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
        
        private static void ApplyGotoOffsets(EntityFrameTable entityFrameTable, IEnumerable<UnresolvedGotoFrame> unresolvedGotoFrames, 
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
                        if (entityFrame.NextFrameIndex < 0 || entityFrame.NextFrameIndex >= entityFrameTable.Frames.Count)
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

        private static void ApplyActorDefinition(EntityFrameTable entityFrameTable, EntityDefinition definition, ActorDefinition current,
            ActorDefinition parent, Dictionary<string, int> masterLabelTable, int offset, string vanillaActorName)
        {
            UnresolvedGotoFrames.Clear();

            AddLabelsToMasterTable(current, masterLabelTable, current.Name, offset);
            AddFrameAndNonGotoFlowControl(entityFrameTable, current, definition, UnresolvedGotoFrames, masterLabelTable, offset, vanillaActorName);
            PurgeAnyControlFlowStopOverride(current, masterLabelTable);
            HandleGotoFlowOverrides(current, parent.Name, masterLabelTable);
            ApplyGotoOffsets(entityFrameTable, UnresolvedGotoFrames, masterLabelTable, parent.Name, definition);
        }
    }
}