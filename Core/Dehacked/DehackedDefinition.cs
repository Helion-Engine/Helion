using Helion.Util.Parser;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helion.Dehacked
{
    public partial class DehackedDefinition
    {
        private static readonly string ThingName = "Thing";
        private static readonly string FrameName = "Frame";
        private static readonly string SoundName = "Sound";
        private static readonly string AmmoName = "Ammo";
        private static readonly string WeaponName = "Weapon";
        private static readonly string CheatName = "Cheat";
        private static readonly string MiscName = "Misc";
        private static readonly string TextName = "Text";

        private static readonly HashSet<string> BaseTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ThingName,
            FrameName,
            SoundName,
            AmmoName,
            WeaponName,
            CheatName,
            MiscName,
            TextName
        };

        private static readonly string IDNumber = "ID #";
        private static readonly string InitFrame = "Initial frame";
        private static readonly string Hitpoints = "Hit points";
        private static readonly string FirstMovingFrame = "First moving frame";
        private static readonly string AlertSound = "Alert sound";
        private static readonly string ReactionTime = "Reaction time";
        private static readonly string AttackSound = "Attack sound";
        private static readonly string InjuryFrame = "Injury frame";
        private static readonly string PainChance = "Pain chance";
        private static readonly string PainSound = "Pain sound";
        private static readonly string CloseAttackFrame = "Close attack frame";
        private static readonly string FarAttackFrame = "Far attack frame";
        private static readonly string DeathFrame = "Death frame";
        private static readonly string ExplodingFrame = "Exploding frame";
        private static readonly string DeathSound = "Death sound";
        private static readonly string Speed = "Speed";
        private static readonly string Width = "Width";
        private static readonly string Height = "Height";
        private static readonly string Mass = "Mass";
        private static readonly string MisileDamage = "Missile damage";
        private static readonly string ActionSound = "Action sound";
        private static readonly string Bits = "Bits";
        private static readonly string RespawnFrame = "Respawn frame";

        private static readonly string Duration = "Duration";
        private static readonly string SpriteNum = "Sprite number";
        private static readonly string SpriteSubNum = "Sprite subnumber";
        private static readonly string NextFrame = "Next frame";
        private static readonly string Unknown1 = "Unknown 1";
        private static readonly string Unknown2 = "Unknown 2";

        private static readonly string MaxAmmo = "Max ammo";
        private static readonly string PerAmmo = "Per ammo";

        private static readonly string AmmoType = "Ammo type";
        private static readonly string DeselectFrame = "Deselect frame";
        private static readonly string SelectFrame = "Select frame";
        private static readonly string BobbingFrame = "Bobbing frame";
        private static readonly string ShootingFrame = "Shooting frame";
        private static readonly string FiringFrame = "Firing frame";

        private static readonly string ChangeMusic = "Change music";
        private static readonly string Chainsaw = "Chainsaw";
        private static readonly string God = "God mode";
        private static readonly string AmmoAndKeys = "Ammo & Keys";
        private static readonly string AmmoCheat = "Ammo";
        private static readonly string NoClip1 = "No Clipping 1";
        private static readonly string NoClip2 = "No Clipping 2";
        private static readonly string Invincibility = "Invincibility ";
        private static readonly string Berserk = "Berserk";
        private static readonly string Invisibility = "Invisibility";
        private static readonly string RadSuit = "Radiation Suit";
        private static readonly string AutoMap = "Auto-map";
        private static readonly string LiteAmp = "Lite-amp Goggles";
        private static readonly string Behold = "BEHOLD menu";
        private static readonly string LevelWarp = "Level Warp";
        private static readonly string MapCheat = "Map cheat";
        private static readonly string PlayerPos = "Player Position";

        public readonly List<DehackedThing> Things = new();
        public readonly List<DehackedFrame> Frames = new();
        public readonly List<DehackedAmmo> Ammo = new();
        public readonly List<DehackedWeapon> Weapons = new();
        public readonly List<DehackedString> Strings = new();
        public DehackedCheat? Cheat { get; private set; }

        private readonly EntityDefinitionComposer m_entityDefinitionComposer;

        public DehackedDefinition(EntityDefinitionComposer composer)
        {
            m_entityDefinitionComposer = composer;
        }

        public void Parse(string data)
        {
            SimpleParser parser = new SimpleParser();
            parser.Parse(data, keepEmptyLines: true, splitSpecialChars: false);

            while (!parser.IsDone())
            {
                string item = parser.ConsumeString();

                if (item.StartsWith('#'))
                {
                    parser.ConsumeLine();
                    continue;
                }

                if (item.Equals(ThingName, StringComparison.OrdinalIgnoreCase))
                    ParseThing(parser);
                else if (item.Equals(FrameName, StringComparison.OrdinalIgnoreCase))
                    ParseFrame(parser);
                else if (item.Equals(AmmoName, StringComparison.OrdinalIgnoreCase))
                    ParseAmmo(parser);
                else if (item.Equals(WeaponName, StringComparison.OrdinalIgnoreCase))
                    ParseWeapon(parser);
                else if (item.Equals(CheatName, StringComparison.OrdinalIgnoreCase))
                    ParseCheat(parser);
                else if (item.Equals(TextName, StringComparison.OrdinalIgnoreCase))
                    ParseText(parser, data);
                else
                    parser.ConsumeLine();
            }

            ApplyThings();
        }

        private int GetNextBlockIndex(int startIndex, string data)
        {
            throw new NotImplementedException();
        }

        private void ApplyThings()
        {
            foreach (var thing in Things)
            {
                int index = thing.Number - 1;
                if (index < 0 || index >= ActorNames.Length)
                {
                    // Log.Error
                    continue;
                }

                string actorName = ActorNames[index];
                var definition = m_entityDefinitionComposer.GetByName(actorName);
                if (definition == null)
                    continue;

                var properties = definition.Properties;
                if (thing.Hitpoints.HasValue)
                    properties.Health = thing.Hitpoints.Value;
                if (thing.ReactionTime.HasValue)
                    properties.ReactionTime = thing.ReactionTime.Value;
                if (thing.PainChance.HasValue)
                    properties.PainChance = thing.PainChance.Value;
                if (thing.Speed.HasValue)
                    properties.Speed = thing.Speed.Value;
                if (thing.Width.HasValue)
                    properties.Radius = GetDouble(thing.Width.Value);
                if (thing.Height.HasValue)
                    properties.Height = GetDouble(thing.Height.Value);
                if (thing.Mass.HasValue)
                    properties.Mass = thing.Mass.Value;
                if (thing.MisileDamage.HasValue)
                    properties.Damage.Value = thing.MisileDamage.Value;
                if (thing.Bits.HasValue)
                    SetActorFlags(definition, thing.Bits.Value);

                if (thing.AlertSound.HasValue)
                    properties.SeeSound = GetSound(thing.AlertSound.Value);
                if (thing.AttackSound.HasValue)
                    properties.AttackSound = GetSound(thing.AttackSound.Value);
                if (thing.PainSound.HasValue)
                    properties.PainSound = GetSound(thing.PainSound.Value);
                if (thing.DeathSound.HasValue)
                    properties.DeathSound = GetSound(thing.DeathSound.Value);
            }
        }
  

        private static void SetActorFlags(EntityDefinition def, uint value)
        {
            def.Flags.ClearAll();
            ThingProperties thingProperties = (ThingProperties)value;
            if (thingProperties.HasFlag(ThingProperties.SPECIAL))
                def.Flags.Special = true;
            if (thingProperties.HasFlag(ThingProperties.SOLID))
                def.Flags.Solid = true;
            if (thingProperties.HasFlag(ThingProperties.SHOOTABLE))
                def.Flags.Shootable = true;
            if (thingProperties.HasFlag(ThingProperties.NOSECTOR))
                def.Flags.NoSector = true;
            if (thingProperties.HasFlag(ThingProperties.NOBLOCKMAP))
                def.Flags.NoBlockmap = true;
            if (thingProperties.HasFlag(ThingProperties.AMBUSH))
                def.Flags.Ambush = true;
            if (thingProperties.HasFlag(ThingProperties.JUSTHIT))
                def.Flags.JustHit = true;
            if (thingProperties.HasFlag(ThingProperties.JUSTATTACKED))
                def.Flags.JustAttacked = true;
            if (thingProperties.HasFlag(ThingProperties.SPAWNCEILING))
                def.Flags.SpawnCeiling = true;
            if (thingProperties.HasFlag(ThingProperties.NOGRAVITY))
                def.Flags.NoGravity = true;
            if (thingProperties.HasFlag(ThingProperties.DROPOFF))
                def.Flags.Dropoff = true;
            if (thingProperties.HasFlag(ThingProperties.PICKUP))
                def.Flags.Pickup = true;
            if (thingProperties.HasFlag(ThingProperties.NOCLIP))
                def.Flags.NoClip = true;
            if (thingProperties.HasFlag(ThingProperties.SLIDE))
                def.Flags.SlidesOnWalls = true;
            if (thingProperties.HasFlag(ThingProperties.FLOAT))
                def.Flags.Float = true;
            if (thingProperties.HasFlag(ThingProperties.TELEPORT))
                def.Flags.Teleport = true;
            if (thingProperties.HasFlag(ThingProperties.MISSILE))
                def.Flags.Missile = true;
            if (thingProperties.HasFlag(ThingProperties.DROPPED))
                def.Flags.Dropped = true;
            if (thingProperties.HasFlag(ThingProperties.SHADOW))
                def.Flags.Shadow = true;
            if (thingProperties.HasFlag(ThingProperties.NOBLOOD))
                def.Flags.NoBlood = true;
            if (thingProperties.HasFlag(ThingProperties.CORPSE))
                def.Flags.Corpse = true;
            if (thingProperties.HasFlag(ThingProperties.COUNTKILL))
                def.Flags.CountKill = true;
            if (thingProperties.HasFlag(ThingProperties.COUNTITEM))
                def.Flags.CountItem = true;
            if (thingProperties.HasFlag(ThingProperties.SKULLFLY))
                def.Flags.Skullfly = true;
            if (thingProperties.HasFlag(ThingProperties.NOTDMATCH))
                def.Flags.NotDMatch = true;
            if (thingProperties.HasFlag(ThingProperties.NOTDMATCH))
                def.Flags.NotDMatch = true;
            if (thingProperties.HasFlag(ThingProperties.TOUCHY))
                def.Flags.Touchy = true;
            if (thingProperties.HasFlag(ThingProperties.BOUNCES))
                def.Flags.MbfBouncer = true;
            if (thingProperties.HasFlag(ThingProperties.FRIEND))
                def.Flags.Friendly = true;
            
            // TODO can we support these?
            //if (thingProperties.HasFlag(ThingProperties.TRANSLATION1))
            //if (thingProperties.HasFlag(ThingProperties.TRANSLATION2))
            //if (thingProperties.HasFlag(ThingProperties.INFLOAT))
            //    def.Flags.InFloat = true;
        }

        private static double GetDouble(int value) => value / 65536.0;

        private static string GetSound(int sound)
        {
            if (sound < 0 || sound >= SoundStrings.Length)
            {
                // Log.Error
                return string.Empty;
            }

            return SoundStrings[sound];
        }

        private void ParseThing(SimpleParser parser)
        {
            DehackedThing thing = new();
            thing.Number = parser.ConsumeInteger();
            if (parser.Peek('('))
                thing.Name = parser.ConsumeLine();

            while (!IsBlockComplete(parser))
            {
                string line = parser.PeekLine();
                if (line.StartsWith(IDNumber, StringComparison.OrdinalIgnoreCase))
                    thing.ID = GetIntProperty(parser, IDNumber);
                else if(line.StartsWith(InitFrame, StringComparison.OrdinalIgnoreCase))
                    thing.InitFrame = GetIntProperty(parser, InitFrame);
                else if (line.StartsWith(Hitpoints, StringComparison.OrdinalIgnoreCase))
                    thing.Hitpoints = GetIntProperty(parser, Hitpoints);
                else if (line.StartsWith(FirstMovingFrame, StringComparison.OrdinalIgnoreCase))
                    thing.FirstMovingFrame = GetIntProperty(parser, FirstMovingFrame);
                else if (line.StartsWith(AlertSound, StringComparison.OrdinalIgnoreCase))
                    thing.AlertSound = GetIntProperty(parser, AlertSound);
                else if (line.StartsWith(ReactionTime, StringComparison.OrdinalIgnoreCase))
                    thing.ReactionTime = GetIntProperty(parser, ReactionTime);
                else if (line.StartsWith(AttackSound, StringComparison.OrdinalIgnoreCase))
                    thing.AttackSound = GetIntProperty(parser, AttackSound);
                else if (line.StartsWith(InjuryFrame, StringComparison.OrdinalIgnoreCase))
                    thing.InjuryFrame = GetIntProperty(parser, InjuryFrame);
                else if (line.StartsWith(PainChance, StringComparison.OrdinalIgnoreCase))
                    thing.PainChance = GetIntProperty(parser, PainChance);
                else if (line.StartsWith(PainSound, StringComparison.OrdinalIgnoreCase))
                    thing.PainSound = GetIntProperty(parser, PainSound);
                else if (line.StartsWith(CloseAttackFrame, StringComparison.OrdinalIgnoreCase))
                    thing.CloseAttackFrame = GetIntProperty(parser, CloseAttackFrame);
                else if (line.StartsWith(FarAttackFrame, StringComparison.OrdinalIgnoreCase))
                    thing.FarAttackFrame = GetIntProperty(parser, FarAttackFrame);
                else if (line.StartsWith(DeathFrame, StringComparison.OrdinalIgnoreCase))
                    thing.DeathFrame = GetIntProperty(parser, DeathFrame);
                else if (line.StartsWith(ExplodingFrame, StringComparison.OrdinalIgnoreCase))
                    thing.ExplodingFrame = GetIntProperty(parser, ExplodingFrame);
                else if (line.StartsWith(DeathSound, StringComparison.OrdinalIgnoreCase))
                    thing.DeathSound = GetIntProperty(parser, DeathSound);
                else if (line.StartsWith(Speed, StringComparison.OrdinalIgnoreCase))
                    thing.Speed = GetIntProperty(parser, Speed);
                else if (line.StartsWith(Width, StringComparison.OrdinalIgnoreCase))
                    thing.Width = GetIntProperty(parser, Width);
                else if (line.StartsWith(Height, StringComparison.OrdinalIgnoreCase))
                    thing.Height = GetIntProperty(parser, Height);
                else if (line.StartsWith(Mass, StringComparison.OrdinalIgnoreCase))
                    thing.Mass = GetIntProperty(parser, Mass);
                else if (line.StartsWith(MisileDamage, StringComparison.OrdinalIgnoreCase))
                    thing.MisileDamage = GetIntProperty(parser, MisileDamage);
                else if (line.StartsWith(ActionSound, StringComparison.OrdinalIgnoreCase))
                    thing.ActionSound = GetIntProperty(parser, ActionSound);
                else if (line.StartsWith(RespawnFrame, StringComparison.OrdinalIgnoreCase))
                    thing.RespawnFrame = GetIntProperty(parser, RespawnFrame);
                else if (line.StartsWith(Bits, StringComparison.OrdinalIgnoreCase))
                    thing.Bits = GetThingBits(parser, Bits);
                else
                    parser.ConsumeLine();
            }

            Things.Add(thing);
        }

        private void ParseFrame(SimpleParser parser)
        {
            DehackedFrame frame = new();
            frame.Frame = parser.ConsumeInteger();

            while (!IsBlockComplete(parser))
            {
                string line = parser.PeekLine();
                if (line.StartsWith(SpriteNum, StringComparison.OrdinalIgnoreCase))
                    frame.SpriteNumber = GetIntProperty(parser, SpriteNum);
                else if (line.StartsWith(SpriteSubNum, StringComparison.OrdinalIgnoreCase))
                    frame.SpriteSubNumber = GetIntProperty(parser, SpriteSubNum);
                else if (line.StartsWith(Duration, StringComparison.OrdinalIgnoreCase))
                    frame.Duration = GetIntProperty(parser, Duration);
                else if (line.StartsWith(NextFrame, StringComparison.OrdinalIgnoreCase))
                    frame.Duration = GetIntProperty(parser, NextFrame);
                else if (line.StartsWith(Unknown1, StringComparison.OrdinalIgnoreCase))
                    frame.Unknown1 = GetIntProperty(parser, Unknown1);
                else if (line.StartsWith(Unknown2, StringComparison.OrdinalIgnoreCase))
                    frame.Unknown2 = GetIntProperty(parser, Unknown2);
                else
                    parser.ConsumeLine();
            }

            Frames.Add(frame);
        }

        private void ParseAmmo(SimpleParser parser)
        {
            DehackedAmmo ammo = new();
            ammo.AmmoNumber = parser.ConsumeInteger();

            while (!IsBlockComplete(parser))
            {
                string line = parser.PeekLine();
                if (line.StartsWith(MaxAmmo, StringComparison.OrdinalIgnoreCase))
                    ammo.MaxAmmo = GetIntProperty(parser, MaxAmmo);
                else if (line.StartsWith(PerAmmo, StringComparison.OrdinalIgnoreCase))
                    ammo.PerAmmo = GetIntProperty(parser, PerAmmo);
                else
                    parser.ConsumeLine();
            }

            Ammo.Add(ammo);
        }

        private void ParseWeapon(SimpleParser parser)
        {
            DehackedWeapon weapon = new();
            weapon.WeaponNumber = parser.ConsumeInteger();

            while (!IsBlockComplete(parser))
            {
                string line = parser.PeekLine();
                if (line.StartsWith(DeselectFrame, StringComparison.OrdinalIgnoreCase))
                    weapon.DeselectFrame = GetIntProperty(parser, DeselectFrame);
                else if (line.StartsWith(SelectFrame, StringComparison.OrdinalIgnoreCase))
                    weapon.SelectFrame = GetIntProperty(parser, SelectFrame);
                else if (line.StartsWith(AmmoType, StringComparison.OrdinalIgnoreCase))
                    weapon.AmmoType = GetIntProperty(parser, AmmoType);
                else if (line.StartsWith(BobbingFrame, StringComparison.OrdinalIgnoreCase))
                    weapon.BobbingFrame = GetIntProperty(parser, BobbingFrame);
                else if (line.StartsWith(ShootingFrame, StringComparison.OrdinalIgnoreCase))
                    weapon.ShootingFrame = GetIntProperty(parser, ShootingFrame);
                else if (line.StartsWith(FiringFrame, StringComparison.OrdinalIgnoreCase))
                    weapon.FiringFrame = GetIntProperty(parser, FiringFrame);
                else
                    parser.ConsumeLine();
            }

            Weapons.Add(weapon);
        }

        private void ParseCheat(SimpleParser parser)
        {
            Cheat = new();
            parser.ConsumeInteger();

            while (!IsBlockComplete(parser))
            {
                string line = parser.PeekLine();
                if (line.StartsWith(ChangeMusic, StringComparison.OrdinalIgnoreCase))
                    Cheat.ChangeMusic = GetStringProperty(parser, ChangeMusic);
                else if (line.StartsWith(Chainsaw, StringComparison.OrdinalIgnoreCase))
                    Cheat.Chainsaw = GetStringProperty(parser, Chainsaw);
                else if (line.StartsWith(God, StringComparison.OrdinalIgnoreCase))
                    Cheat.God = GetStringProperty(parser, God);
                else if (line.StartsWith(AmmoAndKeys, StringComparison.OrdinalIgnoreCase))
                    Cheat.AmmoAndKeys = GetStringProperty(parser, AmmoAndKeys);
                else if (line.StartsWith(AmmoCheat, StringComparison.OrdinalIgnoreCase))
                    Cheat.Ammo = GetStringProperty(parser, AmmoCheat);
                else if (line.StartsWith(NoClip1, StringComparison.OrdinalIgnoreCase))
                    Cheat.NoClip1 = GetStringProperty(parser, NoClip1);
                else if (line.StartsWith(NoClip2, StringComparison.OrdinalIgnoreCase))
                    Cheat.NoClip2 = GetStringProperty(parser, NoClip2);
                else if (line.StartsWith(Invincibility, StringComparison.OrdinalIgnoreCase))
                    Cheat.Invincibility = GetStringProperty(parser, Invincibility);
                else if (line.StartsWith(Invisibility, StringComparison.OrdinalIgnoreCase))
                    Cheat.Invisibility = GetStringProperty(parser, Invisibility);
                else if (line.StartsWith(RadSuit, StringComparison.OrdinalIgnoreCase))
                    Cheat.RadSuit = GetStringProperty(parser, RadSuit);
                else if (line.StartsWith(AutoMap, StringComparison.OrdinalIgnoreCase))
                    Cheat.AutoMap = GetStringProperty(parser, AutoMap);
                else if (line.StartsWith(LiteAmp, StringComparison.OrdinalIgnoreCase))
                    Cheat.LiteAmp = GetStringProperty(parser, LiteAmp);
                else if (line.StartsWith(Behold, StringComparison.OrdinalIgnoreCase))
                    Cheat.Behold = GetStringProperty(parser, Behold);
                else if (line.StartsWith(LevelWarp, StringComparison.OrdinalIgnoreCase))
                    Cheat.LevelWarp = GetStringProperty(parser, LevelWarp);
                else if (line.StartsWith(MapCheat, StringComparison.OrdinalIgnoreCase))
                    Cheat.LevelWarp = GetStringProperty(parser, MapCheat);
                else if (line.StartsWith(PlayerPos, StringComparison.OrdinalIgnoreCase))
                    Cheat.PlayerPos = GetStringProperty(parser, PlayerPos);
                else if (line.StartsWith(Berserk, StringComparison.OrdinalIgnoreCase))
                    Cheat.Berserk = GetStringProperty(parser, Berserk);
                else
                    parser.ConsumeLine();
            }
        }

        private void ParseText(SimpleParser parser, string data)
        {
            DehackedString text = new();
            text.OldSize = parser.ConsumeInteger();
            text.NewSize = parser.ConsumeInteger();

            StringBuilder sb = new();

            while (!IsBlockComplete(parser))
            {
                sb.Append(parser.ConsumeLine());
                sb.Append('\n');
            }

            while (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                sb.Length--;

            if (text.OldSize > sb.Length)
            {
                // uh oh
                return;
            }


            string sbText = sb.ToString();
            text.OldString = sbText.Substring(0, text.OldSize);
            text.NewString = sbText.Substring(text.OldSize);

            Strings.Add(text);
        }

        private static bool IsBlockComplete(SimpleParser parser) =>
            parser.IsDone() || BaseTypes.Contains(parser.PeekString());

        private static uint GetThingBits(SimpleParser parser, string property)
        {
            ConsumeProperty(parser, property);
            parser.ConsumeString("=");
            uint? bits = (uint?)parser.ConsumeIfInt();
            if (bits.HasValue)
                return bits.Value;

            return ParseThingStringBits(parser);
        }

        private static uint ParseThingStringBits(SimpleParser parser)
        {
            uint bits = 0;
            while (!BaseTypes.Contains(parser.PeekString()))
            {
                if (ThingPropertyStrings.TryGetValue(parser.ConsumeString(), out uint flag))
                    bits |= flag;
            }

            return bits;
        }

        private static string GetStringProperty(SimpleParser parser, string property)
        {
            ConsumeProperty(parser, property);
            parser.ConsumeString("=");
            return parser.ConsumeString();
        }

        private static int GetIntProperty(SimpleParser parser, string property)
        {
            ConsumeProperty(parser, property);
            parser.ConsumeString("=");
            return parser.ConsumeInteger();
        }

        private static void ConsumeProperty(SimpleParser parser, string property)
        {
            for (int i = 0; i < property.Count(x => x == ' ') + 1; i++)
                parser.ConsumeString();
        }
    }
}
