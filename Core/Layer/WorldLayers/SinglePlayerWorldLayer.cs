using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Render.Commands;
using Helion.Render.Shared;
using Helion.Render.Shared.Drawers;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Models;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Impl.SinglePlayer;
using Helion.World.StatusBar;
using NLog;
using System;
using Helion.Util.Timing;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.WorldLayers
{
    public class SinglePlayerWorldLayer : WorldLayer
    {
        private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly (ConfigValueEnum<Key>, TickCommands)[] m_consumeDownKeys;
        private readonly (ConfigValueEnum<Key>, TickCommands)[] m_consumePressedKeys;
        private readonly WorldHudDrawer m_worldHudDrawer;
        private readonly SinglePlayerWorld m_world;
        private TickerInfo m_lastTickInfo = new(0, 0);
        private TickCommand m_tickCommand = new();
        private bool m_disposed;

        public override WorldBase World => m_world;
        public MapInfoDef CurrentMap { get; set; }

        private SinglePlayerWorldLayer(GameLayer parent, Config config, HelionConsole console, ArchiveCollection archiveCollection,
            IAudioSystem audioSystem, SinglePlayerWorld world, MapInfoDef mapInfoDef)
            : base(parent, config, console, archiveCollection, audioSystem)
        {
            CurrentMap = mapInfoDef;
            m_world = world;
            m_worldHudDrawer = new(archiveCollection);

            m_ticker.Start();

            m_consumeDownKeys = new[]
            {
                (config.Controls.Forward,  TickCommands.Forward),
                (config.Controls.Left,     TickCommands.Left),
                (config.Controls.Backward, TickCommands.Backward),
                (config.Controls.Right,    TickCommands.Right),
                (config.Controls.Jump,     TickCommands.Jump),
                (config.Controls.Crouch,   TickCommands.Crouch),
                (config.Controls.Attack,   TickCommands.Attack),
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

        ~SinglePlayerWorldLayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public static SinglePlayerWorldLayer? Create(GameLayer parent, Config config, HelionConsole console, 
            IAudioSystem audioSystem, ArchiveCollection archiveCollection, MapInfoDef mapInfoDef, 
            SkillDef skillDef, IMap map, Player? existingPlayer, WorldModel? worldModel)
        {
            string displayName = mapInfoDef.GetMapNameWithPrefix(archiveCollection);
            Log.Info(displayName);
            TextureManager.Init(archiveCollection, mapInfoDef);
            SinglePlayerWorld? world = CreateWorldGeometry(config, audioSystem, archiveCollection, mapInfoDef, skillDef, map,
                existingPlayer, worldModel);
            if (world == null)
                return null;
            return new SinglePlayerWorldLayer(parent, config, console, archiveCollection, audioSystem, world, mapInfoDef);
        }

        private static SinglePlayerWorld? CreateWorldGeometry(Config config, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection, MapInfoDef mapDef, SkillDef skillDef, IMap map,
                Player? existingPlayer, WorldModel? worldModel)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map, config);
            if (geometry == null)
                return null;

            try
            {
                return new SinglePlayerWorld(config, archiveCollection, audioSystem, geometry, mapDef, skillDef, map,
                    existingPlayer, worldModel);
            }
            catch(HelionException e)
            {
                Log.Error(e.Message);
            }

            return null;
        }

        public override void HandleInput(InputEvent input)
        {
            if (!m_world.Paused)
            {
                HandleMovementInput(input);
                m_world.HandleFrameInput(input);
            }

            if (input.ConsumeKeyPressed(Config.Controls.HudDecrease))
                ChangeHudSize(false);
            else if (input.ConsumeKeyPressed(Config.Controls.HudIncrease))
                ChangeHudSize(true);
            else if (input.ConsumeKeyPressed(Config.Controls.Save))
                OpenSaveGameMenu();
            else if (input.ConsumeKeyPressed(Config.Controls.Load))
                OpenLoadGameMenu();
			
			base.HandleInput(input);
        }

        private void OpenSaveGameMenu()
        {
            // TODO
            // SaveMenu saveMenu = new();
            // MenuLayer menuLayer = new(Parent, saveMenu);
            // Parent.Add(menuLayer);
        }

        private void OpenLoadGameMenu()
        {
            // TODO
        }

        public override void RunLogic()
        {
            m_lastTickInfo = m_ticker.GetTickerInfo();
            int ticksToRun = m_lastTickInfo.Ticks;

            if (ticksToRun <= 0)
                return;

            m_world.HandleTickCommand(m_tickCommand);
            m_tickCommand = new TickCommand();

            if (ticksToRun > TickOverflowThreshold)
            {
                Log.Warn("Large tick overflow detected (likely due to delays/lag), reducing ticking amount");
                ticksToRun = 1;
            }

            while (ticksToRun > 0)
            {
                m_world.Tick();
                ticksToRun--;
            }
            
            base.RunLogic();
        }

        public override void Render(RenderCommands renderCommands)
        {
            Camera camera = m_world.Player.GetCamera(m_lastTickInfo.Fraction);
            Player player = m_world.Player;
            renderCommands.DrawWorld(m_world, camera, m_lastTickInfo.Ticks, m_lastTickInfo.Fraction, player);

            // TODO: Should not be passing the window dimension as the viewport.
            m_worldHudDrawer.Draw(player, m_world, m_lastTickInfo.Fraction, Console, renderCommands.WindowDimension,
                Config, renderCommands);
            
            base.Render(renderCommands);
        }

        protected override void PerformDispose()
        {
            if (m_disposed)
                return;

            m_world.Dispose();

            m_disposed = true;

            base.PerformDispose();
        }

        private void HandleMovementInput(InputEvent input)
        {
            foreach (var (inputKey, command) in m_consumeDownKeys)
                if (input.ConsumeKeyPressedOrDown(inputKey))
                    m_tickCommand.Add(command);

            foreach (var (inputKey, command) in m_consumePressedKeys)
                if (input.ConsumeKeyPressed(inputKey))
                    m_tickCommand.Add(command);
        }

        private void ChangeHudSize(bool increase)
        {
            int size = (int)Config.Hud.StatusBarSize.Value;
            if (increase)
                size++;
            else
                size--;

            size = Math.Clamp(size, 0, Enum.GetValues(typeof(StatusBarSizeType)).Length - 1);

            if ((int)Config.Hud.StatusBarSize.Value != size)
            {
                Config.Hud.StatusBarSize.Set((StatusBarSizeType)size);
                m_world.SoundManager.PlayStaticSound(Constants.MenuSounds.Change);
            }
        }
    }
}