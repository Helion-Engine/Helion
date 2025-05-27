using Helion.Models;
using Helion.Resources.Archives;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Special;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World;

public partial class WorldBase
{
    private static readonly List<EntityModel> s_entityModels = new(1024);
    private static readonly List<PlayerModel> s_playerModels = [];
    private static readonly List<SectorModel> s_sectorModels = new(256);
    private static readonly List<LineModel> s_lineModels = new(256);
    private static readonly List<ConfigValueModel> s_configValueModels = [];
    private static readonly List<FileModel> s_fileModels = [];
    private static readonly List<string> s_visitedMaps = [];
    private static readonly SpecialModelData s_specialModelData = new();
    private static readonly WorldModel s_worldModel = new();

    public WorldModel ToWorldModel()
    {
        DataCache.FreeEntityModels(s_entityModels);
        DataCache.FreePlayerModels(s_playerModels);
        s_entityModels.Clear();
        s_playerModels.Clear();
        s_sectorModels.Clear();
        s_lineModels.Clear();
        s_configValueModels.Clear();
        s_fileModels.Clear();
        s_visitedMaps.Clear();
        s_specialModelData.Clear();

        SetSectorModels(s_sectorModels, s_specialModelData.SectorDamageSpecials);
        SpecialManager.GetSpecialModels(s_specialModelData);

        s_worldModel.ConfigValues = GetConfigValuesModel();
        s_worldModel.Files = GetGameFilesModel(s_fileModels);
        s_worldModel.MapName = MapName;
        s_worldModel.WorldState = WorldState;
        s_worldModel.Gametick = Gametick;
        s_worldModel.LevelTime = LevelTime;
        s_worldModel.SoundCount = m_soundCount;
        s_worldModel.Gravity = Gravity;
        s_worldModel.RandomIndex = Random.RandomIndex;
        s_worldModel.Skill = SkillLevel;
        s_worldModel.CurrentBossTarget = CurrentBossTarget;
        s_worldModel.Players = GetPlayerModels();
        s_worldModel.Entities = GetEntityModels();
        s_worldModel.Sectors = s_sectorModels;
        s_worldModel.Lines = GetLineModels();
        s_worldModel.VisitedMaps = GetVisitedMaps();
        s_worldModel.TotalTime = GlobalData.TotalTime;
        s_worldModel.Specials = s_specialModelData.Specials;
        s_worldModel.MoveSpecials = s_specialModelData.MoveSpecials;
        s_worldModel.ScrollSpecials = s_specialModelData.ScrollSpecials;
        s_worldModel.LightChangeSpecials = s_specialModelData.LightChangeSpecials;
        s_worldModel.LightFireFlickerDoomSpecials = s_specialModelData.LightFireFlickerDoomSpecials;
        s_worldModel.LightFlickerDoomSpecials = s_specialModelData.LightFlickerDoomSpecials;
        s_worldModel.LightPulsateSpecials = s_specialModelData.LightPulsateSpecials;
        s_worldModel.LightStrobeSpecials = s_specialModelData.LightStrobeSpecials;
        s_worldModel.PushSpecials = s_specialModelData.PushSpecials;
        s_worldModel.StairSpecials = s_specialModelData.StairSpecials;
        s_worldModel.ElevatorSpecials = s_specialModelData.ElevatorSpecials;
        s_worldModel.DamageSpecials = s_specialModelData.SectorDamageSpecials;
        s_worldModel.SwitchSpecials = s_specialModelData.SwitchSpecials;
        s_worldModel.TotalMonsters = LevelStats.TotalMonsters;
        s_worldModel.TotalItems = LevelStats.TotalItems;
        s_worldModel.TotalSecrets = LevelStats.TotalSecrets;
        s_worldModel.KillCount = LevelStats.KillCount;
        s_worldModel.ItemCount = LevelStats.ItemCount;
        s_worldModel.SecretCount = LevelStats.SecretCount;
        s_worldModel.MusicName = m_activeMusic;
        return s_worldModel;
    }

    private List<string> GetVisitedMaps()
    {
        for (int i = 0; i < GlobalData.VisitedMaps.Count; i++)
            s_visitedMaps.Add(GlobalData.VisitedMaps[i].MapName);
        return s_visitedMaps;
    }

    private IList<ConfigValueModel> GetConfigValuesModel()
    {
        s_configValueModels.Clear();
        foreach (var (path, component) in Config.GetComponents())
        {
            if (!component.Attribute.Serialize)
                continue;

            s_configValueModels.Add(new ConfigValueModel(path, component.Value.ObjectValue));
        }
        return s_configValueModels;
    }

    public GameFilesModel GetGameFilesModel() => GetGameFilesModel([]);

    public GameFilesModel GetGameFilesModel(List<FileModel> files)
    {
        return new GameFilesModel()
        {
            IWad = GetIWadFileModel(),
            Files = GetFileModels(),
        };
    }

    private IList<PlayerModel> GetPlayerModels()
    {
        s_playerModels.EnsureCapacity(EntityManager.Players.Count + EntityManager.VoodooDolls.Count);
        foreach (var player in EntityManager.Players)
            s_playerModels.Add(player.ToPlayerModel(DataCache.GetPlayerModel()));
        foreach (var player in EntityManager.VoodooDolls)
            s_playerModels.Add(player.ToPlayerModel(DataCache.GetPlayerModel()));
        return s_playerModels;
    }

    private FileModel GetIWadFileModel()
    {
        Archive? archive = ArchiveCollection.IWad;
        if (archive != null)
            return archive.ToFileModel();

        return new FileModel();
    }

    private List<FileModel> GetFileModels()
    {
        var archives = ArchiveCollection.Archives;
        s_fileModels.EnsureCapacity(archives.Count());
        foreach (var archive in archives)
        {
            if (archive.ExtractedFrom != null || archive.MD5 == Archive.DefaultMD5)
                continue;
            s_fileModels.Add(archive.ToFileModel());
        }

        return s_fileModels;
    }

    private List<EntityModel> GetEntityModels()
    {
        s_entityModels.EnsureCapacity(EntityManager.EntityCount);
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (!entity.IsPlayer)
                s_entityModels.Add(entity.ToEntityModel(DataCache.GetEntityModel()));
        }
        return s_entityModels;
    }

    private void SetSectorModels(List<SectorModel> sectorModels, List<SectorDamageSpecialModel> sectorDamageSpecialModels)
    {
        for (int i = 0; i < Sectors.Count; i++)
        {
            Sector sector = Sectors[i];
            if (sector.SoundTarget.NotNull() || sector.DataChanged)
                sectorModels.Add(sector.ToSectorModel(this));
            if (sector.SectorDamageSpecial != null)
                sectorDamageSpecialModels.Add(sector.SectorDamageSpecial.ToSectorDamageSpecialModel());
        }
    }

    private List<LineModel> GetLineModels()
    {
        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            ref StructLine structLine = ref StructLines.Data[i];
            if (structLine.SeenForAutomap)
                line.DataChanges |= LineDataTypes.Automap;

            if (!line.DataChanged)
                continue;

            s_lineModels.Add(line.ToLineModel(this));
        }

        return s_lineModels;
    }
}
