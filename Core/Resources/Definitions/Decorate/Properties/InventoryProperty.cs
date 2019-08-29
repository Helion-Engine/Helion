using System.Collections.Generic;

namespace Helion.Resources.Definitions.Decorate.Properties
{
    public struct InventoryProperty
    {
        public string? AltHUDIcon;
        public int? Amount;
        public List<string>? ForbiddenTo;
        public int? GiveQuest;
        public int? InterHubAmount;
        public string? Icon;
        public int? MaxAmount;
        public string? PickupFlash;
        public string? PickupMessage;
        public string? PickupSound;
        public int? RespawnTicks; // TODO: Spelling is wrong if we do dynamic writing...
        public List<string>? RestrictedTo;
        public string? UseSound;
    }
}