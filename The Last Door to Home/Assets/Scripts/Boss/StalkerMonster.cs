using System.Collections;
using UnityEngine;

/*
Purpose: Controls a boss minion that spawns away from the player, periodically locks a target point, and dashes to it.
Attached GameObject: The enemy prefab or a scene enemy object with a Collider2D and optional Animator.
Main responsibilities: Choose a valid spawn cell, wait, snapshot the player's position, move in a straight line, pause, and repeat.
Inputs: Arena settings, player reference or auto-discovery, and timing or movement values from the Inspector.
Outputs or effects: Repositions and moves the enemy within the arena, and logs when it collides with the player.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify collider setup, Rigidbody2D trigger events, and arena coordinates in Play Mode.
*/

public class StalkerMonster : MonoBehaviour
{
    [Header("区域设置")]
    [SerializeField] private MonsterController controller;
    [SerializeField] private int minColumnDifference = 3;
    [SerializeField] private int minRowDifference = 3;

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
    }

    // Starts the movement loop automatically when requested.
    private void OnEnable()
    {
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
        PlaceAtRandomValidSpawn();

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

    // Runs the repeated delay, lock, dash, and wait sequence.
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

            Vector2Int targetCell = controller.GetClosestCellToPlayer();
            Vector2 targetPosition = controller.GetCellCenter(targetCell.x, targetCell.y);
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

    // Places the enemy on a random grid cell that is far enough from the player.
    private void PlaceAtRandomValidSpawn()
    {
        ResolveController();

        if (controller == null)
        {
            return;
        }

        int safeColumns = controller.Columns;
        int safeRows = controller.Rows;
        Vector2Int playerCell = controller.Player != null ? controller.GetClosestCellToPlayer() : controller.GetCenterCell();

        const int maxAttempts = 64;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int candidateCell = GetRandomCell(safeColumns, safeRows);
            if (IsSpawnCellValid(candidateCell, playerCell))
            {
                Vector2 candidatePosition = controller.GetCellCenter(candidateCell.x, candidateCell.y);
                transform.position = new Vector3(candidatePosition.x, candidatePosition.y, transform.position.z);
                return;
            }
        }

        Vector2 fallbackPosition = GetFallbackSpawnCellCenter(safeColumns, safeRows, playerCell);
        transform.position = new Vector3(fallbackPosition.x, fallbackPosition.y, transform.position.z);
    }

    // Finds the shared arena config when not explicitly assigned.
    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }
    }

    // Returns a random cell inside the configured arena.
    private Vector2Int GetRandomCell(int safeColumns, int safeRows)
    {
        int randomColumn = Random.Range(0, safeColumns);
        int randomRow = Random.Range(0, safeRows);
        return new Vector2Int(randomColumn, randomRow);
    }

    // Returns the cell center farthest from the player when random attempts fail.
    private Vector2 GetFallbackSpawnCellCenter(int safeColumns, int safeRows, Vector2Int playerCell)
    {
        Vector2 bestPosition = controller != null ? controller.GetArenaCenter() : Vector2.zero;
        float bestDistance = -1f;

        for (int rowIndex = 0; rowIndex < safeRows; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < safeColumns; columnIndex++)
            {
                Vector2Int candidateCell = new Vector2Int(columnIndex, rowIndex);
                if (!IsSpawnCellValid(candidateCell, playerCell))
                {
                    continue;
                }

                Vector2 candidate = controller.GetCellCenter(columnIndex, rowIndex);
                float distance = Vector2.Distance(candidate, controller.GetCellCenter(playerCell.x, playerCell.y));
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestPosition = candidate;
                }
            }
        }

        return bestPosition;
    }

    // Validates the requested spawn cell using separate column or row distance rules.
    private bool IsSpawnCellValid(Vector2Int candidateCell, Vector2Int playerCell)
    {
        int columnDifference = Mathf.Abs(candidateCell.x - playerCell.x);
        int rowDifference = Mathf.Abs(candidateCell.y - playerCell.y);
        return columnDifference >= Mathf.Max(0, minColumnDifference)
            || rowDifference >= Mathf.Max(0, minRowDifference);
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
