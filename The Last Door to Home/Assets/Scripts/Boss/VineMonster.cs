using System;
using System.Collections;
using UnityEngine;

/*
Purpose: Moves one vine monster through rows in a looping attack pattern.
Attached GameObject: The vine monster object that owns the animation and hitbox.
Main responsibilities: Reposition the vine by row, play its animation, enable or disable damage windows, and repeat the pattern.
Inputs: Inspector references for the shared arena controller and timing values.
Outputs or effects: Reuses this vine object itself instead of spawning duplicate prefabs.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify the arena origin, cell size, object pivot, and hitbox timing in Play Mode.
*/

public class VineMonster : MonoBehaviour
{
    public event Action<VineMonster> RoundCompleted;

    private enum HorizontalAlignment
    {
        Center,
        LeftEdge,
        RightEdge
    }

    private enum VerticalPattern
    {
        TopToBottomThenBack,
        BottomToTopThenBack
    }

    [Header("区域设置")]
    [SerializeField] private bool useArenaSettings = true;
    [SerializeField] private MonsterController controller;
    [SerializeField] private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
    [SerializeField] private VerticalPattern verticalPattern = VerticalPattern.TopToBottomThenBack;
    [Tooltip("基于对齐点额外补的世界坐标偏移。适合微调素材锚点。")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;

    [Header("时序")]
    [SerializeField] private float rowVisibleDuration = 0.7f;
    [SerializeField] private float gapBetweenRows = 0.35f;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool loopForever = true;

    private Coroutine patternRoutine;
    private Animator animator;
    private Collider2D hitbox;
    private Renderer[] renderersToToggle;
    private bool isAttackEnabled;

    // Starts the configured pattern automatically when requested.
    private void Start()
    {
        ResolveController();
        ResolveComponents();
        SetActiveState(false);

        if (playOnStart)
        {
            ActivateMonster();
        }
    }

    // Starts the row pattern routine.
    public void ActivateMonster()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ResolveController();
        isAttackEnabled = true;

        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
        }

        patternRoutine = StartCoroutine(useArenaSettings ? PlayFirstPatternRoutine() : PlayStaticPatternRoutine());
    }

    // Stops the active pattern immediately.
    public void DeactivateMonster()
    {
        isAttackEnabled = false;

        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }

        SetActiveState(false);
    }

    // Spawns one vine row at a time and loops from top-to-bottom and back again.
    private IEnumerator PlayFirstPatternRoutine()
    {
        if (!TryGetArenaValues(out Vector2 topLeftPosition, out int safeColumns, out int safeRows, out float safeCellSize))
        {
            Debug.LogWarning("VineMonster 找不到 MonsterController 配置。", this);
            yield break;
        }

        float safeVisibleDuration = Mathf.Max(0.01f, rowVisibleDuration);
        float safeGap = Mathf.Max(0f, gapBetweenRows);
        float arenaWidth = safeColumns * safeCellSize;

        do
        {
            if (verticalPattern == VerticalPattern.TopToBottomThenBack)
            {
                for (int rowIndex = 0; rowIndex < safeRows; rowIndex++)
                {
                    yield return PlayRow(rowIndex, topLeftPosition, arenaWidth, safeCellSize, safeVisibleDuration, safeGap);
                }

                for (int rowIndex = safeRows - 2; rowIndex >= 0; rowIndex--)
                {
                    yield return PlayRow(rowIndex, topLeftPosition, arenaWidth, safeCellSize, safeVisibleDuration, safeGap);
                }
            }
            else
            {
                for (int rowIndex = safeRows - 1; rowIndex >= 0; rowIndex--)
                {
                    yield return PlayRow(rowIndex, topLeftPosition, arenaWidth, safeCellSize, safeVisibleDuration, safeGap);
                }

                for (int rowIndex = 1; rowIndex < safeRows; rowIndex++)
                {
                    yield return PlayRow(rowIndex, topLeftPosition, arenaWidth, safeCellSize, safeVisibleDuration, safeGap);
                }
            }

            RoundCompleted?.Invoke(this);
        }
        while (loopForever && isAttackEnabled);

        patternRoutine = null;
    }

    // Repeats the same visible and gap timing without moving the vine away from its placed position.
    private IEnumerator PlayStaticPatternRoutine()
    {
        float safeVisibleDuration = Mathf.Max(0.01f, rowVisibleDuration);
        float safeGap = Mathf.Max(0f, gapBetweenRows);

        do
        {
            RestartAnimation(animator);
            SetActiveState(true);
            yield return new WaitForSeconds(safeVisibleDuration);
            SetActiveState(false);

            if (safeGap > 0f)
            {
                yield return new WaitForSeconds(safeGap);
            }

            RoundCompleted?.Invoke(this);
        }
        while (loopForever && isAttackEnabled);

        patternRoutine = null;
    }

    // Spawns one row, waits for its lifetime, then waits for the configured gap.
    private IEnumerator PlayRow(int rowIndex, Vector2 topLeftPosition, float arenaWidth, float safeCellSize, float safeVisibleDuration, float safeGap)
    {
        transform.position = GetSpawnPosition(topLeftPosition, rowIndex, arenaWidth, safeCellSize);
        RestartAnimation(animator);
        SetActiveState(true);

        bool hasLoggedHit = false;
        float elapsed = 0f;
        while (elapsed < safeVisibleDuration)
        {
            if (!hasLoggedHit && isAttackEnabled && controller != null && controller.IsPlayerOverlapping(hitbox))
            {
                controller.TryDamagePlayer("VineMonster", this);
                hasLoggedHit = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetActiveState(false);

        if (safeGap > 0f)
        {
            yield return new WaitForSeconds(safeGap);
        }
    }

    // Calculates the spawn point for one row based on the chosen alignment and anchor.
    private Vector3 GetSpawnPosition(Vector2 topLeftPosition, int rowIndex, float arenaWidth, float safeCellSize)
    {
        Vector3 topLeft = new Vector3(topLeftPosition.x, topLeftPosition.y, transform.position.z);
        float spawnX = topLeft.x;

        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Center:
                spawnX = topLeft.x + arenaWidth * 0.5f;
                break;
            case HorizontalAlignment.LeftEdge:
                spawnX = topLeft.x;
                break;
            case HorizontalAlignment.RightEdge:
                spawnX = topLeft.x + arenaWidth;
                break;
        }

        float spawnY = topLeft.y - safeCellSize * (rowIndex + 0.5f);
        return new Vector3(spawnX + spawnOffset.x, spawnY + spawnOffset.y, topLeft.z);
    }

    // Finds the shared arena config when not explicitly assigned.
    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }
    }

    // Finds this monster's own visual and collision components when not explicitly assigned.
    private void ResolveComponents()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (hitbox == null)
        {
            hitbox = GetComponentInChildren<Collider2D>(true);
        }

        if (renderersToToggle == null || renderersToToggle.Length == 0)
        {
            renderersToToggle = GetComponentsInChildren<Renderer>(true);
        }

        if (hitbox is BoxCollider2D boxCollider)
        {
            boxCollider.isTrigger = true;
        }
    }

    // Reads shared arena values from the scene config.
    private bool TryGetArenaValues(out Vector2 topLeftPosition, out int safeColumns, out int safeRows, out float safeCellSize)
    {
        if (!useArenaSettings)
        {
            topLeftPosition = Vector2.zero;
            safeColumns = 1;
            safeRows = 1;
            safeCellSize = 1f;
            return false;
        }

        ResolveController();

        if (controller == null)
        {
            topLeftPosition = Vector2.zero;
            safeColumns = 1;
            safeRows = 1;
            safeCellSize = 1f;
            return false;
        }

        topLeftPosition = controller.TopLeftPosition;
        safeColumns = controller.Columns;
        safeRows = controller.Rows;
        safeCellSize = controller.CellSize;
        return true;
    }

    // Restarts the spawned vine animation from its first frame.
    private void RestartAnimation(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);
    }

    // Shows or hides this vine monster's visuals and hitbox based on whether it is currently attacking.
    private void SetActiveState(bool isActive)
    {
        ResolveComponents();

        if (hitbox != null)
        {
            hitbox.enabled = isActive;
        }

        if (renderersToToggle != null)
        {
            for (int i = 0; i < renderersToToggle.Length; i++)
            {
                if (renderersToToggle[i] != null)
                {
                    renderersToToggle[i].enabled = isActive;
                }
            }
        }

        if (animator != null && !isActive)
        {
            animator.enabled = false;
        }
    }

    // Lets vines without arena movement enabled still damage the player while staying in place.
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayerOnContact(other);
    }

    // Keeps stationary vines hazardous while the player remains inside the trigger.
    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayerOnContact(other);
    }

    // Applies contact damage for static vines that do not use arena row movement.
    private void TryDamagePlayerOnContact(Collider2D other)
    {
        if (useArenaSettings || !isAttackEnabled)
        {
            return;
        }

        ResolveController();
        PlayerMove playerMove = other.GetComponentInParent<PlayerMove>();
        if (playerMove == null || controller == null)
        {
            return;
        }

        controller.TryDamagePlayer("VineMonster", this);
    }

    // Draws the configured arena bounds in the editor for easier placement.
    private void OnDrawGizmosSelected()
    {
        if (!useArenaSettings)
        {
            return;
        }

        if (!TryGetArenaValues(out Vector2 topLeftPosition, out int safeColumns, out int safeRows, out float safeCellSize))
        {
            return;
        }

        float width = safeColumns * safeCellSize;
        float height = safeRows * safeCellSize;
        Vector3 topLeft = new Vector3(topLeftPosition.x, topLeftPosition.y, transform.position.z);
        Vector3 center = new Vector3(topLeft.x + width * 0.5f, topLeft.y - height * 0.5f, topLeft.z);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0.05f));
    }
}
