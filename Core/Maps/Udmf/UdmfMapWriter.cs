using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Doom.Components;
using Helion.Maps.Hexen.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Udmf.Components;
using Helion.Util;
using Helion.World.Geometry.Lines;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Helion.Maps.Udmf;

public static class UdmfMapWriter
{
    private static readonly MapLineFlags DefaultMapLineFlags = MapLineFlags.Doom(0);

    public static void WriteMap(IMap map, TextWriter writer, UdmfNamespace ns)
    {
        writer.WriteLine($"namespace = \"{ns.ToString().ToLower(CultureInfo.InvariantCulture)}\";");
        writer.WriteLine();

        var vertices = map.GetVertices();
        Dictionary<Vec2D, int> vertexLookup = new(vertices.Count);
        foreach (var thing in map.GetThings())
            WriteThing(thing, writer);

        int vertexIndex = 0;
        foreach (var vertex in vertices)
        {
            WriteVertex(vertex, writer);
            vertexLookup[vertex.Position] = vertexIndex++;
        }

        foreach (var line in map.GetLines())
            WriteLine(line, vertexLookup, writer, ns);

        foreach (var side in map.GetSides())
            WriteSide(side, writer);

        foreach (var sector in map.GetSectors())
            WriteSector(sector, writer, ns);
    }

    private static void WriteSector(ISector sector, TextWriter writer, UdmfNamespace ns)
    {
        writer.WriteLine("sector");
        writer.WriteLine("{");
        writer.WriteLine($"heightfloor = {sector.FloorZ};");
        writer.WriteLine($"heightceiling = {sector.CeilingZ};");
        writer.WriteLine($"texturefloor = \"{sector.FloorTexture}\";");
        writer.WriteLine($"textureceiling = \"{sector.CeilingTexture}\";");
        writer.WriteLine($"lightlevel = {sector.LightLevel};");

        if (sector is DoomSector doomSector)
        {
            if (sector.Tag != 0)
                writer.WriteLine($"id = {doomSector.Tag};");

            if (doomSector.SectorType != 0)
            {
                if (ns != UdmfNamespace.Doom)
                {
                    var zdoomType = VanillaSectorSpecTranslator.Translate(doomSector.SectorType, out _);
                    writer.WriteLine($"special = {(int)zdoomType};");
                }
                else
                {
                    writer.WriteLine($"special = {doomSector.SectorType};");
                }
            }
        }

        writer.WriteLine("}");
        writer.WriteLine();
    }

    private static void WriteSide(ISide side, TextWriter writer)
    {
        writer.WriteLine("sidedef");
        writer.WriteLine("{");
        writer.WriteLine($"sector = {side.GetSector().Id};");
        if (side.UpperTexture != Constants.NoTexture)
            writer.WriteLine($"texturetop = \"{side.UpperTexture}\";");
        if (side.LowerTexture != Constants.NoTexture)
            writer.WriteLine($"texturebottom = \"{side.LowerTexture}\";");
        if (side.MiddleTexture != Constants.NoTexture)
            writer.WriteLine($"texturemiddle = \"{side.MiddleTexture}\";");
        if (side.Offset.X != 0)
            writer.WriteLine($"offsetx = {side.Offset.X};");
        if (side.Offset.Y != 0)
            writer.WriteLine($"offsety = {side.Offset.Y};");
        writer.WriteLine("}");
        writer.WriteLine();
    }

    private static void WriteLine(ILine line, Dictionary<Vec2D, int> vertexLookup, TextWriter writer, UdmfNamespace ns)
    {
        writer.WriteLine("linedef");
        writer.WriteLine("{");
        writer.WriteLine($"v1 = {vertexLookup[line.StartPosition]};");
        writer.WriteLine($"v2 = {vertexLookup[line.EndPosition]};");
        writer.WriteLine($"sidefront = {line.GetFront().Id};");
        var back = line.GetBack();
        if (back != null)
            writer.WriteLine($"sideback = {back.Id};");
        if (line.Flags.TwoSided)
            writer.WriteLine("twosided = true;");
        if (line.Flags.BlockPlayersAndMonsters)
            writer.WriteLine("blocking = true;");
        if (line.Flags.UpperUnpegged)
            writer.WriteLine("dontpegtop = true;");
        if (line.Flags.LowerUnpegged)
            writer.WriteLine("dontpegbottom = true;");
        if (line.Flags.BlockSound)
            writer.WriteLine("blocksound = true;");
        if (line.Flags.DrawAsOneSidedAutomap)
            writer.WriteLine("secret = true;");
        if (line.Flags.NoDrawAutomap)
            writer.WriteLine("dontdraw = true;");
        if (line.Flags.AlwaysDrawAutomap)
            writer.WriteLine("mapped = true;");
        if (line.Flags.BlockMonsters)
            writer.WriteLine("blockmonsters = true;");

        if (GetTranslatedLineSpecialData(line, ns, out var zdoomType, out var specialArgs, out var lineFlags))
        {
            writer.WriteLine($"special = {zdoomType};");
            if (specialArgs.Arg0 != 0)
                writer.WriteLine($"arg0 = {specialArgs.Arg0};");
            if (specialArgs.Arg1 != 0)
                writer.WriteLine($"arg1 = {specialArgs.Arg1};");
            if (specialArgs.Arg2 != 0)
                writer.WriteLine($"arg2 = {specialArgs.Arg2};");
            if (specialArgs.Arg3 != 0)
                writer.WriteLine($"arg3 = {specialArgs.Arg3};");
            if (specialArgs.Arg4 != 0)
                writer.WriteLine($"arg4 = {specialArgs.Arg4};");
            if ((lineFlags.Activations & LineActivations.UseLine) != 0)
            {
                writer.WriteLine("playeruse = true;");
                if ((lineFlags.Activations & LineActivations.Monster) != 0)
                    writer.WriteLine("monsteruse = true;");
            }
            if ((lineFlags.Activations & LineActivations.CrossLine) != 0)
            {
                writer.WriteLine("playercross = true;");
                if ((lineFlags.Activations & LineActivations.Monster) != 0)
                    writer.WriteLine("monstercross = true;");
            }
            if ((lineFlags.Activations & (LineActivations.ImpactLine | LineActivations.Hitscan)) != 0)
                writer.WriteLine("impact = true;");
            if (lineFlags.Repeat)
                writer.WriteLine("repeatspecial = true;");
        }
        else
        {
            if (line.Special != 0)
                writer.WriteLine($"special = {line.Special};");

            if (line.SectorTag != 0)
            {
                writer.WriteLine($"id = {line.SectorTag};");
                writer.WriteLine($"arg0 = {line.SectorTag};");
            }
        }

        writer.WriteLine("}");
        writer.WriteLine();
    }

    private static bool GetTranslatedLineSpecialData(ILine line, UdmfNamespace ns, out int zdoomType, out SpecialArgs specialArgs, out LineFlags lineFlags)
    {
        specialArgs = default;
        lineFlags = new(DefaultMapLineFlags);
        if (ns != UdmfNamespace.Doom)
        {
            if (line is DoomLine doomLine && doomLine.LineType != VanillaLineSpecialType.None)
            {
                zdoomType = (int)VanillaLineSpecTranslator.Translate(ref lineFlags, doomLine.LineType, doomLine.SectorTag, ref specialArgs, out _, out _);
                return true;
            }

            if (line is HexenLine hexenLine && hexenLine.LineType != ZDoomLineSpecialType.None)
            {
                zdoomType = (int)hexenLine.LineType;
                lineFlags = new(hexenLine.Flags);
                specialArgs = hexenLine.Args;
                return true;
            }

            if (line is UdmfLine udmfLine && udmfLine.LineType != ZDoomLineSpecialType.None)
            {
                zdoomType = (int)udmfLine.LineType;
                lineFlags = new(udmfLine.Flags);
                specialArgs = udmfLine.Args;
                return true;
            }
        }

        zdoomType = 0;
        return false;
    }

    private static void WriteVertex(IVertex vertex, TextWriter writer)
    {
        writer.WriteLine("vertex");
        writer.WriteLine("{");
        writer.WriteLine($"x = {vertex.Position.X};");
        writer.WriteLine($"y = {vertex.Position.Y};");
        writer.WriteLine("}");
        writer.WriteLine();
    }

    private static void WriteThing(IThing thing, TextWriter writer)
    {
        writer.WriteLine("thing");
        writer.WriteLine("{");
        writer.WriteLine($"x = {thing.Position.X};");
        writer.WriteLine($"y = {thing.Position.Y};");
        writer.WriteLine($"angle = {thing.Angle};");
        writer.WriteLine($"type = {thing.EditorNumber};");
        if (thing.Flags.Skill1)
            writer.WriteLine("skill1 = true;");
        if (thing.Flags.Skill2)
            writer.WriteLine("skill2 = true;");
        if (thing.Flags.Skill3)
            writer.WriteLine("skill3 = true;");
        if (thing.Flags.Skill4)
            writer.WriteLine("skill4 = true;");
        if (thing.Flags.Skill5)
            writer.WriteLine("skill5 = true;");
        if (thing is DoomThing)
        {
            if (!thing.Flags.MultiPlayer)
                writer.WriteLine("single = true;");
        }
        else
        {
            if (thing.Flags.SinglePlayer)
                writer.WriteLine("single = true;");
        }
        if (thing.Flags.Cooperative)
            writer.WriteLine("coop = true;");
        if (thing.Flags.Deathmatch)
            writer.WriteLine("dm = true;");
        if (thing.Flags.Ambush)
            writer.WriteLine("ambush = true;");
        writer.WriteLine("}");
        writer.WriteLine();
    }
}
