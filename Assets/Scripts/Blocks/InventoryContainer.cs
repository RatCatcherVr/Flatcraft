using System;
using Mirror;
using UnityEngine;

public class InventoryContainer : Block
{
    public override void ServerInitialize()
    {
        base.ServerInitialize();

        if (!GetData().HasTag("inventoryId"))
        {
            Inventory inv = NewInventory();
            location.SetData(GetData().SetTag("inventoryId", inv.id.ToString()));
        }

        if (GetData().HasTag("loottable"))
        {
            string loottableName = GetData().GetTag("loottable");
            Loottable loottable = Loottable.Load(loottableName);
            Inventory inv = GetInventory();
            System.Random random = new System.Random();

            foreach (ItemStack item in loottable.GetRandomItems())
            {
                for (int attempts = 0; attempts < 5; attempts++)
                {
                    int slot = random.Next(0, inv.size);
                    if (inv.GetItem(slot).material == Material.Air)
                    {
                        inv.SetItem(slot, item);
                        break;
                    }
                }
            }
            location.SetData(GetData().RemoveTag("loottable"));
        }
    }

    public virtual Inventory NewInventory()
    {
        return Inventory.Create("Inventory", 0, "inventory name not set");
    }

    public override void Break(bool drop = true)
    {
        GetInventory().DropAll(location);
        GetInventory().Delete();

        base.Break(drop);
    }

    public override void Interact(PlayerInstance player)
    {
        base.Interact(player);

        GetInventory().holder = location;
        GetInventory().Open(player);
    }

    public Inventory GetInventory()
    {
        int invId = int.Parse(GetData().GetTag("inventoryId"));
        return Inventory.Get(invId);
    }
}