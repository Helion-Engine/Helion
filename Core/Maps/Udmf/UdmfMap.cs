using Helion.Geometry;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Udmf.Components;
using Helion.Resources.Archives;
using Helion.Resources.Definitions.Compatibility;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.World.Geometry.Lines;
using Helion.World.Special;
using System;
using System.Collections.Generic;

namespace Helion.Maps.Udmf;

public class UdmfMap : IMap
{
    record struct Property(string Name, string Value);

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

    private static void Parse(string textmap, List<UdmfVertex> vertices, List<UdmfSector> sectors, List<UdmfSide> sides,
        List<UdmfLine> lines, List<UdmfThing> things)
    {
        var parser = new SimpleParser();
        parser.Parse(textmap);

        parser.ConsumeString("namespace");
        parser.Consume('=');
        var ns = parser.ConsumeString();
        parser.Consume(';');

        if (!ns.EqualsIgnoreCase("zdoom") && !ns.Equals("dsda"))
            throw new Exception($"Unsupported udmf namespace: {ns}");

        List<Sidedef> sidedefs = new(sides.Capacity);
        List<Linedef> linedefs = new(lines.Capacity);

        while (!parser.IsDone())
        {
            var type = parser.ConsumeString();
            parser.Consume('{');

            if (type.EqualsIgnoreCase("vertex"))
                ParseVertex(parser, vertices);
            else if (type.EqualsIgnoreCase("linedef"))
                ParseLine(parser, linedefs);
            else if (type.EqualsIgnoreCase("sidedef"))
                ParseSide(parser, sidedefs);
            else if (type.EqualsIgnoreCase("sector"))
                ParseSector(parser, sectors);
            else if (type.EqualsIgnoreCase("thing"))
                ParseThing(parser, things);
            else
                ConsumeBlock(parser);

            parser.Consume('}');
        }

        sides.EnsureCapacity(sidedefs.Count);
        foreach (var side in sidedefs)
            sides.Add(CreateSide(sides.Count, side, sectors));

        MapLines(lines, vertices, sides, linedefs);
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
                thing.Angle = Convert.ToUInt16(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("type"))
                thing.EditorNumber = Convert.ToUInt16(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg0"))
                thing.Args.Arg0 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg1"))
                thing.Args.Arg1 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg2"))
                thing.Args.Arg2 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg3"))
                thing.Args.Arg3 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg4"))
                thing.Args.Arg4 = Convert.ToInt32(prop.Value);
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
            else if (prop.Name.EqualsIgnoreCase("single"))
                thing.Flags.SinglePlayer = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("coop"))
                thing.Flags.Cooperative = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("dm"))
                thing.Flags.Deathmatch = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("ambush"))
                thing.Flags.Ambush = prop.Value.EqualsIgnoreCase("true");
        }

        thing.Position = new(x, y, z);
        things.Add(thing);
    }

    private static void MapLines(List<UdmfLine> lines, List<UdmfVertex> vertices, List<UdmfSide> sides, List<Linedef> linedefs)
    {
        lines.EnsureCapacity(linedefs.Count);
        foreach (var line in linedefs)
        {
            UdmfLine udmfLine = new()
            {
                Id = lines.Count,
                StartPosition = vertices[line.StartVertex].Position,
                EndPosition = vertices[line.EndVertex].Position,
                Front = sides[line.SideFront],
                Back = line.SideBack.HasValue ? sides[line.SideBack.Value] : null,
                Flags = line.Flags,
                Special = line.Special,
                ActivationType = line.ActivationType,
                Args = line.Args,
                Alpha = line.Alpha
            };

            lines.Add(udmfLine);
        }
    }

    private static UdmfSide CreateSide(int id, Sidedef sidedef, List<UdmfSector> sectors)
    {
        return new()
        {
            Id = id,
            Sector = sectors[sidedef.Sector],
            UpperTexture = sidedef.TextureTop,
            MiddleTexture = sidedef.TextureMiddle,
            LowerTexture = sidedef.TextureBottom,
            UpperOffset = new(sidedef.TopOffsetX, sidedef.TopOffsetY),
            MiddleOffset = new(sidedef.MiddleOffsetX, sidedef.MiddleOffsetY),
            BottomOffset = new(sidedef.BottomOffsetX, sidedef.BottomOffsetY),
            UpperScale = new(sidedef.TopScaleX, sidedef.TopScaleY),
            MiddleScale = new(sidedef.MiddleScaleX, sidedef.MiddleScaleY),
            BottomScale = new(sidedef.BottomScaleX, sidedef.BottomScaleY),
        };
    }

    private static void ParseSide(SimpleParser parser, List<Sidedef> sides)
    {
        Sidedef side = new();
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser);
            if (prop.Name.EqualsIgnoreCase("sector"))
                side.Sector = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturetop"))
                side.TextureTop = prop.Value;
            else if (prop.Name.EqualsIgnoreCase("texturemiddle"))
                side.TextureMiddle = prop.Value;
            else if (prop.Name.EqualsIgnoreCase("texturebottom"))
                side.TextureBottom = prop.Value;
            else if (prop.Name.EqualsIgnoreCase("offsetx_top"))
                side.TopOffsetX = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsety_top"))
                side.TopOffsetY = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsetx_mid"))
                side.MiddleOffsetX = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsety_mid"))
                side.MiddleOffsetY = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsetx_bottom"))
                side.BottomOffsetX = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("offsety_bottom"))
                side.BottomOffsetY = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scalex_top"))
                side.TopScaleX = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scaley_top"))
                side.TopScaleY = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scalex_mid"))
                side.MiddleScaleX = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scaley_mid"))
                side.MiddleScaleY = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scalex_bottom"))
                side.BottomScaleX = Convert.ToSingle(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("scaley_bottom"))
                side.BottomScaleY = Convert.ToSingle(prop.Value);
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
                sector.FloorZ = Convert.ToInt16(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("heightceiling"))
                sector.CeilingZ = Convert.ToInt16(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("texturefloor"))
                sector.FloorTexture = prop.Value;
            else if (prop.Name.EqualsIgnoreCase("textureceiling"))
                sector.CeilingTexture = prop.Value;
            else if (prop.Name.EqualsIgnoreCase("lightlevel"))
                sector.LightLevel = Convert.ToInt16(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("id"))
                sector.Tag = Convert.ToUInt16(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("special"))
                sector.Special = (ZDoomSectorSpecialType)Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("rotationfloor"))
                sector.RotationFloor = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("rotationceiling"))
                sector.RotationCeiling = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xpanningfloor"))
                sector.PanningFloorX = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("ypanningfloor"))
                sector.PanningFloorY = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xpanningceiling"))
                sector.PanningCeilingX = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xscalefloor"))
                sector.ScaleFloorX = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("yscalefloor"))
                sector.ScaleFloorY = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("xscaleceiling"))
                sector.ScaleCeilingX = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("yscaleceiling"))
                sector.ScaleCeilingY = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightfloorabsolute"))
                sector.LightFloorAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("lightceilingabsolute"))
                sector.LightFloorAbsolute = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("silent"))
                sector.Silent = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("gravity"))
                sector.Gravity = Convert.ToDouble(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightfloor"))
                sector.LightFloor = Convert.ToInt16(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("lightceiling"))
                sector.LightCeiling = Convert.ToInt16(prop.Value);
        }

        sectors.Add(sector);
    }

    private static void ParseLine(SimpleParser parser, List<Linedef> lines)
    {
        Linedef line = new();
        while (!IsBlockComplete(parser))
        {
            var prop = ParseProperty(parser);
            if (prop.Name.EqualsIgnoreCase("v1"))
                line.StartVertex = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("v2"))
                line.EndVertex = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("sidefront"))
                line.SideFront = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("sideback"))
                line.SideBack = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("special"))
                line.Special = (ZDoomLineSpecialType)Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg0"))
                line.Args.Arg0 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg1"))
                line.Args.Arg1 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg2"))
                line.Args.Arg2 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg3"))
                line.Args.Arg3 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("arg4"))
                line.Args.Arg4 = Convert.ToInt32(prop.Value);
            else if (prop.Name.EqualsIgnoreCase("blocking"))
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
            else if (prop.Name.EqualsIgnoreCase("blocksound"))
                line.Flags.BlockSound = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.EqualsIgnoreCase("twosided"))
                line.Flags.TwoSided = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.Equals("dontpegtop"))
                line.Flags.UpperUnpegged = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.Equals("dontpegbottom"))
                line.Flags.LowerUnpegged = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.Equals("translucent"))
                line.Alpha = 0.75f;
            else if (prop.Name.Equals("transparent"))
                line.Alpha = 0.25f;
            else if (prop.Name.Equals("secret"))
                line.Flags.DrawAsOneSidedAutomap = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.Equals("mapped"))
                line.Flags.AlwaysDrawAutomap = prop.Value.EqualsIgnoreCase("true");
            else if (prop.Name.Equals("repeatspecial") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.RepeatSpecial = true;
            else if (prop.Name.Equals("playercross") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Player | LineActivations.CrossLine;
            else if (prop.Name.Equals("playeruse") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Player | LineActivations.UseLine;
            else if (prop.Name.Equals("monstercross") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Monster | LineActivations.CrossLine;
            else if (prop.Name.Equals("monsteruse") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Monster | LineActivations.UseLine;
            else if (prop.Name.Equals("impact") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.ImpactLine;
            else if (prop.Name.Equals("playerpush") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Player | LineActivations.ImpactLine;
            else if (prop.Name.Equals("monsterpush") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Monster | LineActivations.ImpactLine;
            else if (prop.Name.Equals("missilecross") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.Activations |= LineActivations.Projectile | LineActivations.CrossLine;
            else if (prop.Name.Equals("passuse") && prop.Value.EqualsIgnoreCase("true"))
                line.Flags.PassThrough = true;
            //else if (prop.Name.Equals("anycross") && prop.Value.EqualsIgnoreCase("true"))
            //    line.Flags.Activations |= LineActivations.Projectile | LineActivations.CrossLine;
            //else if (prop.Name.Equals("checkswitchrange") && prop.Value.EqualsIgnoreCase("true"))
            //    line.Flags.Activations |= LineActivations.Projectile | LineActivations.CrossLine;
            //else if (prop.Name.Equals("firstsideonly") && prop.Value.EqualsIgnoreCase("true"))
            //    line.Flags.Activations |= LineActivations.Projectile | LineActivations.CrossLine;
            //else if (prop.Name.Equals("playeruseback") && prop.Value.EqualsIgnoreCase("true"))
            //    line.Flags.Activations |= LineActivations.Projectile | LineActivations.CrossLine;
            //else if (prop.Name.Equals("jumpover") && prop.Value.EqualsIgnoreCase("true"))
            //    line.Flags.Activations |= LineActivations.Projectile | LineActivations.CrossLine;
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
        while (parser.PeekString() != "}")
            parser.ConsumeLine();
    }

    private static Property ParseProperty(SimpleParser parser)
    {
        var type = parser.ConsumeString();
        parser.Consume('=');
        var value = parser.ConsumeString();
        parser.Consume(';');
        return new(type, value);
    }

    private static bool IsBlockComplete(SimpleParser parser) => parser.Peek('}');
}
