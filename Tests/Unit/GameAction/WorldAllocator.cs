using FluentAssertions;
using Helion.Audio.Impl;
using Helion.Bsp.Zdbsp;
using Helion.Layer.Worlds;
using Helion.Maps.Shared;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.IWad;
using Helion.Util.Configs.Impl;
using Helion.Util.Extensions;
using Helion.Util.Profiling;
using Helion.World;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace Helion.Tests.Unit.GameAction
{
    internal static class WorldAllocator
    {
        private static SinglePlayerWorld? StaticWorld = null;
        private static string LastResource = string.Empty;
        private static string LastFileName = string.Empty;
        private static string LastMapName = string.Empty;

        public static SinglePlayerWorld LoadMap(string resourceZip, string fileName, string mapName, Action<SinglePlayerWorld> onInit,
            IWadType iwadType = IWadType.Doom2, SkillLevel skillLevel = SkillLevel.Medium)
        {
            if (UseExistingWorld(resourceZip, fileName, mapName, out SinglePlayerWorld? existingWorld))
                return existingWorld;

            LastResource = resourceZip;
            LastFileName = fileName;
            LastMapName = mapName;

            using ZipArchive archive = ZipFile.OpenRead(resourceZip);
            archive.ExtractToDirectory(Directory.GetCurrentDirectory(), true);

            var config = new Config();
            var profiler = new Profiler();
            var audioSystem = new MockAudioSystem();
            ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(), config.Compatibility);
            archiveCollection.Load(new string[] { fileName }, iwad: null, iwadTypeOverride: iwadType ).Should().BeTrue();

            var mapDef = archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMapInfoOrDefault(mapName);
            var skillDef = archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(skillLevel);
            if (skillDef == null)
                throw new Exception("Failed to load skill definition");

            var map = archiveCollection.FindMap(mapDef.MapName);
            if (map == null)
                throw new Exception("Failed to load map");

            Zdbsp zdbsp = new();
            if (!zdbsp.RunZdbsp(map, mapName, mapDef, out var outputMap) || outputMap == null)
                throw new Exception("Failed to create bsp");

            // Not the greatest dependency...
            TextureManager.Init(archiveCollection, mapDef);
            TextureManager.Instance.UnitTest = true;

            SinglePlayerWorld? world = WorldLayer.CreateWorldGeometry(new GlobalData(), config, audioSystem, archiveCollection, profiler, mapDef, skillDef, outputMap,
                null, null);
            if (world == null)
                throw new Exception("Failed to create world");

            StaticWorld = world;
            world.Start(null);
            onInit(world);
            return world;
        }

        private static bool UseExistingWorld(string resourceZip, string fileName, string mapName, 
            [NotNullWhen(true)] out SinglePlayerWorld? existingWorld)
        {
            existingWorld = StaticWorld;
            if (StaticWorld == null)
                return false;

            if (existingWorld != null && resourceZip.EqualsIgnoreCase(LastResource) && fileName.EqualsIgnoreCase(LastFileName) && mapName.EqualsIgnoreCase(LastMapName))
                return true;

            StaticWorld.ArchiveCollection.Dispose();
            StaticWorld.Dispose();
            return false;
        }
    }
}
