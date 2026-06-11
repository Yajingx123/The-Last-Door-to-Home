using System.Collections;
using UnityEngine;
/*
Purpose: Controls boss, enemy, damage, or boss-ending behavior.
Attached GameObject: Boss/enemy GameObject, damage hitbox, or boss-scene controller.
Main responsibilities: Updates combat movement/state, resolves contact damage, handles defeat, and triggers ending or door behavior.
Inputs: Player position, colliders, serialized combat settings, health/progression state, and scene triggers.
Outputs or effects: Moves enemies, applies damage, updates animations, changes story/ending state, or loads scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify combat states, damage timing, defeat conditions, and ending transitions.
*/

public class StalkerMonster : MonoBehaviour
{
    [Header("道具速度变化 / Item Speed Change")]
    [Tooltip("拿到这个 PickableItem.itemUniqueID 后，把 Stalker 速度改成下面的数值。留空则不启用。")]
    [SerializeField] private string speedBoostItemUniqueID = "";
    [SerializeField] private float speedBoostMoveSpeed = 7f;

    [Header("区域设置 / Area Settings")]
    [SerializeField] private MonsterController controller;

    [Header("行为 / Behavior")]
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

    // Initializes component references and singleton ownership before Start runs.
    private void Awake()
    {
        initialPosition = transform.position;
        hitbox = GetComponentInChildren<Collider2D>(true);
        renderersToToggle = GetComponentsInChildren<Renderer>(true);
        ApplyInventorySpeedEffect();
    }

    // Registers callbacks or resets transient state when the component becomes active.
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

    // Unregisters callbacks when the component becomes inactive.
    private void OnDisable()
    {
        Inventory.ItemCollected -= HandleItemCollected;

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }
    }

    // Executes the action associated with the current selection.
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

    // Handles the deactivate monster step for this script.
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

    // Handles the enable idle damage step for this script.
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

    // Handles the behavior routine step for this script.
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

    // Moves the current selection or object in the requested direction.
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

    // Returns the requested value or runtime object.
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

    // Resolves the best available value for the requested data.
    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }
    }

    // Handles 2D trigger entry events for this object.
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

    // Updates the requested value or component state.
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

    // Draws editor-only debug helpers while this object is selected.
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
