using System;
using Helion.Audio.Sounds;
using Helion.Render.Common.Renderers;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Menus.New.Misc;

public class ConfirmMenu(SoundManager soundManager, Action<bool> confirmCallback, params string[] messages) : 
    Menu(soundManager)
{
    public override bool HandleInput(IConsumableInput input, out Menu? newMenu)
    {
        if (input.ConsumeKeyPressed(Key.Y))
            confirmCallback(true);
        else if (input.HasAnyKeyPressed())
            confirmCallback(false);
        
        newMenu = null;
        return false;
    }

    public override void RunLogic(TickerInfo tickerInfo)
    {
        // Nothing to do here.
    }

    public override void Render(IRenderableSurfaceContext surface, IHudRenderContext ctx)
    {
        for (int i = 0; i < messages.Length; i++)
        {
            string message = messages[i];
            // TODO: Draw the message
        }
    }
}