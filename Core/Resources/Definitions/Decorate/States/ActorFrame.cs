namespace Helion.Resources.Definitions.Decorate.States
{
    public class ActorFrame
    {
        public readonly string Sprite;
        public readonly char Frame;
        public readonly int Ticks;
        public readonly ActorFrameProperties Properties;
        public readonly ActionFunction? ActionFunction;
        public ActorFlowControl? FlowControl;

        public ActorFrame(string sprite, char frame, int ticks, ActorFrameProperties properties, 
            ActionFunction? actionFunction)
        {
            Sprite = sprite;
            Frame = frame;
            Ticks = ticks;
            Properties = properties;
            ActionFunction = actionFunction;
        }

        public override string ToString() => $"{Sprite} {Frame} {Ticks} action={ActionFunction} flow={FlowControl}]";
    }
}