using Helion.Util.Parser;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Dehacked
{
    public partial class DehackedDefinition
    {
        private static readonly string Thing = "Thing";
        private static readonly string Frame = "Frame";
        private static readonly string Sound = "Sound";
        private static readonly string Ammo = "Ammo";
        private static readonly string Weapon = "Weapon";
        private static readonly string Cheat = "Cheat";
        private static readonly string Misc = "Misc";
        private static readonly string Text = "Text";

        private static readonly HashSet<string> BaseTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            Thing,
            Frame,
            Sound,
            Ammo,
            Weapon,
            Cheat,
            Misc,
            Text
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

        public readonly List<DehackedThing> Things = new();

        private readonly EntityDefinitionComposer m_entityDefinitionComposer;

        public DehackedDefinition(EntityDefinitionComposer composer)
        {
            m_entityDefinitionComposer = composer;
        }

        public void Parse(string data)
        {
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            while (!parser.IsDone())
            {
                string item = parser.ConsumeString();

                if (item.StartsWith('#'))
                {
                    parser.ConsumeLine();
                    continue;
                }

                if (item.Equals(Thing, StringComparison.OrdinalIgnoreCase))
                    ParseThing(parser);
            }

            ApplyThings();
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

            while (!BaseTypes.Contains(parser.PeekString()))
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
