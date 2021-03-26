using System;
using System.Collections.Generic;
using Helion.Util.Configs;
using Helion.Resources.Definitions.Language;
using Helion.Util.Geometry.Vectors;
using Helion.Util.RandomGenerators;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Resources.Archives.Collection;
using Helion.Util.Container;
using Helion.Models;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;

namespace Helion.World
{
    public interface IWorld : IDisposable
    {
        CIString MapName { get; }
        int Gametick { get; }
        int LevelTime { get; }
        double Gravity { get; }
        WorldState WorldState { get; }
        IList<Line> Lines { get; }
        IList<Side> Sides { get; }
        IList<Wall> Walls { get; }
        IList<Sector> Sectors { get; }
        BspTree BspTree { get; }
        LinkableList<Entity> Entities { get; }
        Vec3D ListenerPosition { get; }
        double ListenerAngle { get; }
        double ListenerPitch { get; }
        Entity ListenerEntity { get; }
        IRandom Random { get; }
        EntityManager EntityManager { get; }
        WorldSoundManager SoundManager { get; }
        BlockmapTraverser BlockmapTraverser { get; }
        Config Config { get; }
        SkillDef SkillDefinition { get; }
        ArchiveCollection ArchiveCollection { get; }
        MapInfoDef MapInfo { get; }
        bool Paused { get; }

        void Link(Entity entity);
        void Tick();
        void Pause();
        void Resume();
        IEnumerable<Sector> FindBySectorTag(int tag);
        IEnumerable<Entity> FindByTid(int tid);
        void ExitLevel(LevelChangeType type);
        Entity[] GetBossTargets();
        int CurrentBossTarget { get; set; }
        void TelefragBlockingEntities(Entity entity);
        bool EntityUse(Entity entity);
        bool CanActivate(Entity entity, Line line, ActivationContext context);
        bool ActivateSpecialLine(Entity entity, Line line, ActivationContext context);
        bool GetAutoAimEntity(Entity startEntity, in Vec3D start, double angle, double distance, out double pitch, out Entity? entity);
        Entity? FireProjectile(Entity shooter, double pitch, double distance, bool autoAim, string projectClassName, double zOffset = 0.0);
        void FireHitscanBullets(Entity shooter, int bulletCount, double spreadAngleRadians, double spreadPitchRadians, double pitch, double distance, bool autoAim);
        Entity? FireHitscan(Entity shooter, double angle, double pitch, double distance, int damage);
        bool DamageEntity(Entity target, Entity? source, int damage, Thrust thrust = Thrust.HorizontalAndVertical, Sector? sectorSource = null);
        void KillEntity(Entity entity, Entity? source, bool forceGib = false);
        bool GiveItem(Player player, Entity item, EntityFlags? flags, bool pickupFlash = true);
        void PerformItemPickup(Entity entity, Entity item);
        void HandleEntityHit(Entity entity, in Vec3D previousVelocity, TryMoveData? tryMove);
        bool CheckLineOfSight(Entity from, Entity to);
        void RadiusExplosion(Entity source, int radius);
        TryMoveData TryMoveXY(Entity entity, Vec2D position, bool stepMove = true);
        SectorMoveStatus MoveSectorZ(Sector sector, SectorPlane sectorPlane, SectorPlaneType moveType,
            MoveDirection direction, double speed, double destZ, CrushData? crush);
        void HandleEntityDeath(Entity deathEntity, Entity? deathSource);
        void DisplayMessage(Player player, Player? other, string message, LanguageMessageType type);
        // Checks if the entity will be blocked by another entity at the given position. Will use the entity definition's height and solid values.
        bool IsPositionBlockedByEntity(Entity entity, in Vec3D position);
        bool IsPositionBlocked(Entity entity);
        void CreateTeleportFog(in Vec3D pos, bool playSound = true);
        bool IsSectorIdValid(int sectorId) => sectorId >= 0 && sectorId < Sectors.Count;
        bool IsLineIdValid(int lineId) => lineId >= 0 && lineId < Lines.Count;

        WorldModel ToWorldModel();
        GameFilesModel GetGameFilesModel();
    }
}