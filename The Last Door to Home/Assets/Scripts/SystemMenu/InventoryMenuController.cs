using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
Purpose: Provides a global Tab inventory menu for showing collected items in gameplay scenes.
Attached GameObject: Auto-created runtime singleton.
Main responsibilities: Listen for Tab, display a grid-style inventory, and block gameplay input while open.
Inputs: Active scene info, player input, and current inventory state.
Outputs or effects: Pauses gameplay, shows an overlay, and restores normal input when closed.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify Tab toggles in gameplay scenes, grid selection works, and the detail panel matches the selected item.
*/

public class InventoryMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string AnimScenesFolderToken = "/anim_scenes/";
    private const string BuiltinFontResourcePath = "LegacyRuntime.ttf";
    private const int ColumnCount = 2;

    private static InventoryMenuController instance;

    private Canvas menuCanvas;
    private GameObject overlayObject;
    private Text titleText;
    private Text hintText;
    private Image detailIconImage;
    private Text detailTitleText;
    private Text detailBodyText;
    private Text footerText;
    private readonly List<Text> slotTexts = new List<Text>();
    private readonly List<Image> slotIconImages = new List<Image>();
    private List<InventoryItemRecord> cachedItems = new List<InventoryItemRecord>();

    private bool isMenuOpen;
    private int selectedIndex;

    public static bool IsOpen => instance != null && instance.isMenuOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        instance = FindObjectOfType<InventoryMenuController>();
        if (instance != null)
        {
            instance.Initialize();
            return;
        }

        GameObject go = new GameObject("InventoryMenuController");
        instance = go.AddComponent<InventoryMenuController>();
        instance.Initialize();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!isMenuOpen)
        {
            if (!ShouldAllowInventoryMenu()) return;
            if (!Input.GetKeyDown(KeyCode.Tab)) return;

            OpenMenu();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
            return;
        }

        if (cachedItems.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelectionVertical(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelectionVertical(1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelectionHorizontal(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelectionHorizontal(1);
        }
    }

    private void Initialize()
    {
        if (menuCanvas != null)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        BuildUi();
        HideMenuImmediate();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HideMenuImmediate();
    }

    private bool ShouldAllowInventoryMenu()
    {
        if (SceneTransition.IsTransitioning) return false;
        if (EscapeMenuController.IsMenuOpen) return false;
        if (IsSceneExcluded(SceneManager.GetActiveScene())) return false;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlayerControlLocked) return false;
        return true;
    }

    private static bool IsSceneExcluded(Scene scene)
    {
        if (!scene.IsValid()) return true;
        if (string.Equals(scene.name, MainMenuSceneName, System.StringComparison.OrdinalIgnoreCase)) return true;

        string path = scene.path ?? string.Empty;
        if (path.IndexOf(AnimScenesFolderToken, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

        string sceneName = scene.name ?? string.Empty;
        return sceneName.IndexOf("CutScene", System.StringComparison.OrdinalIgnoreCase) >= 0
            || sceneName.IndexOf("Cutscene", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OpenMenu()
    {
        isMenuOpen = true;
        overlayObject.SetActive(true);
        Time.timeScale = 0f;
        LockGameplayInput(true);
        selectedIndex = 0;
        RefreshInventoryView();
    }

    private void CloseMenu()
    {
        isMenuOpen = false;
        Time.timeScale = 1f;
        overlayObject.SetActive(false);
        LockGameplayInput(false);
    }

    private void HideMenuImmediate()
    {
        isMenuOpen = false;
        Time.timeScale = 1f;
        if (overlayObject != null)
        {
            overlayObject.SetActive(false);
        }
    }

    private void LockGameplayInput(bool lockIt)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.LockPlayer(lockIt);
        }
    }

    private void RefreshInventoryView()
    {
        cachedItems = Inventory.ExportCollectedItems();
        titleText.text = "Inventory";
        hintText.text = cachedItems.Count > 0
            ? "WASD or Arrow Keys: Move Selection"
            : "No items collected yet.";
        footerText.text = "Tab or Esc: Close";

        EnsureSlotCount(Mathf.Max(1, cachedItems.Count));

        if (cachedItems.Count == 0)
        {
            for (int i = 0; i < slotTexts.Count; i++)
            {
                bool active = i == 0;
                slotTexts[i].gameObject.SetActive(active);
                slotIconImages[i].transform.parent.gameObject.SetActive(active);
                if (!active) continue;

                slotTexts[i].text = "EMPTY";
                slotTexts[i].color = new Color(0.85f, 0.85f, 0.85f, 1f);
                ApplyIcon(slotIconImages[i], null);
            }

            detailTitleText.text = "Inventory Empty";
            detailBodyText.text = "Pick up items in exploration scenes and they will appear here.";
            ApplyIcon(detailIconImage, null);
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, cachedItems.Count - 1);
        for (int i = 0; i < slotTexts.Count; i++)
        {
            bool active = i < cachedItems.Count;
            slotTexts[i].transform.parent.gameObject.SetActive(active);
            if (!active) continue;

            InventoryItemRecord item = cachedItems[i];
            slotTexts[i].text = ResolveItemName(item);
            ApplyIcon(slotIconImages[i], item.runtimeIcon);
        }

        RefreshSelectionVisuals();
        RefreshDetailPanel();
    }

    private void MoveSelectionHorizontal(int direction)
    {
        int candidate = selectedIndex + direction;
        if (candidate < 0 || candidate >= cachedItems.Count) return;

        int currentRow = selectedIndex / ColumnCount;
        int candidateRow = candidate / ColumnCount;
        if (currentRow != candidateRow) return;

        selectedIndex = candidate;
        RefreshSelectionVisuals();
        RefreshDetailPanel();
    }

    private void MoveSelectionVertical(int rowDelta)
    {
        int candidate = selectedIndex + rowDelta * ColumnCount;
        if (candidate < 0 || candidate >= cachedItems.Count) return;

        selectedIndex = candidate;
        RefreshSelectionVisuals();
        RefreshDetailPanel();
    }

    private void RefreshSelectionVisuals()
    {
        for (int i = 0; i < slotTexts.Count; i++)
        {
            if (!slotTexts[i].gameObject.activeSelf) continue;

            bool isSelected = i == selectedIndex;
            Text text = slotTexts[i];
            InventoryItemRecord item = cachedItems[i];
            text.text = isSelected ? $"> {ResolveItemName(item)}" : $"  {ResolveItemName(item)}";
            text.color = isSelected ? new Color(1f, 0.92f, 0.45f, 1f) : Color.white;

            Image slotBackground = text.transform.parent.GetComponent<Image>();
            if (slotBackground != null)
            {
                slotBackground.color = isSelected
                    ? new Color(0.24f, 0.22f, 0.12f, 0.95f)
                    : new Color(0.16f, 0.16f, 0.16f, 0.92f);
            }
        }
    }

    private void RefreshDetailPanel()
    {
        if (cachedItems.Count == 0 || selectedIndex < 0 || selectedIndex >= cachedItems.Count)
        {
            detailTitleText.text = "No Selection";
            detailBodyText.text = string.Empty;
            return;
        }

        InventoryItemRecord item = cachedItems[selectedIndex];
        detailTitleText.text = ResolveItemName(item);
        ApplyIcon(detailIconImage, item.runtimeIcon);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Type: {item.itemType}");
        builder.AppendLine($"ID: {item.uniqueID}");
        builder.AppendLine();
        builder.Append("Description: ");
        builder.Append(ResolveDescription(item));

        detailBodyText.text = builder.ToString();
    }

    private static string ResolveItemName(InventoryItemRecord item)
    {
        if (item == null) return "Unknown Item";
        return string.IsNullOrWhiteSpace(item.itemName) ? item.uniqueID : item.itemName;
    }

    private static string ResolveDescription(InventoryItemRecord item)
    {
        if (item == null) return "No information available.";
        if (!string.IsNullOrWhiteSpace(item.itemDescription)) return item.itemDescription;

        switch (item.itemType)
        {
            case ItemType.Key:
                return "A key item that probably unlocks or activates something important.";
            case ItemType.Tool:
                return "A useful tool that may help with interactions or progression.";
            case ItemType.Note:
                return "A note or record that may contain clues.";
            case ItemType.Flower:
                return "A collected flower tied to the world or story.";
            case ItemType.stone:
                return "A strange stone that may have a special purpose.";
            default:
                return "No description has been assigned yet.";
        }
    }

    private void BuildUi()
    {
        menuCanvas = gameObject.GetComponent<Canvas>();
        if (menuCanvas == null)
        {
            menuCanvas = gameObject.AddComponent<Canvas>();
        }

        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = short.MaxValue - 3;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        overlayObject = CreateUiObject("Overlay", transform);
        Image overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
        StretchToFullScreen(overlayObject.GetComponent<RectTransform>());

        GameObject panelObject = CreateUiObject("Panel", overlayObject.transform);
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.1f);
        panelRect.anchorMax = new Vector2(0.88f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        titleText = CreateText("Title", panelObject.transform, 36, TextAnchor.UpperLeft, FontStyle.Bold);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(-72f, 54f);

        hintText = CreateText("Hint", panelObject.transform, 17, TextAnchor.UpperLeft, FontStyle.Italic);
        RectTransform hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -80f);
        hintRect.sizeDelta = new Vector2(-72f, 44f);

        GameObject leftPanel = CreateUiObject("ItemGridPanel", panelObject.transform);
        Image leftPanelImage = leftPanel.AddComponent<Image>();
        leftPanelImage.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.52f, 1f);
        leftRect.offsetMin = new Vector2(28f, 88f);
        leftRect.offsetMax = new Vector2(-12f, -78f);

        GridLayoutGroup gridLayout = leftPanel.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(280f, 56f);
        gridLayout.spacing = new Vector2(14f, 14f);
        gridLayout.padding = new RectOffset(18, 18, 18, 18);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = ColumnCount;
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        GameObject rightPanel = CreateUiObject("DetailPanel", panelObject.transform);
        Image rightPanelImage = rightPanel.AddComponent<Image>();
        rightPanelImage.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.52f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.offsetMin = new Vector2(12f, 88f);
        rightRect.offsetMax = new Vector2(-28f, -78f);

        GameObject detailIconObject = CreateUiObject("DetailIcon", rightPanel.transform);
        detailIconImage = detailIconObject.AddComponent<Image>();
        RectTransform detailIconRect = detailIconObject.GetComponent<RectTransform>();
        detailIconRect.anchorMin = new Vector2(0f, 1f);
        detailIconRect.anchorMax = new Vector2(0f, 1f);
        detailIconRect.pivot = new Vector2(0f, 1f);
        detailIconRect.anchoredPosition = new Vector2(24f, -24f);
        detailIconRect.sizeDelta = new Vector2(96f, 96f);

        detailTitleText = CreateText("DetailTitle", rightPanel.transform, 30, TextAnchor.UpperLeft, FontStyle.Bold);
        RectTransform detailTitleRect = detailTitleText.GetComponent<RectTransform>();
        detailTitleRect.anchorMin = new Vector2(0f, 1f);
        detailTitleRect.anchorMax = new Vector2(1f, 1f);
        detailTitleRect.pivot = new Vector2(0.5f, 1f);
        detailTitleRect.anchoredPosition = new Vector2(58f, -24f);
        detailTitleRect.sizeDelta = new Vector2(-116f, 48f);

        detailBodyText = CreateText("DetailBody", rightPanel.transform, 20, TextAnchor.UpperLeft, FontStyle.Normal);
        RectTransform detailBodyRect = detailBodyText.GetComponent<RectTransform>();
        detailBodyRect.anchorMin = new Vector2(0f, 0f);
        detailBodyRect.anchorMax = new Vector2(1f, 1f);
        detailBodyRect.offsetMin = new Vector2(24f, 24f);
        detailBodyRect.offsetMax = new Vector2(-24f, -134f);

        EnsureSlotCount(1, leftPanel.transform);

        footerText = CreateText("Footer", panelObject.transform, 16, TextAnchor.LowerLeft, FontStyle.Italic);
        RectTransform footerRect = footerText.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.anchoredPosition = new Vector2(0f, 20f);
        footerRect.sizeDelta = new Vector2(-60f, 56f);
    }

    private void EnsureSlotCount(int requiredCount, Transform parentOverride = null)
    {
        Transform parent = parentOverride != null ? parentOverride : (slotTexts.Count > 0 ? slotTexts[0].transform.parent.parent : null);
        if (parent == null && requiredCount > 0) return;

        while (slotTexts.Count < requiredCount)
        {
            GameObject slotObject = CreateUiObject($"Slot_{slotTexts.Count + 1}", parent);
            Image slotImage = slotObject.AddComponent<Image>();
            slotImage.color = new Color(0.16f, 0.16f, 0.16f, 0.92f);

            GameObject iconObject = CreateUiObject("Icon", slotObject.transform);
            Image iconImage = iconObject.AddComponent<Image>();
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            iconRect.sizeDelta = new Vector2(36f, 36f);

            Text slotText = CreateText("Label", slotObject.transform, 21, TextAnchor.MiddleLeft, FontStyle.Normal);
            RectTransform labelRect = slotText.GetComponent<RectTransform>();
            StretchToFullScreen(labelRect);
            labelRect.offsetMin = new Vector2(54f, 0f);
            labelRect.offsetMax = new Vector2(-14f, 0f);

            slotIconImages.Add(iconImage);
            slotTexts.Add(slotText);
        }
    }

    private void ApplyIcon(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = sprite != null
            ? Color.white
            : new Color(0.35f, 0.35f, 0.35f, 1f);
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>(BuiltinFontResourcePath);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = false;
        text.color = Color.white;
        return text;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchToFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
