using Helion.Models;
using Helion.Resources.Archives;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World;

public partial class WorldBase
{
    public WorldModel ToWorldModel()
    {
        List<SectorModel> sectorModels = new(256);
        List<SectorDamageSpecialModel> sectorDamageSpecialModels = new(256);
        SetSectorModels(sectorModels, sectorDamageSpecialModels);

        return new WorldModel()
        {
            ConfigValues = GetConfigValuesModel(),
            Files = GetGameFilesModel(),
            MapName = MapName,
            WorldState = WorldState,
            Gametick = Gametick,
            LevelTime = LevelTime,
            SoundCount = m_soundCount,
            Gravity = Gravity,
            RandomIndex = Random.RandomIndex,
            Skill = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkillLevel(SkillDefinition),
            CurrentBossTarget = CurrentBossTarget,

            Players = GetPlayerModels(),
            Entities = GetEntityModels(),
            Sectors = sectorModels,
            DamageSpecials = sectorDamageSpecialModels,
            Lines = GetLineModels(),
            Specials = SpecialManager.GetSpecialModels(),
            VisitedMaps = GlobalData.VisitedMaps.Select(x => x.MapName).ToList(),
            TotalTime = GlobalData.TotalTime,

            TotalMonsters = LevelStats.TotalMonsters,
            TotalItems = LevelStats.TotalItems,
            TotalSecrets = LevelStats.TotalSecrets,
            KillCount = LevelStats.KillCount,
            ItemCount = LevelStats.ItemCount,
            SecretCount = LevelStats.SecretCount,
            MusicName = m_lastMusicChange == null ? MapInfo.Music : m_lastMusicChange.Name
        };
    }

    private IList<ConfigValueModel> GetConfigValuesModel()
    {
        List<ConfigValueModel> items = new(32);
        foreach (var (path, component) in Config.GetComponents())
        {
            if (!component.Attribute.Serialize)
                continue;

            items.Add(new ConfigValueModel(path, component.Value.ObjectValue));
        }
        return items;
    }

    public GameFilesModel GetGameFilesModel()
    {
        return new GameFilesModel()
        {
            IWad = GetIWadFileModel(),
            Files = GetFileModels(),
        };
    }

    private IList<PlayerModel> GetPlayerModels()
    {
        List<PlayerModel> playerModels = new(EntityManager.Players.Count + EntityManager.VoodooDolls.Count);
        foreach (var player in EntityManager.Players)
            playerModels.Add(player.ToPlayerModel());
        foreach (var player in EntityManager.VoodooDolls)
            playerModels.Add(player.ToPlayerModel());
        return playerModels;
    }

    private FileModel GetIWadFileModel()
    {
        Archive? archive = ArchiveCollection.IWad;
        if (archive != null)
            return archive.ToFileModel();

        return new FileModel();
    }

    private IList<FileModel> GetFileModels()
    {
        List<FileModel> fileModels = new();
        var archives = ArchiveCollection.Archives;
        foreach (var archive in archives)
        {
            if (archive.ExtractedFrom != null || archive.MD5 == Archive.DefaultMD5)
                continue;
            fileModels.Add(archive.ToFileModel());
        }

        return fileModels;
    }

    private List<EntityModel> GetEntityModels()
    {
        List<EntityModel> entityModels = new(2048);
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (!entity.IsPlayer)
                entityModels.Add(entity.ToEntityModel(new EntityModel()));
        }
        return entityModels;
    }

    private void SetSectorModels(List<SectorModel> sectorModels, List<SectorDamageSpecialModel> sectorDamageSpecialModels)
    {
        for (int i = 0; i < Sectors.Count; i++)
        {
            Sector sector = Sectors[i];
            if (sector.SoundTarget.Entity != null || sector.DataChanged)
                sectorModels.Add(sector.ToSectorModel(this));
            if (sector.SectorDamageSpecial != null)
                sectorDamageSpecialModels.Add(sector.SectorDamageSpecial.ToSectorDamageSpecialModel());
        }
    }

    private List<LineModel> GetLineModels()
    {
        List<LineModel> lineModels = new(256);
        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (!line.DataChanged)
                continue;

            lineModels.Add(line.ToLineModel(this));
        }

        return lineModels;
    }
}
