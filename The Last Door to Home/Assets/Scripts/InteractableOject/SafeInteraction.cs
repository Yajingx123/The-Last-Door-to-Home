using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
/*
Purpose: Implements a world interaction used by the player interaction system.
Attached GameObject: Scene object with a Collider2D and interaction-specific serialized settings.
Main responsibilities: Checks interaction requirements, updates inventory/story/scene state, and provides player feedback.
Inputs: Player interaction calls, serialized IDs/text, inventory state, story flags, and optional audio or scene settings.
Outputs or effects: Starts dialogue, changes locked/collected state, updates Inventory/StoryFlags, plays audio, or triggers scene flow.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify successful interaction, missing-requirement feedback, repeated interaction behavior, and save/load persistence.
*/

public class SafeInteraction : MonoBehaviour, IInteractable
{
    [Header("保险箱唯一ID（用于跨场景记忆解锁状态） / Safe Unique ID (Remembers Unlock Across Scenes)")]
    public string safeUniqueID = "safe_01";

    [Header("密码设置 / Password Settings")]
    public string correctPassword = "0420";

    [Header("对白：未破解 / Dialogue: Locked")]
    [TextArea(2, 8)]
    public string[] lockedDialogues;

    [Header("对白：密码错误 / Dialogue: Wrong Password")]
    [TextArea(2, 8)]
    public string[] wrongPasswordDialogues = new string[] { "密码错误。" };

    [Header("对白：破解后（最后一句会出现拾取选项） / Dialogue: Unlocked (Pickup Option On Last Line)")]
    [TextArea(2, 8)]
    public string[] unlockedDialogues;

    [Header("对白：奖励已拾取后 / Dialogue: Reward Already Collected")]
    [TextArea(2, 8)]
    public string[] afterLootDialogues = new string[] { "保险箱已经空了。" };

    [Header("奖励物品（挂了PickableItem的物体） / Reward Item (Object With PickableItem)")]
    public PickableItem rewardItem;

    [Header("密码UI（4位） / Password UI (4 Digits)")]
    public GameObject passwordPanel;
    public TextMeshProUGUI[] digitTexts;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    private bool isUnlocked;
    private bool isPasswordInputActive;
    private int[] currentDigits = new int[4];
    private int currentDigitIndex;
    private int passwordOpenFrame = -1;

    // Initializes component references and singleton ownership before Start runs.
    void Awake()
    {
        isUnlocked = Inventory.IsSafeUnlocked(safeUniqueID);
        if (passwordPanel != null) passwordPanel.SetActive(false);
        ResetPasswordInput();
    }

    // Reads per-frame input and updates frame-dependent runtime state.
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

    // Handles player interaction with this object.
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

        // Locked state: play dialogue first, then open the four-digit password input.
        if (lockedDialogues != null && lockedDialogues.Length > 0)
        {
            DialogueManager.Instance.ShowDialogue(lockedDialogues, null, OpenPasswordInput);
        }
        else
        {
            OpenPasswordInput();
        }
    }

    // Opens the related UI or gameplay flow.
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

    // Attempts the requested operation and reports whether it succeeded.
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

    // Searches the scene hierarchy or data collection for the requested target.
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

    // Returns the requested value or runtime object.
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

    // Submits the current input for the submit password flow.
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

    // Closes the related UI or gameplay flow.
    private void ClosePasswordInput()
    {
        isPasswordInputActive = false;
        if (passwordPanel != null) passwordPanel.SetActive(false);
    }

    // Resets the reset password input state to its default value.
    private void ResetPasswordInput()
    {
        for (int i = 0; i < currentDigits.Length; i++) currentDigits[i] = 0;
        currentDigitIndex = 0;
        RefreshPasswordUI();
    }

    // Refreshes UI text, selection, or cached runtime data.
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

    // Builds data or UI objects required by this system.
    private string BuildPasswordString()
    {
        return string.Concat(
            currentDigits[0].ToString(),
            currentDigits[1].ToString(),
            currentDigits[2].ToString(),
            currentDigits[3].ToString()
        );
    }

    // Shows the show unlocked dialogue with pick UI or dialogue flow.
    private void ShowUnlockedDialogueWithPick()
    {
        if (DialogueManager.Instance == null) return;

        // After unlocking, the final dialogue line opens the pickup option.
        if (rewardItem != null)
        {
            DialogueManager.Instance.ShowDialogue(unlockedDialogues, rewardItem);
        }
        else
        {
            ShowFallbackDialogue(unlockedDialogues, "保险箱打开了。", false);
        }
    }

    // Continues the continue unlocked dialogue with pick after password flow from its current state.
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

    // Returns whether is reward collected is true for the current state.
    private bool IsRewardCollected()
    {
        if (rewardItem == null) return false;
        return Inventory.HasCollected(rewardItem.itemUniqueID);
    }

    // Shows the show fallback dialogue UI or dialogue flow.
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
