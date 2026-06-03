using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

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
    public Image cursor;
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
    private RectTransform cursorRect;
    private float cursorFixedX;
    private readonly List<TextMeshProUGUI> optionSlots = new List<TextMeshProUGUI>();
    private float optionSpacing = 100f;
    private float optionItemHeight = 50f;
    private RectTransform optionPanelRect;
    private Vector2 optionPanelBaseAnchoredPos;
    private bool hasPanelBaseAnchoredPos;
    private TextMeshProUGUI optionTemplate;
    private float optionSlotsBaseCenterY;
    private bool hasOptionSlotsBaseCenterY;

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

        cursorRect = cursor != null ? cursor.GetComponent<RectTransform>() : null;
        if (cursorRect != null)
        {
            cursorFixedX = cursorRect.anchoredPosition.x;
        }

        InitializeOptionTemplate();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (optionPanel == null || !optionPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            currentSelect = Mathf.Max(0, currentSelect - 1);
            UpdateCursor();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            int maxIndex = Mathf.Max(0, currentEntries.Count - 1);
            currentSelect = Mathf.Min(maxIndex, currentSelect + 1);
            UpdateCursor();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelect();
        }
    }

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

        EnsureOptionSlots(entries.Count);

        currentEntries = new List<OptionEntry>(entries);
        closeDialogueWhenConfirmed = closeDialogueOnConfirm;
        onClose = onMenuClosed;
        optionPanel.SetActive(true);
        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        currentSelect = 0;

        for (int i = 0; i < optionSlots.Count; i++)
        {
            bool active = i < currentEntries.Count;
            optionSlots[i].gameObject.SetActive(active);
            if (active) optionSlots[i].text = currentEntries[i].text;
        }

        ResizeOptionPanel(currentEntries.Count);
        UpdateCursor();
    }

    void UpdateCursor()
    {
        if (cursorRect == null || currentEntries == null || currentEntries.Count == 0) return;
        if (currentSelect < 0 || currentSelect >= currentEntries.Count) return;
        if (currentSelect >= optionSlots.Count || optionSlots[currentSelect] == null) return;

        RectTransform optionRect = optionSlots[currentSelect].GetComponent<RectTransform>();
        if (optionRect == null) return;

        Vector2 pos = cursorRect.anchoredPosition;
        pos.x = cursorFixedX;
        pos.y = optionRect.anchoredPosition.y;
        cursorRect.anchoredPosition = pos;
    }

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
