using UnityEngine;
/*
Purpose: Handles player movement input, facing animation, speed modifiers, and footstep playback.
Attached GameObject: Player GameObject with Rigidbody2D and Animator components.
Main responsibilities: Reads movement input, prevents diagonal movement, updates animation parameters, applies movement physics, and stops movement when gameplay is locked.
Inputs: Horizontal/Vertical input axes, inventory speed item state, dialogue/menu lock state, and footstep audio settings.
Outputs or effects: Rigidbody2D velocity, Animator state, footstep audio playback, and the public current-movement state.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify movement in all directions, lock behavior during menus/dialogue, speed item pickup, and footstep timing.
*/

public class PlayerMove : MonoBehaviour
{
    [Header("移动速度 / Movement Speed")]
    public float moveSpeed = 2.5f;
    [Header("道具速度加成 / Item Speed Boost")]
    [Tooltip("拿到这个 PickableItem.itemUniqueID 后，把玩家速度改成下面的数值。留空则不启用。")]
    public string speedBoostItemUniqueID = "";
    public float speedBoostMoveSpeed = 3f;
    [Header("松键后动画缓冲时间（秒） / Animation Buffer After Release (Seconds)")]
    public float stopFreezeDelay = 0.08f;
    [Header("脚步音效 / Footstep Audio")]
    public AudioClip footstepClip;
    public float footstepInterval = 0.38f;
    [Range(0f, 1f)] public float footstepVolume = 0.55f;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 lastMoveDir = Vector2.down; // Default facing direction is down/front.
    private Vector2 inputMoveDir = Vector2.zero;
    private bool isMoving;
    private float stopTimer;
    private float footstepTimer;
    private bool wasMovingThisFrame;

    public bool IsCurrentlyMoving => inputMoveDir.sqrMagnitude > 0.01f;

    // Prepares runtime state after the scene has finished its initial setup.
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        ApplyInventorySpeedEffect();
    }

    // Registers callbacks or resets transient state when the component becomes active.
    private void OnEnable()
    {
        Inventory.ItemCollected += HandleItemCollected;
        ApplyInventorySpeedEffect();
    }

    // Unregisters callbacks when the component becomes inactive.
    private void OnDisable()
    {
        Inventory.ItemCollected -= HandleItemCollected;
    }

    // Reads per-frame input and updates frame-dependent runtime state.
    void Update()
    {
        // Stop movement immediately while dialogue or a system menu owns player input.
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

        // Read raw movement input.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Disallow diagonal movement; vertical input has priority over horizontal input.
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
            // Update facing direction and play the movement animation while input is active.
            lastMoveDir = inputMoveDir;
            anim.SetFloat("MoveX", inputMoveDir.x);
            anim.SetFloat("MoveY", inputMoveDir.y);
            anim.speed = 1f;
            stopTimer = stopFreezeDelay;

            // Evaluate direction changes immediately so quick taps still turn the player.
            anim.Update(0f);
            isMoving = true;
            UpdateFootstepAudio();
        }
        else
        {
            // Keep a short animation buffer after input stops so quick taps remain visible.
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
                // After the buffer ends, freeze on the first frame of the last facing direction.
                footstepTimer = 0f;
                StopFootstepAudio();
                FreezeAtCurrentDirection();
            }
        }
    }

    // Applies physics-related updates on Unity's fixed timestep.
    void FixedUpdate()
    {
        rb.velocity = inputMoveDir * moveSpeed;
    }

    // Handles the force stop immediate step for this script.
    public void ForceStopImmediate()
    {
        inputMoveDir = Vector2.zero;
        stopTimer = 0f;
        footstepTimer = 0f;
        wasMovingThisFrame = false;
        StopFootstepAudio();
        FreezeAtCurrentDirection();
    }

    // Handles the update footstep audio step for this script.
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

    // Stops the stop footstep audio sequence or runtime effect.
    private void StopFootstepAudio()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.StopFootstep();
    }

    // Handles the freeze at current direction step for this script.
    private void FreezeAtCurrentDirection()
    {
        rb.velocity = Vector2.zero;
        anim.SetFloat("MoveX", lastMoveDir.x);
        anim.SetFloat("MoveY", lastMoveDir.y);

        // Reset to the first frame only when transitioning from moving to idle.
        if (isMoving)
        {
            int stateHash;
            if (anim.IsInTransition(0))
            {
                // If the animator is transitioning, lock to the first frame of the next state.
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

    // Handles the event or callback associated with this method.
    private void HandleItemCollected(string uniqueID)
    {
        if (string.IsNullOrWhiteSpace(speedBoostItemUniqueID)) return;
        if (uniqueID != speedBoostItemUniqueID) return;

        moveSpeed = speedBoostMoveSpeed;
    }

    // Applies the requested visual, audio, or gameplay state.
    private void ApplyInventorySpeedEffect()
    {
        if (string.IsNullOrWhiteSpace(speedBoostItemUniqueID)) return;
        if (!Inventory.HasCollected(speedBoostItemUniqueID)) return;

        moveSpeed = speedBoostMoveSpeed;
    }
}
