using Helion.World.Sound;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public const string DefaultSkyTextureName = "SKY1";

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
        Movement = 0,
        WeaponsAndInventory = 1,
        Automap = 2,
        Files = 3,
        HudAndUI = 4,
        System = 5
    };

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InputGroupAttribute(InputType inputGroup): Attribute
    {
        public InputType InputGroup = inputGroup;
    }

    public static class Input
    {
        // Movement
        [InputGroup(InputType.Movement)]
        public const string Forward = "Forward";
        [InputGroup(InputType.Movement)]
        public const string Backward = "Backward";
        [InputGroup(InputType.Movement)]
        public const string Left = "Left";
        [InputGroup(InputType.Movement)]
        public const string Right = "Right";
        [InputGroup(InputType.Movement)]
        public const string Run = "Run";
        [InputGroup(InputType.Movement)]
        public const string Strafe = "Strafe";
        [InputGroup(InputType.Movement)]
        public const string TurnLeft = "TurnLeft";
        [InputGroup(InputType.Movement)]
        public const string TurnRight = "TurnRight";
        [InputGroup(InputType.Movement)]
        public const string LookUp = "LookUp";
        [InputGroup(InputType.Movement)]
        public const string LookDown = "LookDown";
        [InputGroup(InputType.Movement)]
        public const string CenterView = "CenterView";
        [InputGroup(InputType.Movement)]
        public const string Jump = "Jump";
        [InputGroup(InputType.Movement)]
        public const string Crouch = "Crouch";
        [InputGroup(InputType.Movement)]
        public const string Attack = "Attack"; 
        [InputGroup(InputType.Movement)]
        public const string Use = "Use";
        [InputGroup(InputType.Movement)]
        public const string GyroButton = "GyroButton";

        // Weapons/inventory
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string NextWeapon = "NextWeapon";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string PreviousWeapon = "PreviousWeapon";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponSlot1 = "WeaponSlot1";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponSlot2 = "WeaponSlot2";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponSlot3 = "WeaponSlot3";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponSlot4 = "WeaponSlot4";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponSlot5 = "WeaponSlot5";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponSlot6 = "WeaponSlot6";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponSlot7 = "WeaponSlot7";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponGroup1 = "WeaponGroup1";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponGroup2 = "WeaponGroup2";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponGroup3 = "WeaponGroup3";
        [InputGroup(InputType.WeaponsAndInventory)]
        public const string WeaponGroup4 = "WeaponGroup4";

        // Automap
        [InputGroup(InputType.Automap)]
        public const string Automap = "Automap";
        [InputGroup(InputType.Automap)]
        public const string AutoMapIncrease = "AutoMapIncrease";
        [InputGroup(InputType.Automap)]
        public const string AutoMapDecrease = "AutoMapDecrease";
        [InputGroup(InputType.Automap)]
        public const string AutoMapUp = "AutoMapUp";
        [InputGroup(InputType.Automap)]
        public const string AutoMapDown = "AutoMapDown";
        [InputGroup(InputType.Automap)]
        public const string AutoMapLeft = "AutoMapLeft";
        [InputGroup(InputType.Automap)]
        public const string AutoMapRight = "AutoMapRight";
        [InputGroup(InputType.Automap)]
        public const string AutoMapAddMarker = "AutoMapAddMarker";
        [InputGroup(InputType.Automap)]
        public const string AutoMapRemoveNearbyMarkers = "AutoMapRemoveNearbyMarkers";
        [InputGroup(InputType.Automap)]
        public const string AutoMapClearAllMarkers = "AutoMapClearAllMarkers";

        // Files
        [InputGroup(InputType.Files)]
        public const string Save = "Save";
        [InputGroup(InputType.Files)]
        public const string QuickSave = "QuickSave";
        [InputGroup(InputType.Files)]
        public const string Load = "Load";

        // HUD and in-game UI
        [InputGroup(InputType.HudAndUI)]
        public const string HudIncrease = "HudIncrease";
        [InputGroup(InputType.HudAndUI)]
        public const string HudDecrease = "HudDecrease";
        [InputGroup(InputType.HudAndUI)]
        public const string GammaCorrection = "GammaCorrection";

        // System
        [InputGroup(InputType.System)]
        public const string Pause = "Pause";
        [InputGroup(InputType.System)]
        public const string Screenshot = "Screenshot";
        [InputGroup(InputType.System)]
        public const string Console = "Console";
        [InputGroup(InputType.System)]
        public const string OptionsMenu = "OptionsMenu";
        [InputGroup(InputType.System)]
        public const string Menu = "Menu";
    }

    public static readonly HashSet<string> BaseCommands = new(
        typeof(Input)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => f.GetValue(null) as string ?? string.Empty),
        StringComparer.OrdinalIgnoreCase);

    public static readonly Dictionary<InputType?, string[]> CommandsByGroup =
        typeof(Input)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .GroupBy(f => f.GetCustomAttributes(true).Select(a => (a as InputGroupAttribute)?.InputGroup).First())
            .Where(g => g.Key != null)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.GetValue(null) as string ?? string.Empty).ToArray());

    public static readonly HashSet<string> InGameCommands =
        new HashSet<string>(CommandsByGroup[InputType.Movement].Concat(CommandsByGroup[InputType.WeaponsAndInventory]), StringComparer.OrdinalIgnoreCase);

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
    public const int TeleportOffsetDist = 16;
    public const int NullFrameIndex = 0;
    public const double DefaultFriction = 0.90625;
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
