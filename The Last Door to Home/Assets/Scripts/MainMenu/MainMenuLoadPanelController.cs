using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private const string PauseMenuPrefabResourcePath = "SystemMenu/PauseMenu";

    private static MainMenuLoadPanelController instance;

    private Canvas panelCanvas;
    private GameObject overlayObject;
    private Component titleText;
    private Component footerText;
    private readonly List<Component> slotTexts = new List<Component>();
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

        BuildUiFromPrefabOrFallback();
        ClosePanel();
    }

    private void OpenInternal()
    {
        if (overlayObject == null) return;

        selectedIndex = 0;
        footerMessage = "Enter: Load Selected Slot   Esc: Back";
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
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
        if (selectedIndex < 0 || selectedIndex >= slotSummaries.Count || !slotSummaries[selectedIndex].hasData)
        {
            footerMessage = "This save slot is empty.";
            RefreshFooter();
            return;
        }

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

        SetTextValue(titleText, "Continue");
        EnsureSlotTextCount(SaveSystem.SlotCount);
        for (int i = 0; i < slotTexts.Count; i++)
        {
            bool shouldShow = i < slotLabels.Count;
            slotTexts[i].gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;
            SetTextValue(slotTexts[i], slotLabels[i]);
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
            SetTextColor(slotTexts[i], isSelected ? new Color(1f, 0.92f, 0.45f, 1f) : Color.white);
            SetTextValue(slotTexts[i], isSelected ? $"> {slotLabels[i]}" : $"  {slotLabels[i]}");
        }
    }

    private void RefreshFooter()
    {
        if (footerText != null)
        {
            SetTextValue(footerText, footerMessage);
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

    private void BuildUiFromPrefabOrFallback()
    {
        if (BuildUiFromPrefab())
        {
            return;
        }

        BuildUi();
    }

    private bool BuildUiFromPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(PauseMenuPrefabResourcePath);
        if (prefab == null)
        {
            return false;
        }

        overlayObject = Instantiate(prefab, transform);
        overlayObject.name = $"{prefab.name}_Continue";

        panelCanvas = overlayObject.GetComponent<Canvas>();
        if (panelCanvas == null)
        {
            panelCanvas = overlayObject.GetComponentInChildren<Canvas>(true);
        }

        if (panelCanvas == null)
        {
            Destroy(overlayObject);
            overlayObject = null;
            return false;
        }

        if (panelCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            panelCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (panelCanvas.renderMode == RenderMode.ScreenSpaceCamera && panelCanvas.worldCamera == null)
        {
            panelCanvas.worldCamera = Camera.main;
        }

        titleText = FindRequiredTextComponent(overlayObject.transform, "Title");
        footerText = FindRequiredTextComponent(overlayObject.transform, "Footer");

        Transform slotOptionsRoot = FindChildRecursive(overlayObject.transform, "SlotOptions");
        Transform optionsRoot = FindChildRecursive(overlayObject.transform, "Options");
        Transform descriptionRoot = FindChildRecursive(overlayObject.transform, "Description");

        if (titleText == null || footerText == null || (slotOptionsRoot == null && optionsRoot == null))
        {
            Destroy(overlayObject);
            overlayObject = null;
            titleText = null;
            footerText = null;
            return false;
        }

        if (descriptionRoot != null)
        {
            descriptionRoot.gameObject.SetActive(false);
        }

        if (optionsRoot != null)
        {
            optionsRoot.gameObject.SetActive(slotOptionsRoot == null);
        }

        if (slotOptionsRoot != null)
        {
            slotOptionsRoot.gameObject.SetActive(true);
            LoadTextPoolFromContainer(slotTexts, slotOptionsRoot);
            EnsureSlotTextCount(SaveSystem.SlotCount, slotOptionsRoot);
        }
        else
        {
            LoadTextPoolFromContainer(slotTexts, optionsRoot);
            EnsureSlotTextCount(SaveSystem.SlotCount, optionsRoot);
        }

        DisableMouseInteraction();
        return true;
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

        DisableMouseInteraction();
    }

    private void EnsureSlotTextCount(int requiredCount, Transform parentOverride = null)
    {
        Transform parent = parentOverride != null ? parentOverride : (slotTexts.Count > 0 ? slotTexts[0].transform.parent : null);
        if (parent == null && requiredCount > 0) return;

        while (slotTexts.Count < requiredCount)
        {
            Component slotText = CreateText($"Slot_{slotTexts.Count + 1}", parent, 21, TextAnchor.MiddleLeft, FontStyle.Normal);
            LayoutElement layoutElement = slotText.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 30f;
            slotTexts.Add(slotText);
        }
    }

    private void DisableMouseInteraction()
    {
        if (overlayObject == null) return;

        Graphic[] graphics = overlayObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private Component CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, FontStyle fontStyle)
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

    private void LoadTextPoolFromContainer(List<Component> targetPool, Transform parent)
    {
        targetPool.Clear();
        if (parent == null) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            Component slotText = GetSupportedTextComponent(parent.GetChild(i));
            if (slotText != null)
            {
                targetPool.Add(slotText);
            }
        }
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

    private static Component FindRequiredTextComponent(Transform root, string objectName)
    {
        Transform target = FindChildRecursive(root, objectName);
        return target != null ? GetSupportedTextComponent(target) : null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;
        if (parent.name == childName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Component GetSupportedTextComponent(Transform target)
    {
        if (target == null) return null;

        Text legacyText = target.GetComponent<Text>();
        if (legacyText != null)
        {
            return legacyText;
        }

        TMP_Text tmpText = target.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            return tmpText;
        }

        return null;
    }

    private static void SetTextValue(Component textComponent, string value)
    {
        if (textComponent is Text legacyText)
        {
            legacyText.text = value;
            return;
        }

        if (textComponent is TMP_Text tmpText)
        {
            tmpText.text = value;
        }
    }

    private static void SetTextColor(Component textComponent, Color color)
    {
        if (textComponent is Text legacyText)
        {
            legacyText.color = color;
            return;
        }

        if (textComponent is TMP_Text tmpText)
        {
            tmpText.color = color;
        }
    }
}
