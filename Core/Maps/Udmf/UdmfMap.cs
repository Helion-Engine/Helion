using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Udmf.Components;
using Helion.Resources.Archives;
using Helion.Resources.Definitions.Compatibility;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;

namespace Helion.Maps.Udmf;

public sealed class UdmfMap : IMap, IMapSpecials
{
    readonly ref struct Property(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
    {
        public readonly ReadOnlySpan<char> Name = name;
        public readonly ReadOnlySpan<char> Value = value;
    }

    public string ArchivePath { get; set; }
    public string MD5 { get; set; }
    public string Name { get; }
    public UdmfNamespace UdmfNamespace { get; private set; }

    public MapType MapType => MapType.UDMF;
    public List<UdmfLine> Lines;
    public List<UdmfSector> Sectors;
    public List<UdmfSide> Sides;
    public List<UdmfThing> Things;
    public List<UdmfVertex> Vertices;
    public GLComponents? GL { get; private set; }
    public byte[]? Reject { get; set; }
    public CompatibilityMapDefinition? CompatibilityDefinition { get; set; }

    public IReadOnlyList<ILine> GetLines() => Lines;
    public IReadOnlyList<ISector> GetSectors() => Sectors;
    public IReadOnlyList<ISide> GetSides() => Sides;
    public IReadOnlyList<IThing> GetThings() => Things;
    public IReadOnlyList<IVertex> GetVertices() => Vertices;

    private MapEntryCollection? m_map;
    private bool m_loaded;

    private readonly Dictionary<int, UdmfScrollSector> m_scrollSectors = [];

    private static readonly char[] ParseChars = [';', ')', '(', '{', '}'];
    private static readonly FrozenSet<char> ParseCharSet = ParseChars.ToFrozenSet();

    private static readonly DynamicArray<int> ParseIds = [];

    public bool UseAverageScrollCarry()
    {
        return UdmfNamespace != UdmfNamespace.Doom && UdmfNamespace != UdmfNamespace.Dsda;
    }

    public void ClearAllExceptThings()
    {
        Lines = [];
        Sectors = [];
        Sides = [];
        Vertices = [];
        GL = null;
    }

    public void ClearAll()
    {
        ClearAllExceptThings();
        Things = [];
    }

    public void LoadData()
    {
        if (m_loaded || m_map == null)
            return;

        var map = m_map;
        m_loaded = true;
        m_map = null;

        if (map.Textmap == null)
            return;

        GL = GLComponents.Read(map);
        Reject = map.Reject?.ReadData();

        using var textmapStream = map.Textmap.GetStream();
        UdmfNamespace = Parse(textmapStream, Vertices, Sectors, Sides, Lines, Things, m_scrollSectors);
    }

    public void Initialize(IWorld world)
    {
        var carryOptions = UseAverageScrollCarry() ? ScrollPlaneOptions.AverageCarryVelocity : 0;
        foreach (var item in m_scrollSectors.Values)
        {
            if (!world.IsSectorIdValid(item.SectorId))
                continue;

            var sector = world.Sectors[item.SectorId];
            var flags = item.Flags;

            var scrollSpeeds = ScrollUtil.GetScrollSpeeds(item.Speed, ZDoomPlaneScrollType.ScrollAndCarry);
            var plane = sector.GetSectorPlane(item.Face);

            if ((flags & UdmfScrollSectorFlags.Texture) != 0 && scrollSpeeds.ScrollSpeed.HasValue)
                world.SpecialManager.AddSpecial(new ScrollSpecial(ScrollPlaneOptions.Textures, plane, scrollSpeeds.ScrollSpeed.Value));

            flags &= ~UdmfScrollSectorFlags.Texture;
            if (flags != 0 && scrollSpeeds.CarrySpeed.HasValue)
                world.SpecialManager.AddSpecial(new ScrollSpecial((ScrollPlaneOptions)flags | carryOptions, plane, scrollSpeeds.CarrySpeed.Value));
        }
    }

    public static UdmfMap? Create(Archive archive, MapEntryCollection map, CompatibilityMapDefinition? compatibility, bool loadData)
    {
        if (map.Textmap == null)
            return null;

        if (loadData)
        {
            List<UdmfVertex> vertices = new(4096);
            List<UdmfSide> sides = new(4096);
            List<UdmfLine> lines = new(4096);
            List<UdmfThing> things = new(2048);
            List<UdmfSector> sectors = new(2048);
            var udmfMap = new UdmfMap(map, archive.FullPath, map.Name, UdmfNamespace.Unknown, vertices, sectors, sides, lines, things, null, null, compatibility);
            udmfMap.LoadData();
            return udmfMap;
        }

        return new UdmfMap(map, archive.FullPath, map.Name, UdmfNamespace.Unknown, [], [], [], [], [], null, null, compatibility);
    }

    public UdmfMap(MapEntryCollection map, string archiveFullPath, string name, UdmfNamespace ns, List<UdmfVertex> vertices, List<UdmfSector> sectors, List<UdmfSide> sides,
        List<UdmfLine> lines, List<UdmfThing> things, GLComponents? gl, byte[]? reject,
        CompatibilityMapDefinition? compatibility)
    {
        m_map = map;
        ArchivePath = archiveFullPath;
        Name = name;
        Vertices = vertices;
        Sectors = sectors;
        Sides = sides;
        Lines = lines;
        Things = things;
        GL = gl;
        Reject = reject;
        CompatibilityDefinition = compatibility;
        MD5 = string.Empty;
        UdmfNamespace = ns;
    }

    private static UdmfNamespace Parse(Stream textmap, List<UdmfVertex> vertices, List<UdmfSector> sectors, List<UdmfSide> sides,
        List<UdmfLine> lines, List<UdmfThing> things, Dictionary<int, UdmfScrollSector> scrollSectors)
    {
        DynamicArray<char> typeArray = new(256);
        DynamicArray<char> valueArray = new(256);
        var parser = new StreamParser(textmap, ParseCharSet);
        var stringLookup = new Dictionary<string, string>(256);
        var altLookup = stringLookup.GetAlternateLookup<ReadOnlySpan<char>>();

        parser.ConsumeString("namespace");
        parser.Consume('=');
        var ns = parser.ConsumeString();
        parser.Consume(';');

        var udmfNamespace = ParseNamespaceOrThrow(ns);

        while (!parser.IsDone())
        {
            var type = parser.ConsumeStringSpan(typeArray);
            if (type.EqualsIgnoreCase("vertex"))
            {
                parser.Consume('{');
                ParseVertex(parser, vertices, typeArray, valueArray);
                parser.Consume('}');
            }
            else if (type.EqualsIgnoreCase("linedef"))
            {
                parser.Consume('{');
                ParseLine(parser, lines, typeArray, valueArray);
                parser.Consume('}');
            }
            else if (type.EqualsIgnoreCase("sidedef"))
            {
                parser.Consume('{');
                ParseSide(parser, sides, typeArray, valueArray, altLookup);
                parser.Consume('}');
            }
            else if (type.EqualsIgnoreCase("sector"))
            {
                parser.Consume('{');
                ParseSector(parser, sectors, typeArray, valueArray, altLookup, scrollSectors);
                parser.Consume('}');
            }
            else if (type.EqualsIgnoreCase("thing"))
            {
                parser.Consume('{');
                ParseThing(parser, things, typeArray, valueArray);
                parser.Consume('}');
            }
            else
                ConsumeUnknownBlockOrProperty(parser);
        }

        foreach (var side in sides)
        {
            if (side.SectorId >= 0 && side.SectorId < sectors.Count)
                side.Sector = sectors[side.SectorId];
        }

        MapLines(lines, vertices, sides);

        return udmfNamespace;
    }

    public static UdmfNamespace ParseNamespace(ReadOnlySpan<char> ns)
    {
        if (ns.EqualsIgnoreCase("doom"))
            return UdmfNamespace.Doom;
        else if (ns.EqualsIgnoreCase("dsda"))
            return UdmfNamespace.Dsda;
        else if (ns.EqualsIgnoreCase("zdoom"))
            return UdmfNamespace.ZDoom;
        else if (ns.EqualsIgnoreCase("helion"))
            return UdmfNamespace.Helion;
        return UdmfNamespace.Unknown;
    }

    private static UdmfNamespace ParseNamespaceOrThrow(ReadOnlySpan<char> ns)
    {
        var udmfNamespace = ParseNamespace(ns);
        if (udmfNamespace != UdmfNamespace.Unknown)
            return udmfNamespace;

        throw new Exception($"Unsupported udmf namespace: {ns}");
    }

    private static void ConsumeUnknownBlockOrProperty(StreamParser parser)
    {
        var next = parser.ConsumeString();
        if (next.EqualsIgnoreCase("="))
            ConsumeUnknownProperty(parser);
        else if (next.EqualsIgnoreCase("{"))
            ConsumeUnknownBlock(parser);
        else
            throw new Exception("Malformed UDMF TEXTMAP. Expected '=' or '{' but found: " + parser.ConsumeString());
    }

    private static void ConsumeUnknownProperty(StreamParser parser)
    {
        parser.Consume('=');
        parser.ConsumeString();
        parser.Consume(';');
    }

    private static void ConsumeUnknownBlock(StreamParser parser)
    {
        parser.Consume('{');
        while (!parser.Peek('}'))
            parser.ConsumeString();

        parser.Consume('}');
    }

    private static void ParseThing(StreamParser parser, List<UdmfThing> things, DynamicArray<char> typeArray, DynamicArray<char> valueArray)
    {
        var thing = new UdmfThing();
        double x = 0, y = 0, z = double.MinValue;
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser, typeArray, valueArray);
            if (prop.Name.EqualsIgnoreCase("x"))
                x = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("y"))
                y = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("height"))
                z = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("angle"))
                thing.Angle = (ushort)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("type"))
                thing.EditorNumber = (ushort)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("special"))
                thing.Special = (ZDoomLineSpecialType)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg0"))
                thing.Args.Arg0 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg1"))
                thing.Args.Arg1 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg2"))
                thing.Args.Arg2 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg3"))
                thing.Args.Arg3 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg4"))
                thing.Args.Arg4 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("skill1"))
                thing.Flags.Skill1 = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill2"))
                thing.Flags.Skill2 = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill3"))
                thing.Flags.Skill3 = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill4"))
                thing.Flags.Skill4 = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill5"))
                thing.Flags.Skill5 = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("friend"))
                thing.Flags.Friendly = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("single"))
                thing.Flags.SinglePlayer = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("coop"))
                thing.Flags.Cooperative = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("dm"))
                thing.Flags.Deathmatch = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("ambush"))
                thing.Flags.Ambush = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("invisible"))
                thing.Flags.Invisible = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("dormant"))
                thing.Flags.Dormant = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("nocount"))
                thing.Flags.CountKill = thing.Flags.CountItem = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("countsecret"))
                thing.Flags.CountSecret = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("id"))
                thing.ThingId = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("alpha"))
                thing.Alpha = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("translucent"))
                thing.Alpha = 0.25f;
            else if (prop.Name.EqualsIgnoreCase("gravity"))
                thing.Gravity = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("health"))
                thing.Health = (int)parser.ParseFloat(prop.Value);
        }

        thing.Position = new(x, y, z);
        things.Add(thing);
    }

    private static void MapLines(List<UdmfLine> lines, List<UdmfVertex> vertices, List<UdmfSide> sides)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            line.Id = i;
            line.StartPosition = vertices[line.StartVertex].Position;
            line.EndPosition = vertices[line.EndVertex].Position;
            line.Front = sides[line.SideFront];
            line.Back = line.SideBack.HasValue && line.SideBack.Value > 0 && line.SideBack.Value < sides.Count ? sides[line.SideBack.Value] : null;
        }
    }

    private static string GetString(Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup, ReadOnlySpan<char> str)
    {
        if (lookup.TryGetValue(str, out var value))
            return value;

        var newString = str.ToString();
        lookup[newString] = newString;
        return newString;
    }

    private static void ParseSide(StreamParser parser, List<UdmfSide> sides, DynamicArray<char> typeArray, DynamicArray<char> valueArray, 
        Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> stringLookup)
    {
        UdmfSide side = new();
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser, typeArray, valueArray);
            if (prop.Name.EqualsIgnoreCase("sector"))
                side.SectorId = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturetop"))
                side.UpperTexture = GetString(stringLookup, prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturemiddle"))
                side.MiddleTexture = GetString(stringLookup, prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturebottom"))
                side.LowerTexture = GetString(stringLookup, prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsetx"))
                side.Offset.X = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsety"))
                side.Offset.Y = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsetx_top"))
                side.UpperOffset.X = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsety_top"))
                side.UpperOffset.Y = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsetx_mid"))
                side.MiddleOffset.X = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsety_mid"))
                side.MiddleOffset.Y = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsetx_bottom"))
                side.BottomOffset.X = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsety_bottom"))
                side.BottomOffset.Y = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scalex_top"))
                side.UpperScale.X = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scaley_top"))
                side.UpperScale.Y = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scalex_mid"))
                side.MiddleScale.X = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scaley_mid"))
                side.MiddleScale.Y = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scalex_bottom"))
                side.BottomScale.X = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scaley_bottom"))
                side.BottomScale.Y = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("light"))
                side.LightLevel = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("light_top"))
                side.LightLevelUpper = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("light_mid"))
                side.LightLevelMiddle = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("light_bottom"))
                side.LightLevelLower = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightabsolute"))
                side.LightLevelAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("lightabsolute_top"))
                side.LightLevelUpperAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("lightabsolute_mid"))
                side.LightLevelMiddleAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("lightabsolute_bottom"))
                side.LightLevelLowerAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("nofakecontrast"))
                side.NoFakeConstrast = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("smoothlighting"))
                side.SmoothLighting = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("wrapmidtex"))
                side.WrapMidTex = prop.Value.EqualsIgnoreCase("true");
        }

        sides.Add(side);
    }

    private static void ParseSector(StreamParser parser, List<UdmfSector> sectors, DynamicArray<char> typeArray, DynamicArray<char> valueArray,
        Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> stringLookup, Dictionary<int, UdmfScrollSector> scrollSectors)
    {
        UdmfSector sector = new() { Id = sectors.Count };
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser, typeArray, valueArray);
            if (prop.Name.EqualsIgnoreCase("heightfloor"))
                sector.FloorZ = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("heightceiling"))
                sector.CeilingZ = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturefloor"))
                sector.FloorTexture = GetString(stringLookup, prop.Value);
            else if (prop.Name.EqualsIgnoreCase("textureceiling"))
                sector.CeilingTexture = GetString(stringLookup, prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightlevel"))
                sector.LightLevel = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("id"))
                sector.Tag = (ushort)parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("special"))
                sector.Special = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("rotationfloor"))
                sector.RotationFloor = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("rotationceiling"))
                sector.RotationCeiling = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xpanningfloor"))
                sector.PanningFloorX = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("ypanningfloor"))
                sector.PanningFloorY = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xpanningceiling"))
                sector.PanningCeilingX = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("ypanningceiling"))
                sector.PanningCeilingY = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xscalefloor"))
                sector.ScaleFloorX = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("yscalefloor"))
                sector.ScaleFloorY = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xscaleceiling"))
                sector.ScaleCeilingX = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("yscaleceiling"))
                sector.ScaleCeilingY = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightfloorabsolute"))
                sector.LightFloorAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("lightceilingabsolute"))
                sector.LightCeilingAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("silent"))
                sector.Silent = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("noattack"))
                sector.NoAttack = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("gravity"))
                sector.Gravity = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightfloor"))
                sector.LightFloor = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightceiling"))
                sector.LightCeiling = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("damageamount"))
                sector.DamageAmount = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("damageinterval"))
                sector.DamageInterval = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("leakiness"))
                sector.Leakiness = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("skyfloor"))
                sector.SkyFloor = GetString(stringLookup, prop.Value);
            else if (prop.Name.EqualsIgnoreCase("skyceiling"))
                sector.SkyCeiling = GetString(stringLookup, prop.Value);
            else if (prop.Name.EqualsIgnoreCase("moreids"))
                sector.MoreTags = ParseMoreIds(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightcolor"))
                sector.LightColor = (uint)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("fadeColor"))
                sector.FadeColor = (uint)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("fogDensity"))
                sector.FogDensity = parser.ParseInt(prop.Value);

            else if (prop.Name.EqualsIgnoreCase("scrollfloormode"))
                GetScrollSector(sector.Id, SectorPlaneFace.Floor, scrollSectors).Flags = (UdmfScrollSectorFlags)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xscrollfloor"))
                GetScrollSector(sector.Id, SectorPlaneFace.Floor, scrollSectors).Speed.X = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("yscrollfloor"))
                GetScrollSector(sector.Id, SectorPlaneFace.Floor, scrollSectors).Speed.Y = parser.ParseDouble(prop.Value);

            else if (prop.Name.EqualsIgnoreCase("scrollceilingmode"))
                GetScrollSector(sector.Id, SectorPlaneFace.Ceiling, scrollSectors).Flags = (UdmfScrollSectorFlags)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xscrollceiling"))
                GetScrollSector(sector.Id, SectorPlaneFace.Ceiling, scrollSectors).Speed.X = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("yscrollceiling"))
                GetScrollSector(sector.Id, SectorPlaneFace.Ceiling, scrollSectors).Speed.Y = parser.ParseDouble(prop.Value);
        }

        sectors.Add(sector);
    }

    private static int[] ParseMoreIds(ReadOnlySpan<char> value)
    {
        ParseIds.Clear();
        foreach (var token in value.Split(' '))
        {
            if (int.TryParse(value[token.Start.Value..token.End.Value], out int number))
                ParseIds.Add(number);
        }
        return [.. ParseIds];
    }

    private static UdmfScrollSector GetScrollSector(int id, SectorPlaneFace face, Dictionary<int, UdmfScrollSector> scrollSectors)
    {
        int key = id;
        if (face == SectorPlaneFace.Ceiling)
            key |= 1 << 31;
        if (scrollSectors.TryGetValue(key, out var item))
            return item;

        item = new(id, face);
        scrollSectors[key] = item;
        return item;
    }

    private static void ParseLine(StreamParser parser, List<UdmfLine> lines, DynamicArray<char> typeArray, DynamicArray<char> valueArray)
    {
        UdmfLine line = new();
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser, typeArray, valueArray);
            if (prop.Name.EqualsIgnoreCase("v1"))
                line.StartVertex = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("v2"))
                line.EndVertex = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("sidefront"))
                line.SideFront = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("sideback"))
                line.SideBack = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("special"))
                line.LineType = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg0"))
                line.Args.Arg0 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg1"))
                line.Args.Arg1 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg2"))
                line.Args.Arg2 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg3"))
                line.Args.Arg3 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg4"))
                line.Args.Arg4 = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("blocking"))
                line.Flags.BlockPlayers = line.Flags.BlockMonsters = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blockeverything"))
                line.Flags.BlockEverything = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blockmonsters"))
                line.Flags.BlockMonsters = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blockplayers"))
                line.Flags.BlockPlayers = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blockfloaters"))
                line.Flags.BlockFloatMonsters = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blocklandmonsters"))
                line.Flags.BlockLandMonsters = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blockhitscan"))
                line.Flags.BlockHitscan = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blockprojectiles"))
                line.Flags.BlockProjectiles = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blocksound"))
                line.Flags.BlockSound = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blockuse"))
                line.Flags.BlockUse = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("blocksight"))
                line.Flags.BlockSight = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("twosided"))
                line.Flags.TwoSided = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("dontpegtop"))
                line.Flags.UpperUnpegged = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("dontpegbottom"))
                line.Flags.LowerUnpegged = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("translucent"))
                line.Alpha = 0.75f;
            else if (prop.Name.EqualsIgnoreCase("transparent"))
                line.Alpha = 0.25f;
            else if (prop.Name.EqualsIgnoreCase("alpha"))
                line.Alpha = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("locknumber"))
                line.LockNumber = (ZDoomKeyType)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("secret"))
                line.Flags.DrawAsOneSidedAutomap = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("mapped"))
                line.Flags.AlwaysDrawAutomap = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("repeatspecial") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.RepeatSpecial = true;
            else if (prop.Name.EqualsIgnoreCase("playercross") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Player | LineActivations.CrossLine;
            else if (prop.Name.EqualsIgnoreCase("playeruse") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Player | LineActivations.UseLine;
            else if (prop.Name.EqualsIgnoreCase("monstercross") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Monster | LineActivations.CrossLine;
            else if (prop.Name.EqualsIgnoreCase("monsteruse") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Monster | LineActivations.UseLine;
            else if (prop.Name.EqualsIgnoreCase("monsteractivate") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Monster;
            else if (prop.Name.EqualsIgnoreCase("impact") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.ImpactLine | LineActivations.Hitscan | LineActivations.Projectile;
            else if (prop.Name.EqualsIgnoreCase("playerpush") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Player | LineActivations.ImpactLine;
            else if (prop.Name.EqualsIgnoreCase("monsterpush") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Monster | LineActivations.ImpactLine;
            else if (prop.Name.EqualsIgnoreCase("missilecross") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Projectile | LineActivations.CrossLine;
            else if (prop.Name.EqualsIgnoreCase("passuse") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.PassThrough = true;
            else if (prop.Name.EqualsIgnoreCase("anycross") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.CrossLine | LineActivations.Player | LineActivations.Monster | LineActivations.Hitscan;
            else if (prop.Name.EqualsIgnoreCase("playeruseback") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.UseLineBack;
            else if (prop.Name.EqualsIgnoreCase("checkswitchrange") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.CheckSwitchRange;
            else if (prop.Name.EqualsIgnoreCase("firstsideonly") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.FrontSideOnly;
            else if (prop.Name.EqualsIgnoreCase("revealed") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.AlwaysDrawAutomap = true;
            else if (prop.Name.EqualsIgnoreCase("damagespecial") && prop.Value.EqualsIgnoreCase("true"))
                line.DamageSpecial = true;
            else if (prop.Name.EqualsIgnoreCase("deathspecial") && prop.Value.EqualsIgnoreCase("true"))
                line.DeathSpecial = true;
            else if (prop.Name.EqualsIgnoreCase("wrapmidtex") && prop.Value.EqualsIgnoreCase("true"))
                line.WrapMidTex = true;
            else if (prop.Name.EqualsIgnoreCase("midtex3d") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.MidTex3D = true;
            else if (prop.Name.EqualsIgnoreCase("midtex3dimpassible") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.MidTex3DImpassible = true;
            else if (prop.Name.EqualsIgnoreCase("health"))
                line.Health = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("healthgroup"))
                line.HealthGroup = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("id"))
                line.LineId = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("moreids"))
                line.MoreLineIds = ParseMoreIds(prop.Value);
        }

        lines.Add(line);
    }

    private static void ParseVertex(StreamParser parser, List<UdmfVertex> vertices, DynamicArray<char> typeArray, DynamicArray<char> valueArray)
    {
        double x = 0, y = 0;

        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser, typeArray, valueArray);
            if (prop.Name.EqualsIgnoreCase("x"))
                x = parser.ParseDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("y"))
                y = parser.ParseDouble(prop.Value);
        }

        vertices.Add(new(vertices.Count, new(x, y)));
    }

    private static Property ParseProperty(StreamParser parser, DynamicArray<char> typeArray, DynamicArray<char> valueArray)
    {
        var type = parser.ConsumeStringSpan(typeArray);
        parser.Consume('=');
        var value = parser.ConsumeStringSpan(valueArray);
        parser.Consume(';');
        return new(type, value);
    }

    private static bool IsBlockComplete(StreamParser parser) => parser.Peek('}');
}
