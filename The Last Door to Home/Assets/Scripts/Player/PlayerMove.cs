using UnityEngine;

/*
Purpose: Handles player movement input, facing animation, and footstep playback.
Attached GameObject: Player GameObject or a player-specific child object.
Main responsibilities: Read player-facing state, coordinate related components, and apply movement or presentation updates.
Inputs: Inspector references, Unity input, and state from linked gameplay managers.
Outputs or effects: Moves the player or camera, updates animations, and changes immediate gameplay feel.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class PlayerMove : MonoBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 2.5f;
    [Header("道具速度加成")]
    [Tooltip("拿到这个 PickableItem.itemUniqueID 后，把玩家速度改成下面的数值。留空则不启用。")]
    public string speedBoostItemUniqueID = "";
    public float speedBoostMoveSpeed = 3f;
    [Header("松键后动画缓冲时间（秒）")]
    public float stopFreezeDelay = 0.08f;
    [Header("脚步音效")]
    public AudioClip footstepClip;
    public float footstepInterval = 0.38f;
    [Range(0f, 1f)] public float footstepVolume = 0.55f;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 lastMoveDir = Vector2.down; // 默认朝下（正面）
    private Vector2 inputMoveDir = Vector2.zero;
    private bool isMoving;
    private float stopTimer;
    private float footstepTimer;
    private bool wasMovingThisFrame;

    public bool IsCurrentlyMoving => inputMoveDir.sqrMagnitude > 0.01f;

    // Prepares runtime state after the scene finishes its initial setup.
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        ApplyInventorySpeedEffect();
    }

    private void OnEnable()
    {
        Inventory.ItemCollected += HandleItemCollected;
        ApplyInventorySpeedEffect();
    }

    private void OnDisable()
    {
        Inventory.ItemCollected -= HandleItemCollected;
    }

    // Processes per-frame input and keeps this behaviour responsive during gameplay.
    void Update()
    {
        // 对话或系统菜单打开时，立即停止玩家移动并冻结朝向。
        if (EscapeMenuController.IsMenuOpen
            || InventoryMenuController.IsOpen
            || (DialogueManager.Instance != null && DialogueManager.Instance.IsPlayerControlLocked))
        {
            inputMoveDir = Vector2.zero;
            stopTimer = 0f;
            footstepTimer = 0f;
            wasMovingThisFrame = false;
            StopFootstepAudio();
            FreezeAtCurrentDirection();
            return;
        }

        // 输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 🔴 禁止斜走：优先上下，再左右
        inputMoveDir = Vector2.zero;

        if (Mathf.Abs(v) > 0.1f)
        {
            inputMoveDir = new Vector2(0, v);
        }
        else if (Mathf.Abs(h) > 0.1f)
        {
            inputMoveDir = new Vector2(h, 0);
        }

        wasMovingThisFrame = inputMoveDir.sqrMagnitude > 0.01f;

        if (inputMoveDir.sqrMagnitude > 0.01f)
        {
            // 有输入：更新朝向并正常播放动画
            lastMoveDir = inputMoveDir;
            anim.SetFloat("MoveX", inputMoveDir.x);
            anim.SetFloat("MoveY", inputMoveDir.y);
            anim.speed = 1f;
            stopTimer = stopFreezeDelay;

            // 让短按也能在本帧立刻评估方向切换，避免“点一下来不及转向”
            anim.Update(0f);
            isMoving = true;
            UpdateFootstepAudio();
        }
        else
        {
            // 无输入：先给一个很短的播放缓冲，避免短按只看到平移
            if (stopTimer > 0f)
            {
                stopTimer -= Time.deltaTime;
                anim.SetFloat("MoveX", lastMoveDir.x);
                anim.SetFloat("MoveY", lastMoveDir.y);
                anim.speed = 1f;
                StopFootstepAudio();
            }
            else
            {
                // 缓冲结束后，停在最后朝向的第1帧
                footstepTimer = 0f;
                StopFootstepAudio();
                FreezeAtCurrentDirection();
            }
        }
    }

    // Applies physics-driven updates on the fixed timestep.
    void FixedUpdate()
    {
        rb.velocity = inputMoveDir * moveSpeed;
    }

    // Immediately cancels movement and freezes the player in the current facing direction.
    public void ForceStopImmediate()
    {
        inputMoveDir = Vector2.zero;
        stopTimer = 0f;
        footstepTimer = 0f;
        wasMovingThisFrame = false;
        StopFootstepAudio();
        FreezeAtCurrentDirection();
    }

    // Updates timed footstep playback while the player is moving.
    private void UpdateFootstepAudio()
    {
        if (footstepClip == null || !wasMovingThisFrame)
        {
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f) return;

        AudioManager.EnsureInstance().PlayFootstep(footstepClip, footstepVolume);
        footstepTimer = Mathf.Max(0.05f, footstepInterval);
    }

    // Stops the active footstep loop or one-shot playback when movement ends.
    private void StopFootstepAudio()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.StopFootstep();
    }

    // Freezes movement while preserving the current facing animation frame.
    private void FreezeAtCurrentDirection()
    {
        rb.velocity = Vector2.zero;
        anim.SetFloat("MoveX", lastMoveDir.x);
        anim.SetFloat("MoveY", lastMoveDir.y);

        // 仅在“移动 -> 静止”切换时重置到首帧，避免每帧强制重置
        if (isMoving)
        {
            int stateHash;
            if (anim.IsInTransition(0))
            {
                // 如果正在切状态，优先锁定到目标状态的第1帧
                stateHash = anim.GetNextAnimatorStateInfo(0).fullPathHash;
            }
            else
            {
                stateHash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            }

            anim.Play(stateHash, 0, 0f);
        }

        anim.speed = 0f;
        isMoving = false;
    }

    private void HandleItemCollected(string uniqueID)
    {
        if (string.IsNullOrWhiteSpace(speedBoostItemUniqueID)) return;
        if (uniqueID != speedBoostItemUniqueID) return;

        moveSpeed = speedBoostMoveSpeed;
    }

    private void ApplyInventorySpeedEffect()
    {
        if (string.IsNullOrWhiteSpace(speedBoostItemUniqueID)) return;
        if (!Inventory.HasCollected(speedBoostItemUniqueID)) return;

        moveSpeed = speedBoostMoveSpeed;
    }
}
