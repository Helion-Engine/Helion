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

public struct RowEvent
{
    public readonly int Index;

    public RowEvent(int index)
    {
        Index = index;
    }
}

public interface IOptionSection
{
    public event EventHandler<LockEvent>? OnLockChanged;
    public event EventHandler<RowEvent>? OnRowChanged;

    public OptionSectionType OptionType { get; }

    void HandleInput(IConsumableInput input);
    void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY);
    int GetRenderHeight();
    (int,int) GetSelectedRenderY();
    void SetToFirstSelection();
    void SetToLastSelection();
}