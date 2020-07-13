using System;
using System.Collections.Generic;
using Helion.Maps.Specials;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.World.Entities.Definition.Properties.Components;

namespace Helion.World.Entities.Definition.Properties
{
    public class EntityProperties
    {
        public const int NoID = -1;
            
        public int Accuracy;
        public DecorateSpecialActivationType Activation = DecorateSpecialActivationType.Default;
        public string ActiveSound = "";
        public double Alpha = 1.0;
        public AmmoProperty Ammo = default;
        public SpecialArgs Args = default;
        public ArmorProperty Armor = default;
        public string AttackSound = "";
        public string BloodColor = "";
        public string BloodType = "";
        public int BounceCount = int.MaxValue;
        public double BounceFactor = 0.7;
        public string BounceSound = "";
        public DecorateBounceType BounceType = DecorateBounceType.None;
        public double BurnHeight = 20.0 / 4;
        public int CameraHeight;
        public int ConversationID = NoID;
        public string CrushPainSound = "";
        public DamageRangeProperty Damage;
        public double DamageFactor = 1.0;
        public string DamageType = "";
        public double DeathHeight = 20.0 / 4;
        public string DeathSound = "";
        public string DeathType = "";
        public string Decal = "";
        public int DefThreshold;
        public int DesignatedTeam;
        public string DistanceCheck = "";
        public int DontHurtShooter;
        public DropItemProperty? DropItem;
        public int ExplosionDamage;
        public int ExplosionRadius;
        public int FastSpeed;
        public double FloatBobPhase = -1.0;
        public double FloatBobStrength = -1.0;
        public int FloatSpeed = 4;
        public double Friction = 1.0;
        public int FriendlySeeBlocks = 10;
        public string Game = "";
        public int GibHealth = -1000;
        public double Gravity = 1.0;
        public int Health = 1000;
        public HealthPickupAutoUse HealthPickupAutoUse = HealthPickupAutoUse.Never;
        public double Height = 16.0;
        public string HitObituary = "";
        public string HowlSound = "";
        public InventoryProperty Inventory = new InventoryProperty();
        public double Mass = 100.0;
        public double MaxDropOffHeight = 24.0;
        public double MaxStepHeight = 24.0;
        public int MaxTargetRange;
        public int MeleeDamage;
        public int MeleeRange = 44;
        public int MeleeSound;
        public int MeleeThreshold;
        public int MinMissileChance = 200;
        public int MissileHeight;
        public int MissileType;
        public MorphProjectileProperty MorphProjectile = new MorphProjectileProperty();
        public string Obituary = "";
        public int PainChance;
        public string PainSound = "";
        public int PainThreshold;
        public string PainType = "";
        public PlayerProperty Player = new PlayerProperty();
        public PoisonDamageProperty? PoisonDamage;
        public string PoisonDamageType = "";
        public PowerupProperty Powerup = new PowerupProperty();
        public int ProjectileKickBack = 100;
        public int ProjectilePassHeight;
        public double PushFactor = 0.25;
        public PuzzleItemProperty PuzzleItem;
        public double Radius = 20.0;
        public double RadiusDamageFactor;
        public int ReactionTime = 8;
        public double RenderRadius;
        public RenderStyle RenderStyle = RenderStyle.None;
        public int RipLevelMax;
        public int RipLevelMin;
        public int RipperLevel;
        public double Scale = 1.0;
        public string SeeSound = "";
        public double SelfDamageFactor = 1.0;
        public int SpawnId = NoID;
        public string Species = "";
        public int Speed;
        public int SpriteAngle;
        public int SpriteRotation;
        public int Stamina;
        public double StealthAlpha;
        public int StencilColor;
        public string Tag = "";
        public string TeleFogDestType = "";
        public string TeleFogSourceType = "";
        public int Threshold;
        public List<string> Translation = new List<string>();
        public int VSpeed;
        public Range? VisibleAngles;
        public Range? VisiblePitch;
        public double WallBounceFactor = 0.75;
        public string WallBounceSound = "";
        public WeaponPiecesProperty? WeaponPieces;
        public WeaponProperty Weapons = new WeaponProperty();
        public int WeaveIndexXY;
        public int WeaveIndexZ;
        public int WoundHealth = 6;
        public double XScale = 1.0;
        public double YScale = 1.0;

        public EntityProperties()
        {
        }
        
        // TODO: Temporary for now, we will need only to invoke this under some situations.
        //       This also obviously doesn't work for the classes since the reference will
        //       be copied rather than how struct/value copying works.
        public EntityProperties(EntityProperties properties) 
        {
            Accuracy = properties.Accuracy;
            ActiveSound = properties.ActiveSound;
            Ammo = properties.Ammo;
            Args = properties.Args;
            Armor = properties.Armor;
            AttackSound = properties.AttackSound;
            BloodType = properties.BloodType;
            BounceFactor = properties.BounceFactor;
            BounceType = properties.BounceType;
            BurnHeight = properties.BurnHeight;
            ConversationID = properties.ConversationID;
            CrushPainSound = properties.CrushPainSound;
            Damage = properties.Damage;
            DamageFactor = properties.DamageFactor;
            DeathHeight = properties.DeathHeight;
            DeathType = properties.DeathType;
            DefThreshold = properties.DefThreshold;
            DistanceCheck = properties.DistanceCheck;
            DropItem = properties.DropItem;
            ExplosionRadius = properties.ExplosionRadius;
            FloatBobPhase = properties.FloatBobPhase;
            FloatSpeed = properties.FloatSpeed;
            FriendlySeeBlocks = properties.FriendlySeeBlocks;
            GibHealth = properties.GibHealth;
            Health = properties.Health;
            Height = properties.Height;
            HowlSound = properties.HowlSound;
            Mass = properties.Mass;
            MaxStepHeight = properties.MaxStepHeight;
            MeleeDamage = properties.MeleeDamage;
            MeleeSound = properties.MeleeSound;
            MinMissileChance = properties.MinMissileChance;
            MissileType = properties.MissileType;
            Obituary = properties.Obituary;
            PainChance = properties.PainChance;
            PainSound = properties.PainSound;
            PainType = properties.PainType;
            PoisonDamage = properties.PoisonDamage;
            Powerup = properties.Powerup;
            ProjectileKickBack = properties.ProjectileKickBack;
            PushFactor = properties.PushFactor;
            Radius = properties.Radius;
            ReactionTime = properties.ReactionTime;
            RenderStyle = properties.RenderStyle;
            RipLevelMax = properties.RipLevelMax;
            RipperLevel = properties.RipperLevel;
            SeeSound = properties.SeeSound;
            SpawnId = properties.SpawnId;
            Species = properties.Species;
            SpriteAngle = properties.SpriteAngle;
            Stamina = properties.Stamina;
            StencilColor = properties.StencilColor;
            TeleFogDestType = properties.TeleFogDestType;
            Threshold = properties.Threshold;
            VSpeed = properties.VSpeed;
            VisiblePitch = properties.VisiblePitch;
            WallBounceSound = properties.WallBounceSound;
            Weapons = properties.Weapons;
            WeaveIndexXY = properties.WeaveIndexXY;
            WoundHealth = properties.WoundHealth;
            YScale = properties.YScale;
        }
    }
}