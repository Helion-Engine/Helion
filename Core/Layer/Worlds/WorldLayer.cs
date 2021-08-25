using System;
using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Models;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
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
        
        private static readonly (string, TickCommands)[] ConsumeDownKeys = 
        {
            (Constants.Input.Forward,   TickCommands.Forward),
            (Constants.Input.Backward,  TickCommands.Backward),
            (Constants.Input.Left,      TickCommands.Left),
            (Constants.Input.Right,     TickCommands.Right),
            (Constants.Input.TurnLeft,  TickCommands.TurnLeft),
            (Constants.Input.TurnRight, TickCommands.TurnRight),
            (Constants.Input.LookDown,  TickCommands.LookDown),
            (Constants.Input.LookUp,    TickCommands.LookUp),
            (Constants.Input.Jump,      TickCommands.Jump),
            (Constants.Input.Crouch,    TickCommands.Crouch),
            (Constants.Input.Attack,    TickCommands.Attack),
            (Constants.Input.AttackAlt, TickCommands.Attack),
            (Constants.Input.Run,       TickCommands.Speed),
            (Constants.Input.RunAlt,    TickCommands.Speed),
            (Constants.Input.Strafe,    TickCommands.Strafe),
        };
        
        private static readonly (string, TickCommands)[] ConsumeContinuallyDownKeys = 
        {
            (Constants.Input.Use,            TickCommands.Use),
            (Constants.Input.NextWeapon,     TickCommands.NextWeapon),
            (Constants.Input.PreviousWeapon, TickCommands.PreviousWeapon),
            (Constants.Input.WeaponSlot1,    TickCommands.WeaponSlot1),
            (Constants.Input.WeaponSlot2,    TickCommands.WeaponSlot2),
            (Constants.Input.WeaponSlot3,    TickCommands.WeaponSlot3),
            (Constants.Input.WeaponSlot4,    TickCommands.WeaponSlot4),
            (Constants.Input.WeaponSlot5,    TickCommands.WeaponSlot5),
            (Constants.Input.WeaponSlot6,    TickCommands.WeaponSlot6),
            (Constants.Input.WeaponSlot7,    TickCommands.WeaponSlot7),
        };

        public IntermissionLayer? Intermission { get; private set; }
        public MapInfoDef CurrentMap { get; }
        public SinglePlayerWorld World { get; }
        private readonly Config m_config;
        private readonly HelionConsole m_console;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly IAudioSystem m_audioSystem;
        private readonly GameLayerManager m_parent;
        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly FpsTracker m_fpsTracker;
        private TickerInfo m_lastTickInfo = new(0, 0);
        private TickCommand m_tickCommand = new();
        private bool m_drawAutomap;
        private Vec2I m_autoMapOffset = (0, 0);
        private double m_autoMapScale;
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
            m_autoMapScale = config.Hud.AutoMap.Scale;
            World = world;
            CurrentMap = mapInfoDef;

            m_ticker.Start();
        }
        
        ~WorldLayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private void SetupKeys()
        {

        }
        
        public static WorldLayer? Create(GameLayerManager parent, GlobalData globalData, Config config, 
            HelionConsole console, IAudioSystem audioSystem, ArchiveCollection archiveCollection, 
            FpsTracker fpsTracker, MapInfoDef mapInfoDef, SkillDef skillDef, IMap map, Player? existingPlayer, 
            WorldModel? worldModel)
        {
            string displayName = mapInfoDef.GetMapNameWithPrefix(archiveCollection);
            Log.Info(displayName);
            
            TextureManager.Init(archiveCollection, mapInfoDef);
            
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
