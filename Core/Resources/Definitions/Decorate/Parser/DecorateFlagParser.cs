namespace Helion.Resources.Definitions.Decorate.Parser
{
    /// <summary>
    /// Handles parsing the flag components of an actor.
    /// </summary>
    public partial class DecorateParser
    {
        private void ConsumeActorFlag()
        {
            bool flagValue = false;
            if (ConsumeIf('+'))
                flagValue = true;
            else
                Consume('-');

            string flagName = ConsumeIdentifier();

            if (ConsumeIf('.'))
            {
                switch (flagName.ToUpper())
                {
                case "INVENTORY":
                    SetInventoryFlag(flagValue);
                    break;
                case "PLAYERPAWN":
                    SetPlayerPawnFlag(flagValue);
                    break;
                case "WEAPON":
                    SetWeaponFlag(flagValue);
                    break;
                default:
                    Log.Warn("Unknown flag prefix '{0}' for actor {1}", flagName, m_currentDefinition.Name);
                    break;
                }
            }
            else
                SetTopLevelFlag(flagName, flagValue);
        }

        private void SetInventoryFlag(bool flagValue)
        {
            string nestedFlag = ConsumeIdentifier();
            switch (nestedFlag.ToUpper())
            {
            case "ADDITIVETIME":
                m_currentDefinition.Flags.Inventory.AdditiveTime = flagValue;
                break;
            case "ALWAYSPICKUP":
                m_currentDefinition.Flags.Inventory.AlwaysPickup = flagValue;
                break;
            case "ALWAYSRESPAWN":
                m_currentDefinition.Flags.Inventory.AlwaysRespawn = flagValue;
                break;
            case "AUTOACTIVATE":
                m_currentDefinition.Flags.Inventory.AutoActivate = flagValue;
                break;
            case "BIGPOWERUP":
                m_currentDefinition.Flags.Inventory.BigPowerup = flagValue;
                break;
            case "FANCYPICKUPSOUND":
                m_currentDefinition.Flags.Inventory.FancyPickupSound = flagValue;
                break;
            case "HUBPOWER":
                m_currentDefinition.Flags.Inventory.HubPower = flagValue;
                break;
            case "IGNORESKILL":
                m_currentDefinition.Flags.Inventory.IgnoreSkill = flagValue;
                break;
            case "INTERHUBSTRIP":
                m_currentDefinition.Flags.Inventory.InterHubStrip = flagValue;
                break;
            case "INVBAR":
                m_currentDefinition.Flags.Inventory.Invbar = flagValue;
                break;
            case "ISARMOR":
                m_currentDefinition.Flags.Inventory.IsArmor = flagValue;
                break;
            case "ISHEALTH":
                m_currentDefinition.Flags.Inventory.IsHealth = flagValue;
                break;
            case "KEEPDEPLETED":
                m_currentDefinition.Flags.Inventory.KeepDepleted = flagValue;
                break;
            case "NEVERRESPAWN":
                m_currentDefinition.Flags.Inventory.NeverRespawn = flagValue;
                break;
            case "NOATTENPICKUPSOUND":
                m_currentDefinition.Flags.Inventory.NoAttenPickupSound = flagValue;
                break;
            case "NOSCREENBLINK":
                m_currentDefinition.Flags.Inventory.NoScreenBlink = flagValue;
                break;
            case "NOSCREENFLASH":
                m_currentDefinition.Flags.Inventory.NoScreenFlash = flagValue;
                break;
            case "NOTELEPORTFREEZE":
                m_currentDefinition.Flags.Inventory.NoTeleportFreeze = flagValue;
                break;
            case "PERSISTENTPOWER":
                m_currentDefinition.Flags.Inventory.PersistentPower = flagValue;
                break;
            case "PICKUPFLASH":
                m_currentDefinition.Flags.Inventory.PickupFlash = flagValue;
                break;
            case "QUIET":
                m_currentDefinition.Flags.Inventory.Quiet = flagValue;
                break;
            case "RESTRICTABSOLUTELY":
                m_currentDefinition.Flags.Inventory.RestrictAbsolutely = flagValue;
                break;
            case "TOSSED":
                m_currentDefinition.Flags.Inventory.Tossed = flagValue;
                break;
            case "TRANSFER":
                m_currentDefinition.Flags.Inventory.Transfer = flagValue;
                break;
            case "UNCLEARABLE":
                m_currentDefinition.Flags.Inventory.Unclearable = flagValue;
                break;
            case "UNDROPPABLE":
                m_currentDefinition.Flags.Inventory.Undroppable = flagValue;
                break;
            case "UNTOSSABLE":
                m_currentDefinition.Flags.Inventory.Untossable = flagValue;
                break;
            default:
                Log.Warn("Unknown inventory flag suffix '{0}' for actor {1} (in particular, INVENTORY.{2})", nestedFlag, m_currentDefinition.Name);
                break;
            }
        }

        private void SetPlayerPawnFlag(bool flagValue)
        {
            string nestedFlag = ConsumeIdentifier();
            switch (nestedFlag.ToUpper())
            {
            case "CANSUPERMORPH":
                m_currentDefinition.Flags.PlayerPawn.CanSuperMorph = flagValue;
                break;
            case "CROUCHABLEMORPH":
                m_currentDefinition.Flags.PlayerPawn.CrouchableMorph = flagValue;
                break;
            case "NOTHRUSTWHENINVUL":
                m_currentDefinition.Flags.PlayerPawn.NoThrustWhenInvul = flagValue;
                break;
            default:
                Log.Warn("Unknown playerpawn flag suffix '{0}' for actor {1} (in particular, PLAYERPAWN.{2})", nestedFlag, m_currentDefinition.Name);
                break;
            }
        }

        private void SetWeaponFlag(bool flagValue)
        {
            string nestedFlag = ConsumeIdentifier();
            switch (nestedFlag.ToUpper())
            {
            case "ALTAMMOOPTIONAL":
                m_currentDefinition.Flags.Weapon.AltAmmoOptional = flagValue;
                break;
            case "ALTUSESBOTH":
                m_currentDefinition.Flags.Weapon.AltUsesBoth = flagValue;
                break;
            case "AMMOCHECKBOTH":
                m_currentDefinition.Flags.Weapon.AmmoCheckBoth = flagValue;
                break;
            case "AMMOOPTIONAL":
                m_currentDefinition.Flags.Weapon.AmmoOptional = flagValue;
                break;
            case "AXEBLOOD":
                m_currentDefinition.Flags.Weapon.AxeBlood = flagValue;
                break;
            case "BFG":
                m_currentDefinition.Flags.Weapon.Bfg = flagValue;
                break;
            case "CHEATNOTWEAPON":
                m_currentDefinition.Flags.Weapon.CheatNotWeapon = flagValue;
                break;
            case "DONTBOB":
                m_currentDefinition.Flags.Weapon.DontBob = flagValue;
                break;
            case "EXPLOSIVE":
                m_currentDefinition.Flags.Weapon.Explosive = flagValue;
                break;
            case "MELEEWEAPON":
                m_currentDefinition.Flags.Weapon.MeleeWeapon = flagValue;
                break;
            case "NOALERT":
                m_currentDefinition.Flags.Weapon.NoAlert = flagValue;
                break;
            case "NOAUTOAIM":
                m_currentDefinition.Flags.Weapon.NoAutoaim = flagValue;
                break;
            case "NOAUTOFIRE":
                m_currentDefinition.Flags.Weapon.NoAutofire = flagValue;
                break;
            case "NODEATHDESELECT":
                m_currentDefinition.Flags.Weapon.NoDeathDeselect = flagValue;
                break;
            case "NODEATHINPUT":
                m_currentDefinition.Flags.Weapon.NoDeathInput = flagValue;
                break;
            case "NOAUTOSWITCH":
                m_currentDefinition.Flags.Weapon.NoAutoSwitch = flagValue;
                break;
            case "POWEREDUP":
                m_currentDefinition.Flags.Weapon.PoweredUp = flagValue;
                break;
            case "PRIMARYUSESBOTH":
                m_currentDefinition.Flags.Weapon.PrimaryUsesBoth = flagValue;
                break;
            case "READYSNDHALF":
                m_currentDefinition.Flags.Weapon.ReadySndHalf = flagValue;
                break;
            case "STAFF2KICKBACK":
                m_currentDefinition.Flags.Weapon.Staff2Kickback = flagValue;
                break;
            case "SPAWN":
                m_currentDefinition.Flags.Weapon.Spawn = flagValue;
                break;
            case "WIMPY_WEAPON":
                m_currentDefinition.Flags.Weapon.WimpyWeapon = flagValue;
                break;
            default:
                Log.Warn("Unknown weapon flag suffix '{0}' for actor '{1}'", nestedFlag, m_currentDefinition.Name);
                break;
            }
        }

        private void SetTopLevelFlag(string flagName, bool flagValue)
        {
            switch (flagName.ToUpper())
            {
            case "ABSMASKANGLE":
                m_currentDefinition.Flags.AbsMaskAngle = flagValue;
                break;
            case "ABSMASKPITCH":
                m_currentDefinition.Flags.AbsMaskPitch = flagValue;
                break;
            case "ACTIVATEIMPACT":
                m_currentDefinition.Flags.ActivateImpact = flagValue;
                break;
            case "ACTIVATEMCROSS":
                m_currentDefinition.Flags.ActivateMCross = flagValue;
                break;
            case "ACTIVATEPCROSS":
                m_currentDefinition.Flags.ActivatePCross = flagValue;
                break;
            case "ACTLIKEBRIDGE":
                m_currentDefinition.Flags.ActLikeBridge = flagValue;
                break;
            case "ADDITIVEPOISONDAMAGE":
                m_currentDefinition.Flags.AdditivePoisonDamage = flagValue;
                break;
            case "ADDITIVEPOISONDURATION":
                m_currentDefinition.Flags.AdditivePoisonDuration = flagValue;
                break;
            case "AIMREFLECT":
                m_currentDefinition.Flags.AimReflect = flagValue;
                break;
            case "ALLOWBOUNCEONACTORS":
                m_currentDefinition.Flags.AllowBounceOnActors = flagValue;
                break;
            case "ALLOWPAIN":
                m_currentDefinition.Flags.AllowPain = flagValue;
                break;
            case "ALLOWPARTICLES":
                m_currentDefinition.Flags.AllowParticles = flagValue;
                break;
            case "ALLOWTHRUFLAGS":
                m_currentDefinition.Flags.AllowThruFlags = flagValue;
                break;
            case "ALWAYSFAST":
                m_currentDefinition.Flags.AlwaysFast = flagValue;
                break;
            case "ALWAYSPUFF":
                m_currentDefinition.Flags.AlwaysPuff = flagValue;
                break;
            case "ALWAYSRESPAWN":
                m_currentDefinition.Flags.AlwaysRespawn = flagValue;
                break;
            case "ALWAYSTELEFRAG":
                m_currentDefinition.Flags.AlwaysTelefrag = flagValue;
                break;
            case "AMBUSH":
                m_currentDefinition.Flags.Ambush = flagValue;
                break;
            case "AVOIDMELEE":
                m_currentDefinition.Flags.AvoidMelee = flagValue;
                break;
            case "BLASTED":
                m_currentDefinition.Flags.Blasted = flagValue;
                break;
            case "BLOCKASPLAYER":
                m_currentDefinition.Flags.BlockAsPlayer = flagValue;
                break;
            case "BLOCKEDBYSOLIDACTORS":
                m_currentDefinition.Flags.BlockedBySolidActors = flagValue;
                break;
            case "BLOODLESSIMPACT":
                m_currentDefinition.Flags.BloodlessImpact = flagValue;
                break;
            case "BLOODSPLATTER":
                m_currentDefinition.Flags.BloodSplatter = flagValue;
                break;
            case "BOSS":
                m_currentDefinition.Flags.Boss = flagValue;
                break;
            case "BOSSDEATH":
                m_currentDefinition.Flags.BossDeath = flagValue;
                break;
            case "BOUNCEAUTOOFF":
                m_currentDefinition.Flags.BounceAutoOff = flagValue;
                break;
            case "BOUNCEAUTOOFFFLOORONLY":
                m_currentDefinition.Flags.BounceAutoOffFloorOnly = flagValue;
                break;
            case "BOUNCELIKEHERETIC":
                m_currentDefinition.Flags.BounceLikeHeretic = flagValue;
                break;
            case "BOUNCEONACTORS":
                m_currentDefinition.Flags.BounceOnActors = flagValue;
                break;
            case "BOUNCEONCEILINGS":
                m_currentDefinition.Flags.BounceOnCeilings = flagValue;
                break;
            case "BOUNCEONFLOORS":
                m_currentDefinition.Flags.BounceOnFloors = flagValue;
                break;
            case "BOUNCEONUNRIPPABLES":
                m_currentDefinition.Flags.BounceOnUnrippables = flagValue;
                break;
            case "BOUNCEONWALLS":
                m_currentDefinition.Flags.BounceOnWalls = flagValue;
                break;
            case "BRIGHT":
                m_currentDefinition.Flags.Bright = flagValue;
                break;
            case "BUDDHA":
                m_currentDefinition.Flags.Buddha = flagValue;
                break;
            case "BUMPSPECIAL":
                m_currentDefinition.Flags.BumpSpecial = flagValue;
                break;
            case "CANBLAST":
                m_currentDefinition.Flags.CanBlast = flagValue;
                break;
            case "CANBOUNCEWATER":
                m_currentDefinition.Flags.CanBounceWater = flagValue;
                break;
            case "CANNOTPUSH":
                m_currentDefinition.Flags.CannotPush = flagValue;
                break;
            case "CANPASS":
                m_currentDefinition.Flags.CanPass = flagValue;
                break;
            case "CANPUSHWALLS":
                m_currentDefinition.Flags.CanPushWalls = flagValue;
                break;
            case "CANTLEAVEFLOORPIC":
                m_currentDefinition.Flags.CantLeaveFloorPic = flagValue;
                break;
            case "CANTSEEK":
                m_currentDefinition.Flags.CantSeek = flagValue;
                break;
            case "CANUSEWALLS":
                m_currentDefinition.Flags.CanUseWalls = flagValue;
                break;
            case "CAUSEPAIN":
                m_currentDefinition.Flags.CausePain = flagValue;
                break;
            case "CEILINGHUGGER":
                m_currentDefinition.Flags.CeilingHugger = flagValue;
                break;
            case "CORPSE":
                m_currentDefinition.Flags.Corpse = flagValue;
                break;
            case "COUNTITEM":
                m_currentDefinition.Flags.CountItem = flagValue;
                break;
            case "COUNTKILL":
                m_currentDefinition.Flags.CountKill = flagValue;
                break;
            case "COUNTSECRET":
                m_currentDefinition.Flags.CountSecret = flagValue;
                break;
            case "DEFLECT":
                m_currentDefinition.Flags.Deflect = flagValue;
                break;
            case "DEHEXPLOSION":
                m_currentDefinition.Flags.DehExplosion = flagValue;
                break;
            case "DOHARMSPECIES":
                m_currentDefinition.Flags.DoHarmSpecies = flagValue;
                break;
            case "DONTBLAST":
                m_currentDefinition.Flags.DontBlast = flagValue;
                break;
            case "DONTBOUNCEONSHOOTABLES":
                m_currentDefinition.Flags.DontBounceOnShootables = flagValue;
                break;
            case "DONTBOUNCEONSKY":
                m_currentDefinition.Flags.DontBounceOnSky = flagValue;
                break;
            case "DONTCORPSE":
                m_currentDefinition.Flags.DontCorpse = flagValue;
                break;
            case "DONTDRAIN":
                m_currentDefinition.Flags.DontDrain = flagValue;
                break;
            case "DONTFACETALKER":
                m_currentDefinition.Flags.DontFaceTalker = flagValue;
                break;
            case "DONTFALL":
                m_currentDefinition.Flags.DontFall = flagValue;
                break;
            case "DONTGIB":
                m_currentDefinition.Flags.DontGib = flagValue;
                break;
            case "DONTHARMCLASS":
                m_currentDefinition.Flags.DontHarmClass = flagValue;
                break;
            case "DONTHARMSPECIES":
                m_currentDefinition.Flags.DontHarmSpecies = flagValue;
                break;
            case "DONTHURTSPECIES":
                m_currentDefinition.Flags.DontHurtSpecies = flagValue;
                break;
            case "DONTINTERPOLATE":
                m_currentDefinition.Flags.DontInterpolate = flagValue;
                break;
            case "DONTMORPH":
                m_currentDefinition.Flags.DontMorph = flagValue;
                break;
            case "DONTOVERLAP":
                m_currentDefinition.Flags.DontOverlap = flagValue;
                break;
            case "DONTREFLECT":
                m_currentDefinition.Flags.DontReflect = flagValue;
                break;
            case "DONTRIP":
                m_currentDefinition.Flags.DontRip = flagValue;
                break;
            case "DONTSEEKINVISIBLE":
                m_currentDefinition.Flags.DontSeekInvisible = flagValue;
                break;
            case "DONTSPLASH":
                m_currentDefinition.Flags.DontSplash = flagValue;
                break;
            case "DONTSQUASH":
                m_currentDefinition.Flags.DontSquash = flagValue;
                break;
            case "DONTTHRUST":
                m_currentDefinition.Flags.DontThrust = flagValue;
                break;
            case "DONTTRANSLATE":
                m_currentDefinition.Flags.DontTranslate = flagValue;
                break;
            case "DOOMBOUNCE":
                m_currentDefinition.Flags.DoomBounce = flagValue;
                break;
            case "DORMANT":
                m_currentDefinition.Flags.Dormant = flagValue;
                break;
            case "DROPOFF":
                m_currentDefinition.Flags.Dropoff = flagValue;
                break;
            case "DROPPED":
                m_currentDefinition.Flags.Dropped = flagValue;
                break;
            case "EXPLOCOUNT":
                m_currentDefinition.Flags.ExploCount = flagValue;
                break;
            case "EXPLODEONWATER":
                m_currentDefinition.Flags.ExplodeOnWater = flagValue;
                break;
            case "EXTREMEDEATH":
                m_currentDefinition.Flags.ExtremeDeath = flagValue;
                break;
            case "FASTER":
                m_currentDefinition.Flags.Faster = flagValue;
                break;
            case "FASTMELEE":
                m_currentDefinition.Flags.FastMelee = flagValue;
                break;
            case "FIREDAMAGE":
                m_currentDefinition.Flags.FireDamage = flagValue;
                break;
            case "FIRERESIST":
                m_currentDefinition.Flags.FireResist = flagValue;
                break;
            case "FIXMAPTHINGPOS":
                m_currentDefinition.Flags.FixMapThingPos = flagValue;
                break;
            case "FLATSPRITE":
                m_currentDefinition.Flags.FlatSprite = flagValue;
                break;
            case "FLOAT":
                m_currentDefinition.Flags.Float = flagValue;
                break;
            case "FLOATBOB":
                m_currentDefinition.Flags.FloatBob = flagValue;
                break;
            case "FLOORCLIP":
                m_currentDefinition.Flags.FloorClip = flagValue;
                break;
            case "FLOORHUGGER":
                m_currentDefinition.Flags.FloorHugger = flagValue;
                break;
            case "FOILBUDDHA":
                m_currentDefinition.Flags.FoilBuddha = flagValue;
                break;
            case "FOILINVUL":
                m_currentDefinition.Flags.FoilInvul = flagValue;
                break;
            case "FORCEDECAL":
                m_currentDefinition.Flags.ForceDecal = flagValue;
                break;
            case "FORCEINFIGHTING":
                m_currentDefinition.Flags.ForceInFighting = flagValue;
                break;
            case "FORCEPAIN":
                m_currentDefinition.Flags.ForcePain = flagValue;
                break;
            case "FORCERADIUSDMG":
                m_currentDefinition.Flags.ForceRadiusDmg = flagValue;
                break;
            case "FORCEXYBILLBOARD":
                m_currentDefinition.Flags.ForceXYBillboard = flagValue;
                break;
            case "FORCEYBILLBOARD":
                m_currentDefinition.Flags.ForceYBillboard = flagValue;
                break;
            case "FORCEZERORADIUSDMG":
                m_currentDefinition.Flags.ForceZeroRadiusDmg = flagValue;
                break;
            case "FRIENDLY":
                m_currentDefinition.Flags.Friendly = flagValue;
                break;
            case "FRIGHTENED":
                m_currentDefinition.Flags.Frightened = flagValue;
                break;
            case "FRIGHTENING":
                m_currentDefinition.Flags.Frightening = flagValue;
                break;
            case "FULLVOLACTIVE":
                m_currentDefinition.Flags.FullVolActive = flagValue;
                break;
            case "FULLVOLDEATH":
                m_currentDefinition.Flags.FullVolDeath = flagValue;
                break;
            case "GETOWNER":
                m_currentDefinition.Flags.GetOwner = flagValue;
                break;
            case "GHOST":
                m_currentDefinition.Flags.Ghost = flagValue;
                break;
            case "GRENADETRAIL":
                m_currentDefinition.Flags.GrenadeTrail = flagValue;
                break;
            case "HARMFRIENDS":
                m_currentDefinition.Flags.HarmFriends = flagValue;
                break;
            case "HERETICBOUNCE":
                m_currentDefinition.Flags.HereticBounce = flagValue;
                break;
            case "HEXENBOUNCE":
                m_currentDefinition.Flags.HexenBounce = flagValue;
                break;
            case "HITMASTER":
                m_currentDefinition.Flags.HitMaster = flagValue;
                break;
            case "HITOWNER":
                m_currentDefinition.Flags.HitOwner = flagValue;
                break;
            case "HITTARGET":
                m_currentDefinition.Flags.HitTarget = flagValue;
                break;
            case "HITTRACER":
                m_currentDefinition.Flags.HitTracer = flagValue;
                break;
            case "ICECORPSE":
                m_currentDefinition.Flags.IceCorpse = flagValue;
                break;
            case "ICEDAMAGE":
                m_currentDefinition.Flags.IceDamage = flagValue;
                break;
            case "ICESHATTER":
                m_currentDefinition.Flags.IceShatter = flagValue;
                break;
            case "INCOMBAT":
                m_currentDefinition.Flags.InCombat = flagValue;
                break;
            case "INTERPOLATEANGLES":
                m_currentDefinition.Flags.InterpolateAngles = flagValue;
                break;
            case "INVISIBLE":
                m_currentDefinition.Flags.Invisible = flagValue;
                break;
            case "INVULNERABLE":
                m_currentDefinition.Flags.Invulnerable = flagValue;
                break;
            case "ISMONSTER":
                m_currentDefinition.Flags.IsMonster = flagValue;
                break;
            case "JUMPDOWN":
                m_currentDefinition.Flags.JumpDown = flagValue;
                break;
            case "JUSTATTACKED":
                m_currentDefinition.Flags.JustAttacked = flagValue;
                break;
            case "JUSTHIT":
                m_currentDefinition.Flags.JustHit = flagValue;
                break;
            case "LAXTELEFRAGDMG":
                m_currentDefinition.Flags.LaxTeleFragDmg = flagValue;
                break;
            case "LONGMELEERANGE":
                m_currentDefinition.Flags.LongMeleeRange = flagValue;
                break;
            case "LOOKALLAROUND":
                m_currentDefinition.Flags.LookAllAround = flagValue;
                break;
            case "LOWGRAVITY":
                m_currentDefinition.Flags.LowGravity = flagValue;
                break;
            case "MASKROTATION":
                m_currentDefinition.Flags.MaskRotation = flagValue;
                break;
            case "MBFBOUNCER":
                m_currentDefinition.Flags.MbfBouncer = flagValue;
                break;
            case "MIRRORREFLECT":
                m_currentDefinition.Flags.MirrorReflect = flagValue;
                break;
            case "MISSILE":
                m_currentDefinition.Flags.Missile = flagValue;
                break;
            case "MISSILEEVENMORE":
                m_currentDefinition.Flags.MissileEvenMore = flagValue;
                break;
            case "MISSILEMORE":
                m_currentDefinition.Flags.MissileMore = flagValue;
                break;
            case "MONSTER":
                m_currentDefinition.Flags.Monster = flagValue;
                break;
            case "MOVEWITHSECTOR":
                m_currentDefinition.Flags.MoveWithSector = flagValue;
                break;
            case "MTHRUSPECIES":
                m_currentDefinition.Flags.MThruSpecies = flagValue;
                break;
            case "NEVERFAST":
                m_currentDefinition.Flags.NeverFast = flagValue;
                break;
            case "NEVERRESPAWN":
                m_currentDefinition.Flags.NeverRespawn = flagValue;
                break;
            case "NEVERTARGET":
                m_currentDefinition.Flags.NeverTarget = flagValue;
                break;
            case "NOBLOCKMAP":
                m_currentDefinition.Flags.NoBlockmap = flagValue;
                break;
            case "NOBLOCKMONST":
                m_currentDefinition.Flags.NoBlockMonst = flagValue;
                break;
            case "NOBLOOD":
                m_currentDefinition.Flags.NoBlood = flagValue;
                break;
            case "NOBLOODDECALS":
                m_currentDefinition.Flags.NoBloodDecals = flagValue;
                break;
            case "NOBOSSRIP":
                m_currentDefinition.Flags.NoBossRip = flagValue;
                break;
            case "NOBOUNCESOUND":
                m_currentDefinition.Flags.NoBounceSound = flagValue;
                break;
            case "NOCLIP":
                m_currentDefinition.Flags.NoClip = flagValue;
                break;
            case "NODAMAGE":
                m_currentDefinition.Flags.NoDamage = flagValue;
                break;
            case "NODAMAGETHRUST":
                m_currentDefinition.Flags.NoDamageThrust = flagValue;
                break;
            case "NODECAL":
                m_currentDefinition.Flags.NoDecal = flagValue;
                break;
            case "NODROPOFF":
                m_currentDefinition.Flags.NoDropoff = flagValue;
                break;
            case "NOEXPLODEFLOOR":
                m_currentDefinition.Flags.NoExplodeFloor = flagValue;
                break;
            case "NOEXTREMEDEATH":
                m_currentDefinition.Flags.NoExtremeDeath = flagValue;
                break;
            case "NOFEAR":
                m_currentDefinition.Flags.NoFear = flagValue;
                break;
            case "NOFRICTION":
                m_currentDefinition.Flags.NoFriction = flagValue;
                break;
            case "NOFRICTIONBOUNCE":
                m_currentDefinition.Flags.NoFrictionBounce = flagValue;
                break;
            case "NOFORWARDFALL":
                m_currentDefinition.Flags.NoForwardFall = flagValue;
                break;
            case "NOGRAVITY":
                m_currentDefinition.Flags.NoGravity = flagValue;
                break;
            case "NOICEDEATH":
                m_currentDefinition.Flags.NoIceDeath = flagValue;
                break;
            case "NOINFIGHTING":
                m_currentDefinition.Flags.NoInfighting = flagValue;
                break;
            case "NOINFIGHTSPECIES":
                m_currentDefinition.Flags.NoInfightSpecies = flagValue;
                break;
            case "NOINTERACTION":
                m_currentDefinition.Flags.NoInteraction = flagValue;
                break;
            case "NOKILLSCRIPTS":
                m_currentDefinition.Flags.NoKillScripts = flagValue;
                break;
            case "NOLIFTDROP":
                m_currentDefinition.Flags.NoLiftDrop = flagValue;
                break;
            case "NOMENU":
                m_currentDefinition.Flags.NoMenu = flagValue;
                break;
            case "NONSHOOTABLE":
                m_currentDefinition.Flags.NonShootable = flagValue;
                break;
            case "NOPAIN":
                m_currentDefinition.Flags.NoPain = flagValue;
                break;
            case "NORADIUSDMG":
                m_currentDefinition.Flags.NoRadiusDmg = flagValue;
                break;
            case "NOSECTOR":
                m_currentDefinition.Flags.NoSector = flagValue;
                break;
            case "NOSKIN":
                m_currentDefinition.Flags.NoSkin = flagValue;
                break;
            case "NOSPLASHALERT":
                m_currentDefinition.Flags.NoSplashAlert = flagValue;
                break;
            case "NOTARGET":
                m_currentDefinition.Flags.NoTarget = flagValue;
                break;
            case "NOTARGETSWITCH":
                m_currentDefinition.Flags.NoTargetSwitch = flagValue;
                break;
            case "NOTAUTOAIMED":
                m_currentDefinition.Flags.NotAutoaimed = flagValue;
                break;
            case "NOTDMATCH":
                m_currentDefinition.Flags.NotDMatch = flagValue;
                break;
            case "NOTELEFRAG":
                m_currentDefinition.Flags.NoTelefrag = flagValue;
                break;
            case "NOTELEOTHER":
                m_currentDefinition.Flags.NoTeleOther = flagValue;
                break;
            case "NOTELEPORT":
                m_currentDefinition.Flags.NoTeleport = flagValue;
                break;
            case "NOTELESTOMP":
                m_currentDefinition.Flags.NoTelestomp = flagValue;
                break;
            case "NOTIMEFREEZE":
                m_currentDefinition.Flags.NoTimeFreeze = flagValue;
                break;
            case "NOTONAUTOMAP":
                m_currentDefinition.Flags.NotOnAutomap = flagValue;
                break;
            case "NOTRIGGER":
                m_currentDefinition.Flags.NoTrigger = flagValue;
                break;
            case "NOVERTICALMELEERANGE":
                m_currentDefinition.Flags.NoVerticalMeleeRange = flagValue;
                break;
            case "NOWALLBOUNCESND":
                m_currentDefinition.Flags.NoWallBounceSnd = flagValue;
                break;
            case "OLDRADIUSDMG":
                m_currentDefinition.Flags.OldRadiusDmg = flagValue;
                break;
            case "PAINLESS":
                m_currentDefinition.Flags.Painless = flagValue;
                break;
            case "PICKUP":
                m_currentDefinition.Flags.Pickup = flagValue;
                break;
            case "PIERCEARMOR":
                m_currentDefinition.Flags.PierceArmor = flagValue;
                break;
            case "POISONALWAYS":
                m_currentDefinition.Flags.PoisonAlways = flagValue;
                break;
            case "PROJECTILE":
                m_currentDefinition.Flags.Projectile = flagValue;
                break;
            case "PUFFGETSOWNER":
                m_currentDefinition.Flags.PuffGetsOwner = flagValue;
                break;
            case "PUFFONACTORS":
                m_currentDefinition.Flags.PuffOnActors = flagValue;
                break;
            case "PUSHABLE":
                m_currentDefinition.Flags.Pushable = flagValue;
                break;
            case "QUARTERGRAVITY":
                m_currentDefinition.Flags.QuarterGravity = flagValue;
                break;
            case "QUICKTORETALIATE":
                m_currentDefinition.Flags.QuickToRetaliate = flagValue;
                break;
            case "RANDOMIZE":
                m_currentDefinition.Flags.Randomize = flagValue;
                break;
            case "REFLECTIVE":
                m_currentDefinition.Flags.Reflective = flagValue;
                break;
            case "RELATIVETOFLOOR":
                m_currentDefinition.Flags.RelativeToFloor = flagValue;
                break;
            case "RIPPER":
                m_currentDefinition.Flags.Ripper = flagValue;
                break;
            case "ROCKETTRAIL":
                m_currentDefinition.Flags.RocketTrail = flagValue;
                break;
            case "ROLLCENTER":
                m_currentDefinition.Flags.RollCenter = flagValue;
                break;
            case "ROLLSPRITE":
                m_currentDefinition.Flags.RollSprite = flagValue;
                break;
            case "SCREENSEEKER":
                m_currentDefinition.Flags.ScreenSeeker = flagValue;
                break;
            case "SEEINVISIBLE":
                m_currentDefinition.Flags.SeeInvisible = flagValue;
                break;
            case "SEEKERMISSILE":
                m_currentDefinition.Flags.SeekerMissile = flagValue;
                break;
            case "SEESDAGGERS":
                m_currentDefinition.Flags.SeesDaggers = flagValue;
                break;
            case "SHADOW":
                m_currentDefinition.Flags.Shadow = flagValue;
                break;
            case "SHIELDREFLECT":
                m_currentDefinition.Flags.ShieldReflect = flagValue;
                break;
            case "SHOOTABLE":
                m_currentDefinition.Flags.Shootable = flagValue;
                break;
            case "SHORTMISSILERANGE":
                m_currentDefinition.Flags.ShortMissileRange = flagValue;
                break;
            case "SKULLFLY":
                m_currentDefinition.Flags.Skullfly = flagValue;
                break;
            case "SKYEXPLODE":
                m_currentDefinition.Flags.SkyExplode = flagValue;
                break;
            case "SLIDESONWALLS":
                m_currentDefinition.Flags.SlidesOnWalls = flagValue;
                break;
            case "SOLID":
                m_currentDefinition.Flags.Solid = flagValue;
                break;
            case "SPAWNCEILING":
                m_currentDefinition.Flags.SpawnCeiling = flagValue;
                break;
            case "SPAWNFLOAT":
                m_currentDefinition.Flags.SpawnFloat = flagValue;
                break;
            case "SPAWNSOUNDSOURCE":
                m_currentDefinition.Flags.SpawnSoundSource = flagValue;
                break;
            case "SPECIAL":
                m_currentDefinition.Flags.Special = flagValue;
                break;
            case "SPECIALFIREDAMAGE":
                m_currentDefinition.Flags.SpecialFireDamage = flagValue;
                break;
            case "SPECIALFLOORCLIP":
                m_currentDefinition.Flags.SpecialFloorClip = flagValue;
                break;
            case "SPECTRAL":
                m_currentDefinition.Flags.Spectral = flagValue;
                break;
            case "SPRITEANGLE":
                m_currentDefinition.Flags.SpriteAngle = flagValue;
                break;
            case "SPRITEFLIP":
                m_currentDefinition.Flags.SpriteFlip = flagValue;
                break;
            case "STANDSTILL":
                m_currentDefinition.Flags.StandStill = flagValue;
                break;
            case "STAYMORPHED":
                m_currentDefinition.Flags.StayMorphed = flagValue;
                break;
            case "STEALTH":
                m_currentDefinition.Flags.Stealth = flagValue;
                break;
            case "STEPMISSILE":
                m_currentDefinition.Flags.StepMissile = flagValue;
                break;
            case "STRIFEDAMAGE":
                m_currentDefinition.Flags.StrifeDamage = flagValue;
                break;
            case "SUMMONEDMONSTER":
                m_currentDefinition.Flags.SummonedMonster = flagValue;
                break;
            case "SYNCHRONIZED":
                m_currentDefinition.Flags.Synchronized = flagValue;
                break;
            case "TELEPORT":
                m_currentDefinition.Flags.Teleport = flagValue;
                break;
            case "TELESTOMP":
                m_currentDefinition.Flags.Telestomp = flagValue;
                break;
            case "THRUACTORS":
                m_currentDefinition.Flags.ThruActors = flagValue;
                break;
            case "THRUGHOST":
                m_currentDefinition.Flags.ThruGhost = flagValue;
                break;
            case "THRUREFLECT":
                m_currentDefinition.Flags.ThruReflect = flagValue;
                break;
            case "THRUSPECIES":
                m_currentDefinition.Flags.ThruSpecies = flagValue;
                break;
            case "TOUCHY":
                m_currentDefinition.Flags.Touchy = flagValue;
                break;
            case "USEBOUNCESTATE":
                m_currentDefinition.Flags.UseBounceState = flagValue;
                break;
            case "USEKILLSCRIPTS":
                m_currentDefinition.Flags.UseKillScripts = flagValue;
                break;
            case "USESPECIAL":
                m_currentDefinition.Flags.UseSpecial = flagValue;
                break;
            case "VISIBILITYPULSE":
                m_currentDefinition.Flags.VisibilityPulse = flagValue;
                break;
            case "VULNERABLE":
                m_currentDefinition.Flags.Vulnerable = flagValue;
                break;
            case "WALLSPRITE":
                m_currentDefinition.Flags.WallSprite = flagValue;
                break;
            case "WINDTHRUST":
                m_currentDefinition.Flags.WindThrust = flagValue;
                break;
            case "ZDOOMTRANS":
                m_currentDefinition.Flags.ZdoomTrans = flagValue;
                break;
            default:
                Log.Warn("Unknown flag '{0}' for actor {1}", flagName, m_currentDefinition.Name);
                break;
            }
        }
    }
}