using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 Purpose: Stores shared boss arena data and manages common player combat feedback for monster encounters.
Attached GameObject: A single scene object that represents the shared monster arena controller.
 Main responsibilities: Provide arena dimensions, resolve the player, convert between world positions and grid cells, manage battle hearts, and handle hit feedback.
 Inputs: Inspector values for arena layout, player reference, heart UI settings, and optional ending sequence reference.
 Outputs or effects: Centralizes shared boss combat setup so individual monster scripts stay focused on behavior.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify the arena coordinates, player reference, and overlap checks in Play Mode.
*/

public class MonsterController : MonoBehaviour
{
    [Header("玩家")]
    [SerializeField] private Transform player;

    [Header("区域设置")]
    [Tooltip("战斗区域左上角格子的左上角世界坐标。")]
    [SerializeField] private Vector2 topLeftPosition;
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 6;
    [SerializeField] private float cellSize = 1f;

    [Header("受伤设置")]
    [SerializeField] private int maxHearts = 8;
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private float flickerInterval = 0.1f;
    [SerializeField] private float flickerAlpha = 0.25f;

    [Header("爱心UI")]
    [SerializeField] private Vector2 heartsUiOffset = new Vector2(36f, -36f);
    [SerializeField] private Vector2 heartIconSize = new Vector2(36f, 36f);
    [SerializeField] private float heartSpacing = 8f;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Color fullHeartColor = Color.white;

    [Header("生命归零")]
    [SerializeField] private MonsterEndingSequence endingSequence;

    [Header("怪物回合配置")]
    [SerializeField] private VineMonster primaryVineMonster;
    [SerializeField] private StalkerMonster stalkerMonster;
    [SerializeField] private VineMonster secondaryVineMonster;
    [SerializeField] private float stalkerOpeningIdleDuration = 20f;

    private int currentHearts;
    private bool isInvulnerable;
    private bool isDeathSequenceActive;
    private Coroutine invulnerabilityRoutine;
    private SpriteRenderer[] playerRenderers;
    private Color[] originalRendererColors;
    private GameObject heartsUiObject;
    private Image[] heartImages;
    private Coroutine openingStalkerRoutine;

    public Transform Player
    {
        get
        {
            ResolvePlayerReference();
            return player;
        }
    }

    public Vector2 TopLeftPosition => topLeftPosition;
    public int Columns => Mathf.Max(1, columns);
    public int Rows => Mathf.Max(1, rows);
    public float CellSize => Mathf.Max(0.01f, cellSize);

    // Initializes player combat state as early as possible for monster startup order safety.
    private void Awake()
    {
        currentHearts = Mathf.Max(1, maxHearts);
    }

    // Prepares heart state and cached player visuals before gameplay begins.
    private void Start()
    {
        ResolvePlayerReference();
        ResolveEndingSequence();
        CachePlayerRenderers();
        EnsureHeartUI();
        RefreshHeartUI();
        SetupBattlePhases();
    }

    // Finds the player automatically when no explicit reference is assigned.
    public void ResolvePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        PlayerMove playerMove = FindObjectOfType<PlayerMove>();
        if (playerMove != null)
        {
            player = playerMove.transform;
        }
    }

    // Returns true when the player can currently be hurt by monsters.
    public bool CanPlayerTakeDamage => !isInvulnerable && !isDeathSequenceActive && Player != null;

    // Applies one monster hit to the player and starts the proper feedback flow.
    public bool TryDamagePlayer(string sourceName, Object damageSource = null)
    {
        ResolvePlayerReference();
        if (isDeathSequenceActive || isInvulnerable || Player == null)
        {
            return false;
        }

        CachePlayerRenderers();
        currentHearts = Mathf.Max(0, currentHearts - 1);
        RefreshHeartUI();
        Debug.Log($"{sourceName} hit player. Hearts remaining: {currentHearts}", damageSource != null ? damageSource : this);

        if (currentHearts <= 0)
        {
            if (invulnerabilityRoutine != null)
            {
                StopCoroutine(invulnerabilityRoutine);
                invulnerabilityRoutine = null;
            }

            RestorePlayerVisualAlpha(1f);
            BeginDeathSequence();
            return true;
        }

        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
        }

        invulnerabilityRoutine = StartCoroutine(PlayerInvulnerabilityRoutine());
        return true;
    }

    // Sets up the opening monster states so the stalker can wait briefly before joining the fight.
    private void SetupBattlePhases()
    {
        if (primaryVineMonster != null)
        {
            primaryVineMonster.ActivateMonster();
        }

        if (stalkerMonster != null)
        {
            stalkerMonster.EnableIdleDamage();
            StartOpeningStalkerRoutine();
        }

        if (secondaryVineMonster != null)
        {
            secondaryVineMonster.ActivateMonster();
        }
    }

    // Starts the delayed opening activation for the stalker monster.
    private void StartOpeningStalkerRoutine()
    {
        StopOpeningStalkerRoutine();

        if (stalkerMonster == null)
        {
            return;
        }

        if (stalkerOpeningIdleDuration <= 0f)
        {
            stalkerMonster.ActivateMonster();
            return;
        }

        openingStalkerRoutine = StartCoroutine(OpeningStalkerRoutine());
    }

    // Cancels the pending stalker startup when the controller is torn down.
    private void StopOpeningStalkerRoutine()
    {
        if (openingStalkerRoutine == null)
        {
            return;
        }

        StopCoroutine(openingStalkerRoutine);
        openingStalkerRoutine = null;
    }

    // Waits the configured time, then starts the stalker movement loop.
    private IEnumerator OpeningStalkerRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, stalkerOpeningIdleDuration));

        if (stalkerMonster != null)
        {
            stalkerMonster.ActivateMonster();
        }

        openingStalkerRoutine = null;
    }

    // Returns the center of the requested grid cell.
    public Vector2 GetCellCenter(int columnIndex, int rowIndex)
    {
        float x = topLeftPosition.x + CellSize * (columnIndex + 0.5f);
        float y = topLeftPosition.y - CellSize * (rowIndex + 0.5f);
        return new Vector2(x, y);
    }

    // Converts a world position into the nearest valid grid cell indices.
    public Vector2Int GetClosestCell(Vector2 worldPosition)
    {
        float localX = (worldPosition.x - topLeftPosition.x) / CellSize - 0.5f;
        float localY = (topLeftPosition.y - worldPosition.y) / CellSize - 0.5f;

        int column = Mathf.Clamp(Mathf.RoundToInt(localX), 0, Columns - 1);
        int row = Mathf.Clamp(Mathf.RoundToInt(localY), 0, Rows - 1);
        return new Vector2Int(column, row);
    }

    // Returns the player's current nearest grid cell.
    public Vector2Int GetClosestCellToPlayer()
    {
        Transform currentPlayer = Player;
        if (currentPlayer == null)
        {
            return GetCenterCell();
        }

        return GetClosestCell(currentPlayer.position);
    }

    // Returns the center cell of the arena.
    public Vector2Int GetCenterCell()
    {
        return new Vector2Int((Columns - 1) / 2, (Rows - 1) / 2);
    }

    // Returns the world-space center point of the arena.
    public Vector2 GetArenaCenter()
    {
        float centerX = topLeftPosition.x + Columns * CellSize * 0.5f;
        float centerY = topLeftPosition.y - Rows * CellSize * 0.5f;
        return new Vector2(centerX, centerY);
    }

    // Returns true when the target collider overlaps any player collider.
    public bool IsPlayerOverlapping(Collider2D targetCollider)
    {
        Transform currentPlayer = Player;
        if (targetCollider == null || currentPlayer == null)
        {
            return false;
        }

        Collider2D[] playerColliders = currentPlayer.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled)
            {
                continue;
            }

            ColliderDistance2D distance = targetCollider.Distance(playerCollider);
            if (distance.isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    // Finds the ending sequence automatically when no explicit reference is assigned.
    public void ResolveEndingSequence()
    {
        if (endingSequence == null)
        {
            endingSequence = FindObjectOfType<MonsterEndingSequence>();
        }
    }

    // Caches player sprite renderers so flicker and fade effects can reuse them.
    private void CachePlayerRenderers()
    {
        Transform currentPlayer = Player;
        if (currentPlayer == null)
        {
            playerRenderers = null;
            originalRendererColors = null;
            return;
        }

        playerRenderers = currentPlayer.GetComponentsInChildren<SpriteRenderer>(true);
        if (playerRenderers == null)
        {
            originalRendererColors = null;
            return;
        }

        originalRendererColors = new Color[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            originalRendererColors[i] = playerRenderers[i].color;
        }
    }

    // Creates a runtime heart counter under the best available canvas.
    private void EnsureHeartUI()
    {
        if (heartImages != null && heartImages.Length == Mathf.Max(1, maxHearts))
        {
            return;
        }

        Canvas parentCanvas = FindBestUiCanvas();
        if (parentCanvas == null)
        {
            GameObject canvasObject = new GameObject("MonsterBattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            parentCanvas = canvasObject.GetComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (heartsUiObject != null)
        {
            Destroy(heartsUiObject);
        }

        heartsUiObject = new GameObject("HeartsUI", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        heartsUiObject.transform.SetParent(parentCanvas.transform, false);

        RectTransform rectTransform = heartsUiObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = heartsUiOffset;
        rectTransform.sizeDelta = new Vector2(
            Mathf.Max(1, maxHearts) * (heartIconSize.x + heartSpacing),
            heartIconSize.y + 12f);

        HorizontalLayoutGroup layoutGroup = heartsUiObject.GetComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = heartSpacing;

        int safeMaxHearts = Mathf.Max(1, maxHearts);
        heartImages = new Image[safeMaxHearts];

        for (int i = 0; i < safeMaxHearts; i++)
        {
            GameObject heartObject = new GameObject($"Heart_{i + 1}", typeof(RectTransform), typeof(Image));
            heartObject.transform.SetParent(heartsUiObject.transform, false);

            RectTransform heartRect = heartObject.GetComponent<RectTransform>();
            heartRect.sizeDelta = heartIconSize;

            Image heartImage = heartObject.GetComponent<Image>();
            heartImage.raycastTarget = false;
            heartImage.preserveAspect = true;
            heartImages[i] = heartImage;
        }
    }

    // Chooses an existing UI canvas when possible so the heart UI fits the current scene.
    private Canvas FindBestUiCanvas()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.dialoguePanel != null)
        {
            return DialogueManager.Instance.dialoguePanel.GetComponentInParent<Canvas>();
        }

        return FindObjectOfType<Canvas>();
    }

    // Rebuilds the heart string after each damage or initialization step.
    private void RefreshHeartUI()
    {
        EnsureHeartUI();
        if (heartImages == null || heartImages.Length == 0)
        {
            return;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
            {
                continue;
            }

            bool isFilled = i < currentHearts;
            heartImages[i].sprite = fullHeartSprite;
            heartImages[i].color = fullHeartColor;
            heartImages[i].enabled = isFilled && heartImages[i].sprite != null;
        }
    }

    // Makes the player flicker during a short invulnerability window after taking a hit.
    private IEnumerator PlayerInvulnerabilityRoutine()
    {
        isInvulnerable = true;
        RestorePlayerVisualAlpha(1f);

        float duration = Mathf.Max(0.01f, invulnerabilityDuration);
        float interval = Mathf.Max(0.03f, flickerInterval);
        float elapsed = 0f;
        bool lowAlpha = false;

        while (elapsed < duration)
        {
            lowAlpha = !lowAlpha;
            RestorePlayerVisualAlpha(lowAlpha ? flickerAlpha : 1f);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        RestorePlayerVisualAlpha(1f);
        isInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    // Starts the external ending flow once all hearts are gone.
    private void BeginDeathSequence()
    {
        if (isDeathSequenceActive)
        {
            return;
        }

        isInvulnerable = true;
        isDeathSequenceActive = true;
        RestorePlayerVisualAlpha(1f);
        ResolveEndingSequence();
        if (endingSequence != null && Player != null)
        {
            endingSequence.PlayEnding(Player.gameObject);
        }
    }

    // Restores all tracked player sprite renderers to the requested alpha multiplier.
    private void RestorePlayerVisualAlpha(float alphaMultiplier)
    {
        if (playerRenderers == null || originalRendererColors == null)
        {
            return;
        }

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null)
            {
                continue;
            }

            Color color = originalRendererColors[i];
            color.a = originalRendererColors[i].a * Mathf.Clamp01(alphaMultiplier);
            playerRenderers[i].color = color;
        }
    }

    // Draws the shared arena bounds in the Scene view.
    private void OnDrawGizmosSelected()
    {
        float width = Columns * CellSize;
        float height = Rows * CellSize;
        Vector3 center = new Vector3(
            topLeftPosition.x + width * 0.5f,
            topLeftPosition.y - height * 0.5f,
            transform.position.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0.05f));
    }

    // Cleans up runtime UI if this controller is destroyed.
    private void OnDestroy()
    {
        StopOpeningStalkerRoutine();

        if (heartsUiObject != null)
        {
            Destroy(heartsUiObject);
        }
    }
}
