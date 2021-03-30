using System;
using Helion.Input;
using Helion.Maps.Specials.ZDoom;
using Helion.Render.Commands;
using Helion.Resources.Definitions.MapInfo;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;

namespace Helion.Layer
{
    public class IntermissionLayer : GameLayer
    {
        private readonly IWorld m_world;
        private readonly Player m_player;
        private readonly MapInfoDef m_mapInfo;
        private readonly ClusterDef? m_endGameCluster;
        private readonly Action m_nextMapFunc;
        private bool m_invokedNextMapFunc;
        private double m_killPercent;
        private double m_itemPercent;
        private double m_secretPercent;
        private IntermissionState m_intermissionState = IntermissionState.Started;

        protected override double Priority => 0.65;

        public IntermissionLayer(IWorld world, Player player, MapInfoDef mapInfo, ClusterDef? endGameCluster, 
            Action nextMapFunc)
        {
            m_world = world;
            m_player = player;
            m_mapInfo = mapInfo;
            m_endGameCluster = endGameCluster;
            m_nextMapFunc = nextMapFunc;

            CalculatePercentages();
        }

        private void CalculatePercentages()
        {
            int totalMonsters = 0;
            int monstersDead = 0;
            int totalItems = 0; // TODO: Get from the world; have the world cache at beginning.
            int itemsPickedUp = 0;
            int totalSecrets = m_player.SecretsFound;
            int secretsVisited = m_player.SecretsFound;
            
            foreach (Entity entity in m_world.Entities)
            {
                if (entity.Definition.Flags.IsMonster)
                {
                    totalMonsters++;
                    if (entity.IsDead)
                        monstersDead++;
                }

                if (entity.Flags.Pickup)
                    totalItems++;
            }
            
            // Since secrets have their sector type turned off, any secrets
            // encountered means the player has not touched them.
            foreach (Sector sector in m_world.Sectors)
                if (sector.SectorSpecialType == ZDoomSectorSpecialType.Secret)
                    totalSecrets++;

            if (totalMonsters != 0)
                m_killPercent = (double)monstersDead / totalMonsters;
            if (totalItems != 0)
                m_itemPercent = (double)itemsPickedUp / totalItems;
            if (totalSecrets != 0)
                m_secretPercent = (double)secretsVisited / totalSecrets;
        }

        public override void HandleInput(InputEvent input)
        {
            bool pressedKey = input.HasAnyKeyPressed();
            input.ConsumeAll();
            
            if (pressedKey)
            {
                if (!m_invokedNextMapFunc)
                {
                    m_invokedNextMapFunc = true;
                    m_nextMapFunc();
                }
                else
                {
                    AdvanceToNextState();
                }
            }
            
            base.HandleInput(input);
        }

        private void AdvanceToNextState()
        {
            m_intermissionState = m_intermissionState switch
            {
                IntermissionState.Started => IntermissionState.TallyingKills,
                IntermissionState.TallyingKills => IntermissionState.TallyingItems,
                IntermissionState.TallyingItems => IntermissionState.TallyingSecrets,
                IntermissionState.TallyingSecrets => IntermissionState.ShowingPar,
                IntermissionState.ShowingPar => IntermissionState.ShowingPar,
                IntermissionState.NextMap => IntermissionState.NextMap,
                _ => throw new Exception($"Unexpected intermission: {m_intermissionState}")
            };
        }

        public override void Render(RenderCommands renderCommands)
        {
            // TODO
            
            base.Render(renderCommands);
        }
    }

    internal enum IntermissionState
    {
        Started,
        TallyingKills,
        TallyingItems,
        TallyingSecrets,
        ShowingPar,
        NextMap
    }
}
