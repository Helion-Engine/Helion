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

namespace Helion.World.Entities.Definition.Composer;

public static class DefinitionStateApplier
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<string, EntityFrame> ProcessedFrames = new();
    private static readonly List<UnresolvedGotoFrame> UnresolvedGotoFrames = new();
    private static readonly List<string> ModifiedLabels = new();

    private class FrameLabel
    {
        public FrameLabel(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
    }

    public static void Apply(EntityFrameTable entityFrameTable, EntityDefinition definition, IList<ActorDefinition> actorDefinitions)
    {
        if (actorDefinitions.Count < 2 || actorDefinitions[0] == null)
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
        Dictionary<string, FrameLabel> masterLabelTable = new(StringComparer.OrdinalIgnoreCase);
        string vanillaActorName = GetVanillaActorName(actorDefinitions);

        // We always have to apply the first definition to it, which should
        // be the Actor class. However to reduce code duplication, we'll be
        // passing itself as the parent. There should be no recursion nor
        // should there be any Super::Label goto's, so this is okay unless
        // the user has done something critically wrong.
        int offset = 0;
        ActorDefinition baseActor = actorDefinitions[0];
        ApplyActorDefinition(entityFrameTable, definition, baseActor, baseActor, masterLabelTable, offset, vanillaActorName, true);
        offset += baseActor.States.Frames.Count;

        HashSet<ActorDefinition> skipActors = new();
        ActorDefinition? lastActorDef = null;
        foreach (ActorDefinition current in actorDefinitions)
        {
            bool skipSuper = current.FlagProperties.SkipSuper.HasValue && current.FlagProperties.SkipSuper.Value;
            if (skipSuper && lastActorDef != null)
                skipActors.Add(lastActorDef);
            lastActorDef = current;
        }

        actorDefinitions.Window(2).ForEach(list =>
        {
            ActorDefinition parent = list[0];
            ActorDefinition current = list[1];
            bool includeGenericLabels = !skipActors.Contains(current);

            ApplyActorDefinition(entityFrameTable, definition, current, parent, masterLabelTable, offset, vanillaActorName,
                includeGenericLabels);

            offset += current.States.Frames.Count;
        });

        // Now that all the labels have been handled/added/pruned/linked,
        // we can add them in safely at the end.
        ApplyAllLabels(definition, masterLabelTable);
    }

    private static string GetVanillaActorName(IList<ActorDefinition> actorDefinitions)
    {
        for (int i = actorDefinitions.Count - 1; i >= 0; i--)
        {
            ActorDefinition current = actorDefinitions[i];
            if (!current.FlagProperties.SkipSuper.HasValue || !current.FlagProperties.SkipSuper.Value)
                return current.Name;
        }

        return actorDefinitions.Last().Name;
    }

    private static void ApplyAllLabels(EntityDefinition definition, Dictionary<string, FrameLabel> masterLabelTable)
    {
        masterLabelTable.ForEach(pair => definition.States.Labels[pair.Key] = pair.Value.Index);
    }

    private static string GetProcessedFrameKey(string name, int frame) =>  $"{name}::{frame}";

    private static void AddFrameAndNonGotoFlowControl(EntityFrameTable entityFrameTable, ActorDefinition current, EntityDefinition definition,
        IList<UnresolvedGotoFrame> unresolvedGotoFrames, Dictionary<string, FrameLabel> masterLabelTable, int offset,
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
            string key = GetProcessedFrameKey(current.Name, currentFrameOffset);
            if (ProcessedFrames.TryGetValue(key, out EntityFrame? existingFrame))
            {
                if (indicesWithLabels.Contains(currentFrameOffset))
                    UpdateMasterLabelTable(currentFrameOffset, existingFrame, masterLabelTable, out _);
                continue;
            }

            int absoluteFrameOffset = startingFrameOffset + localFrameOffset;
            ActorFrame frame = current.States.Frames[localFrameOffset];
            if (indicesWithLabels.Contains(currentFrameOffset))
                lastLabelIndex = absoluteFrameOffset;

            EntityFrameProperties properties = new EntityFrameProperties(frame.Properties);
            ActionFunction? actionFunction = Find(frame.ActionFunction?.FunctionName);
            EntityFrame entityFrame = new EntityFrame(entityFrameTable, frame.Sprite, frame.Frame, frame.Ticks, properties,
                actionFunction, entityFrameTable.Frames.Count + 1, vanillaActorName);

            HandleNonGotoFlowControl(frame, entityFrame, absoluteFrameOffset, lastLabelIndex, unresolvedGotoFrames);

            ProcessedFrames[key] = entityFrame;
            entityFrameTable.AddFrame(entityFrame);
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

    private static void UpdateMasterLabelTable(int frameOffset, EntityFrame frame, Dictionary<string, FrameLabel> masterLabelTable,
        out List<string> modifiedLabels)
    {
        ModifiedLabels.Clear();
        modifiedLabels = ModifiedLabels;

        // TODO this sucks
        foreach (var pair in masterLabelTable)
        {
            if (pair.Value.Index != frameOffset)
                continue;

            masterLabelTable[pair.Key].Index = frame.MasterFrameIndex;
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

    private static void AddLabelsToMasterTable(ActorDefinition definition, IDictionary<string, FrameLabel> masterLabelTable,
        string upperActorName, int offset, bool includeGenericLabels)
    {
        foreach (var pair in definition.States.Labels)
        {
            if (includeGenericLabels)
                masterLabelTable[pair.Key] = new FrameLabel(pair.Value + offset);
            masterLabelTable[$"{upperActorName}::{pair.Key}"] = new FrameLabel(pair.Value + offset);
        }
    }

    private static void PurgeAnyControlFlowStopOverride(ActorDefinition current, IDictionary<string, FrameLabel> masterLabelTable)
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

    private static int FindGotoOverrideOffset(IDictionary<string, FrameLabel> masterLabelTable, ActorFlowOverride flowOverride,
        string upperImmediateParentName)
    {
        if (flowOverride.Label == null)
        {
            Log.Error("Malformed flow override offset label (report this to a developer!)");
            return 0;
        }

        string key;
        int offset = flowOverride.Offset ?? 0;
        if (flowOverride.Parent == null)
        {
            key = GetProcessedFrameKey(upperImmediateParentName, masterLabelTable[$"{flowOverride.Label}"].Index);
            return GetProcessedFrameMasterIndex(key) + offset;
        }

        string label = $"{flowOverride.Parent}::{flowOverride.Label}";
        if (flowOverride.Parent.Equals("SUPER", StringComparison.OrdinalIgnoreCase))
            label = $"{upperImmediateParentName}::{flowOverride.Label}";

        key = GetProcessedFrameKey(upperImmediateParentName, masterLabelTable[label].Index);
        return GetProcessedFrameMasterIndex(key) + offset;
    }

    private static int GetProcessedFrameMasterIndex(string key)
    {
        if (!ProcessedFrames.TryGetValue(key, out EntityFrame? entityFrame))
        {
            Log.Error($"Bad processed frame key: {key}");
            return 0;
        }

        return entityFrame.MasterFrameIndex;
    }

    private static void HandleGotoFlowOverrides(ActorDefinition current, string upperImmediateParentName,
        IDictionary<string, FrameLabel> masterLabelTable)
    {
        foreach ((string label, ActorFlowOverride flowOverride) in current.States.FlowOverrides)
        {
            if (flowOverride.BranchType != ActorStateBranch.Goto)
                continue;

            int index = FindGotoOverrideOffset(masterLabelTable, flowOverride, upperImmediateParentName);
            masterLabelTable[label].Index = index;
            masterLabelTable[$"{current.Name}::{label}"].Index = index;
        }
    }

    private static void ApplyGotoOffsets(EntityFrameTable entityFrameTable, IEnumerable<UnresolvedGotoFrame> unresolvedGotoFrames,
        Dictionary<string, FrameLabel> masterLabelTable, string upperImmediateParentName, EntityDefinition definition)
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
                if (masterLabelTable.TryGetValue(flowControl.Label, out FrameLabel? frameLabel))
                {
                    entityFrame.NextFrameIndex = frameLabel.Index + flowControl.Offset;
                    if (entityFrame.NextFrameIndex < 0 || entityFrame.NextFrameIndex >= entityFrameTable.Frames.Count)
                        Log.Error($"Invalid goto offset '{flowControl.Label}' in actor '{definition.Name}'");
                }
                else
                {
                    Log.Error($"Unable to resolve goto label '{flowControl.Label}' in actor '{definition.Name}'");
                }

                continue;
            }

            string targetLabel = $"{flowControl.Parent}::{flowControl.Label}";
            if (flowControl.Parent.Equals("SUPER", StringComparison.OrdinalIgnoreCase))
                targetLabel = $"{upperImmediateParentName}::{flowControl.Label}";

            if (masterLabelTable.TryGetValue(targetLabel, out FrameLabel? parentFrameLabel))
                entityFrame.NextFrameIndex = parentFrameLabel.Index;
            else
                Log.Error("Unable to resolve inheritance goto label '{0}' in actor '{1}', actor is likely malformed", targetLabel, definition.Name);
        }
    }

    private static void ApplyActorDefinition(EntityFrameTable entityFrameTable, EntityDefinition definition, ActorDefinition current,
        ActorDefinition parent, Dictionary<string, FrameLabel> masterLabelTable, int offset, string vanillaActorName, bool includeGenericLabels)
    {
        UnresolvedGotoFrames.Clear();

        Dictionary<string, FrameLabel> offsetMasterTable;
        if (includeGenericLabels)
        {
            offsetMasterTable = masterLabelTable;
            AddLabelsToMasterTable(current, masterLabelTable, current.Name, offset, true);
        }
        else
        {
            // If generic labels aren't included then a table still has to be built with them
            // The master label table is duplicated from the master so goto's can still work
            // Skip_Super causes this to happen where it can access parent labels but shouldn't include the generic label
            offsetMasterTable = new(masterLabelTable);
            AddLabelsToMasterTable(current, masterLabelTable, current.Name, offset, false);
            AddLabelsToMasterTable(current, offsetMasterTable, current.Name, offset, true);
        }

        // FrameLabel pointer will exist in both masterLabelTable and offsetMasterTable
        // Any modifications to offsetMasterTable will be automatically applied to masterLabelTable
        AddFrameAndNonGotoFlowControl(entityFrameTable, current, definition, UnresolvedGotoFrames, offsetMasterTable, offset, vanillaActorName);
        PurgeAnyControlFlowStopOverride(current, offsetMasterTable);
        HandleGotoFlowOverrides(current, parent.Name, offsetMasterTable);
        ApplyGotoOffsets(entityFrameTable, UnresolvedGotoFrames, offsetMasterTable, parent.Name, definition);
    }
}

