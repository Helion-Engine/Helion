using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition.Properties.Components;

public class InventoryProperty
{
    public string AltHUDIcon = string.Empty;
    public int Amount;
    public bool DefMaxAmount;
    public IList<string> ForbiddenTo = Array.Empty<string>();
    public int GiveQuest;
    public int InterHubAmount;
    public string Icon = string.Empty;
    public int MaxAmount;
    public string PickupFlash = string.Empty;
    public string PickupMessage = string.Empty;
    public string PickupSound = string.Empty;
    public int RespawnTics;
    public IList<string> RestrictedTo = Array.Empty<string>();
    public string UseSound = string.Empty;
    public int PickupBonusCount = 6;
    public bool MessageOnly = false;
    public bool NoItem = false;
}
