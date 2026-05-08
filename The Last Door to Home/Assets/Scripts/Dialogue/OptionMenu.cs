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

    [Header("玩家物体（拖Player）")]
    public GameObject player;

    private int currentSelect;
    private List<OptionEntry> currentEntries = new List<OptionEntry>();
    private Action onClose;
    private bool closeDialogueWhenConfirmed;
    private RectTransform cursorRect;
    private float cursorFixedX;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (optionPanel != null) optionPanel.SetActive(false);

        cursorRect = cursor != null ? cursor.GetComponent<RectTransform>() : null;
        if (cursorRect != null)
        {
            cursorFixedX = cursorRect.anchoredPosition.x;
        }
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

        currentEntries = entries;
        closeDialogueWhenConfirmed = closeDialogueOnConfirm;
        onClose = onMenuClosed;
        optionPanel.SetActive(true);
        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        currentSelect = 0;

        for (int i = 0; i < options.Length; i++)
        {
            bool active = i < currentEntries.Count;
            options[i].gameObject.SetActive(active);
            if (active) options[i].text = currentEntries[i].text;
        }

        UpdateCursor();
    }

    void UpdateCursor()
    {
        if (cursorRect == null || options == null || options.Length == 0 || options[currentSelect] == null) return;

        RectTransform optionRect = options[currentSelect].GetComponent<RectTransform>();
        if (optionRect == null) return;

        Vector2 pos = cursorRect.anchoredPosition;
        pos.x = cursorFixedX;
        pos.y = optionRect.anchoredPosition.y;
        cursorRect.anchoredPosition = pos;
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
