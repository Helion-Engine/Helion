using Helion.Util.Parser;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helion.Dehacked
{
    public partial class DehackedDefinition
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly List<DehackedThing> Things = new();
        public readonly List<DehackedFrame> Frames = new();
        public readonly List<DehackedAmmo> Ammo = new();
        public readonly List<DehackedWeapon> Weapons = new();
        public readonly List<DehackedString> Strings = new();
        public readonly List<DehackedPointer> Pointers = new();

        public DehackedCheat? Cheat { get; private set; }
        public int DoomVersion { get; private set; }
        public int PatchFormat { get; set; }

        public void Parse(string data)
        {
            SimpleParser parser = new SimpleParser();
            parser.Parse(data, keepEmptyLines: true, splitSpecialChars: false);

            ParseHeader(parser);

            while (!parser.IsDone())
            {
                string item = parser.PeekString();
                if (item.StartsWith('#'))
                {
                    parser.ConsumeLine();
                    continue;
                }

                if (BaseTypes.Contains(item))
                    parser.ConsumeString();

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
                    ParseText(parser);
                else if (item.Equals(PointerName, StringComparison.OrdinalIgnoreCase))
                    ParsePointer(parser);
                else
                    UnknownWarning(parser, "type");
            }
        }

        private static void UnknownWarning(SimpleParser parser, string type)
        {
            string line = parser.ConsumeLine();
            if (string.IsNullOrWhiteSpace(line))
                return;
            int lineNumber = parser.GetCurrentLine();
            Log.Warn($"Dehacked: Skipping unknown {type}: {line} line:{lineNumber}");
        }

        private void ParseHeader(SimpleParser parser)
        {
            while (!parser.IsDone() && (DoomVersion == 0 || PatchFormat == 0))
            {
                string item = parser.PeekLine();
                if (item.StartsWith('#'))
                {
                    parser.ConsumeLine();
                    continue;
                }

                if (item.StartsWith(DoomVersionName, StringComparison.OrdinalIgnoreCase))
                    DoomVersion = GetIntProperty(parser, DoomVersionName);
                else if (item.StartsWith(PatchFormatName, StringComparison.OrdinalIgnoreCase))
                    PatchFormat = GetIntProperty(parser, PatchFormatName);
                else
                    parser.ConsumeLine();
            }
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
                    UnknownWarning(parser, "thing type");
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
                    frame.NextFrame = GetIntProperty(parser, NextFrame);
                else if (line.StartsWith(Unknown1, StringComparison.OrdinalIgnoreCase))
                    frame.Unknown1 = GetIntProperty(parser, Unknown1);
                else if (line.StartsWith(Unknown2, StringComparison.OrdinalIgnoreCase))
                    frame.Unknown2 = GetIntProperty(parser, Unknown2);
                else
                    UnknownWarning(parser, "frame type");
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
                    UnknownWarning(parser, "ammo type");
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
                    UnknownWarning(parser, "weapon type");
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
                    UnknownWarning(parser, "cheat type");
            }
        }

        private void ParseText(SimpleParser parser)
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
                Log.Warn($"Dehacked: Invalid dehacked string length:{text.OldSize} line:{parser.GetCurrentLine()}");
                return;
            }

            string sbText = sb.ToString();
            text.OldString = sbText.Substring(0, text.OldSize);
            text.NewString = sbText.Substring(text.OldSize);

            Strings.Add(text);
        }

        private void ParsePointer(SimpleParser parser)
        {
            DehackedPointer pointer = new();
            pointer.Number = parser.ConsumeInteger();

            parser.ConsumeString("(Frame");
            string frame = parser.ConsumeString().Replace(")", string.Empty);

            if (!int.TryParse(frame, out int frameNumber))
            {
                Log.Warn($"Dehacked: Invalid frame:{frame} line:{parser.GetCurrentLine()}");
                return;
            }

            pointer.Frame = frameNumber;

            while (!IsBlockComplete(parser))
            {
                string line = parser.PeekLine();
                if (line.StartsWith("Codep Frame", StringComparison.OrdinalIgnoreCase))
                    pointer.CodePointerFrame = GetIntProperty(parser, DeselectFrame);
                else
                    UnknownWarning(parser, "pointer type");

                parser.ConsumeLine();
            }

            Pointers.Add(pointer);
        }

        private bool IsBlockComplete(SimpleParser parser)
        {
            if (parser.IsDone())
                return true;

            // Dehacked base types are all proceeded by a number, check to not confuse with random text
            if (BaseTypes.Contains(parser.PeekString()) && parser.PeekString(1, out string? data) && 
                int.TryParse(data, out _))
                    return true;

            return false;
        }

        private uint GetThingBits(SimpleParser parser, string property)
        {
            ConsumeProperty(parser, property);
            parser.ConsumeString("=");
            uint? bits = (uint?)parser.ConsumeIfInt();
            if (bits.HasValue)
                return bits.Value;

            return ParseThingStringBits(parser);
        }

        private uint ParseThingStringBits(SimpleParser parser)
        {
            uint bits = 0;
            while (!BaseTypes.Contains(parser.PeekString()))
            {
                string stringFlag = parser.ConsumeString();
                if (ThingPropertyStrings.TryGetValue(stringFlag, out uint flag))
                    bits |= flag;
                else
                    Log.Warn($"Dehacked: Invalid thing flag {stringFlag}.");
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
