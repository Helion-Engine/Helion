using System;
using System.Collections.Generic;

namespace Helion.Models;

public class InventoryModel
{
    public IList<InventoryItemModel> Items { get; set; } = Array.Empty<InventoryItemModel>();
    public IList<string> Weapons { get; set; } = Array.Empty<string>();
    public IList<PowerupModel> Powerups { get; set; } = Array.Empty<PowerupModel>();
}
