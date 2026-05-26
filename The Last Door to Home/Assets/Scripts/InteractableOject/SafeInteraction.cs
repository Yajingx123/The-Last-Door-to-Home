using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class SafeInteraction : MonoBehaviour, IInteractable
{
    [Header("保险箱唯一ID（用于跨场景记忆解锁状态）")]
    public string safeUniqueID = "safe_01";

    [Header("密码设置")]
    public string correctPassword = "0420";

    [Header("对白：未破解")]
    [TextArea(2, 8)]
    public string[] lockedDialogues;

    [Header("对白：密码错误")]
    [TextArea(2, 8)]
    public string[] wrongPasswordDialogues = new string[] { "密码错误。" };

    [Header("对白：破解后（最后一句会出现拾取选项）")]
    [TextArea(2, 8)]
    public string[] unlockedDialogues;

    [Header("对白：奖励已拾取后")]
    [TextArea(2, 8)]
    public string[] afterLootDialogues = new string[] { "保险箱已经空了。" };

    [Header("奖励物品（挂了PickableItem的物体）")]
    public PickableItem rewardItem;

    [Header("密码UI（4位）")]
    public GameObject passwordPanel;
    public TextMeshProUGUI[] digitTexts;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    private bool isUnlocked;
    private bool isPasswordInputActive;
    private int[] currentDigits = new int[4];
    private int currentDigitIndex;
    private int passwordOpenFrame = -1;

    void Awake()
    {
        isUnlocked = Inventory.IsSafeUnlocked(safeUniqueID);
        if (passwordPanel != null) passwordPanel.SetActive(false);
        ResetPasswordInput();
    }

    void Update()
    {
        if (!isPasswordInputActive) return;
        if (Time.frameCount == passwordOpenFrame) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentDigitIndex = Mathf.Max(0, currentDigitIndex - 1);
            RefreshPasswordUI();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentDigitIndex = Mathf.Min(3, currentDigitIndex + 1);
            RefreshPasswordUI();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            currentDigits[currentDigitIndex] = (currentDigits[currentDigitIndex] + 1) % 10;
            RefreshPasswordUI();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            currentDigits[currentDigitIndex] = (currentDigits[currentDigitIndex] + 9) % 10;
            RefreshPasswordUI();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SubmitPassword();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePasswordInput();
            if (DialogueManager.Instance != null) DialogueManager.Instance.CloseDialogueAfterOption();
        }
    }

    public void OnInteract()
    {
        if (DialogueManager.Instance == null) return;

        if (IsRewardCollected())
        {
            ShowFallbackDialogue(afterLootDialogues, "保险箱已经空了。");
            return;
        }

        if (isUnlocked)
        {
            ShowUnlockedDialogueWithPick();
            return;
        }

        // 未破解：先对话，最后一句打开4位密码输入面板
        if (lockedDialogues != null && lockedDialogues.Length > 0)
        {
            DialogueManager.Instance.ShowDialogue(lockedDialogues, null, OpenPasswordInput);
        }
        else
        {
            OpenPasswordInput();
        }
    }

    private void OpenPasswordInput()
    {
        TryResolvePasswordUIReferences();

        if (passwordPanel == null || digitTexts == null || digitTexts.Length < 4)
        {
            Debug.LogWarning("SafeInteraction: passwordPanel 或 digitTexts 未正确配置（需要4个数字文本）。", this);
            ShowFallbackDialogue(new string[] { "Password panel is not configured." }, "Password panel is not configured.", true);
            return;
        }

        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);

        ResetPasswordInput();
        isPasswordInputActive = true;
        passwordOpenFrame = Time.frameCount;
        passwordPanel.SetActive(true);
        RefreshPasswordUI();
    }

    private void TryResolvePasswordUIReferences()
    {
        if (passwordPanel == null)
        {
            Transform panelTransform = transform.Find("PasswordPanel");
            if (panelTransform == null)
            {
                panelTransform = FindChildByName(transform, "PasswordPanel");
            }
            if (panelTransform != null) passwordPanel = panelTransform.gameObject;
        }

        bool needRebindDigits = digitTexts == null || digitTexts.Length < 4;
        if (!needRebindDigits)
        {
            for (int i = 0; i < digitTexts.Length; i++)
            {
                if (digitTexts[i] == null)
                {
                    needRebindDigits = true;
                    break;
                }
            }
        }

        if (!needRebindDigits) return;

        List<TextMeshProUGUI> found = new List<TextMeshProUGUI>();
        if (passwordPanel != null)
        {
            found.AddRange(passwordPanel.GetComponentsInChildren<TextMeshProUGUI>(true));
        }

        if (found.Count < 4)
        {
            found.Clear();
            found.AddRange(GetComponentsInChildren<TextMeshProUGUI>(true));
        }

        List<TextMeshProUGUI> digitCandidates = new List<TextMeshProUGUI>();
        for (int i = 0; i < found.Count; i++)
        {
            TextMeshProUGUI tmp = found[i];
            if (tmp == null) continue;

            string lowerName = tmp.name.ToLowerInvariant();
            if (lowerName.Contains("digit") || lowerName.Contains("num") || lowerName.Contains("code"))
            {
                digitCandidates.Add(tmp);
            }
        }

        List<TextMeshProUGUI> source = digitCandidates.Count >= 4 ? digitCandidates : found;
        if (source.Count >= 4)
        {
            source.Sort((a, b) => string.CompareOrdinal(GetHierarchyPath(a.transform), GetHierarchyPath(b.transform)));
            digitTexts = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++) digitTexts[i] = source[i];
        }
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName) return child;
            Transform nested = FindChildByName(child, targetName);
            if (nested != null) return nested;
        }
        return null;
    }

    private string GetHierarchyPath(Transform node)
    {
        if (node == null) return string.Empty;

        Stack<string> names = new Stack<string>();
        Transform current = node;
        while (current != null)
        {
            names.Push(current.GetSiblingIndex().ToString("D3"));
            current = current.parent;
        }

        return string.Join("/", names.ToArray());
    }

    private void SubmitPassword()
    {
        string input = BuildPasswordString();

        ClosePasswordInput();

        if (input == correctPassword)
        {
            isUnlocked = true;
            Inventory.MarkSafeUnlocked(safeUniqueID);
            ContinueUnlockedDialogueWithPickAfterPassword();
            return;
        }

        ShowFallbackDialogue(wrongPasswordDialogues, "密码错误。", true);
    }

    private void ClosePasswordInput()
    {
        isPasswordInputActive = false;
        if (passwordPanel != null) passwordPanel.SetActive(false);
    }

    private void ResetPasswordInput()
    {
        for (int i = 0; i < currentDigits.Length; i++) currentDigits[i] = 0;
        currentDigitIndex = 0;
        RefreshPasswordUI();
    }

    private void RefreshPasswordUI()
    {
        if (digitTexts == null || digitTexts.Length < 4) return;

        for (int i = 0; i < 4; i++)
        {
            if (digitTexts[i] == null) continue;
            digitTexts[i].text = currentDigits[i].ToString();
            digitTexts[i].color = (i == currentDigitIndex) ? selectedColor : normalColor;
        }
    }

    private string BuildPasswordString()
    {
        return string.Concat(
            currentDigits[0].ToString(),
            currentDigits[1].ToString(),
            currentDigits[2].ToString(),
            currentDigits[3].ToString()
        );
    }

    private void ShowUnlockedDialogueWithPick()
    {
        if (DialogueManager.Instance == null) return;

        // 破解后的对白最后一句弹出拾取选项；拾取提示由PickableItem内部处理
        if (rewardItem != null)
        {
            DialogueManager.Instance.ShowDialogue(unlockedDialogues, rewardItem);
        }
        else
        {
            ShowFallbackDialogue(unlockedDialogues, "保险箱打开了。", false);
        }
    }

    private void ContinueUnlockedDialogueWithPickAfterPassword()
    {
        if (DialogueManager.Instance == null) return;

        if (rewardItem != null)
        {
            DialogueManager.Instance.ContinueDialogueAfterOption(unlockedDialogues, rewardItem);
        }
        else
        {
            ShowFallbackDialogue(unlockedDialogues, "保险箱打开了。", true);
        }
    }

    private bool IsRewardCollected()
    {
        if (rewardItem == null) return false;
        return Inventory.HasCollected(rewardItem.itemUniqueID);
    }

    private void ShowFallbackDialogue(string[] lines, string fallback, bool continueAfterOption = false)
    {
        if (DialogueManager.Instance == null) return;

        if (lines != null && lines.Length > 0)
        {
            if (continueAfterOption)
            {
                DialogueManager.Instance.ContinueDialogueAfterOption(lines);
            }
            else
            {
                DialogueManager.Instance.ShowDialogue(lines);
            }
        }
        else
        {
            if (continueAfterOption)
            {
                DialogueManager.Instance.ContinueDialogueAfterOption(new string[] { fallback });
            }
            else
            {
                DialogueManager.Instance.ShowDialogue(new string[] { fallback });
            }
        }
    }
}
