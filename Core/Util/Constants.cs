﻿using System;

namespace Helion.Util
{
    /// <summary>
    /// A collection of constants used throughout the application.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The name of the application.
        /// </summary>
        public const string ApplicationName = "Helion";

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
        /// The public release version.
        /// </summary>
        public static readonly Version ApplicationVersion = new Version(0, 1, 0, 0);

        /// <summary>
        /// The name of a 'missing' texture in a map.
        /// </summary>
        public static readonly CIString NoTexture = "-";

        /// <summary>
        /// The index of a 'missing' texture in a map.
        /// </summary>
        public static readonly int NoTextureIndex = 0;

        /// <summary>
        /// The sky flat texture name.
        /// </summary>
        public static readonly CIString SkyTexture = "F_SKY1";

        /// <summary>
        /// The name of the decorate player class.
        /// </summary>
        public static readonly CIString PlayerClass = "DoomPlayer";

        public static readonly string ActorCrushState = "CRUSH";

        /// <summary>
        /// The name of the actor class that is at the base of all decorate
        /// inheritance hierarchies.
        /// </summary>
        public static readonly CIString BaseActorClass = "ACTOR";

        /// <summary>
        /// The name of the 4 letter sprite that should not be drawn in the
        /// renderer if encountered as a frame.
        /// </summary>
        public static readonly CIString InvisibleSprite = "TNT1";

        /// <summary>
        /// The texture name of the debug box image for seeing the bounding box
        /// of things in game.
        /// </summary>
        public static readonly CIString DebugBoxTexture = "DEBUGBOX";

        public static readonly string PlatStartSound = "plats/pt1_strt";
        public static readonly string PlatStopSound = "plats/pt1_stop";
        public static readonly string PlatMoveSound = "plats/pt1_mid";

        public static readonly string DoorOpenSlowSound = "doors/dr1_open";
        public static readonly string DoorCloseSlowSound = "doors/dr1_clos";
        public static readonly string DoorOpenFastSound = "doors/dr2_open";
        public static readonly string DoorCloseFastSound = "doors/dr2_clos";

        public static readonly string SwitchNormSound = "switches/normbutn";
        public static readonly string SwitchExitSound = "switches/exitbutn";

        public static readonly string TeleportSound = "misc/teleport";

        public static class MenuSounds
        {
            public static readonly string Activate = "menu/activate";
            public static readonly string Backup = "menu/backup";
            public static readonly string Prompt = "menu/prompt";
            public static readonly string Cursor = "menu/cursor";
            public static readonly string Change = "menu/change";
            public static readonly string Invalid = "menu/invalid";
            public static readonly string Dismiss = "menu/dismiss";
            public static readonly string Choose = "menu/choose";
            public static readonly string Clear = "menu/clear";
        }

        public static class FrameStates
        {
            public static readonly string Spawn = "Spawn";
            public static readonly string Idle = "Idle";
            public static readonly string See = "See";
            public static readonly string Melee = "Melee";
            public static readonly string Missile = "Missile";
            public static readonly string Pain = "Pain";
            public static readonly string Death = "Death";
            public static readonly string XDeath = "XDeath";
            public static readonly string Raise = "Raise";
            public static readonly string Heal = "Heal";
            public static readonly string Crash = "Crash";
            public static readonly string Crush = "Crush";
            public static readonly string Wound = "Wound";
            public static readonly string Bounce = "Bounce";
            public static readonly string Ready = "Ready";
            public static readonly string Deselect = "Deselect";
            public static readonly string Select = "Select";
            public static readonly string Fire = "Fire";
            public static readonly string Flash = "Flash";
        }

        public const double EntityShootDistance = 8192.0;
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

        public const int WeaponLowerSpeed = 6;
        public const int WeaponRaiseSpeed = 6;
        public const int WeaponBottom = 128;
        public const int WeaponTop = 32;

        public const int ExtraLightFactor = 32;

        public const double MaxSoundDistance = 2048.0;
        
        public const string MenuSelectIconActive = "M_SKULL1";
        public const string MenuSelectIconInactive = "M_SKULL2";
    }
}