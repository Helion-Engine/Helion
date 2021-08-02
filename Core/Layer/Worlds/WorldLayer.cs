using System;
using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Models;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Impl.SinglePlayer;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.Worlds
{
    public partial class WorldLayer : IGameLayerParent
    {
        private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public IntermissionLayer? Intermission { get; private set; }
        public MapInfoDef CurrentMap { get; set; }
        public SinglePlayerWorld World { get; private set; }
        private readonly Config m_config;
        private readonly HelionConsole m_console;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly IAudioSystem m_audioSystem;
        private readonly GameLayerManager m_parent;
        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly FpsTracker m_fpsTracker;
        private (ConfigValueEnum<Key>, TickCommands)[] m_consumeDownKeys = {};
        private (ConfigValueEnum<Key>, TickCommands)[] m_consumePressedKeys = {};
        private TickerInfo m_lastTickInfo = new(0, 0);
        private TickCommand m_tickCommand = new();
        private bool m_drawAutomap;
        private bool m_disposed;
        
        private Player Player => World.Player;
        public bool ShouldFocus => !World.Paused;
        
        public WorldLayer(GameLayerManager parent, Config config, HelionConsole console, ArchiveCollection archiveCollection,
            IAudioSystem audioSystem, FpsTracker fpsTracker, SinglePlayerWorld world, MapInfoDef mapInfoDef)
        {
            m_config = config;
            m_console = console;
            m_archiveCollection = archiveCollection;
            m_audioSystem = audioSystem;
            m_parent = parent;
            m_fpsTracker = fpsTracker;
            World = world;
            CurrentMap = mapInfoDef;
            
            SetupKeys(config);
            
            m_ticker.Start();
        }
        
        ~WorldLayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private void SetupKeys(Config config)
        {
            m_consumeDownKeys = new[]
            {
                (config.Controls.Forward,   TickCommands.Forward),
                (config.Controls.Backward,  TickCommands.Backward),
                (config.Controls.Left,      TickCommands.Left),
                (config.Controls.Right,     TickCommands.Right),
                (config.Controls.TurnLeft,  TickCommands.TurnLeft),
                (config.Controls.TurnRight, TickCommands.TurnRight),
                (config.Controls.LookDown,  TickCommands.LookDown),
                (config.Controls.LookUp,    TickCommands.LookUp),
                (config.Controls.Jump,      TickCommands.Jump),
                (config.Controls.Crouch,    TickCommands.Crouch),
                (config.Controls.Attack,    TickCommands.Attack),
                (config.Controls.AttackAlt, TickCommands.Attack),
                (config.Controls.Run,       TickCommands.Speed),
                (config.Controls.RunAlt,    TickCommands.Speed),
                (config.Controls.Strafe,    TickCommands.Strafe),
            };

            m_consumePressedKeys = new[]
            {
                (config.Controls.Use,            TickCommands.Use),
                (config.Controls.NextWeapon,     TickCommands.NextWeapon),
                (config.Controls.PreviousWeapon, TickCommands.PreviousWeapon),
                (config.Controls.WeaponSlot1,    TickCommands.WeaponSlot1),
                (config.Controls.WeaponSlot2,    TickCommands.WeaponSlot2),
                (config.Controls.WeaponSlot3,    TickCommands.WeaponSlot3),
                (config.Controls.WeaponSlot4,    TickCommands.WeaponSlot4),
                (config.Controls.WeaponSlot5,    TickCommands.WeaponSlot5),
                (config.Controls.WeaponSlot6,    TickCommands.WeaponSlot6),
                (config.Controls.WeaponSlot7,    TickCommands.WeaponSlot7),
            };
        }
        
        public static WorldLayer? Create(GameLayerManager parent, GlobalData globalData, Config config, 
            HelionConsole console, IAudioSystem audioSystem, ArchiveCollection archiveCollection, 
            FpsTracker fpsTracker, MapInfoDef mapInfoDef, SkillDef skillDef, IMap map, Player? existingPlayer, 
            WorldModel? worldModel)
        {
            string displayName = mapInfoDef.GetMapNameWithPrefix(archiveCollection);
            Log.Info(displayName);
            
            TextureManager.Init(archiveCollection);
            TextureManager.Instance.SkyTextureName = mapInfoDef.Sky1.Name;
            
            SinglePlayerWorld? world = CreateWorldGeometry(globalData, config, audioSystem, archiveCollection, mapInfoDef, skillDef, 
                map, existingPlayer, worldModel);
            if (world == null)
                return null;
            
            return new WorldLayer(parent, config, console, archiveCollection, audioSystem, fpsTracker, world, mapInfoDef);
        }

        private static SinglePlayerWorld? CreateWorldGeometry(GlobalData globalData, Config config, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection, MapInfoDef mapDef, SkillDef skillDef, IMap map,
            Player? existingPlayer, WorldModel? worldModel)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map, config);
            if (geometry == null)
                return null;

            try
            {
                return new SinglePlayerWorld(globalData, config, archiveCollection, audioSystem, geometry, mapDef, skillDef, map,
                    existingPlayer, worldModel);
            }
            catch (HelionException e)
            {
                Log.Error(e.Message);
            }

            return null;
        }

        public void Remove(object layer)
        {
            if (ReferenceEquals(layer, Intermission))
            {
                Intermission?.Dispose();
                Intermission = null;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            World.Dispose();
            
            Intermission?.Dispose();
            Intermission = null;

            m_disposed = true;
        }
    }
}
