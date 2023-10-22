using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Options;
using Helion.Window;
using System;

namespace Helion.Layer.Options;

public enum Lock
{
    Locked,
    Unlocked
}

public struct LockEvent
{
    public readonly Lock Lock;
    public readonly string Message;

    public LockEvent(Lock setLock, string message)
    {
        Lock = setLock;
        Message = message;
    }

    public LockEvent(Lock setLock)
    {
        Lock = setLock;
        Message = string.Empty;
    }
}

public interface IOptionSection
{
    public event EventHandler<LockEvent>? OnLockChanged;

    public OptionSectionType OptionType { get; }

    void HandleInput(IConsumableInput input);
    void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY);
    int GetRenderHeight();
    (int,int) GetSelectedRenderY();
    void SetToFirstSelection();
    void SetToLastSelection();
}