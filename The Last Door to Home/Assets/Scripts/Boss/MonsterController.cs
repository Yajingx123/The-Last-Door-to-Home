using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/*
Purpose: Controls boss, enemy, damage, or boss-ending behavior.
Attached GameObject: Boss/enemy GameObject, damage hitbox, or boss-scene controller.
Main responsibilities: Updates combat movement/state, resolves contact damage, handles defeat, and triggers ending or door behavior.
Inputs: Player position, colliders, serialized combat settings, health/progression state, and scene triggers.
Outputs or effects: Moves enemies, applies damage, updates animations, changes story/ending state, or loads scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify combat states, damage timing, defeat conditions, and ending transitions.
*/

public class MonsterController : MonoBehaviour
{
    [Header("玩家 / Player")]
    [SerializeField] private Transform player;

    [Header("区域设置 / Area Settings")]
    [Tooltip("战斗区域左上角格子的左上角世界坐标。")]
    [SerializeField] private Vector2 topLeftPosition;
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 6;
    [SerializeField] private float cellSize = 1f;

    [Header("受伤设置 / Damage Settings")]
    [SerializeField] private int maxHearts = 8;
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private float flickerInterval = 0.1f;
    [SerializeField] private float flickerAlpha = 0.25f;

    [Header("爱心UI / Heart UI")]
    [SerializeField] private Vector2 heartsUiOffset = new Vector2(36f, -36f);
    [SerializeField] private Vector2 heartIconSize = new Vector2(36f, 36f);
    [SerializeField] private float heartSpacing = 8f;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Color fullHeartColor = Color.white;

    [Header("生命归零 / Zero Health")]
    [SerializeField] private MonsterEndingSequence endingSequence;

    [Header("怪物回合配置 / Monster Phase Setup")]
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

    // Initializes component references and singleton ownership before Start runs.
    private void Awake()
    {
        currentHearts = Mathf.Max(1, maxHearts);
    }

    // Prepares runtime state after the scene has finished its initial setup.
    private void Start()
    {
        ResolvePlayerReference();
        ResolveEndingSequence();
        CachePlayerRenderers();
        EnsureHeartUI();
        RefreshHeartUI();
        SetupBattlePhases();
    }

    // Resolves the best available value for the requested data.
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

    // Attempts the requested operation and reports whether it succeeded.
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

    // Updates the requested value or component state.
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

    // Starts the start opening stalker routine sequence or runtime effect.
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

    // Stops the stop opening stalker routine sequence or runtime effect.
    private void StopOpeningStalkerRoutine()
    {
        if (openingStalkerRoutine == null)
        {
            return;
        }

        StopCoroutine(openingStalkerRoutine);
        openingStalkerRoutine = null;
    }

    // Opens the related UI or gameplay flow.
    private IEnumerator OpeningStalkerRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, stalkerOpeningIdleDuration));

        if (stalkerMonster != null)
        {
            stalkerMonster.ActivateMonster();
        }

        openingStalkerRoutine = null;
    }

    // Returns the requested value or runtime object.
    public Vector2 GetCellCenter(int columnIndex, int rowIndex)
    {
        float x = topLeftPosition.x + CellSize * (columnIndex + 0.5f);
        float y = topLeftPosition.y - CellSize * (rowIndex + 0.5f);
        return new Vector2(x, y);
    }

    // Returns the requested value or runtime object.
    public Vector2Int GetClosestCell(Vector2 worldPosition)
    {
        float localX = (worldPosition.x - topLeftPosition.x) / CellSize - 0.5f;
        float localY = (topLeftPosition.y - worldPosition.y) / CellSize - 0.5f;

        int column = Mathf.Clamp(Mathf.RoundToInt(localX), 0, Columns - 1);
        int row = Mathf.Clamp(Mathf.RoundToInt(localY), 0, Rows - 1);
        return new Vector2Int(column, row);
    }

    // Returns the requested value or runtime object.
    public Vector2Int GetClosestCellToPlayer()
    {
        Transform currentPlayer = Player;
        if (currentPlayer == null)
        {
            return GetCenterCell();
        }

        return GetClosestCell(currentPlayer.position);
    }

    // Returns the requested value or runtime object.
    public Vector2Int GetCenterCell()
    {
        return new Vector2Int((Columns - 1) / 2, (Rows - 1) / 2);
    }

    // Returns the requested value or runtime object.
    public Vector2 GetArenaCenter()
    {
        float centerX = topLeftPosition.x + Columns * CellSize * 0.5f;
        float centerY = topLeftPosition.y - Rows * CellSize * 0.5f;
        return new Vector2(centerX, centerY);
    }

    // Returns whether is player overlapping is true for the current state.
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

    // Resolves the best available value for the requested data.
    public void ResolveEndingSequence()
    {
        if (endingSequence == null)
        {
            endingSequence = FindObjectOfType<MonsterEndingSequence>();
        }
    }

    // Caches references or values needed by cache player renderers.
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

    // Ensures the required ensure heart ui objects or state exist.
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

    // Searches the scene hierarchy or data collection for the requested target.
    private Canvas FindBestUiCanvas()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.dialoguePanel != null)
        {
            return DialogueManager.Instance.dialoguePanel.GetComponentInParent<Canvas>();
        }

        return FindObjectOfType<Canvas>();
    }

    // Refreshes UI text, selection, or cached runtime data.
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

    // Plays the player invulnerability routine sequence or audio feedback.
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

    // Begins the begin death sequence sequence.
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

    // Handles the restore player visual alpha step for this script.
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

    // Draws editor-only debug helpers while this object is selected.
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

    // Cleans up runtime references before the object is destroyed.
    private void OnDestroy()
    {
        StopOpeningStalkerRoutine();

        if (heartsUiObject != null)
        {
            Destroy(heartsUiObject);
        }
    }
}
