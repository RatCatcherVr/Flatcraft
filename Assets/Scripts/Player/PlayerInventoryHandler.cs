using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerInventoryHandler : NetworkBehaviour
{
    private Material _actionBarLastSelectedMaterial;
    private int _framesSinceInventoryOpen;
    private Player _player;

    private Button _openButton;
    private Button _closeButton;
    private Button _nextSlotButton;
    private Button _prevSlotButton;
    private Button _splitButton;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Start()
    {
        if (!isOwned) return;
        StartCoroutine(SetupMobile());
    }

    private IEnumerator SetupMobile()
    {
        yield return new WaitForSeconds(0.5f);

        _openButton = GameObject.Find("InventoryButton")?.GetComponent<Button>();
        _closeButton = GameObject.Find("InventoryCloseButton")?.GetComponent<Button>();
        _nextSlotButton = GameObject.Find("NextSlotButton")?.GetComponent<Button>();
        _prevSlotButton = GameObject.Find("PrevSlotButton")?.GetComponent<Button>();
        _splitButton = GameObject.Find("SplitStackButton")?.GetComponent<Button>();

        _openButton?.onClick.AddListener(MobileOpenInventory);

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(MobileCloseAllMenus);
            _closeButton.gameObject.SetActive(false);
        }

        _nextSlotButton?.onClick.AddListener(NextSlot);
        _prevSlotButton?.onClick.AddListener(PreviousSlot);
        _splitButton?.onClick.AddListener(() => CMD_SplitStack());
    }

    private void Update()
    {
        if (isServer) GetInventory().holder = _player.Location;
        if (!isOwned) return;

        ActionBarMessageUpdate();

        bool inventoryOpen = Inventory.IsAnyOpen(_player.playerInstance);
        bool anyMenuOpen = inventoryOpen || PauseMenu.active || GameObject.Find("ContainerInventoryMenu(Clone)") != null;

        _framesSinceInventoryOpen = inventoryOpen || anyMenuOpen ? 0 : _framesSinceInventoryOpen + 1;

        _openButton?.gameObject.SetActive(!anyMenuOpen && _framesSinceInventoryOpen > 0);
        _closeButton?.gameObject.SetActive(anyMenuOpen);

        PerformInput();
    }

    [Client]
    private void PerformInput()
    {
        bool canInteract = PlayerInteraction.CanInteractWithWorld();

        if (!Application.isMobilePlatform)
        {
            float scrollAmount = -Input.mouseScrollDelta.y;
            if (scrollAmount != 0)
            {
                int newSelectedSlot = (GetInventory().selectedSlot + (int)scrollAmount + 9) % 9;
                CMD_UpdateSelectedSlot(newSelectedSlot);
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && _framesSinceInventoryOpen > 10)
        {
            if (Inventory.IsAnyOpen(_player.playerInstance))
                MobileCloseAllMenus();
            else
                CMD_OpenInventory();
        }

        if (!canInteract) return;

        if (Input.GetKeyDown(KeyCode.Q)) CMD_DropSelected();
        if (Input.GetKeyDown(KeyCode.X)) CMD_SplitStack();

        HotbarSlotInput();
    }

    public void NextSlot() => CMD_UpdateSelectedSlot((GetInventory().selectedSlot + 1) % 9);
    public void PreviousSlot() => CMD_UpdateSelectedSlot((GetInventory().selectedSlot + 8) % 9);

    public void MobileOpenInventory()
    {
        if (!isOwned || _framesSinceInventoryOpen <= 10) return;
        CMD_OpenInventory();
    }

    public void MobileCloseAllMenus()
    {
        if (!isOwned) return;

        GetInventory().Close();
        Destroy(GameObject.Find("ContainerInventoryMenu(Clone)"));
        Destroy(GameObject.Find("CraftingInventoryMenu(Clone)"));
        _framesSinceInventoryOpen = 11;

        PauseMenu.active = false;
    }

    [Command]
    public void CMD_SplitStack()
    {
        PlayerInventory inv = GetInventory();
        int slot = inv.selectedSlot;
        ItemStack stack = inv.GetItem(slot);

        if (stack.Amount <= 1) return;

        int half = stack.Amount / 2;
        int remainder = stack.Amount - half;

        int emptySlot = -1;
        for (int i = 0; i < 36; i++)
        {
            if (inv.GetItem(i).Amount == 0)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1) return;

        ItemStack newStack = stack;
        newStack.Amount = half;

        stack.Amount = remainder;

        inv.SetItem(slot, stack);
        inv.SetItem(emptySlot, newStack);
    }

    private void HotbarSlotInput()
    {
        KeyCode[] numpadCodes = {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };

        foreach (KeyCode keyCode in numpadCodes)
            if (Input.GetKeyDown(keyCode))
                CMD_UpdateSelectedSlot(Array.IndexOf(numpadCodes, keyCode));
    }

    [Client]
    private void ActionBarMessageUpdate()
    {
        Material selectedMaterial = GetInventory().GetSelectedItem().material;

        if (selectedMaterial != _actionBarLastSelectedMaterial && selectedMaterial != Material.Air)
            ActionBar.message = selectedMaterial.ToString().Replace('_', ' ');

        _actionBarLastSelectedMaterial = selectedMaterial;
    }

    [Command]
    public void CMD_DropSelected()
    {
        ItemStack selectedItem = GetInventory().GetSelectedItem();

        if (selectedItem.Amount <= 0) return;

        ItemStack droppedItem = new ItemStack(selectedItem.material, 1);
        selectedItem.Amount--;

        DropItem(droppedItem);
        GetInventory().SetItem(GetInventory().selectedSlot, selectedItem);
    }

    [Server]
    public void DropItem(ItemStack item)
    {
        item.Drop(
            _player.Location + new Location(_player.facingLeft ? -1 : 1, 1),
            new Vector2(3 * (_player.facingLeft ? -1 : 1), 0f)
        );
    }

    [Command]
    private void CMD_OpenInventory()
    {
        GetInventory().Open(_player.playerInstance);
    }

    [Command]
    private void CMD_UpdateSelectedSlot(int slot)
    {
        GetInventory().selectedSlot = slot;
    }

    public PlayerInventory GetInventory()
    {
        Inventory inventory = Inventory.Get(_player.inventoryId);

        if (inventory == null)
        {
            inventory = PlayerInventory.CreatePreset();
            _player.inventoryId = inventory.id;
        }

        return (PlayerInventory)inventory;
    }
}