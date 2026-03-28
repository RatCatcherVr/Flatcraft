using Mirror;
using UnityEngine;

public class CraftingInventory : Inventory
{
    [Server]
    public static Inventory CreatePreset() => Create("CraftingInventory", 10, "Crafting");

    public ItemStack[] GetCraftingTableItems()
    {
        ItemStack[] table = new ItemStack[9];
        for (int i = 0; i <= 8; i++) table[i] = GetItem(i);
        return table;
    }

    [Server]
    public override void Close()
    {
        base.Close();
        // Removed null check on Location holder because Location is a struct
        foreach (ItemStack item in GetCraftingTableItems())
        {
            if (item.material != Material.Air)
                item.Drop(holder);
        }
        Clear();
    }

    public int GetCraftingResultSlot() => 9;
}