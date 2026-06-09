using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Purpose: Teleports an eye monster around the arena, and after every two player touches removes one configured target in sequence.
Attached GameObject: The eye monster object with its visual renderers and a trigger collider.
Main responsibilities: Choose valid spawn cells, fade in, detect player contact, relocate, and remove linked monsters in order.
Inputs: Shared arena settings from MonsterController, timing values, and an ordered list of target GameObjects to remove.
Outputs or effects: Moves this eye monster between grid cells without damaging the player and disables linked targets over time.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify the trigger collider size, fade timing, and ordered target removal flow in Play Mode.
*/

public class EyeMonster : MonoBehaviour
{
    [Header("区域设置")]
    [SerializeField] private MonsterController controller;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;

    [Header("出现规则")]
    [SerializeField] private int spawnDistanceInCells = 3;
    [SerializeField] private int maxSpawnDistanceInCells = 8;
    [SerializeField] private float fadeInDuration = 1f;

    [Header("碰撞反馈")]
    [SerializeField] private float blinkDurationOnTouch = 1f;
    [SerializeField] private int blinkCountOnTouch = 5;

    [Header("消除目标")]
    [SerializeField] private List<GameObject> targetsToRemove = new List<GameObject>();
    [SerializeField] private int touchesPerRemoval = 2;

    private Collider2D hitbox;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private bool isRelocating;
    private bool hasClearedAllTargets;
    private int touchCount;
    private int nextTargetIndex;

    public bool HasClearedAllTargets => hasClearedAllTargets;

    // Places the eye in the arena and keeps it visible from the start.
    private void Start()
    {
        ResolveController();
        ResolveComponents();
        MoveToNextSpawnPosition();
        StartCoroutine(FadeInRoutine());
    }

    // Detects the player stepping on the visible eye monster.
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandlePlayerContact(other);
    }

    // Keeps the eye responsive even if it becomes visible while the player is already overlapping it.
    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandlePlayerContact(other);
    }

    // Relocates the eye immediately when the player touches it.
    private void TryHandlePlayerContact(Collider2D other)
    {
        if (isRelocating || other == null)
        {
            return;
        }

        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerMove>() == null)
        {
            return;
        }

        isRelocating = true;
        StartCoroutine(BlinkAndRelocateRoutine());
    }

    // Plays a short blink, then hides and teleports the eye to its next legal position or disappears after removing the last target.
    private IEnumerator BlinkAndRelocateRoutine()
    {
        SetHitboxEnabled(false);

        float totalDuration = Mathf.Max(0.01f, blinkDurationOnTouch);
        int safeBlinkCount = Mathf.Max(1, blinkCountOnTouch);
        float stepDuration = totalDuration / (safeBlinkCount * 2f);

        for (int i = 0; i < safeBlinkCount; i++)
        {
            SetRendererAlpha(0f);
            yield return new WaitForSeconds(stepDuration);
            SetRendererAlpha(1f);
            yield return new WaitForSeconds(stepDuration);
        }

        SetVisibleImmediate(false);
        bool hasRemovedFinalTarget = TryAdvanceRemovalProgress();
        if (hasRemovedFinalTarget)
        {
            isRelocating = false;
            yield break;
        }

        MoveToNextSpawnPosition();
        yield return StartCoroutine(FadeInRoutine());
    }

    // Reveals the eye gradually after it spawns or relocates.
    private IEnumerator FadeInRoutine()
    {
        SetHitboxEnabled(false);

        float duration = Mathf.Max(0.01f, fadeInDuration);
        float elapsed = 0f;
        SetRendererAlpha(0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetRendererAlpha(t);
            yield return null;
        }

        SetRendererAlpha(1f);
        SetHitboxEnabled(true);
        isRelocating = false;
    }

    // Picks a valid cell on the same row or column, starting at the configured minimum distance away from the player.
    private void MoveToNextSpawnPosition()
    {
        ResolveController();
        if (controller == null)
        {
            return;
        }

        Vector2Int playerCell = controller.GetClosestCellToPlayer();
        int minDistance = Mathf.Max(1, spawnDistanceInCells);
        int maxDistance = Mathf.Max(minDistance, maxSpawnDistanceInCells);
        int maxCandidateCount = Mathf.Max(1, controller.Columns + controller.Rows) * 2;
        Vector2Int[] candidates = new Vector2Int[maxCandidateCount];
        int candidateCount = 0;

        for (int column = 0; column < controller.Columns; column++)
        {
            int horizontalDistance = Mathf.Abs(column - playerCell.x);
            if (horizontalDistance < minDistance || horizontalDistance > maxDistance)
            {
                continue;
            }

            TryAddCandidate(new Vector2Int(column, playerCell.y), candidates, ref candidateCount);
        }

        for (int row = 0; row < controller.Rows; row++)
        {
            int verticalDistance = Mathf.Abs(row - playerCell.y);
            if (verticalDistance < minDistance || verticalDistance > maxDistance)
            {
                continue;
            }

            TryAddCandidate(new Vector2Int(playerCell.x, row), candidates, ref candidateCount);
        }

        if (candidateCount == 0)
        {
            Vector2 arenaCenter = controller.GetArenaCenter();
            transform.position = new Vector3(arenaCenter.x + spawnOffset.x, arenaCenter.y + spawnOffset.y, transform.position.z);
            return;
        }

        Vector2Int chosenCell = candidates[Random.Range(0, candidateCount)];
        Vector2 worldCenter = controller.GetCellCenter(chosenCell.x, chosenCell.y);
        transform.position = new Vector3(worldCenter.x + spawnOffset.x, worldCenter.y + spawnOffset.y, transform.position.z);
    }

    // Adds one legal spawn candidate when it still fits inside the arena and is not already present.
    private void TryAddCandidate(Vector2Int cell, Vector2Int[] candidates, ref int candidateCount)
    {
        if (controller == null)
        {
            return;
        }

        if (cell.x < 0 || cell.x >= controller.Columns || cell.y < 0 || cell.y >= controller.Rows)
        {
            return;
        }

        for (int i = 0; i < candidateCount; i++)
        {
            if (candidates[i] == cell)
            {
                return;
            }
        }

        candidates[candidateCount] = cell;
        candidateCount++;
    }

    // Finds shared arena references and caches renderers/collider.
    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }
    }

    // Finds the visual and collision components used by the eye monster.
    private void ResolveComponents()
    {
        if (hitbox == null)
        {
            hitbox = GetComponentInChildren<Collider2D>(true);
        }

        if (hitbox is BoxCollider2D boxCollider)
        {
            boxCollider.isTrigger = true;
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            originalColors = null;
            return;
        }

        bool needsColorCache = originalColors == null || originalColors.Length != spriteRenderers.Length;
        if (!needsColorCache)
        {
            return;
        }

        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }
    }

    // Shows or hides the eye monster immediately without playing an animation.
    private void SetVisibleImmediate(bool visible)
    {
        SetHitboxEnabled(visible);
        SetRendererAlpha(visible ? 1f : 0f);
    }

    // Applies a shared alpha multiplier to every tracked sprite renderer.
    private void SetRendererAlpha(float alpha)
    {
        ResolveComponents();
        if (spriteRenderers == null || originalColors == null)
        {
            return;
        }

        float safeAlpha = Mathf.Clamp01(alpha);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            Color color = originalColors[i];
            color.a = originalColors[i].a * safeAlpha;
            spriteRenderers[i].color = color;
        }
    }

    // Enables contact detection only while the eye is active.
    private void SetHitboxEnabled(bool enabled)
    {
        ResolveComponents();
        if (hitbox != null)
        {
            hitbox.enabled = enabled;
        }
    }

    // Counts touches and removes the next configured target whenever the threshold is reached.
    private bool TryAdvanceRemovalProgress()
    {
        touchCount++;
        int requiredTouches = Mathf.Max(1, touchesPerRemoval);
        if (touchCount < requiredTouches)
        {
            return false;
        }

        touchCount = 0;

        while (nextTargetIndex < targetsToRemove.Count)
        {
            GameObject target = targetsToRemove[nextTargetIndex];
            nextTargetIndex++;

            if (target == null)
            {
                continue;
            }

            RemoveTarget(target);
            bool removedLastConfiguredTarget = !HasRemainingValidTargets();
            if (removedLastConfiguredTarget)
            {
                hasClearedAllTargets = true;
                gameObject.SetActive(false);
                return true;
            }

            return false;
        }

        return false;
    }

    // Disables one configured target safely, including known monster cleanup hooks.
    private void RemoveTarget(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        VineMonster vineMonster = target.GetComponent<VineMonster>();
        if (vineMonster == null)
        {
            vineMonster = target.GetComponentInChildren<VineMonster>(true);
        }

        if (vineMonster != null)
        {
            vineMonster.DeactivateMonster();
        }

        StalkerMonster stalkerMonster = target.GetComponent<StalkerMonster>();
        if (stalkerMonster == null)
        {
            stalkerMonster = target.GetComponentInChildren<StalkerMonster>(true);
        }

        if (stalkerMonster != null)
        {
            stalkerMonster.DeactivateMonster();
        }

        target.SetActive(false);
    }

    // Returns true when there is still another non-null target left to remove later.
    private bool HasRemainingValidTargets()
    {
        for (int i = nextTargetIndex; i < targetsToRemove.Count; i++)
        {
            if (targetsToRemove[i] != null)
            {
                return true;
            }
        }

        return false;
    }
}
