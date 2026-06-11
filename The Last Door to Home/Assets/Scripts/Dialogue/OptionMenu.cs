using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

/*
Purpose: Displays interactive option choices and routes the player selection to callbacks.
Attached GameObject: Dialogue UI manager or option menu GameObject in the scene canvas.
Main responsibilities: Control dialogue UI state, react to input, and notify dependent gameplay systems.
Inputs: UI references, dialogue content arrays, callbacks, and player input.
Outputs or effects: Shows or hides UI, locks controls, and triggers dialogue-related side effects.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class OptionMenu : MonoBehaviour
{
    [Serializable]
    public class OptionEntry
    {
        public string text;
        public Func<bool> canExecute;
        public Action onExecute;
        public Action onBlocked;
    }

    public static OptionMenu Instance;

    [Header("UI")]
    public GameObject optionPanel;
    public TextMeshProUGUI[] options;
    [SerializeField] private int maxOptions = 4;
    [SerializeField] private float minPanelHeight = 120f;
    [SerializeField] private float panelPaddingTop = 20f;
    [SerializeField] private float panelPaddingBottom = 20f;
    [SerializeField] private float optionLineGap = 20f;

    [Header("玩家物体（拖Player）")]
    public GameObject player;

    private int currentSelect;
    private List<OptionEntry> currentEntries = new List<OptionEntry>();
    private Action onClose;
    private bool closeDialogueWhenConfirmed;
    private readonly List<TextMeshProUGUI> optionSlots = new List<TextMeshProUGUI>();
    private readonly List<string> currentOptionLabels = new List<string>();
    private float optionSpacing = 100f;
    private float optionItemHeight = 50f;
    private RectTransform optionPanelRect;
    private Vector2 optionPanelBaseAnchoredPos;
    private bool hasPanelBaseAnchoredPos;
    private TextMeshProUGUI optionTemplate;
    private float optionSlotsBaseCenterY;
    private bool hasOptionSlotsBaseCenterY;
    private readonly Color selectedTextColor = new Color(1f, 0.92f, 0.45f, 1f);
    private readonly Color normalTextColor = Color.white;

    // Initializes cached references and one-time component state before gameplay begins.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (optionPanel != null) optionPanel.SetActive(false);
        optionPanelRect = optionPanel != null ? optionPanel.GetComponent<RectTransform>() : null;
        if (optionPanelRect != null)
        {
            optionPanelBaseAnchoredPos = optionPanelRect.anchoredPosition;
            hasPanelBaseAnchoredPos = true;
        }

        InitializeOptionTemplate();
    }

    // Cleans up cached state and running effects during teardown.
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Processes per-frame input and keeps this behaviour responsive during gameplay.
    void Update()
    {
        if (EscapeMenuController.IsMenuOpen) return;
        if (InventoryMenuController.IsOpen) return;
        if (optionPanel == null || !optionPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            currentSelect = Mathf.Max(0, currentSelect - 1);
            RefreshVisualSelection();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            int maxIndex = Mathf.Max(0, currentEntries.Count - 1);
            currentSelect = Mathf.Min(maxIndex, currentSelect + 1);
            RefreshVisualSelection();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelect();
        }
    }

    // Shows the available pick options for this interaction.
    public void ShowPickOptions(PickableItem item)
    {
        if (item == null) return;

        var entries = new List<OptionEntry>
        {
            new OptionEntry
            {
                text = "Pick Up",
                canExecute = () => true,
                onExecute = () => item.PickUp()
            },
            new OptionEntry
            {
                text = "Leave",
                canExecute = () => true
            }
        };

        ShowOptions(entries, true, null);
    }

    // Shows the supplied option entries in the dialogue option menu.
    public void ShowOptions(List<OptionEntry> entries, bool closeDialogueOnConfirm = true, Action onMenuClosed = null)
    {
        if (optionPanel == null)
        {
            Debug.LogWarning("OptionMenu: optionPanel 引用为空或已销毁，无法显示选项。", this);
            if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(false);
            return;
        }

        if (entries == null || entries.Count == 0)
        {
            return;
        }

        int clampedCount = Mathf.Min(entries.Count, Mathf.Max(1, maxOptions));
        if (entries.Count > clampedCount)
        {
            Debug.LogWarning($"OptionMenu: 收到 {entries.Count} 个选项，但当前最多只显示 {clampedCount} 个。多余选项将被忽略。", this);
        }

        EnsureOptionSlots(clampedCount);

        currentEntries = new List<OptionEntry>(clampedCount);
        currentOptionLabels.Clear();
        for (int i = 0; i < clampedCount; i++)
        {
            currentEntries.Add(entries[i]);
            currentOptionLabels.Add(entries[i].text);
        }

        closeDialogueWhenConfirmed = closeDialogueOnConfirm;
        onClose = onMenuClosed;
        optionPanel.SetActive(true);
        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        currentSelect = 0;

        for (int i = 0; i < optionSlots.Count; i++)
        {
            bool active = i < currentEntries.Count;
            optionSlots[i].gameObject.SetActive(active);
        }

        ResizeOptionPanel(currentEntries.Count);
        RefreshVisualSelection();
    }

    // Refreshes the option text styling to match the pause menu selection style.
    void RefreshVisualSelection()
    {
        if (currentEntries == null || currentEntries.Count == 0) return;

        for (int i = 0; i < optionSlots.Count; i++)
        {
            TextMeshProUGUI optionText = optionSlots[i];
            if (optionText == null) continue;

            bool active = i < currentEntries.Count;
            optionText.gameObject.SetActive(active);
            if (!active) continue;

            bool isSelected = i == currentSelect;
            string label = i < currentOptionLabels.Count ? currentOptionLabels[i] : currentEntries[i].text;
            optionText.text = isSelected ? $"> {label}" : $"  {label}";
            optionText.color = isSelected ? selectedTextColor : normalTextColor;
        }
    }

    // Initializes the reusable option UI template reference.
    private void InitializeOptionTemplate()
    {
        optionSlots.Clear();
        if (options == null || options.Length == 0) return;

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == null) continue;
            optionTemplate = options[i];
            optionSlots.Add(optionTemplate);
            break;
        }

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == null || options[i] == optionTemplate) continue;
            options[i].gameObject.SetActive(false);
        }

        if (optionSlots.Count == 0) return;

        RectTransform templateRect = optionTemplate.GetComponent<RectTransform>();
        if (templateRect != null)
        {
            optionItemHeight = Mathf.Max(1f, templateRect.sizeDelta.y);
            optionSpacing = optionItemHeight + Mathf.Max(0f, optionLineGap);
            optionSlotsBaseCenterY = templateRect.anchoredPosition.y;
            hasOptionSlotsBaseCenterY = true;
        }
    }

    // Ensures the option menu has enough UI slots for the current entries.
    private void EnsureOptionSlots(int requiredCount)
    {
        if (requiredCount <= 0) return;
        if (optionTemplate == null) InitializeOptionTemplate();
        if (optionTemplate == null) return;

        RectTransform templateRect = optionTemplate.GetComponent<RectTransform>();
        if (!hasOptionSlotsBaseCenterY && templateRect != null)
        {
            optionSlotsBaseCenterY = templateRect.anchoredPosition.y;
            hasOptionSlotsBaseCenterY = true;
        }

        for (int i = optionSlots.Count; i < requiredCount; i++)
        {
            TextMeshProUGUI slot = Instantiate(optionTemplate, optionTemplate.transform.parent);
            slot.name = $"{optionTemplate.name}_Auto_{i + 1}";

            RectTransform rect = slot.GetComponent<RectTransform>();
            if (rect != null && templateRect != null)
            {
                rect.anchoredPosition = templateRect.anchoredPosition;
                rect.sizeDelta = templateRect.sizeDelta;
            }

            slot.gameObject.SetActive(false);
            optionSlots.Add(slot);
        }
    }

    // Resizes the option panel to fit the active entry count.
    private void ResizeOptionPanel(int count)
    {
        if (optionPanel == null) return;
        RectTransform panelRect = optionPanelRect != null ? optionPanelRect : optionPanel.GetComponent<RectTransform>();
        if (panelRect == null) return;

        if (!hasPanelBaseAnchoredPos)
        {
            optionPanelBaseAnchoredPos = panelRect.anchoredPosition;
            hasPanelBaseAnchoredPos = true;
        }

        float spacing = Mathf.Max(0f, optionSpacing - optionItemHeight);
        float contentHeight = count * optionItemHeight + Mathf.Max(0, count - 1) * spacing;
        float targetHeight = panelPaddingTop + panelPaddingBottom + contentHeight;

        Vector2 size = panelRect.sizeDelta;
        size.y = Mathf.Max(minPanelHeight, targetHeight);
        panelRect.sizeDelta = size;

        panelRect.anchoredPosition = optionPanelBaseAnchoredPos;
        RepositionOptionSlots(count);
    }

    // Repositions option slots so the current menu layout stays aligned.
    private void RepositionOptionSlots(int count)
    {
        if (count <= 0) return;
        if (!hasOptionSlotsBaseCenterY) return;

        float startY = optionSlotsBaseCenterY + ((count - 1) * 0.5f * optionSpacing);
        for (int i = 0; i < optionSlots.Count; i++)
        {
            RectTransform rect = optionSlots[i] != null ? optionSlots[i].GetComponent<RectTransform>() : null;
            if (rect == null) continue;

            Vector2 anchored = rect.anchoredPosition;
            anchored.y = startY - i * optionSpacing;
            rect.anchoredPosition = anchored;
        }
    }

    // Confirms the currently highlighted option and runs its callback.
    void ConfirmSelect()
    {
        if (currentEntries == null || currentEntries.Count == 0) return;
        if (currentSelect < 0 || currentSelect >= currentEntries.Count) return;

        OptionEntry entry = currentEntries[currentSelect];
        bool canExec = entry.canExecute == null || entry.canExecute.Invoke();

        if (!canExec)
        {
            if (optionPanel != null) optionPanel.SetActive(false);
            if (closeDialogueWhenConfirmed && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.CloseDialogueAfterOption();
            }
            entry.onBlocked?.Invoke();
            onClose?.Invoke();

            if (DialogueManager.Instance != null)
            {
                if (!DialogueManager.Instance.IsDialogueActive)
                {
                    DialogueManager.Instance.LockPlayer(false);
                }
            }
            return;
        }

        if (optionPanel != null) optionPanel.SetActive(false);
        if (closeDialogueWhenConfirmed && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CloseDialogueAfterOption();
        }

        entry.onExecute?.Invoke();
        onClose?.Invoke();

        if (DialogueManager.Instance != null)
        {
            if (!DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.LockPlayer(false);
            }
        }
    }
}
