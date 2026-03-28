using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryHandler : NetworkBehaviour
{
    private Material _actionBarLastSelectedMaterial;
    private int _framesSinceInventoryOpen;

    private Player _player;

    private Button _openButton;
    private Button _closeButton;

    private Button _nextSlotButton;
    private Button _prevSlotButton;

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

        GameObject openObj = GameObject.Find("InventoryButton");
        if (openObj != null)
        {
            _openButton = openObj.GetComponent<Button>();
            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
                _openButton.onClick.AddListener(MobileOpenInventory);
            }
        }

        GameObject closeObj = GameObject.Find("InventoryCloseButton");
        if (closeObj != null)
        {
            _closeButton = closeObj.GetComponent<Button>();
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(MobileCloseAllMenus);
                _closeButton.gameObject.SetActive(false);
            }
        }

        GameObject nextObj = GameObject.Find("NextSlotButton");
        if (nextObj != null)
        {
            _nextSlotButton = nextObj.GetComponent<Button>();
            if (_nextSlotButton != null)
            {
                _nextSlotButton.onClick.RemoveAllListeners();
                _nextSlotButton.onClick.AddListener(NextSlot);
            }
        }

        GameObject prevObj = GameObject.Find("PrevSlotButton");
        if (prevObj != null)
        {
            _prevSlotButton = prevObj.GetComponent<Button>();
            if (_prevSlotButton != null)
            {
                _prevSlotButton.onClick.RemoveAllListeners();
                _prevSlotButton.onClick.AddListener(PreviousSlot);
            }
        }
    }

    private void Update()
    {
        if (isServer) GetInventory().holder = _player.Location;

        if (!isOwned) return;

        ActionBarMessageUpdate();

        bool inventoryOpen = Inventory.IsAnyOpen(_player.playerInstance);
        bool anyMenuOpen = inventoryOpen ||
                           (ChatMenu.instance != null && ChatMenu.instance.open) ||
                           SignEditMenu.IsLocalMenuOpen() ||
                           PauseMenu.active ||
                           GameObject.Find("ContainerInventoryMenu(Clone)") != null; // Stay "Open" if the clone is there

        if (inventoryOpen || anyMenuOpen)
        {
            _framesSinceInventoryOpen = 0;
            if (_openButton != null) _openButton.gameObject.SetActive(false);
        }
        else
        {
            _framesSinceInventoryOpen++;
            if (_openButton != null) _openButton.gameObject.SetActive(true);
        }

        if (_closeButton != null) _closeButton.gameObject.SetActive(anyMenuOpen);

        PerformInput();
    }

    [Client]
    private void PerformInput()
    {
        bool canInteract = PlayerInteraction.CanInteractWithWorld();

        if (!UnityEngine.Application.isMobilePlatform)
        {
            float scrollAmount = -Input.mouseScrollDelta.y;

            if (scrollAmount != 0)
            {
                int newSelectedSlot = GetInventory().selectedSlot + (int)scrollAmount;
                newSelectedSlot = (newSelectedSlot + 9) % 9;
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

        if (Input.GetKeyDown(KeyCode.Q))
            CMD_DropSelected();

        HotbarSlotInput();
    }

    public void NextSlot()
    {
        if (!isOwned) return;
        int newSlot = (GetInventory().selectedSlot + 1) % 9;
        CMD_UpdateSelectedSlot(newSlot);
    }

    public void PreviousSlot()
    {
        if (!isOwned) return;
        int newSlot = (GetInventory().selectedSlot - 1 + 9) % 9;
        CMD_UpdateSelectedSlot(newSlot);
    }

    public void MobileOpenInventory()
    {
        if (!isOwned) return;
        if (_framesSinceInventoryOpen > 10)
            CMD_OpenInventory();
    }

    public void MobileCloseAllMenus()
    {
        if (!isOwned) return;

        // 1. Force the logic to close
        PlayerInventory inv = GetInventory();
        if (inv != null) inv.Close();

        // 2. Destroy the physical menu clones
        GameObject containerUI = GameObject.Find("ContainerInventoryMenu(Clone)");
        if (containerUI != null) Destroy(containerUI);

        GameObject craftingUI = GameObject.Find("CraftingInventoryMenu(Clone)");
        if (craftingUI != null) Destroy(craftingUI);

        // 3. Reset the frames and static variables
        _framesSinceInventoryOpen = 11; // Setting this higher than 10 unlocks input

        if (ChatMenu.instance != null) ChatMenu.instance.open = false;

        if (SignEditMenu.IsLocalMenuOpen())
        {
            var signMenu = GameObject.FindObjectOfType<SignEditMenu>();
            if (signMenu != null) signMenu.gameObject.SetActive(false);
        }

        PauseMenu.active = false;

        if (_closeButton != null) _closeButton.gameObject.SetActive(false);
        if (_openButton != null) _openButton.gameObject.SetActive(true);
    }

    private void HotbarSlotInput()
    {
        KeyCode[] numpadCodes = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 };
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
        ItemStack droppedItem = selectedItem;
        droppedItem.Amount = 1;
        selectedItem.Amount--;
        DropItem(droppedItem);
        GetInventory().SetItem(GetInventory().selectedSlot, selectedItem);
    }

    [Server]
    public void DropItem(ItemStack item)
    {
        item.Drop(_player.Location + new Location(1 * (_player.facingLeft ? -1 : 1), 1),
                  new Vector2(3 * (_player.facingLeft ? -1 : 1), 0f));
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