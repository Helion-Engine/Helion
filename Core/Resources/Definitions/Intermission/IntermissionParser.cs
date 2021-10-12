using Helion.Util.Parser;
using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Intermission;

public static class IntermissionParser
{
    private static readonly string Background = "Background";
    private static readonly string Splat = "Splat";
    private static readonly string Pointer = "Pointer";
    private static readonly string Animation = "Animation";
    private static readonly string IfLeaving = "IfLeaving";
    private static readonly string IfEntering = "IfEntering";
    private static readonly string IfVisited = "IfVisited";
    private static readonly string Spots = "Spots";

    private static readonly HashSet<string> Items = new(StringComparer.OrdinalIgnoreCase)
    {
        Background,
        Splat,
        Pointer,
        Animation,
        IfLeaving,
        IfEntering,
        Spots
    };

    public static IntermissionDef Parse(string data)
    {
        IntermissionDef def = new() { Animations = new List<IntermissionAnimation>() };
        SimpleParser parser = new();
        parser.Parse(data);

        while (!parser.IsDone())
        {
            string item = parser.ConsumeString();

            if (item.Equals(Background, StringComparison.OrdinalIgnoreCase))
                def.Background = parser.ConsumeString();
            else if (item.Equals(Splat, StringComparison.OrdinalIgnoreCase))
                def.Splat = parser.ConsumeString();
            else if (item.Equals(Pointer, StringComparison.OrdinalIgnoreCase))
                def.Pointer = GetPointer(parser);
            else if (item.Equals(Animation, StringComparison.OrdinalIgnoreCase))
                def.Animations.Add(ParseAnimation(parser, string.Empty, IntermissionAnimationType.Normal));
            else if (item.Equals(IfLeaving, StringComparison.OrdinalIgnoreCase))
                def.Animations.Add(ParseAnimation(parser, parser.ConsumeString(), IntermissionAnimationType.IfLeaving));
            else if (item.Equals(IfEntering, StringComparison.OrdinalIgnoreCase))
                def.Animations.Add(ParseAnimation(parser, parser.ConsumeString(), IntermissionAnimationType.IfEntering));
            else if (item.Equals(IfVisited, StringComparison.OrdinalIgnoreCase))
                def.Animations.Add(ParseAnimation(parser, parser.ConsumeString(), IntermissionAnimationType.IfVisited));
            else if (item.Equals(Spots, StringComparison.OrdinalIgnoreCase))
                def.Spots = ParseSpots(parser);
        }

        return def;
    }

    private static IList<IntermissionSpot> ParseSpots(SimpleParser parser)
    {
        List<IntermissionSpot> spots = new();
        parser.Consume('{');
        while (!parser.Peek('}'))
        {
            spots.Add(new IntermissionSpot()
            {
                MapName = parser.ConsumeString(),
                X = parser.ConsumeInteger(),
                Y = parser.ConsumeInteger()
            });
        }

        return spots;
    }

    private static IntermissionAnimation ParseAnimation(SimpleParser parser, string mapName, IntermissionAnimationType type)
    {
        bool isPic = parser.Peek("Pic");
        if (isPic)
            parser.ConsumeString();

        if (parser.Peek("Animation"))
            parser.ConsumeString();

        IntermissionAnimation animation = new();
        animation.Type = type;
        animation.MapName = mapName;
        animation.X = parser.ConsumeInteger();
        animation.Y = parser.ConsumeInteger();
        if (!isPic)
            animation.Tics = parser.ConsumeInteger();
        animation.Items = new List<string>();

        if (parser.ConsumeIf("ONCE"))
            animation.Once = true;

        if (parser.Peek('{'))
        {
            parser.ConsumeString();
            while (!parser.Peek('}'))
                animation.Items.Add(parser.ConsumeString());

            parser.ConsumeString();
        }
        else
        {
            animation.Items.Add(parser.ConsumeString());
        }

        return animation;
    }

    private static IList<string> GetPointer(SimpleParser parser)
    {
        List<string> pointers = new();
        while (!Items.Contains(parser.PeekString()))
            pointers.Add(parser.ConsumeString());

        return pointers;
    }
}

