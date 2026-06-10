using System.Collections;
using UnityEngine;

/*
Purpose: Controls a boss minion that periodically locks the player's position and slides straight toward it.
Attached GameObject: The enemy prefab or a scene enemy object with a Collider2D and optional Animator.
Main responsibilities: Wait, snapshot the player's position, move in a straight line, pause, and repeat.
Inputs: Arena settings, player reference or auto-discovery, and timing or movement values from the Inspector.
Outputs or effects: Repositions and moves the enemy within the arena, and logs when it collides with the player.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify collider setup, Rigidbody2D trigger events, and arena coordinates in Play Mode.
*/

public class StalkerMonster : MonoBehaviour
{
    [Header("道具速度变化")]
    [Tooltip("拿到这个 PickableItem.itemUniqueID 后，把 Stalker 速度改成下面的数值。留空则不启用。")]
    [SerializeField] private string speedBoostItemUniqueID = "";
    [SerializeField] private float speedBoostMoveSpeed = 7f;

    [Header("区域设置")]
    [SerializeField] private MonsterController controller;

    [Header("行为")]
    [SerializeField] private float lockDelay = 3f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stayDuration = 2f;
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool visibleWhenIdle = true;

    private Coroutine behaviorRoutine;
    private Collider2D hitbox;
    private Renderer[] renderersToToggle;
    private Vector3 initialPosition;
    private bool isAttackEnabled;

    // Starts the movement loop automatically when requested.
    private void Awake()
    {
        initialPosition = transform.position;
        hitbox = GetComponentInChildren<Collider2D>(true);
        renderersToToggle = GetComponentsInChildren<Renderer>(true);
        ApplyInventorySpeedEffect();
    }

    // Starts the movement loop automatically when requested.
    private void OnEnable()
    {
        Inventory.ItemCollected += HandleItemCollected;
        ApplyInventorySpeedEffect();

        if (startOnEnable)
        {
            ActivateMonster();
        }
        else
        {
            DeactivateMonster();
        }
    }

    // Stops active routines when the object is disabled.
    private void OnDisable()
    {
        Inventory.ItemCollected -= HandleItemCollected;

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }
    }

    // Begins the full spawn-and-chase loop.
    public void ActivateMonster()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ResolveController();
        isAttackEnabled = true;
        SetIdleVisualState(true);

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
        }

        behaviorRoutine = StartCoroutine(BehaviorRoutine());
    }

    // Stops attacking and returns this monster to its placed idle state.
    public void DeactivateMonster()
    {
        isAttackEnabled = false;

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }

        transform.position = initialPosition;

        if (hitbox != null)
        {
            hitbox.enabled = false;
        }

        SetIdleVisualState(visibleWhenIdle);
    }

    // Keeps the monster stationary at its placed idle point while leaving contact damage active.
    public void EnableIdleDamage()
    {
        isAttackEnabled = false;

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }

        transform.position = initialPosition;

        if (hitbox != null)
        {
            hitbox.enabled = true;
        }

        SetIdleVisualState(visibleWhenIdle);
    }

    // Runs the repeated delay, lock, slide, and wait sequence.
    private IEnumerator BehaviorRoutine()
    {
        if (hitbox != null)
        {
            hitbox.enabled = true;
        }

        while (isAttackEnabled)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, lockDelay));

            ResolveController();
            if (controller == null || controller.Player == null)
            {
                Debug.LogWarning("StalkerMonster 找不到 MonsterController 或玩家引用。", this);
                yield return null;
                continue;
            }

            Vector2 targetPosition = GetLockedPlayerPosition();
            yield return MoveToTarget(targetPosition);
            yield return new WaitForSeconds(Mathf.Max(0f, stayDuration));
        }
    }

    // Moves in a straight line toward the locked target point.
    private IEnumerator MoveToTarget(Vector2 targetPosition)
    {
        float safeSpeed = Mathf.Max(0.01f, moveSpeed);

        while ((targetPosition - (Vector2)transform.position).sqrMagnitude > 0.001f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, safeSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
    }

    // Locks the player's current grid position before the monster starts moving.
    private Vector2 GetLockedPlayerPosition()
    {
        ResolveController();
        if (controller == null || controller.Player == null)
        {
            return transform.position;
        }

        Vector2Int targetCell = controller.GetClosestCellToPlayer();
        return controller.GetCellCenter(targetCell.x, targetCell.y);
    }

    // Finds the shared arena config when not explicitly assigned.
    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }
    }

    // Applies damage when this monster collides with the player.
    private void OnTriggerEnter2D(Collider2D other)
    {
        ResolveController();
        PlayerMove playerMove = other.GetComponentInParent<PlayerMove>();
        if (playerMove == null || controller == null)
        {
            return;
        }

        controller.TryDamagePlayer("StalkerMonster", this);
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

    // Shows or hides the stalker visuals without changing object activation.
    private void SetIdleVisualState(bool isVisible)
    {
        if (renderersToToggle == null)
        {
            return;
        }

        for (int i = 0; i < renderersToToggle.Length; i++)
        {
            if (renderersToToggle[i] != null)
            {
                renderersToToggle[i].enabled = isVisible;
            }
        }
    }

    // Draws the arena bounds in the editor for easier placement.
    private void OnDrawGizmosSelected()
    {
        ResolveController();
        if (controller == null)
        {
            return;
        }

        int safeColumns = controller.Columns;
        int safeRows = controller.Rows;
        float safeCellSize = controller.CellSize;

        float width = safeColumns * safeCellSize;
        float height = safeRows * safeCellSize;
        Vector3 center = new Vector3(
            controller.TopLeftPosition.x + width * 0.5f,
            controller.TopLeftPosition.y - height * 0.5f,
            transform.position.z);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0.05f));
    }
}
