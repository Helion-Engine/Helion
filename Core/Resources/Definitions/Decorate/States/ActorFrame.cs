namespace Helion.Resources.Definitions.Decorate.States
{
    public class ActorFrame
    {
        public readonly string Sprite;
        public readonly int Frame;
        public readonly int Ticks;
        public readonly ActorFrameProperties Properties;
        public readonly ActorActionFunction? ActionFunction;
        public ActorFlowControl? FlowControl;

        public ActorFrame(string sprite, int frame, int ticks, ActorFrameProperties properties, 
            ActorActionFunction? actionFunction)
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