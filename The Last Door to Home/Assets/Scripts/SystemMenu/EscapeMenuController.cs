using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
Purpose: Provides a global Escape pause menu outside of main menu and cutscene scenes.
Attached GameObject: Auto-created runtime singleton.
Main responsibilities: Listen for Escape, show a keyboard-driven pause UI, and route menu actions.
Inputs: Active scene info, player input, save metadata, and audio manager state.
Outputs or effects: Pauses gameplay, shows UI, adjusts audio settings, and triggers scene transitions.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify gameplay pauses, save/load slots work, menu input stays inside the pause UI, and submenu actions behave as expected.
*/

public class EscapeMenuController : MonoBehaviour
{
    private enum MenuPage
    {
        Main,
        SaveSlots,
        LoadSlots,
        Music,
        KeyMapping,
        Information
    }

    private const string DefaultMainMenuSceneName = "MainMenu";
    private const string AnimScenesFolderToken = "/anim_scenes/";
    private const string BuiltinFontResourcePath = "LegacyRuntime.ttf";
    private const float VolumeStep = 0.05f;
    private const int VolumeBarSegmentCount = 12;

    private static EscapeMenuController instance;

    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;

    private Canvas menuCanvas;
    private GameObject overlayObject;
    private Text titleText;
    private Text descriptionText;
    private Text footerText;
    private readonly List<Text> optionTexts = new List<Text>();
    private readonly List<string> currentOptionLabels = new List<string>();
    private List<SaveSlotSummary> slotSummaries = new List<SaveSlotSummary>();

    private bool isMenuOpen;
    private int selectedIndex;
    private MenuPage currentPage = MenuPage.Main;
    private string footerMessage = "W/S or Arrow Keys: Move   Enter: Select   Esc: Back";

    public static bool IsMenuOpen => instance != null && instance.isMenuOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        instance = FindObjectOfType<EscapeMenuController>();
        if (instance != null)
        {
            instance.Initialize();
            return;
        }

        GameObject go = new GameObject("EscapeMenuController");
        instance = go.AddComponent<EscapeMenuController>();
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
            if (!ShouldAllowEscapeMenu()) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            OpenMenu();
            return;
        }

        HandleOpenMenuInput();
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

    private bool ShouldAllowEscapeMenu()
    {
        if (SceneTransition.IsTransitioning) return false;
        if (IsSceneExcluded(SceneManager.GetActiveScene())) return false;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlayerControlLocked) return false;
        return true;
    }

    private bool IsSceneExcluded(Scene scene)
    {
        if (!scene.IsValid()) return true;
        if (scene.name == mainMenuSceneName) return true;

        string path = scene.path ?? string.Empty;
        if (path.IndexOf(AnimScenesFolderToken, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

        string sceneName = scene.name ?? string.Empty;
        return sceneName.IndexOf("CutScene", System.StringComparison.OrdinalIgnoreCase) >= 0
            || sceneName.IndexOf("Cutscene", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OpenMenu()
    {
        GameSessionTracker.EnsureSessionStarted();
        isMenuOpen = true;
        overlayObject.SetActive(true);
        Time.timeScale = 0f;
        currentPage = MenuPage.Main;
        selectedIndex = 0;
        footerMessage = "W/S or Arrow Keys: Move   Enter: Select   Esc: Close";
        LockGameplayInput(true);
        RefreshCurrentPage();
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
        currentPage = MenuPage.Main;
        selectedIndex = 0;
        footerMessage = "W/S or Arrow Keys: Move   Enter: Select   Esc: Close";
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

    private void HandleOpenMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentPage == MenuPage.Main)
            {
                CloseMenu();
            }
            else
            {
                ShowMainPage();
            }
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

        if (currentPage == MenuPage.Music)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                AdjustSelectedVolume(-VolumeStep);
                return;
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                AdjustSelectedVolume(VolumeStep);
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetSelectedVolumeToDefault();
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ActivateSelectedOption();
        }
    }

    private void MoveSelection(int direction)
    {
        if (currentOptionLabels.Count == 0) return;

        selectedIndex += direction;
        if (selectedIndex < 0)
        {
            selectedIndex = currentOptionLabels.Count - 1;
        }
        else if (selectedIndex >= currentOptionLabels.Count)
        {
            selectedIndex = 0;
        }

        RefreshVisualSelection();
    }

    private void ActivateSelectedOption()
    {
        switch (currentPage)
        {
            case MenuPage.Main:
                ActivateMainMenuOption();
                break;
            case MenuPage.SaveSlots:
                SaveToSelectedSlot();
                break;
            case MenuPage.LoadSlots:
                LoadFromSelectedSlot();
                break;
            case MenuPage.Music:
                if (selectedIndex == 2)
                {
                    ShowMainPage();
                }
                break;
            case MenuPage.KeyMapping:
            case MenuPage.Information:
                ShowMainPage();
                break;
        }
    }

    private void ActivateMainMenuOption()
    {
        switch (selectedIndex)
        {
            case 0:
                BackToMenu();
                break;
            case 1:
                ShowSaveSlotsPage();
                break;
            case 2:
                ShowLoadSlotsPage();
                break;
            case 3:
                ShowMusicPage();
                break;
            case 4:
                ShowKeyMappingPage();
                break;
            case 5:
                ShowInformationPage();
                break;
        }
    }

    private void SaveToSelectedSlot()
    {
        if (!SaveSystem.SaveToSlot(selectedIndex, out string message))
        {
            footerMessage = message;
            RefreshFooter();
            return;
        }

        footerMessage = message;
        RefreshCurrentPage();
    }

    private void LoadFromSelectedSlot()
    {
        if (!SaveSystem.LoadFromSlot(selectedIndex, out string message))
        {
            footerMessage = message;
            RefreshFooter();
            return;
        }

        CloseMenu();
    }

    private void AdjustSelectedVolume(float delta)
    {
        AudioManager audioManager = AudioManager.EnsureInstance();
        if (audioManager == null) return;

        if (selectedIndex == 0)
        {
            audioManager.SetBgmVolume(audioManager.BgmVolume + delta);
            footerMessage = "Adjusted BGM volume.";
        }
        else if (selectedIndex == 1)
        {
            audioManager.SetSfxVolume(audioManager.SfxVolume + delta);
            footerMessage = "Adjusted SFX volume.";
        }
        else
        {
            return;
        }

        RefreshCurrentPage();
    }

    private void ResetSelectedVolumeToDefault()
    {
        AudioManager audioManager = AudioManager.EnsureInstance();
        if (audioManager == null) return;

        if (selectedIndex == 0)
        {
            audioManager.ResetBgmVolumeToDefault();
            footerMessage = "BGM volume reset to default.";
        }
        else if (selectedIndex == 1)
        {
            audioManager.ResetSfxVolumeToDefault();
            footerMessage = "SFX volume reset to default.";
        }
        else
        {
            return;
        }

        RefreshCurrentPage();
    }

    private void ShowMainPage()
    {
        currentPage = MenuPage.Main;
        selectedIndex = 0;
        footerMessage = "W/S or Arrow Keys: Move   Enter: Select   Esc: Close";
        RefreshCurrentPage();
    }

    private void ShowSaveSlotsPage()
    {
        currentPage = MenuPage.SaveSlots;
        selectedIndex = 0;
        footerMessage = "Enter: Overwrite Save Slot   Esc: Back";
        RefreshCurrentPage();
    }

    private void ShowLoadSlotsPage()
    {
        currentPage = MenuPage.LoadSlots;
        selectedIndex = 0;
        footerMessage = "Enter: Load Selected Slot   Esc: Back";
        RefreshCurrentPage();
    }

    private void ShowMusicPage()
    {
        currentPage = MenuPage.Music;
        selectedIndex = 0;
        footerMessage = "Left/Right: Adjust   R: Reset to Default   Esc: Back";
        RefreshCurrentPage();
    }

    private void ShowKeyMappingPage()
    {
        currentPage = MenuPage.KeyMapping;
        selectedIndex = 0;
        footerMessage = "Enter or Esc: Back";
        RefreshCurrentPage();
    }

    private void ShowInformationPage()
    {
        currentPage = MenuPage.Information;
        selectedIndex = 0;
        footerMessage = "Enter or Esc: Back";
        RefreshCurrentPage();
    }

    private void RefreshCurrentPage()
    {
        if (overlayObject == null) return;

        currentOptionLabels.Clear();
        switch (currentPage)
        {
            case MenuPage.Main:
                titleText.text = "Pause Menu";
                descriptionText.text = "Choose a system action.";
                currentOptionLabels.Add("Back To Menu");
                currentOptionLabels.Add("Save");
                currentOptionLabels.Add("Load");
                currentOptionLabels.Add("Music");
                currentOptionLabels.Add("Key Mapping");
                currentOptionLabels.Add("Information");
                break;
            case MenuPage.SaveSlots:
                titleText.text = "Save";
                descriptionText.text = "Choose one of the ten save slots. Press Enter to overwrite the selected slot.";
                AppendSlotLabels();
                break;
            case MenuPage.LoadSlots:
                titleText.text = "Load";
                descriptionText.text = "Choose a save slot to restore. Boss scenes load from their scene start instead of battle state.";
                AppendSlotLabels();
                break;
            case MenuPage.Music:
                titleText.text = "Music";
                descriptionText.text = "Adjust the current audio mix.";
                currentOptionLabels.Add(BuildVolumeLabel("BGM", AudioManager.EnsureInstance().BgmVolume));
                currentOptionLabels.Add(BuildVolumeLabel("SFX", AudioManager.EnsureInstance().SfxVolume));
                currentOptionLabels.Add("Back");
                break;
            case MenuPage.KeyMapping:
                titleText.text = "Key Mapping";
                descriptionText.text = BuildKeyMappingText();
                currentOptionLabels.Add("Back");
                break;
            case MenuPage.Information:
                titleText.text = "Information";
                descriptionText.text = BuildInformationText();
                currentOptionLabels.Add("Back");
                break;
        }

        EnsureOptionTextCount(Mathf.Max(currentOptionLabels.Count, SaveSystem.SlotCount));
        for (int i = 0; i < optionTexts.Count; i++)
        {
            bool shouldShow = i < currentOptionLabels.Count;
            optionTexts[i].gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;
            optionTexts[i].text = currentOptionLabels[i];
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, currentOptionLabels.Count - 1));
        RefreshVisualSelection();
        RefreshFooter();
    }

    private void AppendSlotLabels()
    {
        slotSummaries = SaveSystem.GetSlotSummaries();
        for (int i = 0; i < slotSummaries.Count; i++)
        {
            currentOptionLabels.Add(BuildSlotLabel(slotSummaries[i]));
        }
    }

    private void RefreshVisualSelection()
    {
        for (int i = 0; i < optionTexts.Count; i++)
        {
            if (!optionTexts[i].gameObject.activeSelf) continue;

            bool isSelected = i == selectedIndex;
            optionTexts[i].color = isSelected ? new Color(1f, 0.92f, 0.45f, 1f) : Color.white;
            optionTexts[i].text = isSelected ? $"> {currentOptionLabels[i]}" : $"  {currentOptionLabels[i]}";
        }
    }

    private void RefreshFooter()
    {
        if (footerText != null)
        {
            footerText.text = footerMessage;
        }
    }

    private void BackToMenu()
    {
        CloseMenu();
        SceneTransition.LoadScene(mainMenuSceneName);
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

    private static string BuildVolumeLabel(string label, float volume)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f);
        int markerIndex = Mathf.RoundToInt(Mathf.Clamp01(volume) * VolumeBarSegmentCount);
        StringBuilder builder = new StringBuilder();
        builder.Append(label);
        builder.Append("  [");

        for (int i = 0; i <= VolumeBarSegmentCount; i++)
        {
            builder.Append(i == markerIndex ? '|' : '-');
        }

        builder.Append("] ");
        builder.Append(percent);
        builder.Append('%');
        return builder.ToString();
    }

    private static string BuildKeyMappingText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("W A S D: Move or menu navigation");
        builder.AppendLine("Arrow Keys: Move or menu navigation");
        builder.AppendLine("Enter: Confirm or interact");
        builder.AppendLine("Esc: Pause or back");
        builder.Append("P: Open pager phone when available");
        return builder.ToString();
    }

    private static string BuildInformationText()
    {
        return "Save and Load now use ten overwriteable slots. Stored data includes scene, exploration position, inventory, safe and door unlocks, and triggered story progression.";
    }

    private void BuildUi()
    {
        menuCanvas = gameObject.GetComponent<Canvas>();
        if (menuCanvas == null)
        {
            menuCanvas = gameObject.AddComponent<Canvas>();
        }

        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = short.MaxValue - 1;

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
        panelRect.anchorMin = new Vector2(0.1f, 0.07f);
        panelRect.anchorMax = new Vector2(0.9f, 0.93f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        titleText = CreateText("Title", panelObject.transform, 36, TextAnchor.UpperLeft, FontStyle.Bold);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(-72f, 54f);

        descriptionText = CreateText("Description", panelObject.transform, 18, TextAnchor.UpperLeft, FontStyle.Normal);
        RectTransform descriptionRect = descriptionText.GetComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0.5f, 1f);
        descriptionRect.anchoredPosition = new Vector2(0f, -84f);
        descriptionRect.sizeDelta = new Vector2(-72f, 96f);

        GameObject optionsRoot = CreateUiObject("Options", panelObject.transform);
        RectTransform optionsRect = optionsRoot.GetComponent<RectTransform>();
        optionsRect.anchorMin = new Vector2(0f, 0f);
        optionsRect.anchorMax = new Vector2(1f, 1f);
        optionsRect.offsetMin = new Vector2(28f, 86f);
        optionsRect.offsetMax = new Vector2(-28f, -176f);

        VerticalLayoutGroup optionLayout = optionsRoot.AddComponent<VerticalLayoutGroup>();
        optionLayout.childAlignment = TextAnchor.UpperLeft;
        optionLayout.childControlWidth = true;
        optionLayout.childControlHeight = false;
        optionLayout.childForceExpandWidth = true;
        optionLayout.childForceExpandHeight = false;
        optionLayout.spacing = 8f;

        EnsureOptionTextCount(SaveSystem.SlotCount, optionsRoot.transform);

        footerText = CreateText("Footer", panelObject.transform, 16, TextAnchor.LowerLeft, FontStyle.Italic);
        RectTransform footerRect = footerText.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.anchoredPosition = new Vector2(0f, 22f);
        footerRect.sizeDelta = new Vector2(-60f, 64f);
    }

    private void EnsureOptionTextCount(int requiredCount, Transform parentOverride = null)
    {
        Transform parent = parentOverride != null ? parentOverride : (optionTexts.Count > 0 ? optionTexts[0].transform.parent : null);
        if (parent == null && requiredCount > 0) return;

        while (optionTexts.Count < requiredCount)
        {
            Text optionText = CreateText($"Option_{optionTexts.Count + 1}", parent, 20, TextAnchor.MiddleLeft, FontStyle.Normal);
            LayoutElement layoutElement = optionText.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 28f;
            optionTexts.Add(optionText);
        }
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = GetBuiltinFont();
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

    private static Font GetBuiltinFont()
    {
        return Resources.GetBuiltinResource<Font>(BuiltinFontResourcePath);
    }
}
