using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerMovement : NetworkBehaviour
{
    private const float SneakSpeed = 1.5f;
    private const float SprintSpeed = 5.6f;

    [SyncVar] public bool sprinting;
    [SyncVar] public bool sneaking;

    private bool _ladderSneakingLastFrame;

    private Player _player;
    private EntityMovement _movement;

    public Joystick joystick;

    private bool _isButtonSprinting;
    private bool _isButtonSneaking;

    private void Start()
    {
        _player = GetComponent<Player>();
        _movement = GetComponent<EntityMovement>();

        if (!isOwned) return;

        StartCoroutine(SetupMobile());
    }

    private IEnumerator SetupMobile()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject stickObj = GameObject.Find("Stick");
        if (stickObj != null)
            joystick = stickObj.GetComponent<Joystick>();

        GameObject sprintObj = GameObject.Find("SprintButton");
        if (sprintObj != null)
            AddTrigger(sprintObj, SetMobileSprint);

        GameObject crouchObj = GameObject.Find("CrouchButton");
        if (crouchObj != null)
            AddTrigger(crouchObj, SetMobileSneak);
    }

    private void Update()
    {
        if (!isOwned) return;

        HandleSpeedState();
        PerformInput();
        CrouchOnLadderCheck();
    }

    private void HandleSpeedState()
    {
        if (_movement == null) return;

        if (sprinting)
            _movement.speed = SprintSpeed;
        else if (sneaking)
            _movement.speed = SneakSpeed;
        else
            _movement.speed = _movement.walkSpeed;
    }

    private void PerformInput()
    {
        if (!PlayerInteraction.CanInteractWithWorld()) return;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space) || (joystick != null && joystick.Vertical() > 0.5f))
            _movement.Jump();

        WalkInput();
        SprintInput();
        SneakInput();
    }

    private void WalkInput()
    {
        float input = joystick != null ? joystick.Horizontal() : 0f;
        int direction = 0;

        if (input < -0.3f) direction = -1;
        if (input > 0.3f) direction = 1;

        if (Input.GetKey(KeyCode.A)) direction = -1;
        if (Input.GetKey(KeyCode.D)) direction = 1;

        if (direction == 0) return;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (sneaking && WillFallInDirection(direction)) { rb.velocity = new Vector2(0, rb.velocity.y); return; }

        _movement.Walk(direction);
    }

    private void SprintInput()
    {
        if (_isButtonSprinting && _player.hunger > 6 && !sneaking)
        {
            sprinting = true;
            return;
        }

        if (!_isButtonSprinting && !Input.GetKey(KeyCode.LeftControl))
            sprinting = false;
    }

    private void SneakInput()
    {
        sneaking = _isButtonSneaking || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.S);
    }

    public void SetMobileSprint(bool holding)
    {
        _isButtonSprinting = holding;
    }

    public void SetMobileSneak(bool holding)
    {
        _isButtonSneaking = holding;
    }

    private bool WillFallInDirection(int direction)
    {
        if (!_player.isOnGround) return false;
        Block nextGroundBlock = Location.LocationByPosition(transform.position + new Vector3(direction * .1f, -.6f)).GetBlock();
        return (!nextGroundBlock || !nextGroundBlock.IsSolid);
    }

    private void CrouchOnLadderCheck()
    {
        bool isLadderSneaking = _player.isOnClimbable && sneaking;

        if (isLadderSneaking && !_ladderSneakingLastFrame)
        {
            GetComponent<Rigidbody2D>().gravityScale = 0;
            _ladderSneakingLastFrame = true;
        }

        if (!isLadderSneaking && _ladderSneakingLastFrame)
        {
            GetComponent<Rigidbody2D>().gravityScale = 1;
            _ladderSneakingLastFrame = false;
        }
    }

    private void AddTrigger(GameObject obj, System.Action<bool> action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((e) => action(true));
        trigger.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener((e) => action(false));
        trigger.triggers.Add(up);
    }
}