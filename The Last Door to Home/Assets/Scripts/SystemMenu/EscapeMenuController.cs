using System.Collections.Generic;
using System.Text;
using TMPro;
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
        Information
    }

    private const string DefaultMainMenuSceneName = "MainMenu";
    private const string AnimScenesFolderToken = "/anim_scenes/";
    private const string PauseMenuPrefabResourcePath = "SystemMenu/PauseMenu";
    private const float VolumeStep = 0.05f;
    private const int VolumeBarSegmentCount = 12;

    private static EscapeMenuController instance;

    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;

    private Canvas menuCanvas;
    private GameObject overlayObject;
    private Component titleText;
    private Component descriptionText;
    private Component footerText;
    private Component aboutGameText;
    private GameObject musicTextRoot;
    private Component backgroundMusicText;
    private Component soundEffectsText;
    private readonly List<Component> optionTexts = new List<Component>();
    private readonly List<Component> slotOptionTexts = new List<Component>();
    private readonly List<string> currentOptionLabels = new List<string>();
    private readonly List<Component> currentDisplayTargets = new List<Component>();
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
        if (!BuildUiFromPrefab())
        {
            Debug.LogWarning($"EscapeMenuController: Could not load required prefab at Resources/{PauseMenuPrefabResourcePath}.prefab", this);
        }
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
        if (overlayObject == null) return;

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
        int optionCount = currentDisplayTargets.Count;
        if (optionCount == 0) return;

        selectedIndex += direction;
        if (selectedIndex < 0)
        {
            selectedIndex = optionCount - 1;
        }
        else if (selectedIndex >= optionCount)
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
                break;
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
        currentDisplayTargets.Clear();
        SetComponentActive(aboutGameText, false);
        SetGameObjectActive(musicTextRoot, false);
        SetTextPoolVisible(optionTexts, false);
        SetTextPoolVisible(slotOptionTexts, false);

        switch (currentPage)
        {
            case MenuPage.Main:
                SetTextValue(titleText, "Pause Menu");
                SetOptionalText(descriptionText, "Choose a system action.");
                currentOptionLabels.Add("Back To Menu");
                currentOptionLabels.Add("Save");
                currentOptionLabels.Add("Load");
                currentOptionLabels.Add("Music");
                currentOptionLabels.Add("About Game");
                AddSequentialTargets(optionTexts, currentOptionLabels.Count);
                break;
            case MenuPage.SaveSlots:
                SetTextValue(titleText, "Save");
                SetOptionalText(descriptionText, "Choose one of the ten save slots. Press Enter to overwrite the selected slot.");
                AppendSlotLabels();
                AddSequentialTargets(GetSlotOptionTextPool(), currentOptionLabels.Count);
                break;
            case MenuPage.LoadSlots:
                SetTextValue(titleText, "Load");
                SetOptionalText(descriptionText, "Choose a save slot to restore. Boss scenes load from their scene start instead of battle state.");
                AppendSlotLabels();
                AddSequentialTargets(GetSlotOptionTextPool(), currentOptionLabels.Count);
                break;
            case MenuPage.Music:
                SetTextValue(titleText, "Music");
                SetOptionalText(descriptionText, string.Empty);
                currentOptionLabels.Add(BuildVolumeLabel("Background Music", AudioManager.EnsureInstance().BgmVolume));
                currentOptionLabels.Add(BuildVolumeLabel("Sound Effects", AudioManager.EnsureInstance().SfxVolume));

                if (backgroundMusicText != null && soundEffectsText != null)
                {
                    SetGameObjectActive(musicTextRoot, true);
                    currentDisplayTargets.Add(backgroundMusicText);
                    currentDisplayTargets.Add(soundEffectsText);
                }
                else
                {
                    AddSequentialTargets(optionTexts, currentOptionLabels.Count);
                }
                break;
            case MenuPage.Information:
                SetTextValue(titleText, "About Game");
                SetOptionalText(descriptionText, string.Empty);
                SetComponentActive(aboutGameText, aboutGameText != null);
                break;
            default:
                SetOptionalText(descriptionText, string.Empty);
                break;
        }

        for (int i = 0; i < currentDisplayTargets.Count; i++)
        {
            currentDisplayTargets[i].gameObject.SetActive(true);
            SetTextValue(currentDisplayTargets[i], currentOptionLabels[i]);
            ApplyEntryLayout(currentDisplayTargets[i]);
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, currentDisplayTargets.Count - 1));
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
        for (int i = 0; i < currentDisplayTargets.Count; i++)
        {
            bool isSelected = i == selectedIndex;
            SetTextColor(currentDisplayTargets[i], isSelected ? new Color(1f, 0.92f, 0.45f, 1f) : Color.white);
            SetTextValue(currentDisplayTargets[i], isSelected ? $"> {currentOptionLabels[i]}" : $"  {currentOptionLabels[i]}");
            ApplyEntryLayout(currentDisplayTargets[i]);
        }
    }

    private void RefreshFooter()
    {
        if (footerText != null)
        {
            SetTextValue(footerText, footerMessage);
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
        builder.AppendLine();
        builder.Append("[");

        for (int i = 0; i <= VolumeBarSegmentCount; i++)
        {
            builder.Append(i == markerIndex ? '|' : '-');
        }

        builder.Append("] ");
        builder.Append(percent);
        builder.Append('%');
        return builder.ToString();
    }

    private bool BuildUiFromPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(PauseMenuPrefabResourcePath);
        if (prefab == null)
        {
            return false;
        }

        overlayObject = Instantiate(prefab, transform);
        overlayObject.name = prefab.name;

        menuCanvas = overlayObject.GetComponent<Canvas>();
        if (menuCanvas == null)
        {
            menuCanvas = overlayObject.GetComponentInChildren<Canvas>(true);
        }

        if (menuCanvas == null)
        {
            Destroy(overlayObject);
            overlayObject = null;
            return false;
        }

        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = menuCanvas.gameObject.AddComponent<CanvasScaler>();
        }

        if (menuCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            menuCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (menuCanvas.renderMode == RenderMode.ScreenSpaceCamera && menuCanvas.worldCamera == null)
        {
            menuCanvas.worldCamera = Camera.main;
        }

        titleText = FindRequiredTextComponent(overlayObject.transform, "Title");
        descriptionText = FindRequiredTextComponent(overlayObject.transform, "Description");
        footerText = FindRequiredTextComponent(overlayObject.transform, "Footer");
        aboutGameText = FindRequiredTextComponent(overlayObject.transform, "AboutGameText");
        Transform musicTextTransform = FindChildRecursive(overlayObject.transform, "MusicText");
        musicTextRoot = musicTextTransform != null ? musicTextTransform.gameObject : null;
        backgroundMusicText = FindRequiredTextComponent(overlayObject.transform, "BackgroundMusicText");
        soundEffectsText = FindRequiredTextComponent(overlayObject.transform, "SoundEffectsText");
        Transform optionsRoot = FindChildRecursive(overlayObject.transform, "Options");
        Transform slotOptionsRoot = FindChildRecursive(overlayObject.transform, "SlotOptions");

        if (titleText == null || footerText == null || optionsRoot == null)
        {
            Destroy(overlayObject);
            overlayObject = null;
            titleText = null;
            descriptionText = null;
            footerText = null;
            aboutGameText = null;
            return false;
        }

        LoadTextPoolFromContainer(optionTexts, optionsRoot);

        if (slotOptionsRoot != null)
        {
            LoadTextPoolFromContainer(slotOptionTexts, slotOptionsRoot);
        }
        else
        {
            slotOptionTexts.Clear();
        }

        if (aboutGameText != null)
        {
            aboutGameText.gameObject.SetActive(false);
        }

        SetGameObjectActive(musicTextRoot, false);

        return true;
    }

    private List<Component> GetSlotOptionTextPool()
    {
        return slotOptionTexts.Count > 0 ? slotOptionTexts : optionTexts;
    }

    private void LoadTextPoolFromContainer(List<Component> targetPool, Transform parent)
    {
        targetPool.Clear();
        if (parent == null) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            Component optionText = GetSupportedTextComponent(parent.GetChild(i));
            if (optionText != null)
            {
                targetPool.Add(optionText);
            }
        }
    }

    private void AddSequentialTargets(List<Component> sourcePool, int count)
    {
        int targetCount = Mathf.Min(sourcePool.Count, count);
        for (int i = 0; i < targetCount; i++)
        {
            currentDisplayTargets.Add(sourcePool[i]);
        }
    }

    private static void SetTextPoolVisible(List<Component> targetPool, bool visible)
    {
        for (int i = 0; i < targetPool.Count; i++)
        {
            targetPool[i].gameObject.SetActive(visible && targetPool[i].gameObject.activeSelf);
            if (!visible)
            {
                targetPool[i].gameObject.SetActive(false);
            }
        }
    }

    private void ApplyEntryLayout(Component textComponent)
    {
        if (textComponent == null) return;

        LayoutElement layoutElement = textComponent.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = textComponent.gameObject.AddComponent<LayoutElement>();
        }

        string currentText = GetTextValue(textComponent);
        bool isMultiline = !string.IsNullOrEmpty(currentText) && currentText.Contains("\n");
        layoutElement.preferredHeight = isMultiline ? 72f : 28f;
    }

    private static void SetOptionalText(Component textComponent, string value)
    {
        SetTextValue(textComponent, value);
        SetComponentActive(textComponent, !string.IsNullOrEmpty(value));
    }

    private static void SetComponentActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private static void SetGameObjectActive(GameObject gameObject, bool active)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(active);
        }
    }

    private static Component FindRequiredTextComponent(Transform root, string objectName)
    {
        Transform target = FindChildRecursive(root, objectName);
        return target != null ? GetSupportedTextComponent(target) : null;
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

    private static string GetTextValue(Component textComponent)
    {
        if (textComponent is Text legacyText)
        {
            return legacyText.text;
        }

        if (textComponent is TMP_Text tmpText)
        {
            return tmpText.text;
        }

        return string.Empty;
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
}
