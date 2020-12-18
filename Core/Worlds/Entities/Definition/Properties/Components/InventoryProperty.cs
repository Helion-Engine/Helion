using System.Collections.Generic;

namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public class InventoryProperty
    {
        public string AltHUDIcon = "";
        public int Amount;
        public bool DefMaxAmount;
        public List<string> ForbiddenTo = new List<string>();
        public int GiveQuest;
        public int InterHubAmount;
        public string Icon = "";
        public int MaxAmount;
        public string PickupFlash = "";
        public string PickupMessage = "";
        public string PickupSound = "";
        public int RespawnTics;
        public List<string> RestrictedTo = new List<string>();
        public string UseSound = "";
    }
}