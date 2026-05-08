using Helion.Graphics.Palettes;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Helion.Dehacked;

public partial class DehackedDefinition
{
    struct DehackedProp
    {
        public string Prop;
        public string Value;
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex PointerRegex = new(@"^\(\S+ (\d+)\)");

    public event EventHandler<string>? OnUnknownItem;

    public List<DehackedThing> Things = [];
    public List<DehackedFrame> Frames = [];
    public List<DehackedAmmo> Ammo = [];
    public List<DehackedWeapon> Weapons = [];
    public List<DehackedString> Strings = [];
    public List<DehackedPointer> Pointers = [];
    public List<DehackedSound> Sounds = [];

    public List<BexString> BexStrings = [];
    public List<BexPar> BexPars = [];
    public List<BexItem> BexSounds = [];
    public List<BexItem> BexSprites = [];

    public Dictionary<int, string> SoundLookup = [];
    public Dictionary<int, EntityDefinition> DefinitionLookup = [];

    private readonly StringBuilder m_sb = new();

    public DehackedCheat? Cheat { get; private set; }
    public DehackedMisc? Misc { get; private set; }
    public int DoomVersion { get; private set; }
    public int PatchFormat { get; set; }
    public bool HasBloodColor => BloodColors.Count > 0;
    public readonly HashSet<PaletteColor> BloodColors = [];

    public DehackedDefinition()
    {
        for (int i = 0; i < SoundStrings.Length; i++)
            SoundLookup[i] = SoundStrings[i];
    }

    public void FinalizeData()
    {
        // Clear out data not required at runtime
        Things = [];
        Frames = [];
        Ammo = [];
        Weapons = [];
        Strings = [];
        Pointers = [];
        Sounds = [];
        BexStrings = [];
        BexPars = [];
        BexSounds = [];
        BexSprites = [];
        Cheat = null;
        Misc = null;
        m_sb.Clear();
    }

    public void LoadActorDefinitions(EntityDefinitionComposer composer)
    {
        for (int i = 0; i < ActorNames.Length; i++)
        {
            var def = composer.GetByName(ActorNames[i]);
            if (def != null)
                DefinitionLookup[i] = def;
        }
    }

    public void Parse(string data)
    {
        data = data.Replace('\0', ' ').StripNonUtf8Chars();
        SimpleParser parser = CreateDehackedParser(data);
        parser = ParseHeader(parser, data);

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

            if (item.EqualsIgnoreCase(ThingName))
                ParseThing(parser);
            else if (item.EqualsIgnoreCase(FrameName))
                ParseFrame(parser);
            else if (item.EqualsIgnoreCase(AmmoName))
                ParseAmmo(parser);
            else if (item.EqualsIgnoreCase(WeaponName))
                ParseWeapon(parser);
            else if (item.EqualsIgnoreCase(CheatName))
                ParseCheat(parser);
            else if (item.EqualsIgnoreCase(TextName))
                ParseText(parser);
            else if (item.EqualsIgnoreCase(PointerName))
                ParsePointer(parser);
            else if (item.StartsWith(MiscName, StringComparison.OrdinalIgnoreCase))
                ParseMisc(parser, itemLine);
            else if (item.EqualsIgnoreCase(SoundName))
                ParseSound(parser);
            else if (item.EqualsIgnoreCase(BexStringName))
                ParseBexString(parser);
            else if (item.EqualsIgnoreCase(BexPointerName))
                ParseBexPointer(parser);
            else if (item.EqualsIgnoreCase(BexParName))
                ParseBexPar(parser);
            else if (item.EqualsIgnoreCase(BexSoundName))
                ParseBexItem(parser, BexSounds);
            else if (item.EqualsIgnoreCase(BexSpriteName))
                ParseBexItem(parser, BexSprites);
            else if (IsUselessLine(item))
                parser.ConsumeLine();
            else
                UnknownWarning(parser, "type", item);

            ConsumeLine(parser, itemLine);
        }
    }

    private static readonly char[] SpecialChars = ['='];

    private static SimpleParser CreateDehackedParser(string data)
    {
        SimpleParser parser = new(keepBeginningSpaces: true);
        parser.SetSpecialChars(SpecialChars);
        parser.SetCommentCallback(IsComment);
        parser.Parse(data, keepEmptyLines: true, parseQuotes: false);
        return parser;
    }

    private static bool IsUselessLine(string item)
    {
        if (item.EqualsIgnoreCase("Engine"))
            return true;
        if (item.EqualsIgnoreCase("IWAD"))
            return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetEntityDefinition(int thingNumber, [NotNullWhen(true)] out EntityDefinition? def) =>
        DefinitionLookup.TryGetValue(thingNumber - 1, out def);

    public bool TryGetId24PickupType(EntityDefinitionComposer composer, int pickupItemType, [NotNullWhen(true)] out EntityDefinition? definition)
    {
        definition = null;
        if (pickupItemType < 0 || pickupItemType >= Id24PickupLookup.Length) 
            return false;

        definition = composer.GetByName(Id24PickupLookup[pickupItemType]);
        if (definition == null)
            return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetSoundName(int soundIndex, [NotNullWhen(true)] out string? soundName) =>
        SoundLookup.TryGetValue(soundIndex, out soundName);

    private static bool IsComment(string data, int lineStartIndex, int index) => lineStartIndex == 0 && data[index] == '#';

    private void UnknownWarning(SimpleParser parser, string type, string? prefix = null)
    {
        var lineNumber = parser.GetCurrentLine();
        var line = parser.ConsumeLine();
        if (string.IsNullOrWhiteSpace(line) && string.IsNullOrWhiteSpace(prefix))
            return;
        if (prefix != null)
            line = prefix + " " + line;
        OnUnknownItem?.Invoke(this, line);
        Log.Warn($"Dehacked: Skipping unknown {type}: {line} line:{lineNumber}");
    }

    private SimpleParser ParseHeader(SimpleParser parser, string data)
    {
        DoomVersion = 0;
        PatchFormat = 0;
        while (!parser.IsDone() && (DoomVersion == 0 || PatchFormat == 0))
        {
            var line = parser.PeekLine();
            if (line.StartsWith('#'))
            {
                parser.ConsumeLine();
                continue;
            }

            if (GetProperty(line, out var dehackedProp))
            {
                if (dehackedProp.Prop.EqualsIgnoreCase(DoomVersionName))
                    DoomVersion = GetIntProperty(dehackedProp);
                else if (dehackedProp.Prop.EqualsIgnoreCase(PatchFormatName))
                    PatchFormat = GetIntProperty(dehackedProp);
            }
            
            parser.ConsumeLine();
        }

        // No header, reset to normal
        if (parser.IsDone())
            return CreateDehackedParser(data);

        return parser;
    }

    private void ParseThing(SimpleParser parser)
    {
        int lineNumber = parser.GetCurrentLine();
        DehackedThing thing = new();
        thing.Number = parser.ConsumeInteger();
        if (parser.Peek('('))
            thing.Name = parser.ConsumeLine();
        ConsumeLine(parser, lineNumber);

        while (!IsBlockComplete(parser))
        {
            lineNumber = parser.GetCurrentLine();
            var line = parser.PeekLine();
            if (GetProperty(line, out var dehackedProp))
            {
                var prop = dehackedProp.Prop;
                if (prop.EqualsIgnoreCase(IDNumber))
                    thing.ID = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(InitFrame))
                    thing.InitFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Hitpoints))
                    thing.Hitpoints = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(FirstMovingFrame))
                    thing.FirstMovingFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(AlertSound))
                    thing.AlertSound = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(ReactionTime))
                    thing.ReactionTime = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(AttackSound))
                    thing.AttackSound = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(InjuryFrame))
                    thing.InjuryFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PainChance))
                    thing.PainChance = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PainSound))
                    thing.PainSound = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(CloseAttackFrame))
                    thing.CloseAttackFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(FarAttackFrame))
                    thing.FarAttackFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DeathFrame))
                    thing.DeathFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(ExplodingFrame))
                    thing.ExplodingFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DeathSound))
                    thing.DeathSound = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Speed))
                    thing.Speed = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Width))
                    thing.Width = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Height))
                    thing.Height = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Mass))
                    thing.Mass = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MisileDamage))
                    thing.MisileDamage = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(ActionSound))
                    thing.ActionSound = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(RespawnFrame))
                    thing.RespawnFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DroppedItem))
                    thing.DroppedItem = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(GibHealth))
                    thing.GibHealth = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Bits))
                    thing.Bits = GetBits(dehackedProp, ThingPropertyStrings);
                else if (prop.EqualsIgnoreCase(Mbf21Bits))
                    thing.Mbf21Bits = GetBits(dehackedProp,ThingPropertyStringsMbf21);
                else if (prop.EqualsIgnoreCase(InfightingGroup))
                    thing.InfightingGroup = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(ProjectileGroup))
                    thing.ProjectileGroup = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(SplashGroup))
                    thing.SplashGroup = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(RipSound))
                    thing.RipSound = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(FastSpeed))
                    thing.FastSpeed = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MeleeRange))
                    thing.MeleeRange = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Id24Bits))
                    thing.Id24Bits = GetBits(dehackedProp,ThingPropertyStringsId24);
                else if (prop.EqualsIgnoreCase(MinRespawnTicks))
                    thing.MinRespawnTicks = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(RespawnDice))
                    thing.RespawnDice = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PickupAmmoType))
                    thing.PickupAmmoType = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PickupAmmoCategory))
                    thing.PickupAmmoCategory = (Id24AmmoCategory)GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PickupWeaponType))
                    thing.PickupWeaponType = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PickupItemType))
                    thing.PickupItemType = (Id24PickupType?)GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PickupBonusCount))
                    thing.PickupBonusCount = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PickupSound))
                    thing.PickupSound = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PickupMessage))
                    thing.PickupMessage = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(TranslationLump))
                    thing.TranslationLump = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(SelfDamageFactor))
                    thing.SelfDamageFactor = MathHelper.FromFixed(GetIntProperty(dehackedProp));
                else if (prop.EqualsIgnoreCase(BloodColor))
                {
                    thing.BloodColor = GetIntProperty(dehackedProp);
                    BloodColors.Add((PaletteColor)thing.BloodColor);
                }
                else if (!IgnoreLine(dehackedProp))
                    UnknownWarning(parser, "thing type");
            }
            else
                UnknownWarning(parser, "thing type");

            ConsumeLine(parser, lineNumber);
        }

        Things.Add(thing);
    }

    private static bool GetProperty(string line, out DehackedProp prop)
    {
        var index = line.IndexOf('=');
        if (index == -1)
        {
            prop = default;
            return false;
        }

        prop = new()
        {
            Prop = line[..index].Trim(),
            Value = line[(index + 1)..].Trim()
        };
        return true;
    }

    private static bool IgnoreLine(DehackedProp prop) =>
        prop.Prop.EqualsIgnoreCase(Plural) || prop.Prop.EqualsIgnoreCase(Name1) || prop.Prop.EqualsIgnoreCase(RetroBits) || prop.Prop.EqualsIgnoreCase(Bits2) || prop.Prop.EqualsIgnoreCase(Bits3);

    private void ParseFrame(SimpleParser parser)
    {
        int lineNumber = parser.GetCurrentLine();
        DehackedFrame frame = new();
        frame.Frame = parser.ConsumeInteger();

        // Sometimes there is text after the frame. eg. Frame 10 (description)
        ConsumeLine(parser, lineNumber);

        while (!IsBlockComplete(parser))
        {
            lineNumber = parser.GetCurrentLine();
            var line = parser.PeekLine();
            if (GetProperty(line, out var dehackedProp))
            {
                var prop = dehackedProp.Prop;
                if (prop.EqualsIgnoreCase(SpriteNum))
                    frame.SpriteNumber = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(SpriteSubNum))
                    frame.SpriteSubNumber = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Duration))
                    frame.Duration = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(NextFrame))
                    frame.NextFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Unknown1))
                    frame.Unknown1 = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Unknown2))
                    frame.Unknown2 = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Mbf21Bits))
                    frame.Mbf21Bits = GetBits(dehackedProp, FramePropertyStringsMbf21);
                else if (IsArgs(dehackedProp))
                    SetFrameArgs(parser, line, frame);
                else
                    UnknownWarning(parser, "frame type");
            }
            else
                UnknownWarning(parser, "frame type");

            ConsumeLine(parser, lineNumber);
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
        int value = ConsumeDehackedInteger(parser.ConsumeString());

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

    private static bool IsArgs(in DehackedProp dehackedProp)
    {
        var prop = dehackedProp.Prop;
        if (prop.Length < 5 || !prop.StartsWith("ARGS", StringComparison.OrdinalIgnoreCase))
            return false;

        return char.IsDigit(prop[4]);
    }

    private void ParseAmmo(SimpleParser parser)
    {
        int lineNumber = parser.GetCurrentLine();
        DehackedAmmo ammo = new();
        ammo.AmmoNumber = parser.ConsumeInteger();
        ConsumeLine(parser, lineNumber);

        while (!IsBlockComplete(parser))
        {
            lineNumber = parser.GetCurrentLine();
            var line = parser.PeekLine();
            if (GetProperty(line, out var dehackedProp))
            {
                var prop = dehackedProp.Prop;
                if (prop.EqualsIgnoreCase(MaxAmmo))
                    ammo.MaxAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(PerAmmo))
                    ammo.PerAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(InitialAmmo))
                    ammo.InitialAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MaxUpgradedAmmo))
                    ammo.MaxUpgradedAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(BoxAmmo))
                    ammo.BoxAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(BackpackAmmo))
                    ammo.BackpackAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(WeaponAmmo))
                    ammo.WeaponAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DroppedAmmo))
                    ammo.DroppedAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DroppedBoxAmmo))
                    ammo.DroppedBoxAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DroppedBackpackAmmo))
                    ammo.DroppedBackpackAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DroppedWeaponAmmo))
                    ammo.DroppedWeaponAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(DeathmatchWeaponAmmo))
                    ammo.DeathmatchWeaponAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Skill1Multiplier))
                    ammo.Skill1Multiplier = MathHelper.FromFixed(GetIntProperty(dehackedProp));
                else if (prop.EqualsIgnoreCase(Skill2Multiplier))
                    ammo.Skill2Multiplier = MathHelper.FromFixed(GetIntProperty(dehackedProp));
                else if (prop.EqualsIgnoreCase(Skill3Multiplier))
                    ammo.Skill3Multiplier = MathHelper.FromFixed(GetIntProperty(dehackedProp));
                else if (prop.EqualsIgnoreCase(Skill4Multiplier))
                    ammo.Skill4Multiplier = MathHelper.FromFixed(GetIntProperty(dehackedProp));
                else if (prop.EqualsIgnoreCase(Skill5Multiplier))
                    ammo.Skill5Multiplier = MathHelper.FromFixed(GetIntProperty(dehackedProp));
                else
                    UnknownWarning(parser, "ammo type");
            }
            else
                UnknownWarning(parser, "ammo type");
            ConsumeLine(parser, lineNumber);
        }

        Ammo.Add(ammo);
    }

    private void ParseWeapon(SimpleParser parser)
    {
        DehackedWeapon weapon = new();
        int lineNumber = parser.GetCurrentLine();
        weapon.WeaponNumber = parser.ConsumeInteger();
        ConsumeLine(parser, lineNumber);

        while (!IsBlockComplete(parser))
        {
            lineNumber = parser.GetCurrentLine();
            var line = parser.PeekLine();
            if (GetProperty(line, out var dehackedProp))
            {
                var prop = dehackedProp.Prop;
                if (prop.EqualsIgnoreCase(DeselectFrame))
                    weapon.DeselectFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(SelectFrame))
                    weapon.SelectFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(AmmoType))
                    weapon.AmmoType = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(BobbingFrame))
                    weapon.BobbingFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(ShootingFrame))
                    weapon.ShootingFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(FiringFrame))
                    weapon.FiringFrame = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(AmmoPerShot))
                    weapon.AmmoPerShot = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(AmmoUse))
                    weapon.AmmoPerShot = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MinAmmo))
                    weapon.MinAmmo = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(Mbf21Bits))
                    weapon.Mbf21Bits = GetBits(dehackedProp, WeaponPropertyStringsMbf21);
                else if (prop.EqualsIgnoreCase(WeaponSlotPriority))
                    weapon.SlotPriority = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(WeaponSlot))
                    weapon.Slot = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(WeaponSwitchPriority))
                    weapon.SwitchPriority = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(InitialOwned))
                    weapon.InitialOwned = GetIntProperty(dehackedProp) != 0;
                else if (prop.EqualsIgnoreCase(InitialRaised))
                    weapon.InitialRaised = GetIntProperty(dehackedProp) != 0;
                else if (prop.EqualsIgnoreCase(CarouselIcon))
                    weapon.CarouselIcon = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(AllowSwitchWithOwnedWeapon))
                    weapon.AllowSwitchWithOwnedWeapon = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(NoSwitchWithOwnedWeapon))
                    weapon.NoSwitchWithOwnedWeapon = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(AllowSwitchWithOwnedItem))
                    weapon.AllowSwitchWithOwnedItem = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(NoSwitchWithOwnedItem))
                    weapon.NoSwitchWithOwnedItem = GetIntProperty(dehackedProp);
                else
                    UnknownWarning(parser, "weapon type");
            }
            else
                UnknownWarning(parser, "weapon type");

            ConsumeLine(parser, lineNumber);
        }

        Weapons.Add(weapon);
    }

    private void ParseCheat(SimpleParser parser)
    {
        int lineNumber = parser.GetCurrentLine();
        Cheat = new();
        ConsumeLine(parser, lineNumber);

        while (!IsBlockComplete(parser))
        {
            lineNumber = parser.GetCurrentLine();
            var line = parser.PeekLine();
            if (GetProperty(line, out var dehackedProp))
            {
                var prop = dehackedProp.Prop;
                if (prop.EqualsIgnoreCase(ChangeMusic))
                    Cheat.ChangeMusic = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(Chainsaw))
                    Cheat.Chainsaw = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(God))
                    Cheat.God = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(AmmoAndKeys))
                    Cheat.AmmoAndKeys = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(AmmoCheat))
                    Cheat.Ammo = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(NoClip1))
                    Cheat.NoClip1 = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(NoClip2))
                    Cheat.NoClip2 = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(Invincibility))
                    Cheat.Invincibility = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(Invisibility))
                    Cheat.Invisibility = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(RadSuit))
                    Cheat.RadSuit = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(AutoMap))
                    Cheat.AutoMap = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(LiteAmp))
                    Cheat.LiteAmp = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(Behold))
                    Cheat.Behold = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(LevelWarp))
                    Cheat.LevelWarp = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(MapCheat))
                    Cheat.LevelWarp = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(PlayerPos))
                    Cheat.PlayerPos = dehackedProp.Value;
                else if (prop.EqualsIgnoreCase(Berserk))
                    Cheat.Berserk = dehackedProp.Value;
                else
                    UnknownWarning(parser, "cheat type");
            }
            else
                UnknownWarning(parser, "cheat type");

            ConsumeLine(parser, lineNumber);
        }
    }

    private void ParseText(SimpleParser parser)
    {
        DehackedString text = new();
        text.OldSize = parser.ConsumeInteger();
        text.NewSize = parser.ConsumeInteger();
        m_sb.Clear();

        while (!IsBlockComplete(parser))
        {
            m_sb.Append(parser.ConsumeLine(keepBeginningSpaces: true));
            m_sb.Append('\n');
            // Empty strings get eaten by IsBlockComplete
            if (string.IsNullOrEmpty(parser.PeekString()))
                m_sb.Append('\n');
        }

        while (m_sb.Length > 0 && m_sb[m_sb.Length - 1] == '\n')
            m_sb.Length--;

        if (text.OldSize > m_sb.Length)
        {
            Log.Warn($"Dehacked: Invalid dehacked string length:{text.OldSize} line:{parser.GetCurrentLine()}");
            return;
        }

        string sbText = m_sb.ToString();
        text.OldString = sbText.Substring(0, text.OldSize);
        text.NewString = sbText.Substring(text.OldSize);

        Strings.Add(text);
    }

    private void ParsePointer(SimpleParser parser)
    {
        DehackedPointer pointer = new();
        pointer.Number = parser.ConsumeInteger();

        var offset = parser.GetCurrentOffset();
        string text = parser.ConsumeLine();
        var match = PointerRegex.Match(text);
        if (!match.Success || match.Groups.Count < 2)
            throw new ParserException(offset.Line, offset.Char, -1, $"Invalid pointer text: {text}");

        string frame = match.Groups[1].Value;
        if (!int.TryParse(frame, out int frameNumber))
        {
            Log.Warn($"Dehacked: Invalid frame:{frame} line:{parser.GetCurrentLine()}");
            return;
        }

        pointer.Frame = frameNumber;

        while (!IsBlockComplete(parser))
        {
            var line = parser.PeekLine();
            if (GetProperty(line, out var dehackedProp))
            {
                if (dehackedProp.Prop.EqualsIgnoreCase("Codep Frame"))
                    pointer.CodePointerFrame = GetIntProperty(dehackedProp);
                else
                    UnknownWarning(parser, "pointer type");
            }

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
            int lineNumber = parser.GetCurrentLine();
            var item = parser.PeekLine();
            if (GetProperty(item, out var dehackedProp))
            {
                var prop = dehackedProp.Prop;
                if (prop.EqualsIgnoreCase(InitialHealth))
                    Misc.InitialHealth = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(InitialBullets))
                    Misc.InitialBullets = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MaxHealth))
                    Misc.MaxHealth = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MaxArmor))
                    Misc.MaxArmor = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(GreenArmorClass))
                    Misc.GreenArmorClass = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(BlueArmorClass))
                    Misc.BlueArmorClass = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MaxSoulsphere))
                    Misc.MaxSoulsphere = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(SoulsphereHealth))
                    Misc.SoulsphereHealth = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MegasphereHealth))
                    Misc.MegasphereHealth = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(GodModeHealth))
                    Misc.GodModeHealth = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(IDFAArmorClass))
                    Misc.IdfaArmorClass = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(IDFAArmor))
                    Misc.IdfaArmor = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(IDKFAArmorClass))
                    Misc.IdkfaArmorClass = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(IDKFAArmor))
                    Misc.IdkfaArmor = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(BFGCellsPerShot))
                    Misc.BfgCellsPerShot = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MonstersInfight))
                    Misc.MonstersInfight = (MonsterInfightType)GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(MonstersIgnore))
                    Misc.MonstersIgnoreEachOther = GetIntProperty(dehackedProp) != 0;
                else
                    UnknownWarning(parser, "misc");
            }
            else
                UnknownWarning(parser, "misc");

            ConsumeLine(parser, lineNumber);
        }
    }

    private void ParseSound(SimpleParser parser)
    {
        DehackedSound sound = new();
        sound.Number = parser.ConsumeInteger();
        if (parser.Peek('('))
           parser.ConsumeLine();

        while (!IsBlockComplete(parser))
        {
            int lineNumber = parser.GetCurrentLine();
            var line = parser.PeekLine();
            if (IgnoreSoundProperties.Any(x => line.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                parser.ConsumeLine();
                continue;
            }

            if (GetProperty(line, out var dehackedProp))
            {
                var prop = dehackedProp.Prop;
                if (prop.EqualsIgnoreCase(SoundZeroOne))
                    sound.ZeroOne = GetIntProperty(dehackedProp);
                else if (prop.EqualsIgnoreCase(SoundValue))
                    sound.Priority = GetIntProperty(dehackedProp);
                else
                    UnknownWarning(parser, "sound");
            }
            else
                UnknownWarning(parser, "sound");
            ConsumeLine(parser, lineNumber);
        }

        Sounds.Add(sound);
    }

    private void ParseBexString(SimpleParser parser)
    {
        parser.ConsumeString();

        while (!IsBlockComplete(parser, isBex: true))
        {
            BexString bexString = new();
            if (ConsumeBexStringMnemonic(parser, out var mnemonic))
            {
                bexString.Mnemonic = mnemonic;
                parser.ConsumeString("=");
                bexString.Value = ConsumeBexTextValue(parser);
                BexStrings.Add(bexString);
            }
        }
    }

    private static bool ConsumeBexStringMnemonic(SimpleParser parser, [NotNullWhen(true)] out string? mnemonic)
    {
        var startLine = parser.GetCurrentLine();
        var line = parser.PeekLine();
        var index = line.IndexOf('=');
        if (index != -1)
        {
            string value = line[..index].Trim();
            while (parser.PeekString() != "=" && startLine == parser.GetCurrentLine())
                parser.ConsumeString();

            if (startLine == parser.GetCurrentLine())
            {
                mnemonic = value;
                return true;
            }
        }
        else
        {
            parser.ConsumeLineSpan();
        }

        mnemonic = null;
        return false;
    }

    private string ConsumeBexTextValue(SimpleParser parser)
    {
        m_sb.Clear();
        while (true)
        {
            var value = parser.ConsumeLine().Replace("\\n", "\n");
            if (!value.EndsWith('\\'))
            {
                m_sb.Append(value);
                return m_sb.ToString();
            }
            m_sb.Append(value.AsSpan(0, value.Length - 1));
        }
    }

    private void ParseBexPointer(SimpleParser parser)
    {
        parser.ConsumeString();

        while (!IsBexPointerBlockComplete(parser))
        {
            int lineNumber = parser.GetCurrentLine();
            parser.ConsumeString("Frame");
            int frame = parser.ConsumeInteger();
            parser.ConsumeString("=");
            string name = parser.ConsumeString();
            Pointers.Add(new DehackedPointer() { Frame = frame, CodePointerMnemonic = name });
            ConsumeLine(parser, lineNumber);
        }
    }

    private void ParseBexPar(SimpleParser parser)
    {
        parser.ConsumeString();

        while (!IsBlockComplete(parser, isBex: true))
        {
            int lineNumber = parser.GetCurrentLine();
            parser.ConsumeString("par");
            int item1 = parser.ConsumeInteger();
            int item2 = parser.ConsumeInteger();
            int? item3 = null;

            if (parser.GetCurrentLine() == lineNumber && parser.PeekInteger(out int peekInt))
                item3 = peekInt;

            if (item3.HasValue)
                BexPars.Add(new BexPar() { Episode = item1, Map = item2, Par = item3.Value });
            else
                BexPars.Add(new BexPar() { Map = item1, Par = item2 });
            ConsumeLine(parser, lineNumber);
        }
    }

    private void ParseBexItem(SimpleParser parser, List<BexItem> items)
    {
        parser.ConsumeString();

        while (!IsBlockComplete(parser, isBex: true))
        {
            int lineNumber = parser.GetCurrentLine();
            string? mnemonic = null;
            int? index = parser.ConsumeIfInt();
            if (index == null)
                mnemonic = parser.ConsumeString();

            parser.ConsumeIf("=");

            string entry = parser.ConsumeString();
            items.Add(new BexItem() { Mnemonic = mnemonic, Index = index, EntryName = entry });
            ConsumeLine(parser, lineNumber);
        }
    }

    private static void ConsumeLine(SimpleParser parser, int lineNumber)
    {
        if (parser.GetCurrentLine() == lineNumber)
            parser.ConsumeLine();
    }

    private static bool IsBexPointerBlockComplete(SimpleParser parser)
    {
        if (parser.PeekString(0, out string? frame) && parser.PeekString(2, out string? equal)
            && frame != null && equal != null)
        {
            return !frame.Equals("Frame", StringComparison.OrdinalIgnoreCase) || !equal.Equals("=", StringComparison.Ordinal);
        }

        return true;
    }

    private bool IsBlockComplete(SimpleParser parser, bool isBex = false)
    {
        if (parser.IsDone())
            return true;

        string peek = parser.PeekString();
        while (string.IsNullOrEmpty(peek))
        {
            parser.ConsumeString();
            if (parser.IsDone())
                return true;

            peek = parser.PeekString();
        }

        if (BexBaseTypes.Contains(peek))
            return true;

        // Dehacked base types are all proceeded by a number, check to not confuse with random text
        if (BaseTypes.Contains(peek) && parser.PeekString(1, out string? data) &&
            int.TryParse(data, out _))
            return true;

        return false;
    }

    private static uint GetBits(in DehackedProp prop, IReadOnlyDictionary<string, uint> lookup)
    {
        if (int.TryParse(prop.Value, out var parseBits))
            return (uint)parseBits;

        return ParseStringBits(prop.Value, lookup);
    }

    private static readonly string[] StringBitsSplit = ["+", "|", ",", " "];

    private static uint ParseStringBits(string value, IReadOnlyDictionary<string, uint> lookup)
    {
        uint bits = 0;
        var items = value.Split(StringBitsSplit, StringSplitOptions.RemoveEmptyEntries);

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

    private static int GetIntProperty(DehackedProp prop)
    {
        return ConsumeDehackedInteger(prop.Value);
    }

    // Dehacked parsers used sscanf which would read until a non digit was hit. Also supports hex and octal because why not.
    private static int ConsumeDehackedInteger(string data)
    {
        var span = data.AsSpan().TrimStart();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(TrimNumberGarbage(span[2..], hex: true), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                return value;
        }

        if (span.Length > 1 && span[0] == '0')
        {
            if (TryParseOctal(TrimNumberGarbage(span[1..]), out var value))
                return value;
        }

        span = TrimNumberGarbage(span);
        if (int.TryParse(span, out var i))
            return i;

        // Matching existing behavior that returns 0 on failure.
        Log.Warn($"Dehacked bad integer: {data}");
        return 0;
    }

    private static ReadOnlySpan<char> TrimNumberGarbage(ReadOnlySpan<char> span, bool hex = false)
    {
        int end = 0;
        if (span[0] == '-')
            end++;
        while (end < span.Length && (char.IsDigit(span[end]) || (!hex || IsHexChar(span[end]))))
            end++;

        return span[0..end];
    }

    static bool IsHexChar(char c)
    {
        return (c >= 'a' && c <= 'f') ||
               (c >= 'A' && c <= 'F');
    }

    private static bool TryParseOctal(ReadOnlySpan<char> s, out int value)
    {
        value = 0;

        foreach (char c in s)
        {
            if ((uint)(c - '0') > 7)
                return false;

            value = (value << 3) + (c - '0');
        }

        return true;
    }

    private static void ConsumeProperty(SimpleParser parser, string property)
    {
        for (int i = 0; i < property.Count(x => x == ' ') + 1; i++)
            parser.ConsumeString();
    }
}
