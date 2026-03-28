using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Inventory : NetworkBehaviour
{
    public static int MaxStackSize = 64;
    [SyncVar] public Location holder;
    [SyncVar] public string invName;
    [SyncVar] public int size;
    [SyncVar] public string type;
    [SyncVar] public int id;
    [SyncVar] public GameObject inventoryMenu;
    [SyncVar] public bool open;

    public GameObject inventoryMenuPrefab;

    public readonly SyncList<ItemStack> items = new SyncList<ItemStack>();

    private void Start()
    {
        WorldManager.instance.loadedInventories[id] = this;
    }

    public virtual void Update()
    {
        if ((Time.time % 5f) - Time.deltaTime <= 0 && isServer)
            Save();
    }

    [Server]
    public static Inventory Create(string type, int size, string invName)
    {
        int id = Random.Range(1, 9999999);
        return Create(type, size, invName, id);
    }

    [Server]
    public static Inventory Create(string type, int size, string invName, int id)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>("Prefabs/Inventories/" + type));
        Inventory inventory = obj.GetComponent<Inventory>();

        inventory.size = size;
        inventory.type = type;
        inventory.invName = invName;
        inventory.id = id;

        for (int i = 0; i < size; i++)
            inventory.items.Add(new ItemStack());

        WorldManager.instance.loadedInventories[id] = inventory;

        NetworkServer.Spawn(obj);
        return inventory;
    }

    public static Inventory Get(int id)
    {
        if (WorldManager.instance.loadedInventories.ContainsKey(id))
            return WorldManager.instance.loadedInventories[id];

        if (NetworkServer.active && Directory.Exists(WorldManager.world.GetPath() + "\\inventories\\" + id))
            return Load(id);

        return null;
    }

    public static bool IsAnyOpen(PlayerInstance playerInstance)
    {
        foreach (Inventory inv in WorldManager.instance.loadedInventories.Values)
        {
            if (inv == null) continue;
            if (inv.open && inv.inventoryMenu != null)
            {
                InventoryMenu menu = inv.inventoryMenu.GetComponent<InventoryMenu>();
                if (menu != null && menu.ownerPlayerInstance == playerInstance)
                    return true;
            }
        }
        return false;
    }

    [Server]
    public void Save()
    {
        string path = WorldManager.world.GetPath() + "\\inventories\\" + id;
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        List<string> itemLines = new List<string>();
        foreach (ItemStack item in items)
            itemLines.Add(item.ToString());

        File.WriteAllLines(path + "\\items.dat", itemLines);
        File.WriteAllLines(path + "\\type.dat", new List<string> { type });
        File.WriteAllLines(path + "\\invName.dat", new List<string> { invName });
        File.WriteAllLines(path + "\\size.dat", new List<string> { size.ToString() });
    }

    [Server]
    public static Inventory Load(int id)
    {
        string path = WorldManager.world.GetPath() + "\\inventories\\" + id;
        string[] itemLines = File.ReadAllLines(path + "\\items.dat");
        List<ItemStack> items = new List<ItemStack>();
        foreach (string itemLine in itemLines)
        {
            try { items.Add(new ItemStack(itemLine)); }
            catch { items.Add(new ItemStack()); }
        }

        string type = File.ReadAllLines(path + "\\type.dat")[0];
        string invName = File.ReadAllLines(path + "\\invName.dat")[0];
        int size = int.Parse(File.ReadAllLines(path + "\\size.dat")[0]);

        Inventory inv = Create(type, size, invName, id);
        inv.items.Clear();
        foreach (var i in items) inv.items.Add(i);
        return inv;
    }

    [Server]
    public void Delete()
    {
        Close();
        string path = WorldManager.world.GetPath() + "\\inventories\\" + id;
        if (Directory.Exists(path)) Directory.Delete(path, true);
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    public void SetItem(int slot, ItemStack item)
    {
        if (slot < items.Count) items[slot] = item;
    }

    public ItemStack GetItem(int slot)
    {
        if (slot >= items.Count) return new ItemStack();
        return items[slot];
    }

    [Server]
    public void Clear()
    {
        for (int slot = 0; slot < size; slot++)
            SetItem(slot, new ItemStack());
    }

    [Server]
    public virtual bool AddItem(ItemStack item)
    {
        for (int slot = 0; slot < size; slot++)
        {
            ItemStack invItem = GetItem(slot);
            if (invItem.material == item.material && invItem.Amount + item.Amount <= MaxStackSize)
            {
                invItem.Amount += item.Amount;
                SetItem(slot, invItem);
                return true;
            }
        }
        for (int slot = 0; slot < size; slot++)
            if (GetItem(slot).material == Material.Air)
            {
                SetItem(slot, item);
                return true;
            }
        return false;
    }

    public bool Contains(Material mat)
    {
        foreach (ItemStack item in items)
            if (item.material == mat) return true;
        return false;
    }

    [Server]
    public void DropAll(Location dropPosition)
    {
        foreach (ItemStack item in items)
        {
            if (item.material != Material.Air)
                item.Drop(dropPosition + new Location(0, 1), true);
        }
        Clear();
    }

    [Server]
    public void Open(PlayerInstance playerInstance)
    {
        if (open && inventoryMenu != null) return;

        open = true;
        GameObject obj = Instantiate(inventoryMenuPrefab);
        InventoryMenu menu = obj.GetComponent<InventoryMenu>();
        menu.inventoryIds.Add(0, id);
        menu.ownerPlayerInstance = playerInstance;
        NetworkServer.Spawn(obj);
        inventoryMenu = obj;
    }

    [Server]
    public virtual void Close()
    {
        if (!open) return;
        open = false;

        if (inventoryMenu != null)
        {
            InventoryMenu menu = inventoryMenu.GetComponent<InventoryMenu>();
            if (menu != null) menu.Close();
        }
        inventoryMenu = null;
    }

    [Command(requiresAuthority = false)]
    public void RequestClose() => Close();
}