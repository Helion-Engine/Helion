using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Helion.Util.Extensions;
using Helion.World.Sound;

namespace Helion.Util;

/// <summary>
/// A collection of constants used throughout the application.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The name of the resources archive that should be loaded in all
    /// instances of the application.
    /// </summary>
    public const string AssetsFileName = "assets.pk3";

    /// <summary>
    /// How many gameticks occur per second.
    /// </summary>
    public const double TicksPerSecond = 35.0;

    /// <summary>
    /// The name of a 'missing' texture in a map.
    /// </summary>
    public const string NoTexture = "-";

    public const string BlackTextureName = "__BLACK_TEX__";

    /// <summary>
    /// The index of a 'missing' texture in a map.
    /// </summary>
    public const int NoTextureIndex = 0;

    public const int NullCompatibilityTextureIndex = 1;

    public const int HitscanTestDamage = int.MinValue;

    public const short ClearBlock = short.MaxValue;

    public const double VertexGapPush = 0.01;

    /// <summary>
    /// The name of the decorate player class.
    /// </summary>
    public const string PlayerClass = "DoomPlayer";

    /// <summary>
    /// The name of the actor class that is at the base of all decorate
    /// inheritance hierarchies.
    /// </summary>
    public const string BaseActorClass = "ACTOR";

    /// <summary>
    /// The name of the 4 letter sprite that should not be drawn in the
    /// renderer if encountered as a frame.
    /// </summary>
    public const string InvisibleSprite = "TNT1";

    /// <summary>
    /// The texture name of the debug box image for seeing the bounding box
    /// of things in game.
    /// </summary>
    public const string DebugBoxTexture = "DEBUGBOX";

    public const string PlatStartSound = "plats/pt1_strt";
    public const string PlatStopSound = "plats/pt1_stop";
    public const string PlatMoveSound = "plats/pt1_mid";

    public const string DoorOpenSlowSound = "doors/dr1_open";
    public const string DoorCloseSlowSound = "doors/dr1_clos";
    public const string DoorOpenFastSound = "doors/dr2_open";
    public const string DoorCloseFastSound = "doors/dr2_clos";

    public const string SwitchNormSound = "switches/normbutn";
    public const string SwitchExitSound = "switches/exitbutn";

    public const string TeleportSound = "misc/teleport";

    public const string MusicChanger = "MusicChanger";
    public const string AmbientSound = "AmbientSound";

    public const string DefaultSkyTextureName = "SKY1";

    public const string DefaultBackgroundImage = "helion-background";
    public const string Endoom = "ENDOOM";

    public static class MenuSounds
    {
        public const string Activate = "menu/activate";
        public const string Backup = "menu/backup";
        public const string Prompt = "menu/prompt";
        public const string Cursor = "menu/cursor";
        public const string Change = "menu/change";
        public const string Invalid = "menu/invalid";
        public const string Dismiss = "menu/dismiss";
        public const string Choose = "menu/choose";
        public const string Clear = "menu/clear";
    }

    public static class FrameStates
    {
        public const string Spawn = "Spawn";
        public const string Idle = "Idle";
        public const string See = "See";
        public const string Melee = "Melee";
        public const string Missile = "Missile";
        public const string Pain = "Pain";
        public const string Death = "Death";
        public const string XDeath = "XDeath";
        public const string Raise = "Raise";
        public const string Heal = "Heal";
        public const string Crash = "Crash";
        public const string GenericCrush = "GenericCrush";
        public const string Crush = "Crush";
        public const string Wound = "Wound";
        public const string Bounce = "Bounce";
        public const string Ready = "Ready";
        public const string Deselect = "Deselect";
        public const string Select = "Select";
        public const string Fire = "Fire";
        public const string Flash = "Flash";
        public const string Pickup = "Pickup";
    }

    public enum InputType
    {
        Uncategorized = 0,
        Movement = 1,
        WeaponsAndInventory = 2,
        Automap = 3,
        Files = 4,
        HudAndUI = 5,
        System = 6,
        Map = 7,
    };

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InputAttribute(InputType inputGroup, string? uiString = null) : Attribute
    {
        public InputType InputGroup = inputGroup;
        public string? UIString = uiString;
    }

    public static class Input
    {
        // Movement
        [Input(InputType.Movement)]
        public const string Forward = "Forward";
        [Input(InputType.Movement)]
        public const string Backward = "Backward";
        [Input(InputType.Movement)]
        public const string Left = "Left";
        [Input(InputType.Movement)]
        public const string Right = "Right";
        [Input(InputType.Movement)]
        public const string Run = "Run";
        [Input(InputType.Movement)]
        public const string Strafe = "Strafe";
        [Input(InputType.Movement)]
        public const string TurnLeft = "TurnLeft";
        [Input(InputType.Movement)]
        public const string TurnRight = "TurnRight";
        [Input(InputType.Movement)]
        public const string LookUp = "LookUp";
        [Input(InputType.Movement)]
        public const string LookDown = "LookDown";
        [Input(InputType.Movement)]
        public const string CenterView = "CenterView";
        [Input(InputType.Movement)]
        public const string Jump = "Jump";
        [Input(InputType.Movement)]
        public const string Crouch = "Crouch";
        [Input(InputType.Movement)]
        public const string Attack = "Attack";
        [Input(InputType.Movement)]
        public const string Use = "Use";
        [Input(InputType.Movement)]
        public const string GyroButton = "GyroButton";

        // Weapons/inventory
        [Input(InputType.WeaponsAndInventory)]
        public const string NextWeapon = "NextWeapon";
        [Input(InputType.WeaponsAndInventory)]
        public const string PreviousWeapon = "PreviousWeapon";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponSlot1 = "WeaponSlot1";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponSlot2 = "WeaponSlot2";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponSlot3 = "WeaponSlot3";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponSlot4 = "WeaponSlot4";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponSlot5 = "WeaponSlot5";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponSlot6 = "WeaponSlot6";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponSlot7 = "WeaponSlot7";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponGroup1 = "WeaponGroup1";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponGroup2 = "WeaponGroup2";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponGroup3 = "WeaponGroup3";
        [Input(InputType.WeaponsAndInventory)]
        public const string WeaponGroup4 = "WeaponGroup4";

        // Automap
        [Input(InputType.Automap, "Automap")]
        public const string Automap = "Automap";
        [Input(InputType.Automap, "Automap Zoom In")]
        public const string AutoMapIncrease = "AutoMapIncrease";
        [Input(InputType.Automap, "Automap Zoom Out")]
        public const string AutoMapDecrease = "AutoMapDecrease";
        [Input(InputType.Automap, "Automap Up")]
        public const string AutoMapUp = "AutoMapUp";
        [Input(InputType.Automap, "Automap Down")]
        public const string AutoMapDown = "AutoMapDown";
        [Input(InputType.Automap, "Automap Left")]
        public const string AutoMapLeft = "AutoMapLeft";
        [Input(InputType.Automap, "Automap Right")]
        public const string AutoMapRight = "AutoMapRight";
        [Input(InputType.Automap, "Automap Add Marker")]
        public const string AutoMapAddMarker = "AutoMapAddMarker";
        [Input(InputType.Automap, "Automap Remove Nearby Markers")]
        public const string AutoMapRemoveNearbyMarkers = "AutoMapRemoveNearbyMarkers";
        [Input(InputType.Automap, "Automap Clear All Markers")]
        public const string AutoMapClearAllMarkers = "AutoMapClearAllMarkers";

        // Files
        [Input(InputType.Files)]
        public const string Save = "Save";
        [Input(InputType.Files)]
        public const string QuickSave = "QuickSave";
        [Input(InputType.Files)]
        public const string Load = "Load";
        [Input(InputType.Files)]
        public const string QuickLoad = "QuickLoad";

        // HUD and in-game UI
        [Input(InputType.HudAndUI)]
        public const string HudIncrease = "HudIncrease";
        [Input(InputType.HudAndUI)]
        public const string HudDecrease = "HudDecrease";
        [Input(InputType.HudAndUI)]
        public const string GammaCorrection = "GammaCorrection";

        // System
        [Input(InputType.System)]
        public const string Pause = "Pause";
        [Input(InputType.System)]
        public const string Screenshot = "Screenshot";
        [Input(InputType.System)]
        public const string Console = "Console";
        [Input(InputType.System)]
        public const string OptionsMenu = "OptionsMenu";
        [Input(InputType.System)]
        public const string Menu = "Menu";

        [Input(InputType.Map)]
        public const string NextMap = "NextMap";
        [Input(InputType.Map)]
        public const string PreviousMap = "PreviousMap";
    }

    private static StringBuilder m_builder = new();

    /// <summary>
    /// All regular, bindable commands available in the game
    /// </summary>
    public static readonly HashSet<string> BaseCommands = new(
        typeof(Input)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.GetValue(null) as string ?? string.Empty),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Commands, grouped by purpose (e.g. movement)
    /// </summary>
    public static readonly Dictionary<string, string[]> CommandsByGroup =
        typeof(Input)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .GroupBy(f => f.GetCustomAttribute<InputAttribute>()?.InputGroup ?? InputType.Uncategorized)
            .ToDictionary(
                g => StringExtensions.WithWordSpaces(g.Key.ToString(), m_builder),
                g => g.Select(f => f.GetValue(null) as string ?? string.Empty).ToArray(),
                StringComparer.OrdinalIgnoreCase);

    public static readonly List<string> CommandGroupLabels = CommandsByGroup.Keys.Append("Custom").ToList();

    /// <summary>
    /// Commands, with their corresponding UI labels, if specified (else, just spaces added before each upper-case letter)
    /// </summary>
    public static readonly Dictionary<string, string> CommandUILabels =
        typeof(Input)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .GroupBy(f => f.GetValue(null) as string ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.First().GetCustomAttribute<InputAttribute>()?.UIString ?? StringExtensions.WithWordSpaces(g.Key, m_builder));

    public static readonly HashSet<string> InGameCommands =
        new HashSet<string>(CommandsByGroup["Movement"].Concat(CommandsByGroup["Weapons And Inventory"]), StringComparer.OrdinalIgnoreCase);

    public static class ConsoleCommands
    {
        public const string Commands = "commands";
    }

    public static class Fonts
    {
        public const string Small = "SmallFont";
        public const string SmallGray = "SmallFontGrayscale";
        public const string LargeHud = "LargeHudFont";
        public const string Console = "Console";
        public const string SmallGrayFixedWidthNumbers = "SmallFontGrayscaleFixedWidthNumbers";
        public const string VGA = "flexi-ibm-vga-true.regular";
    };

    public static class LightBuffer
    {
        public const int DarkIndex = 0;
        public const int FullBrightIndex = 1;
        public const int ColorMapStartIndex = 2;
        public const int BufferSize = 3;
        public const int SectorIndexStart = ColorMapStartIndex + ColorMapCount;
        public const int FloorOffset = 0;
        public const int CeilingOffset = 1;
        public const int WallOffset = 2;
        public const int UpperWallOffset = 0;
        public const int MiddleWallOffset = 1;
        public const int LowerWallOffset = 2;
        public const int ColorMapCount = 32;
    }

    public static class ColormapBuffer
    {
        public const int DarkIndex = 0;
        public const int FullBrightIndex = 1;
        public const int ColorMapStartIndex = 2;
        public const int BufferSize = 3;
        public const int SectorIndexStart = ColorMapStartIndex + ColorMapCount;
        public const int FloorOffset = 0;
        public const int CeilingOffset = 1;
        public const int WallOffset = 2;
        public const int ColorMapCount = 32;
    }

    public const double Epsilon = 0.00001;
    public const double EntityShootDistance = 2048.0;
    public const double EntityMeleeDistance = 64.0;
    public const double DefaultSpreadAngle = 5.625 * Math.PI / 180.0;
    public const double SuperShotgunSpreadAngle = 11.2 * Math.PI / 180.0;
    public const double SuperShotgunSpreadPitch = 7.1 * Math.PI / 180.0;
    public const int ShotgunBullets = 7;
    public const int SuperShotgunBullets = 20;
    public const double PosRandomSpread = 22.4121094 * Math.PI / 180;
    public const double ShadowRandomSpread = 44.8242188 * Math.PI / 180;
    public const double AutoAimSpread = 5.625 * Math.PI / 180;
    public const int AutoAimTracers = 2;
    public const double MancSpread = Math.PI / 16;
    public const double TracerAngle = 16.0 * Math.PI / 180;
    public const double MeleeAngle = 5 * Math.PI / 180;
    public const double PuffRandZ = (1 << 10) / 65536.0;
    public const int TeleportOffsetDist = 20;
    public const int NullFrameIndex = 0;
    public const double DefaultFriction = 0.90625;
    public const double DefaultMoveFactor = 1.0;
    public const double DefaultFrictionFactor = 2048.0 / 65536.0;
    public const int DefaultGroupNumber = 0;

    public const int WeaponLowerSpeed = 6;
    public const int WeaponRaiseSpeed = 6;
    public const int WeaponBottom = 128;
    public const int WeaponTop = 32;

    public const int ExtraLightFactor = 3;

    public const double MaxSoundDistance = 2048.0;

    public const string MenuSelectIconActive = "M_SKULL1";
    public const string MenuSelectIconInactive = "M_SKULL2";

    public const float DoomVirtualAspectRatio = 1.33333337f;

    public const int MaxTextureHeight = 16384;

    public const double DoomSlowCrushSpeed = 0.125;

    public static readonly int MaxSoundChannels = Enum.GetValues<SoundChannel>().Length;

    public const int DefaultMaxDistance = 6000;

    public const int ScreenshotSaveWidth = 320;
    public const int ScreenshotSaveHeight = 240;
}
