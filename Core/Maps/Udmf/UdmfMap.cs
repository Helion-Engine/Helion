using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Udmf.Components;
using Helion.Resources.Archives;
using Helion.Resources.Definitions.Compatibility;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using System;
using System.Collections.Generic;

namespace Helion.Maps.Udmf;

public class UdmfMap : IMap
{
    readonly ref struct Property(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
    {
        public readonly ReadOnlySpan<char> Name = name;
        public readonly ReadOnlySpan<char> Value = value;
    }

    public Archive Archive { get; }
    public string MD5 { get; set; }
    public string Name { get; }

    public MapType MapType => MapType.UDMF;
    public readonly List<UdmfLine> Lines;
    public readonly List<UdmfSector> Sectors;
    public readonly List<UdmfSide> Sides;
    public readonly List<UdmfThing> Things;
    public readonly List<UdmfVertex> Vertices;
    public GLComponents? GL { get; }
    public byte[]? Reject { get; set; }
    public CompatibilityMapDefinition? CompatibilityDefinition { get; set; }

    public IReadOnlyList<ILine> GetLines() => Lines;
    public IReadOnlyList<INode> GetNodes() => [];
    public IReadOnlyList<ISector> GetSectors() => Sectors;
    public IReadOnlyList<ISide> GetSides() => Sides;
    public IReadOnlyList<IThing> GetThings() => Things;
    public IReadOnlyList<IVertex> GetVertices() => Vertices;

    public static UdmfMap? Create(Archive archive, MapEntryCollection map, CompatibilityMapDefinition? compatibility)
    {
        if (map.Textmap == null)
            return null;

        List<UdmfVertex> vertices = new(4096);
        List<UdmfSide> sides = new(2048);
        List<UdmfLine> lines = new(2048);
        List<UdmfThing> things = new(1024);
        List<UdmfSector> sectors = new(1024);

        Parse(map.Textmap.ReadDataAsString(), vertices, sectors, sides, lines, things);

        GLComponents? gl = GLComponents.Read(map);
        return new(archive, map.Name, vertices, sectors, sides, lines, things, gl, map.Reject?.ReadData(), compatibility);
    }

    public UdmfMap(Archive archive, string name, List<UdmfVertex> vertices, List<UdmfSector> sectors, List<UdmfSide> sides,
        List<UdmfLine> lines, List<UdmfThing> things, GLComponents? gl, byte[]? reject,
        CompatibilityMapDefinition? compatibility)
    {
        Archive = archive;
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
    }

    private static bool CheckSpecial(char c) => c == '=' || c == '{' || c == '}' || c == ';';

    private static void Parse(string textmap, List<UdmfVertex> vertices, List<UdmfSector> sectors, List<UdmfSide> sides,
        List<UdmfLine> lines, List<UdmfThing> things)
    {
        var parser = new SimpleParser();
        parser.SetSpecialCallback(CheckSpecial);
        parser.Parse(textmap);

        parser.ConsumeString("namespace");
        parser.Consume('=');
        var ns = parser.ConsumeStringSpan();
        parser.Consume(';');

        if (!ns.EqualsIgnoreCase("zdoom") && !ns.EqualsIgnoreCase("dsda"))
            throw new Exception($"Unsupported udmf namespace: {ns}");

        while (!parser.IsDone())
        {
            var type = parser.ConsumeStringSpan();
            parser.Consume('{');

            if (type.EqualsIgnoreCase("vertex"))
                ParseVertex(parser, vertices);
            else if (type.EqualsIgnoreCase("linedef"))
                ParseLine(parser, lines);
            else if (type.EqualsIgnoreCase("sidedef"))
                ParseSide(parser, sides);
            else if (type.EqualsIgnoreCase("sector"))
                ParseSector(parser, sectors);
            else if (type.EqualsIgnoreCase("thing"))
                ParseThing(parser, things);
            else
                ConsumeBlock(parser);

            parser.Consume('}');
        }

        foreach (var side in sides)
        {
            if (side.SectorId >= 0 && side.SectorId < sectors.Count)
                side.Sector = sectors[side.SectorId];
        }

        MapLines(lines, vertices, sides);
    }

    private static void ParseThing(SimpleParser parser, List<UdmfThing> things)
    {
        var thing = new UdmfThing();
        double x = 0, y = 0, z = double.MinValue;
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser);
            if (prop.Name.EqualsIgnoreCase("x"))
                x = double.Parse(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("y"))
                y = double.Parse(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("height"))
                z = double.Parse(prop.Value);
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
            // TODO skills not split in thing flags
            else if (prop.Name.EqualsIgnoreCase("skill1"))
                thing.Flags.Easy = thing.Flags.Easy || prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill2"))
                thing.Flags.Easy = thing.Flags.Easy || prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill3"))
                thing.Flags.Medium = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill4"))
                thing.Flags.Hard = thing.Flags.Hard || prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("skill5"))
                thing.Flags.Hard = thing.Flags.Hard || prop.Value.EqualsIgnoreCase("true");
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
            else if (prop.Name.EqualsIgnoreCase("id"))
                thing.ThingId = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("alpha"))
                thing.Alpha = parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("translucent"))
                thing.Alpha = 0.25f;
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
            line.Back = line.SideBack.HasValue ? sides[line.SideBack.Value] : null;
        }
    }

    private static void ParseSide(SimpleParser parser, List<UdmfSide> sides)
    {
        UdmfSide side = new();
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser);
            if (prop.Name.EqualsIgnoreCase("sector"))
                side.SectorId = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturetop"))
                side.UpperTexture = prop.Value.ToString();
            else if (prop.Name.EqualsIgnoreCase("texturemiddle"))
                side.MiddleTexture = prop.Value.ToString();
            else if (prop.Name.EqualsIgnoreCase("texturebottom"))
                side.LowerTexture = prop.Value.ToString();
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
        }

        sides.Add(side);
    }

    private static void ParseSector(SimpleParser parser, List<UdmfSector> sectors)
    {
        UdmfSector sector = new() { Id = sectors.Count };
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser);
            if (prop.Name.EqualsIgnoreCase("heightfloor"))
                sector.FloorZ = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("heightceiling"))
                sector.CeilingZ = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturefloor"))
                sector.FloorTexture = prop.Value.ToString();
            else if (prop.Name.EqualsIgnoreCase("textureceiling"))
                sector.CeilingTexture = prop.Value.ToString();
            else if (prop.Name.EqualsIgnoreCase("lightlevel"))
                sector.LightLevel = (short)parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("id"))
                sector.Tag = (ushort)parser.ParseFloat(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("special"))
                sector.Special = (ZDoomSectorSpecialType)parser.ParseFloat(prop.Value);
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
        }

        sectors.Add(sector);
    }

    private static void ParseLine(SimpleParser parser, List<UdmfLine> lines)
    {
        UdmfLine line = new();
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser);
            if (prop.Name.EqualsIgnoreCase("v1"))
                line.StartVertex = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("v2"))
                line.EndVertex = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("sidefront"))
                line.SideFront = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("sideback"))
                line.SideBack = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("special"))
                line.Special = (ZDoomLineSpecialType)parser.ParseInt(prop.Value);
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
                line.Flags.Activations |= LineActivations.ImpactLine;
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
            else if (prop.Name.EqualsIgnoreCase("health"))
                line.Health = parser.ParseInt(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("healthgroup"))
                line.HealthGroup = parser.ParseInt(prop.Value);
        }

        lines.Add(line);
    }

    private static void ParseVertex(SimpleParser parser, List<UdmfVertex> vertices)
    {
        double x = 0, y = 0;

        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser);
            if (prop.Name.EqualsIgnoreCase("x"))
                x = double.Parse(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("y"))
                y = double.Parse(prop.Value);
        }

        vertices.Add(new(vertices.Count, new(x, y)));
    }

    private static void ConsumeBlock(SimpleParser parser)
    {
        while (parser.PeekStringSpan() != "}")
            parser.ConsumeLineSpan();
    }

    private static Property ParseProperty(SimpleParser parser)
    {
        var type = parser.ConsumeStringSpan();
        parser.Consume('=');
        var value = parser.ConsumeStringSpan();
        parser.Consume(';');
        return new(type, value);
    }

    private static bool IsBlockComplete(SimpleParser parser) => parser.Peek('}');
}
