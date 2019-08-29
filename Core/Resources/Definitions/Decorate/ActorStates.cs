using System;
using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Resources.Definitions.Decorate
{
    public class ActorStates
    {
        public IDictionary<CIString, int> Labels = new Dictionary<CIString, int>();
        public IList<ActorFrame> Frames = new List<ActorFrame>();
    }

    public class ActorFrame
    {
        public readonly string Sprite;
        public readonly char Frame;
        public readonly int Ticks;
        public readonly ActorFrameProperties Properties;
        public readonly ActionFunction? ActionFunction;
        public int NextFrameIndexOffset = 1;

        public ActorFrame(string sprite, char frame, int ticks, ActorFrameProperties properties, 
            ActionFunction? actionFunction)
        {
            Sprite = sprite;
            Frame = frame;
            Ticks = ticks;
            Properties = properties;
            ActionFunction = actionFunction;
        }
    }

    public class ActorFrameProperties
    {
        public bool Bright;
        public bool CanRaise;
        public bool Fast;
        public string? Light;
        public bool NoDelay;
        public Vec2I? Offset;
        public bool Slow;
    }

    public class ActionFunction
    {
        public readonly string FunctionName;

        public ActionFunction(string functionName)
        {
            FunctionName = functionName.ToUpper();
        }
    }
}