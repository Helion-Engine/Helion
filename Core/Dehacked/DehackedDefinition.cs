using Helion.Util.Parser;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helion.Dehacked;

public partial class DehackedDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly List<DehackedThing> Things = new();
    public readonly List<DehackedFrame> Frames = new();
    public readonly List<DehackedAmmo> Ammo = new();
    public readonly List<DehackedWeapon> Weapons = new();
    public readonly List<DehackedString> Strings = new();
    public readonly List<DehackedPointer> Pointers = new();
    public readonly List<DehackedSound> Sounds = new();

    public readonly List<BexString> BexStrings = new();
    public readonly List<BexPar> BexPars = new();
    public readonly List<BexItem> BexSounds = new();
    public readonly List<BexItem> BexSprites = new();

    public DehackedCheat? Cheat { get; private set; }
    public DehackedMisc? Misc { get; private set; }
    public int DoomVersion { get; private set; }
    public int PatchFormat { get; set; }

    public void Parse(string data)
    {
        data = data.Replace('\0', ' ');
        SimpleParser parser = new();
        parser.SetSpecialChars(new char[] { '=' });
        parser.Parse(data, keepEmptyLines: true, parseQuotes: false);

        ParseHeader(parser);

        while (!parser.IsDone())
        {
            string item = parser.PeekString();
            if (item.StartsWith('#'))
            {
                parser.ConsumeLine();
                continue;
            }

            int itemLine = parser.GetCurrentLine();
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
            else if (item.Equals(MiscName, StringComparison.OrdinalIgnoreCase))
                ParseMisc(parser, itemLine);
            else if (item.Equals(SoundName, StringComparison.Ordinal))
                ParseSound(parser);
            else if (item.Equals(BexStringName, StringComparison.OrdinalIgnoreCase))
                ParseBexText(parser);
            else if (item.Equals(BexPointerName, StringComparison.OrdinalIgnoreCase))
                ParseBexPointer(parser);
            else if (item.Equals(BexParName, StringComparison.OrdinalIgnoreCase))
                ParseBexPar(parser);
            else if (item.Equals(BexSoundName, StringComparison.OrdinalIgnoreCase))
                ParseBexItem(parser, BexSounds);
            else if (item.Equals(BexSpriteName, StringComparison.OrdinalIgnoreCase))
                ParseBexItem(parser, BexSprites);
            else
                UnknownWarning(parser, "type", item);
        }
    }

    private static void UnknownWarning(SimpleParser parser, string type, string? prefix = null)
    {
        int lineNumber = parser.GetCurrentLine();
        string line = parser.ConsumeLine();
        if (prefix != null)
            line = prefix + " " + line;
        if (string.IsNullOrWhiteSpace(line))
            return;
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
            else if (line.StartsWith(InitFrame, StringComparison.OrdinalIgnoreCase))
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
                thing.Speed = GetDoubleProperty(parser, Speed);
            else if (line.StartsWith(Width, StringComparison.OrdinalIgnoreCase))
                thing.Width = GetDoubleProperty(parser, Width);
            else if (line.StartsWith(Height, StringComparison.OrdinalIgnoreCase))
                thing.Height = GetDoubleProperty(parser, Height);
            else if (line.StartsWith(Mass, StringComparison.OrdinalIgnoreCase))
                thing.Mass = GetIntProperty(parser, Mass);
            else if (line.StartsWith(MisileDamage, StringComparison.OrdinalIgnoreCase))
                thing.MisileDamage = GetIntProperty(parser, MisileDamage);
            else if (line.StartsWith(ActionSound, StringComparison.OrdinalIgnoreCase))
                thing.ActionSound = GetIntProperty(parser, ActionSound);
            else if (line.StartsWith(RespawnFrame, StringComparison.OrdinalIgnoreCase))
                thing.RespawnFrame = GetIntProperty(parser, RespawnFrame);
            else if (line.StartsWith(Bits, StringComparison.OrdinalIgnoreCase))
                thing.Bits = GetBits(parser, Bits, ThingPropertyStrings);
            else if (line.StartsWith(Mbf21Bits, StringComparison.OrdinalIgnoreCase))
                thing.Mbf21Bits = GetBits(parser, Mbf21Bits, ThingPropertyStringsMbf21);
            else if (line.StartsWith(InfightingGroup, StringComparison.OrdinalIgnoreCase))
                thing.InfightingGroup = GetIntProperty(parser, InfightingGroup);
            else if (line.StartsWith(ProjectileGroup, StringComparison.OrdinalIgnoreCase))
                thing.ProjectileGroup = GetIntProperty(parser, ProjectileGroup);
            else if (line.StartsWith(SplashGroup, StringComparison.OrdinalIgnoreCase))
                thing.SplashGroup = GetIntProperty(parser, SplashGroup);
            else if (line.StartsWith(RipSound, StringComparison.OrdinalIgnoreCase))
                thing.RipSound = GetIntProperty(parser, RipSound);
            else if (line.StartsWith(FastSpeed, StringComparison.OrdinalIgnoreCase))
                thing.FastSpeed = GetDoubleProperty(parser, FastSpeed);
            else if (line.StartsWith(MeleeRange, StringComparison.OrdinalIgnoreCase))
                thing.MeleeRange = GetDoubleProperty(parser, MeleeRange);
            else
                UnknownWarning(parser, "thing type");
        }

        Things.Add(thing);
    }

    private void ParseFrame(SimpleParser parser)
    {
        int startLine = parser.GetCurrentLine();
        DehackedFrame frame = new();
        frame.Frame = parser.ConsumeInteger();

        // Sometimes there is text after the frame. eg. Frame 10 (description)
        if (parser.GetCurrentLine() == startLine)
            parser.ConsumeLine();

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
            else if (line.StartsWith(Mbf21Bits, StringComparison.OrdinalIgnoreCase))
                frame.Mbf21Bits = GetBits(parser, Mbf21Bits, FramePropertyStringsMbf21);
            else if (IsArgs(line))
                SetFrameArgs(parser, line, frame);
            else
                UnknownWarning(parser, "frame type");
        }

        Frames.Add(frame);
    }

    private static void SetFrameArgs(SimpleParser parser, string line, DehackedFrame frame)
    {
        const string FrameArgWarning = "Dehacked: Bad frame arg: ";
        if (line.Length < 5)
            return;

        if (!int.TryParse(line.AsSpan(4, 1), out int index))
        {
            Log.Warn($"{FrameArgWarning}{line}");
            return;
        }

        if (index < 1 || index > 8)
        {
            Log.Warn($"Dehacked: Bad frame arg: {line}");
            return;
        }

        parser.ConsumeString();
        parser.Consume('=');
        int value = parser.ConsumeInteger();

        switch (index)
        {
            case 1:
                frame.Args1 = value;
                break;
            case 2:
                frame.Args2 = value;
                break;
            case 3:
                frame.Args3 = value;
                break;
            case 4:
                frame.Args4 = value;
                break;
            case 5:
                frame.Args5 = value;
                break;
            case 6:
                frame.Args6 = value;
                break;
            case 7:
                frame.Args7 = value;
                break;
            case 8:
                frame.Args8 = value;
                break;
            default:
                break;
        }
    }

    private static bool IsArgs(string line)
    {
        if (line.Length < 5 || !line.StartsWith("ARGS", StringComparison.OrdinalIgnoreCase))
            return false;

        return char.IsDigit(line[4]);
    }

    private void ParseAmmo(SimpleParser parser)
    {
        int startLine = parser.GetCurrentLine();
        DehackedAmmo ammo = new();
        ammo.AmmoNumber = parser.ConsumeInteger();

        if (parser.GetCurrentLine() == startLine)
            parser.ConsumeLine();

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
        parser.ConsumeLine();

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
            else if (line.StartsWith(AmmoPerShot, StringComparison.OrdinalIgnoreCase))
                weapon.AmmoPerShot = GetIntProperty(parser, AmmoPerShot);
            else if (line.StartsWith(Mbf21Bits, StringComparison.OrdinalIgnoreCase))
                weapon.Mbf21Bits = GetBits(parser, Mbf21Bits, WeaponPropertyStringsMbf21);
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

            if (!parser.IsDone())
                parser.ConsumeLine();
        }

        Pointers.Add(pointer);
    }

    private void ParseMisc(SimpleParser parser, int itemLine)
    {
        Misc = new();

        // Can have number in brackets (e.g. [0]) just eat it
        if (parser.GetCurrentLine() == itemLine)
            parser.ConsumeLine();

        while (!IsBlockComplete(parser))
        {
            string item = parser.PeekLine();

            if (item.StartsWith(InitialHealth, StringComparison.OrdinalIgnoreCase))
                Misc.InitialHealth = GetIntProperty(parser, InitialHealth);
            else if (item.StartsWith(InitialBullets, StringComparison.OrdinalIgnoreCase))
                Misc.InitialBullets = GetIntProperty(parser, InitialBullets);
            else if (item.StartsWith(MaxHealth, StringComparison.OrdinalIgnoreCase))
                Misc.MaxHealth = GetIntProperty(parser, MaxHealth);
            else if (item.StartsWith(MaxArmor, StringComparison.OrdinalIgnoreCase))
                Misc.MaxArmor = GetIntProperty(parser, MaxArmor);
            else if (item.StartsWith(GreenArmorClass, StringComparison.OrdinalIgnoreCase))
                Misc.GreenArmorClass = GetIntProperty(parser, GreenArmorClass);
            else if (item.StartsWith(BlueArmorClass, StringComparison.OrdinalIgnoreCase))
                Misc.BlueArmorClass = GetIntProperty(parser, BlueArmorClass);
            else if (item.StartsWith(MaxSoulsphere, StringComparison.OrdinalIgnoreCase))
                Misc.MaxSoulsphere = GetIntProperty(parser, MaxSoulsphere);
            else if (item.StartsWith(SoulsphereHealth, StringComparison.OrdinalIgnoreCase))
                Misc.SoulsphereHealth = GetIntProperty(parser, SoulsphereHealth);
            else if (item.StartsWith(MegasphereHealth, StringComparison.OrdinalIgnoreCase))
                Misc.MegasphereHealth = GetIntProperty(parser, MegasphereHealth);
            else if (item.StartsWith(GodModeHealth, StringComparison.OrdinalIgnoreCase))
                Misc.GodModeHealth = GetIntProperty(parser, GodModeHealth);
            else if (item.StartsWith(IDFAArmorClass, StringComparison.OrdinalIgnoreCase))
                Misc.IdfaArmorClass = GetIntProperty(parser, IDFAArmorClass);
            else if (item.StartsWith(IDFAArmor, StringComparison.OrdinalIgnoreCase))
                Misc.IdfaArmor = GetIntProperty(parser, IDFAArmor);
            else if (item.StartsWith(IDKFAArmorClass, StringComparison.OrdinalIgnoreCase))
                Misc.IdkfaArmorClass = GetIntProperty(parser, IDKFAArmorClass);
            else if (item.StartsWith(IDKFAArmor, StringComparison.OrdinalIgnoreCase))
                Misc.IdkfaArmor = GetIntProperty(parser, IDKFAArmor);
            else if (item.StartsWith(BFGCellsPerShot, StringComparison.OrdinalIgnoreCase))
                Misc.BfgCellsPerShot = GetIntProperty(parser, BFGCellsPerShot);
            else if (item.StartsWith(MonstersInfight, StringComparison.OrdinalIgnoreCase))
                Misc.MonstersInfight = (MonsterInfightType)GetIntProperty(parser, MonstersInfight);
            else
                UnknownWarning(parser, "misc");
        }
    }

    private void ParseSound(SimpleParser parser)
    {
        DehackedSound sound = new();
        sound.Number = parser.ConsumeInteger();

        while (!IsBlockComplete(parser))
        {
            string line = parser.PeekLine();
            if (IgnoreSoundProperties.Any(x => line.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                parser.ConsumeLine();
                continue;
            }

            if (line.StartsWith(SoundZeroOne, StringComparison.OrdinalIgnoreCase))
                sound.ZeroOne = GetIntProperty(parser, SoundZeroOne);
            else if (line.StartsWith(SoundValue, StringComparison.OrdinalIgnoreCase))
                sound.Priority = GetIntProperty(parser, SoundValue);
            else
                UnknownWarning(parser, "sound");
        }

        Sounds.Add(sound);
    }

    private void ParseBexText(SimpleParser parser)
    {
        parser.ConsumeString();
        StringBuilder sb = new();

        while (!IsBlockComplete(parser, isBex: true))
        {
            BexString bexString = new();
            sb.Length = 0;

            bexString.Mnemonic = parser.ConsumeString();
            parser.ConsumeString("=");
            bexString.Value = parser.ConsumeLine();
            BexStrings.Add(bexString);
        }
    }

    private void ParseBexPointer(SimpleParser parser)
    {
        parser.ConsumeString();

        while (!IsBexPointerBlockComplete(parser))
        {
            parser.ConsumeString("Frame");
            int frame = parser.ConsumeInteger();
            parser.ConsumeString("=");
            string name = parser.ConsumeString();
            Pointers.Add(new DehackedPointer() { Frame = frame, CodePointerMnemonic = name });
        }
    }

    private void ParseBexPar(SimpleParser parser)
    {
        parser.ConsumeString();

        while (!IsBlockComplete(parser, isBex: true))
        {
            parser.ConsumeString("par");
            int line = parser.GetCurrentLine();
            int item1 = parser.ConsumeInteger();
            int item2 = parser.ConsumeInteger();
            int? item3 = null;

            if (parser.GetCurrentLine() == line)
                item3 = parser.ConsumeInteger();

            if (item3.HasValue)
                BexPars.Add(new BexPar() { Episode = item1, Map = item2, Par = item3.Value });
            else
                BexPars.Add(new BexPar() { Map = item1, Par = item2 });
        }
    }

    private void ParseBexItem(SimpleParser parser, List<BexItem> items)
    {
        parser.ConsumeString();

        while (!IsBlockComplete(parser, isBex: true))
        {
            string? mnemonic = null;
            int? index = parser.ConsumeIfInt();
            if (index == null)
                mnemonic = parser.ConsumeString();

            string entry = parser.ConsumeString();
            items.Add(new BexItem() { Mnemonic = mnemonic, Index = index, EntryName = entry });
        }
    }

    private static bool IsBexPointerBlockComplete(SimpleParser parser)
    {
        if (parser.PeekString(0, out string? frame) && parser.PeekString(2, out string? equal)
            && frame != null && equal != null)
        {
            return !frame.Equals("Frame", StringComparison.OrdinalIgnoreCase) || !equal.Equals("=");
        }

        return true;
    }

    private bool IsBlockComplete(SimpleParser parser, bool isBex = false)
    {
        if (parser.IsDone())
            return true;

        string peek = parser.PeekString();
        if (isBex && string.IsNullOrEmpty(peek))
            return true;

        if (BexBaseTypes.Contains(peek))
            return true;

        // Dehacked base types are all proceeded by a number, check to not confuse with random text
        if (BaseTypes.Contains(peek) && parser.PeekString(1, out string? data) &&
            int.TryParse(data, out _))
            return true;

        return false;
    }

    private static uint GetBits(SimpleParser parser, string property, IReadOnlyDictionary<string, uint> lookup)
    {
        ConsumeProperty(parser, property);
        parser.ConsumeString("=");
        uint? bits = (uint?)parser.ConsumeIfInt();
        if (bits.HasValue)
            return bits.Value;

        return ParseStringBits(parser, lookup);
    }

    private static uint ParseStringBits(SimpleParser parser, IReadOnlyDictionary<string, uint> lookup)
    {
        uint bits = 0;
        string[] items = parser.ConsumeLine().Split(new string[] { "+", "|", "," }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string item in items)
        {
            string stringFlag = item.Trim();
            if (lookup.TryGetValue(stringFlag, out uint flag))
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

    public static double GetDoubleProperty(SimpleParser parser, string property)
    {
        ConsumeProperty(parser, property);
        parser.ConsumeString("=");
        return parser.ConsumeDouble();
    }

    private static void ConsumeProperty(SimpleParser parser, string property)
    {
        for (int i = 0; i < property.Count(x => x == ' ') + 1; i++)
            parser.ConsumeString();
    }
}
