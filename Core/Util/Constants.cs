using Helion.World.Sound;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// The index of a 'missing' texture in a map.
    /// </summary>
    public const int NoTextureIndex = 0;

    public const int NullCompatibilityTextureIndex = 1;

    public const int HitscanTestDamage = int.MinValue;

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

    public static class Input
    {
        public const string Forward = "Forward";
        public const string Backward = "Backward";
        public const string Left = "Left";
        public const string Right = "Right";
        public const string Use = "Use";
        public const string Run = "Run";
        public const string Strafe = "Strafe";
        public const string TurnLeft = "TurnLeft";
        public const string TurnRight = "TurnRight";
        public const string LookUp = "LookUp";
        public const string LookDown = "LookDown";
        public const string Jump = "Jump";
        public const string Crouch = "Crouch";
        public const string Console = "Console";
        public const string Attack = "Attack";
        public const string NextWeapon = "NextWeapon";
        public const string PreviousWeapon = "PreviousWeapon";
        public const string WeaponSlot1 = "WeaponSlot1";
        public const string WeaponSlot2 = "WeaponSlot2";
        public const string WeaponSlot3 = "WeaponSlot3";
        public const string WeaponSlot4 = "WeaponSlot4";
        public const string WeaponSlot5 = "WeaponSlot5";
        public const string WeaponSlot6 = "WeaponSlot6";
        public const string WeaponSlot7 = "WeaponSlot7";
        public const string Screenshot = "Screenshot";
        public const string HudIncrease = "HudIncrease";
        public const string HudDecrease = "HudDecrease";
        public const string AutoMapIncrease = "AutoMapIncrease";
        public const string AutoMapDecrease = "AutoMapDecrease";
        public const string AutoMapUp = "AutoMapUp";
        public const string AutoMapDown = "AutoMapDown";
        public const string AutoMapLeft = "AutoMapLeft";
        public const string AutoMapRight = "AutoMapRight";
        public const string AutoMapAddMarker = "AutoMapAddMarker";
        public const string AutoMapRemoveNearbyMarkers = "AutoMapRemoveNearbyMarkers";
        public const string AutoMapClearAllMarkers = "AutoMapClearAllMarkers";
        public const string Save = "Save";
        public const string Load = "Load";
        public const string Automap = "Automap";
        public const string CenterView = "CenterView";
        public const string Pause = "Pause";
        public const string QuickSave = "QuickSave";
        public const string OptionsMenu = "OptionsMenu";
        public const string Menu = "Menu";
    }

    public static class Fonts
    {
        public const string Small = "SmallFont";
        public const string SmallGray = "SmallFontGrayscale";
        public const string LargeHud = "LargeHudFont";
        public const string Console = "Console";
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

    public static readonly HashSet<string> BaseCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        Input.Forward,
        Input.Backward,
        Input.Left,
        Input.Right,
        Input.Use,
        Input.Run,
        Input.Strafe,
        Input.TurnLeft,
        Input.TurnRight,
        Input.LookUp,
        Input.LookDown,
        Input.Jump,
        Input.Crouch,
        Input.Console,
        Input.Attack,
        Input.NextWeapon,
        Input.PreviousWeapon,
        Input.WeaponSlot1,
        Input.WeaponSlot2,
        Input.WeaponSlot3,
        Input.WeaponSlot4,
        Input.WeaponSlot5,
        Input.WeaponSlot6,
        Input.WeaponSlot7,
        Input.Screenshot,
        Input.HudIncrease,
        Input.HudDecrease,
        Input.AutoMapIncrease,
        Input.AutoMapDecrease,
        Input.AutoMapUp,
        Input.AutoMapDown,
        Input.AutoMapLeft,
        Input.AutoMapRight,
        Input.AutoMapAddMarker,
        Input.AutoMapRemoveNearbyMarkers,
        Input.AutoMapClearAllMarkers,
        Input.Save,
        Input.Load,
        Input.Automap,
        Input.Pause,
        Input.QuickSave,
        Input.OptionsMenu,
        Input.Menu,
        Input.CenterView,
    };

    public static readonly HashSet<string> InGameCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        Input.Forward,
        Input.Backward,
        Input.Left,
        Input.Right,
        Input.Use,
        Input.Run,
        Input.Strafe,
        Input.TurnLeft,
        Input.TurnRight,
        Input.LookUp,
        Input.LookDown,
        Input.Jump,
        Input.Crouch,
        Input.Attack,
        Input.NextWeapon,
        Input.PreviousWeapon,
        Input.WeaponSlot1,
        Input.WeaponSlot2,
        Input.WeaponSlot3,
        Input.WeaponSlot4,
        Input.WeaponSlot5,
        Input.WeaponSlot6,
        Input.WeaponSlot7,
        Input.CenterView,
    };

    public static class ConsoleCommands
    {
        public const string Commands = "commands";
    }

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

    public static readonly int MaxSoundChannels = Enum.GetValues(typeof(SoundChannel)).Length;
}
