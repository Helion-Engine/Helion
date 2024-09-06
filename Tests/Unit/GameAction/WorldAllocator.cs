using FluentAssertions;
using Helion.Audio.Impl;
using Helion.Layer.Worlds;
using Helion.Maps.Shared;
using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Configs.Impl;
using Helion.Util.Extensions;
using Helion.Util.Profiling;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using Helion.Maps.Bsp.Zdbsp;
using NLog;
using Helion.Dehacked;

namespace Helion.Tests.Unit.GameAction;

internal static class WorldAllocator
{
    public static int TotalTicks;
    private static SinglePlayerWorld? StaticWorld = null;
    private static string LastResource = string.Empty;
    private static string LastFileName = string.Empty;
    private static string LastMapName = string.Empty;
    private static string LastTestKey = string.Empty;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static SinglePlayerWorld LoadMap(string resourceZip, string fileName, string mapName, string testKey, Action<SinglePlayerWorld> onInit,
        IWadType iwadType = IWadType.Doom2, SkillLevel skillLevel = SkillLevel.Medium, Player? existingPlayer = null, WorldModel? worldModel = null,
        bool disposeExistingWorld = true, bool cacheWorld = true,
        Action<ArchiveCollection>? onBeforeInit = null, Config? config = null, string? dehackedPatch = null)
    {
        if (disposeExistingWorld && UseExistingWorld(resourceZip, fileName, mapName, testKey, cacheWorld, out SinglePlayerWorld? existingWorld))
            return existingWorld;

        // Assets.pk3 is copied from the assets project.
        File.Exists(Constants.AssetsFileName).Should().BeTrue();

        LastResource = resourceZip;
        LastFileName = fileName;
        LastMapName = mapName;
        LastTestKey = testKey;

        using ZipArchive archive = ZipFile.OpenRead(resourceZip);
        archive.ExtractToDirectory(Directory.GetCurrentDirectory(), true);
        config ??= CreateConfig();
        var profiler = new Profiler();
        var audioSystem = new MockAudioSystem();
        ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(), config, new DataCache());
        archiveCollection.Load([fileName], iwad: null, iwadTypeOverride: iwadType).Should().BeTrue();

        if (dehackedPatch != null)
        {
            archiveCollection.Definitions.DehackedDefinition = new();
            archiveCollection.Definitions.DehackedDefinition.Parse(dehackedPatch);
            archiveCollection.ApplyDehackedPatch();
        }

        var mapDef = archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMapInfoOrDefault(mapName);
        var skillDef = archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(skillLevel) ?? throw new Exception("Failed to load skill definition");
        var map = archiveCollection.FindMap(mapDef.MapName) ?? throw new Exception("Failed to load map");
        Zdbsp zdbsp = new();
        if (!zdbsp.RunZdbsp(map, mapName, mapDef, out var outputMap) || outputMap == null)
            throw new Exception("Failed to create bsp");

        onBeforeInit?.Invoke(archiveCollection);

        DoomRandom random = worldModel == null ? new() : new(worldModel.RandomIndex);
        SinglePlayerWorld? world = WorldLayer.CreateWorldGeometry(new GlobalData(), config, audioSystem, archiveCollection, profiler, mapDef,
            skillDef, outputMap, existingPlayer, worldModel, random, unitTest: true) ?? throw new Exception("Failed to create world");
        StaticWorld = world;
        world.OnTick += World_OnTick;
        world.Start(worldModel);
        world.OnDestroying += World_OnDestroying;
        onInit(world);
        return world;
    }

    private static void World_OnDestroying(object? sender, EventArgs e)
    {
        Log.Info("Total world test time: {time}", TimeSpan.FromSeconds(TotalTicks / Constants.TicksPerSecond));
    }

    private static void World_OnTick(object? sender, EventArgs e)
    {
        TotalTicks++;
    }

    private static Config CreateConfig()
    {
        Config config = new();
        config.Render.AutomapBspThread.Set(false);
        return config;
    }

    private static bool UseExistingWorld(string resourceZip, string fileName, string mapName, string testKey, bool cacheWorld,
        [NotNullWhen(true)] out SinglePlayerWorld? existingWorld)
    {
        existingWorld = StaticWorld;
        if (StaticWorld == null)
            return false;

        if (cacheWorld && existingWorld != null && resourceZip.EqualsIgnoreCase(LastResource) && fileName.EqualsIgnoreCase(LastFileName) &&
            mapName.EqualsIgnoreCase(LastMapName) && testKey.EqualsIgnoreCase(LastTestKey))
            return true;

        StaticWorld.ArchiveCollection.Dispose();
        StaticWorld.Dispose();
        return false;
    }
}
