using System;
using System.Collections.Generic;

namespace Helion.Models;

public class InventoryModel
{
    public InventoryModel()
    {
    }

    public static InventoryModel Create()
    {
        return new InventoryModel()
        {
            Items = [],
            Weapons = [],
            Powerups = []
        };
    }

    public List<InventoryItemModel> Items { get; set; } = null!;
    public List<string> Weapons { get; set; } = null!;
    public List<PowerupModel> Powerups { get; set; } = null!;
}
