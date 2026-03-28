using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerInteraction : NetworkBehaviour
{
    public const float InteractionsPerPerSecond = 4.5f;
    public const float Reach = 5;

    [HideInInspector] public GameObject crosshair;
    [HideInInspector] public double lastBlockHitTime;
    [HideInInspector] public double lastHitTime;
    [HideInInspector] public double lastBlockInteractionTime;

    private Player _player;
    private bool placeMode = false;
    private Button placeButton;
    private Button inventoryCloseButton;
    private bool buttonsLinked = false;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Start()
    {
        if (!isOwned) return;
        StartCoroutine(AutoFindRoutine());
    }

    private IEnumerator AutoFindRoutine()
    {
        while (!buttonsLinked)
        {
            Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

            foreach (Button btn in allButtons)
            {
                if (btn.gameObject.name == "InventoryCloseButton")
                {
                    inventoryCloseButton = btn;
                    inventoryCloseButton.onClick.RemoveAllListeners();
                    inventoryCloseButton.onClick.AddListener(ForceCloseAll);
                }

                if (btn.gameObject.name == "PlaceButton")
                {
                    placeButton = btn;
                    placeButton.onClick.RemoveAllListeners();
                    placeButton.onClick.AddListener(TogglePlaceMode);
                }
            }

            if (inventoryCloseButton != null && placeButton != null)
            {
                buttonsLinked = true;
            }

            yield return new WaitForSeconds(1.0f);
        }
    }

    private void Update()
    {
        if (!isOwned) return;

        if (Input.GetKeyDown(KeyCode.E))
            ToggleInventoryLikeE();

        if (!CanInteractWithWorld()) return;

        UpdateCrosshair();

        Vector2 inputPos = GetInputPosition();
        if (Vector2.Distance(inputPos, transform.position) > Reach) return;

        HandleInput(inputPos);
    }

    private void ForceCloseAll()
    {
        PlayerInventory inv = _player.GetInventoryHandler().GetInventory();
        if (inv != null) inv.Close();

        GameObject containerUI = GameObject.Find("ContainerInventoryMenu(Clone)");
        if (containerUI != null) Destroy(containerUI);

        GameObject craftingUI = GameObject.Find("CraftingInventoryMenu(Clone)");
        if (craftingUI != null) Destroy(craftingUI);

        if (ChatMenu.instance != null) ChatMenu.instance.open = false;
        PauseMenu.active = false;

        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in canvases)
        {
            string lowerName = c.gameObject.name.ToLower();
            if (lowerName.Contains("chest") || lowerName.Contains("craft"))
            {
                c.gameObject.SetActive(false);
            }
        }
    }

    public void TogglePlaceMode() => placeMode = !placeMode;

    private void ToggleInventoryLikeE()
    {
        if (PlayerInstance.localPlayerInstance == null) return;

        PlayerInventory inv = _player.GetInventoryHandler().GetInventory();
        bool isContainerOpen = GameObject.Find("ContainerInventoryMenu(Clone)") != null;

        if (inv.open || isContainerOpen)
            ForceCloseAll();
        else
            inv.Open(PlayerInstance.localPlayerInstance);
    }

    private void HandleInput(Vector2 inputPos)
    {
        if (UnityEngine.Application.isMobilePlatform)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

                Entity entity = GetEntityAtPosition(inputPos);
                Location loc = GetBlockedTouchLocation(touch.position);

                if (touch.phase == TouchPhase.Began)
                {
                    if (placeMode) { CMD_Interact(loc, 1, true); CMD_TryPlaceBlock(loc); return; }
                    if (entity != null) CMD_HitEntity(entity);
                    else CMD_Interact(loc, 0, true);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (!placeMode)
                    {
                        if (entity != null) CMD_HitEntity(entity);
                        else CMD_Interact(loc, 0, false);
                    }
                }
            }
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject()) return;

        Entity mouseEntity = GetEntityAtPosition(inputPos);
        Location mouseLoc = GetBlockedMouseLocation();

        if (Input.GetMouseButtonDown(0))
        {
            if (placeMode) { CMD_Interact(mouseLoc, 1, true); CMD_TryPlaceBlock(mouseLoc); return; }
            if (mouseEntity != null) CMD_HitEntity(mouseEntity);
            else CMD_Interact(mouseLoc, 0, true);
        }
        else if (Input.GetMouseButton(0))
        {
            if (!placeMode)
            {
                if (mouseEntity != null) CMD_HitEntity(mouseEntity);
                else CMD_Interact(mouseLoc, 0, false);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (mouseEntity != null) CMD_InteractEntity(mouseEntity);
            else CMD_Interact(mouseLoc, 1, true);
        }
    }

    [Command]
    public void CMD_HitEntity(Entity entity)
    {
        float damage = _player.GetInventoryHandler().GetInventory().GetSelectedItem().GetItemEntityDamage();
        Sound.Play(_player.Location, "entity/player/swing", SoundType.Entities, 0.8f, 1.2f);
        _player.ShakeOwnerCamera(.5f);
        DoToolDurability();
        entity.Hit(damage, _player);
        lastHitTime = NetworkTime.time;
    }

    [Command]
    public void CMD_InteractEntity(Entity entity)
    {
        entity.Interact(_player);
        lastHitTime = NetworkTime.time;
    }

    [Command]
    public void CMD_Interact(Location loc, int mouseButton, bool firstFrameDown)
    {
        string matName = _player.GetInventoryHandler().GetInventory().GetSelectedItem().material.ToString();
        Type itemType = Type.GetType(matName);
        if (itemType == null || !itemType.IsSubclassOf(typeof(Item))) itemType = typeof(Item);

        Item item = (Item)Activator.CreateInstance(itemType);
        item.Interact(_player.playerInstance, loc, mouseButton, firstFrameDown);
        lastBlockHitTime = NetworkTime.time;
    }

    [Command]
    public void CMD_TryPlaceBlock(Location loc)
    {
        ItemStack selectedItem = _player.GetInventoryHandler().GetInventory().GetSelectedItem();
        if (selectedItem.material == Material.Air || selectedItem.Amount < 1) return;

        Type blockType = Type.GetType(selectedItem.material.ToString());
        if (blockType == null) return;

        Block blockClass = (Block)Activator.CreateInstance(blockType);
        if (!blockClass.CanExistAt(loc)) return;

        Block currentBlock = loc.GetBlock();
        if (currentBlock != null && !currentBlock.CanBeOverriden) return;

        loc.SetMaterial(selectedItem.material);
        loc.GetBlock().BuildTick();
        loc.Tick();

        _player.GetInventoryHandler().GetInventory().ConsumeSelectedItem();
        lastBlockInteractionTime = NetworkTime.time;
    }

    [Server]
    public void DoToolDurability()
    {
        PlayerInventory inv = _player.GetInventoryHandler().GetInventory();
        ItemStack item = inv.GetSelectedItem();
        item.ApplyDurability();
        inv.SetItem(inv.selectedSlot, item);
    }

    public static Location GetBlockedTouchLocation(Vector2 pos) => Location.LocationByPosition(Camera.main.ScreenToWorldPoint(pos));
    public static Location GetBlockedMouseLocation() => Location.LocationByPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
    private Vector2 GetInputPosition() => (UnityEngine.Application.isMobilePlatform && Input.touchCount > 0) ? (Vector2)Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position) : (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

    public static Entity GetEntityAtPosition(Vector2 position)
    {
        foreach (RaycastHit2D ray in Physics2D.RaycastAll(position, Vector2.zero))
        {
            Entity entity = ray.transform.GetComponent<Entity>();
            if (entity) return entity;
        }
        return null;
    }

    public static bool CanInteractWithWorld()
    {
        if (PlayerInstance.localPlayerInstance == null) return true;
        return !Inventory.IsAnyOpen(PlayerInstance.localPlayerInstance) && GameObject.Find("ContainerInventoryMenu(Clone)") == null;
    }

    private void UpdateCrosshair()
    {
        if (crosshair == null) crosshair = Instantiate(Resources.Load<GameObject>("Prefabs/Crosshair"));
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool range = Vector2.Distance(mousePos, transform.position) <= Reach;
        string name = range ? (GetEntityAtPosition(mousePos) == null ? "full" : "entity") : "empty";
        crosshair.transform.position = GetBlockedMouseLocation().GetPosition();
        crosshair.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/crosshair_" + name);
    }

    private void OnDestroy()
    {
        if (crosshair != null) Destroy(crosshair);
        if (inventoryCloseButton != null) inventoryCloseButton.onClick.RemoveListener(ForceCloseAll);
    }
}