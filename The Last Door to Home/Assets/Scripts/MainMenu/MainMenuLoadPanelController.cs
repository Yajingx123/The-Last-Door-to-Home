using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
Purpose: Shows a keyboard-driven load-slot overlay from the main menu Continue button.
Attached GameObject: Auto-created runtime singleton in the MainMenu scene.
Main responsibilities: Display ten save slots, handle selection input, and load the chosen slot.
Inputs: Main menu input, save slot summaries, and load requests.
Outputs or effects: Opens and closes a UI overlay and triggers save-slot loading.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify Continue opens the slot list, empty slots fail gracefully, and Esc returns to the main menu buttons.
*/

public class MainMenuLoadPanelController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string BuiltinFontResourcePath = "LegacyRuntime.ttf";

    private static MainMenuLoadPanelController instance;

    private Canvas panelCanvas;
    private GameObject overlayObject;
    private Text titleText;
    private Text footerText;
    private readonly List<Text> slotTexts = new List<Text>();
    private readonly List<string> slotLabels = new List<string>();
    private List<SaveSlotSummary> slotSummaries = new List<SaveSlotSummary>();
    private int selectedIndex;
    private string footerMessage = "Enter: Load Selected Slot   Esc: Back";

    public static bool IsOpen => instance != null && instance.overlayObject != null && instance.overlayObject.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.name, MainMenuSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        EnsureInstance();
    }

    public static void OpenPanel()
    {
        EnsureInstance();
        if (instance == null) return;
        instance.OpenInternal();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        instance = FindObjectOfType<MainMenuLoadPanelController>();
        if (instance != null)
        {
            instance.Initialize();
            return;
        }

        GameObject go = new GameObject("MainMenuLoadPanelController");
        instance = go.AddComponent<MainMenuLoadPanelController>();
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

    private void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            TryLoadSelectedSlot();
        }
    }

    private void Initialize()
    {
        if (panelCanvas != null)
        {
            return;
        }

        BuildUi();
        ClosePanel();
    }

    private void OpenInternal()
    {
        if (overlayObject == null) return;

        selectedIndex = 0;
        footerMessage = "Enter: Load Selected Slot   Esc: Back";
        overlayObject.SetActive(true);
        RefreshSlotList();
    }

    private void ClosePanel()
    {
        if (overlayObject != null)
        {
            overlayObject.SetActive(false);
        }
    }

    private void TryLoadSelectedSlot()
    {
        if (!SaveSystem.LoadFromSlot(selectedIndex, out string message))
        {
            footerMessage = message;
            RefreshFooter();
            return;
        }

        ClosePanel();
    }

    private void MoveSelection(int direction)
    {
        if (slotLabels.Count == 0) return;

        selectedIndex += direction;
        if (selectedIndex < 0)
        {
            selectedIndex = slotLabels.Count - 1;
        }
        else if (selectedIndex >= slotLabels.Count)
        {
            selectedIndex = 0;
        }

        RefreshSelection();
    }

    private void RefreshSlotList()
    {
        slotSummaries = SaveSystem.GetSlotSummaries();
        slotLabels.Clear();

        for (int i = 0; i < slotSummaries.Count; i++)
        {
            slotLabels.Add(BuildSlotLabel(slotSummaries[i]));
        }

        titleText.text = "Continue";
        EnsureSlotTextCount(SaveSystem.SlotCount);
        for (int i = 0; i < slotTexts.Count; i++)
        {
            bool shouldShow = i < slotLabels.Count;
            slotTexts[i].gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;
            slotTexts[i].text = slotLabels[i];
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, slotLabels.Count - 1));
        RefreshSelection();
        RefreshFooter();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < slotTexts.Count; i++)
        {
            if (!slotTexts[i].gameObject.activeSelf) continue;

            bool isSelected = i == selectedIndex;
            slotTexts[i].color = isSelected ? new Color(1f, 0.92f, 0.45f, 1f) : Color.white;
            slotTexts[i].text = isSelected ? $"> {slotLabels[i]}" : $"  {slotLabels[i]}";
        }
    }

    private void RefreshFooter()
    {
        if (footerText != null)
        {
            footerText.text = footerMessage;
        }
    }

    private static string BuildSlotLabel(SaveSlotSummary summary)
    {
        if (summary == null)
        {
            return "Slot ??   EMPTY";
        }

        if (!summary.hasData)
        {
            return $"Slot {summary.slotIndex + 1:00}   EMPTY";
        }

        string sceneName = string.IsNullOrWhiteSpace(summary.sceneName) ? "Unknown" : summary.sceneName;
        string playTime = SaveSystem.FormatPlayTime(summary.playTimeSeconds);
        return $"Slot {summary.slotIndex + 1:00}   {sceneName}   {playTime}";
    }

    private void BuildUi()
    {
        panelCanvas = gameObject.GetComponent<Canvas>();
        if (panelCanvas == null)
        {
            panelCanvas = gameObject.AddComponent<Canvas>();
        }

        panelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        panelCanvas.sortingOrder = short.MaxValue - 2;

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
        panelRect.anchorMin = new Vector2(0.18f, 0.12f);
        panelRect.anchorMax = new Vector2(0.82f, 0.88f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        titleText = CreateText("Title", panelObject.transform, 36, TextAnchor.UpperLeft, FontStyle.Bold);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -26f);
        titleRect.sizeDelta = new Vector2(-72f, 54f);

        GameObject slotsRoot = CreateUiObject("Slots", panelObject.transform);
        RectTransform slotsRect = slotsRoot.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 0f);
        slotsRect.anchorMax = new Vector2(1f, 1f);
        slotsRect.offsetMin = new Vector2(28f, 78f);
        slotsRect.offsetMax = new Vector2(-28f, -100f);

        VerticalLayoutGroup slotLayout = slotsRoot.AddComponent<VerticalLayoutGroup>();
        slotLayout.childAlignment = TextAnchor.UpperLeft;
        slotLayout.childControlWidth = true;
        slotLayout.childControlHeight = false;
        slotLayout.childForceExpandWidth = true;
        slotLayout.childForceExpandHeight = false;
        slotLayout.spacing = 10f;

        EnsureSlotTextCount(SaveSystem.SlotCount, slotsRoot.transform);

        footerText = CreateText("Footer", panelObject.transform, 16, TextAnchor.LowerLeft, FontStyle.Italic);
        RectTransform footerRect = footerText.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.anchoredPosition = new Vector2(0f, 20f);
        footerRect.sizeDelta = new Vector2(-60f, 56f);
    }

    private void EnsureSlotTextCount(int requiredCount, Transform parentOverride = null)
    {
        Transform parent = parentOverride != null ? parentOverride : (slotTexts.Count > 0 ? slotTexts[0].transform.parent : null);
        if (parent == null && requiredCount > 0) return;

        while (slotTexts.Count < requiredCount)
        {
            Text slotText = CreateText($"Slot_{slotTexts.Count + 1}", parent, 21, TextAnchor.MiddleLeft, FontStyle.Normal);
            LayoutElement layoutElement = slotText.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 30f;
            slotTexts.Add(slotText);
        }
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
