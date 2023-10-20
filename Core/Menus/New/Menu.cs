using System.Diagnostics.CodeAnalysis;
using Helion.Audio.Sounds;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Timing;
using Helion.Window;

namespace Helion.Menus.New;

public abstract class Menu(SoundManager soundManager)
{
    // Returns a menu if the input would change to a new menu. The caller
    // must remember this. If the caller wants 'Escape' to close a menu,
    // the caller should use the input after it has gone through this
    // function, in case the Escape key was consumed.
    public abstract bool HandleInput(IConsumableInput input, [NotNullWhen(true)] out Menu? newMenu);
    public abstract void RunLogic(TickerInfo tickerInfo);
    public abstract void Render(IRenderableSurfaceContext surface, IHudRenderContext ctx);

    private void PlayOptionSound(string soundName)
    {
        soundManager.PlayStaticSound(soundName);
        soundManager.Update(); // Is this needed?
    }
    
    protected void PlayNextOptionSound()
    {
        PlayOptionSound(Constants.MenuSounds.Cursor);
    }

    protected void PlayChooseOptionSound()
    {
        PlayOptionSound(Constants.MenuSounds.Choose);
    }
}