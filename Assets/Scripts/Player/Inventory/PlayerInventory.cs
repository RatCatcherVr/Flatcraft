using System;
using Mirror;
using UnityEngine;

[Serializable]
public class PlayerInventory : Inventory
{
    [SyncVar] public int selectedSlot;

    [Server]
    public static Inventory CreatePreset()
    {
        return Create("PlayerInventory", 45, "Inventory");
    }

    public ItemStack GetSelectedItem()
    {
        return GetItem(selectedSlot);
    }

    [Server]
    public void ConsumeSelectedItem()
    {
        ItemStack heldItem = GetSelectedItem();
        if (heldItem.material == Material.Air) return;

        heldItem.Amount--;
        if (heldItem.Amount <= 0)
            SetItem(selectedSlot, new ItemStack());
        else
            SetItem(selectedSlot, heldItem);
    }

    public ItemStack[] GetHotbarItems()
    {
        ItemStack[] hotbar = new ItemStack[9];
        for (int i = 0; i < 9; i++)
            hotbar[i] = GetItem(i);
        return hotbar;
    }

    public ItemStack[] GetArmorItems()
    {
        ItemStack[] armor = new ItemStack[4];
        for (int i = 0; i < 4; i++)
            armor[i] = GetItem(GetFirstArmorSlot() + i);
        return armor;
    }

    public ItemStack[] GetCraftingTableItems()
    {
        ItemStack[] table = new ItemStack[4];
        for (int i = 0; i < 4; i++)
            table[i] = GetItem(GetFirstCraftingTableSlot() + i);
        return table;
    }

    [Server]
    public override void Close()
    {
        base.Close();
        for (int slot = GetFirstCraftingTableSlot(); slot < GetFirstCraftingTableSlot() + 4; slot++)
        {
            ItemStack stack = GetItem(slot);
            if (stack.material != Material.Air)
            {
                stack.Drop(holder + new Location(0, 1));
                SetItem(slot, new ItemStack());
            }
        }
        SetItem(GetCraftingResultSlot(), new ItemStack());
    }

    [Server]
    public override bool AddItem(ItemStack item)
    {
        for (int slot = 0; slot < GetFirstArmorSlot(); slot++)
        {
            ItemStack invItem = GetItem(slot);
            if (invItem.material == Material.Air || (invItem.material == item.material && invItem.Amount + item.Amount <= MaxStackSize))
            {
                return base.AddItem(item);
            }
        }
        return false;
    }

    public int GetFirstArmorSlot() => 36;
    public int GetFirstCraftingTableSlot() => 40;
    public int GetCraftingResultSlot() => 44;
}