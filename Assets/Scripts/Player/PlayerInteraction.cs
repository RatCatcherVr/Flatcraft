using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;

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

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Start()
    {
        if (!isOwned) return;
        StartCoroutine(FindPlaceButton());
    }

    private IEnumerator FindPlaceButton()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject btnObj = GameObject.Find("PlaceButton");
        if (btnObj != null)
        {
            placeButton = btnObj.GetComponent<Button>();
            if (placeButton != null)
            {
                placeButton.onClick.RemoveAllListeners();
                placeButton.onClick.AddListener(TogglePlaceMode);
            }
        }
    }

    private void Update()
    {
        if (!isOwned) return;
        if (!CanInteractWithWorld()) return;

        UpdateCrosshair();

        Vector2 inputPos = GetInputPosition();
        float distance = Vector2.Distance(inputPos, transform.position);
        if (distance > Reach) return;

        HandleInput(inputPos);
    }

    public void TogglePlaceMode()
    {
        placeMode = !placeMode;
        UnityEngine.Debug.Log("Place Mode: " + placeMode);
    }

    private Vector2 GetInputPosition()
    {
        if (UnityEngine.Application.isMobilePlatform && Input.touchCount > 0)
            return Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
                    if (placeMode)
                    {
                        CMD_TryPlaceBlock(loc);
                        return;
                    }

                    if (entity != null)
                        CMD_HitEntity(entity);
                    else
                        CMD_Interact(loc, 0, true);
                }
            }

            return;
        }

        if (EventSystem.current.IsPointerOverGameObject()) return;

        Entity mouseEntity = GetEntityAtPosition(inputPos);
        Location mouseLoc = GetBlockedMouseLocation();

        if (Input.GetMouseButtonDown(0))
        {
            if (placeMode)
            {
                CMD_TryPlaceBlock(mouseLoc);
            }
            else if (mouseEntity != null)
            {
                CMD_HitEntity(mouseEntity);
            }
            else
            {
                CMD_Interact(mouseLoc, 0, true);
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (!placeMode)
            {
                if (mouseEntity != null)
                    CMD_HitEntity(mouseEntity);
                else
                    CMD_Interact(mouseLoc, 0, false);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (!placeMode)
            {
                if (mouseEntity != null)
                    CMD_InteractEntity(mouseEntity);
                else
                    CMD_Interact(mouseLoc, 1, true);
            }
        }
    }

    public static Location GetBlockedTouchLocation(Vector2 touchPosition)
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(touchPosition);
        return Location.LocationByPosition(worldPos);
    }

    public static Location GetBlockedMouseLocation()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return Location.LocationByPosition(mousePos);
    }

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
        if (Inventory.IsAnyOpen(PlayerInstance.localPlayerInstance)) return false;
        if (ChatMenu.instance.open) return false;
        if (SignEditMenu.IsLocalMenuOpen()) return false;
        if (PauseMenu.active) return false;
        return true;
    }

    [Server]
    public void DoToolDurability()
    {
        PlayerInventory inv = _player.GetInventoryHandler().GetInventory();
        ItemStack item = inv.GetSelectedItem();
        item.ApplyDurability();
        inv.SetItem(inv.selectedSlot, item);
    }

    [Command]
    public void CMD_HitEntity(Entity entity)
    {
        float damage = _player.GetInventoryHandler().GetInventory().GetSelectedItem().GetItemEntityDamage();
        if (_player.GetVelocity().y < -0.5f)
        {
            damage *= 1.5f;
            entity.GetComponent<EntityParticleEffects>()?.RPC_CriticalDamageEffect();
        }

        Sound.Play(_player.Location, "entity/player/swing", SoundType.Entities, 0.8f, 1.2f);
        _player.ShakeOwnerCamera(.5f);
        DoToolDurability();
        entity.transform.GetComponent<Entity>().Hit(damage, _player);
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
        Type itemType = Type.GetType(_player.GetInventoryHandler().GetInventory().GetSelectedItem().material.ToString());
        if (!itemType.IsSubclassOf(typeof(Item))) itemType = typeof(Item);

        Item item = (Item)Activator.CreateInstance(itemType);
        PlayerInstance player = _player.playerInstance;
        item.Interact(player, loc, mouseButton, firstFrameDown);
        lastBlockHitTime = NetworkTime.time;
    }

    [Command]
    public void CMD_TryPlaceBlock(Location loc)
    {
        ItemStack selectedItem = _player.GetInventoryHandler().GetInventory().GetSelectedItem();

        if (selectedItem.material == Material.Air) return;
        if (selectedItem.Amount < 1) return;

        Material materialToPlace = selectedItem.material;
        Type materialType = Type.GetType(selectedItem.material.ToString());

        if (materialType.IsSubclassOf(typeof(Item)))
            if (materialType.IsSubclassOf(typeof(PlaceableItem)))
                materialToPlace = ((PlaceableItem)Activator.CreateInstance(materialType)).blockMaterial;
            else
                return;

        Block blockClass = (Block)Activator.CreateInstance(Type.GetType(materialToPlace.ToString()));
        if (!blockClass.CanExistAt(loc)) return;

        Block currentBlock = loc.GetBlock();
        if (currentBlock != null && !currentBlock.CanBeOverriden) return;

        loc.SetMaterial(materialToPlace);
        loc.GetBlock().BuildTick();
        loc.Tick();

        _player.GetInventoryHandler().GetInventory().ConsumeSelectedItem();

        lastBlockInteractionTime = NetworkTime.time;
    }

    private void UpdateCrosshair()
    {
        if (crosshair == null)
            crosshair = Instantiate(Resources.Load<GameObject>("Prefabs/Crosshair"));

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool isInRange = Vector2.Distance(mousePosition, transform.position) <= Reach;
        Entity mouseEntity = GetEntityAtPosition(mousePosition);

        string spriteName = "empty";
        if (isInRange)
            spriteName = mouseEntity == null ? "full" : "entity";

        crosshair.transform.position = GetBlockedMouseLocation().GetPosition();
        crosshair.GetComponent<SpriteRenderer>().sprite =
            Resources.Load<Sprite>("Sprites/crosshair_" + spriteName);
    }

    private void OnDestroy()
    {
        Destroy(crosshair);
    }
}