namespace Helion.Models;

public struct InventoryItemModel
{
    public InventoryItemModel()
    {
    }

    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
}
