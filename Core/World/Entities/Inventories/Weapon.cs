using Helion.Models;
using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Inventories;

public class Weapon : InventoryItem, ITickable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly Player Owner;
    public FrameState FrameState;
    public FrameState FlashState;

    public readonly EntityDefinition? AmmoDefinition;
    public readonly string AmmoSprite;

    public bool ReadyToFire;
    public bool ReadyState;

    private bool m_tryingToFire;

    public int KickBack => Definition.Properties.Weapons.DefaultKickBack ? WorldStatic.World.GameInfo.DefKickBack : 
        Definition.Properties.Weapons.KickBack;

    public Weapon(EntityDefinition definition, Player owner, EntityManager entityManager,
        FrameStateModel? frameStateModel = null, FrameStateModel? flashStateModel = null) :
        base(definition, 1)
    {
        Precondition(definition.IsType(EntityDefinitionType.Weapon), "Trying to create a weapon from a non-weapon type");

        Owner = owner;

        if (frameStateModel == null)
            FrameState = new FrameState(owner, definition, false);
        else
            FrameState = new FrameState(owner, definition, frameStateModel);

        if (flashStateModel == null)
            FlashState = new FrameState(owner, definition, false);
        else
            FlashState = new FrameState(owner, definition, flashStateModel);

        AmmoDefinition = WorldStatic.EntityManager.DefinitionComposer.GetByName(definition.Properties.Weapons.AmmoType);
        if (AmmoDefinition != null && AmmoDefinition.States.Labels.TryGetValue(Constants.FrameStates.Spawn, out int frame))
            AmmoSprite = entityManager.World.ArchiveCollection.Definitions.EntityFrameTable.Frames[frame].Sprite + "A0";
        else
            AmmoSprite = string.Empty;
    }

    public void RequestFire()
    {
        m_tryingToFire = true;
    }

    public void SetFireState()
    {
        FrameState.SetState(Constants.FrameStates.Fire);
    }

    public void SetFlashState(int offset = 0)
    {
        Owner.WeaponFlashState = true;
        FlashState.SetState(Constants.FrameStates.Flash, offset, false);
        Owner.WeaponFlashState = false;
    }

    public void SetReadyState()
    {
        FrameState.SetState(Constants.FrameStates.Ready);
    }

    public void Tick()
    {
        ReadyState = false;
        ReadyToFire = false;

        FrameState.Tick();
        Owner.WeaponFlashState = true;
        FlashState.Tick();
        Owner.WeaponFlashState = false;

        if (m_tryingToFire && ReadyToFire)
        {
            SetToFireState();
            ReadyState = false;
            ReadyToFire = false;
        }

        m_tryingToFire = false;
    }

    private void SetToFireState()
    {
        if (!FrameState.SetState(Constants.FrameStates.Fire))
            Log.Warn("Unable to find Fire state for weapon {0}", Definition.Name);
    }

    public override string ToString() => Definition.Name;
}
