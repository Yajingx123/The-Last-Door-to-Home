using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
Purpose: Provides the shared two-phase ending flow used by story and battle defeat sequences.
Attached GameObject: A component that triggers or owns a two-stage ending sequence.
Main responsibilities: Lock the player, fade the player out, run first and second dialogue phases, manage blackout, and finish the ending.
Inputs: Serialized dialogue content, optional images and audio settings, and scene-finish configuration supplied by derived classes.
Outputs or effects: Centralizes the shared ending presentation so individual triggers only define how the sequence starts.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify the dialogue ordering, blackout layering, player fade, and final scene transition in Play Mode.
*/

public abstract class SharedEndingSequence : MonoBehaviour
{
    [Header("第一段对话")]
    [TextArea(2, 8)]
    public string[] firstDialogues;
    public Sprite firstDialogueImage;
    [Min(1)]
    public int firstDialogueImageStartLine = 1;
    [Tooltip("第一段配图从第几句开始时关闭。若大于第一段对话数量，则第一段播完后关闭。")]
    [Min(1)]
    public int firstDialogueImageHideLine = 2;
    [Header("第一段配图出场背景渐变")]
    [SerializeField] private float firstImageDarkenDuration = 0.35f;
    [SerializeField] private float firstImageBrightenDuration = 0.45f;
    [Range(0f, 1f)]
    [SerializeField] private float firstImageBackgroundDarkAlpha = 1f;

    [Header("第二段结局对话")]
    [TextArea(2, 8)]
    public string[] endingDialogues;
    public Sprite endingImage;
    public DialogueAudioSettings endingDialogueAudioSettings;

    [Header("场景切换")]
    public string mainMenuSceneName = "MainMenu";
    public bool clearInventoryOnFinish = false;

    [Header("玩家消失效果")]
    [Tooltip("Player 从第一段对话的第几句开始消失。若大于第一段对话数量，则第一段播完后再消失，然后进入黑屏。")]
    [Min(1)]
    [SerializeField] private int playerFadeStartLine = 1;
    [SerializeField] private float playerFadeDuration = 1.2f;
    [SerializeField] private float postFadeDelay = 0.15f;

    [Header("第二段前黑屏")]
    [SerializeField] private float blackoutFadeDuration = 0.8f;
    [SerializeField] private Color blackoutColor = Color.black;

    private bool triggered;
    private bool isPlayerFadeComplete;
    private bool isPlayerFadeStarted;
    private bool isWaitingToShowFirstDialogueImage;
    private bool isWaitingToHideFirstDialogueImage;
    private bool isFirstDialogueImageVisible;
    private bool isWaitingToStartPlayerFade;
    private GameObject pendingPlayerObject;
    private GameObject blackoutOverlay;
    private CanvasGroup blackoutCanvasGroup;
    private Coroutine firstDialogueImageRevealRoutine;
    private Coroutine firstDialogueImageCloseRoutine;

    // Starts the shared ending flow once and ignores repeat triggers.
    protected void StartEnding(GameObject playerObject)
    {
        if (triggered)
        {
            return;
        }

        triggered = true;
        StartCoroutine(PlayEndingSequence(playerObject));
    }

    // Starts the ending sequence flow after the trigger conditions are met.
    private IEnumerator PlayEndingSequence(GameObject playerObject)
    {
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            dialogueManager.LockPlayer(true);
        }

        PlayerMove playerMove = playerObject != null ? playerObject.GetComponent<PlayerMove>() : null;
        if (playerMove != null)
        {
            playerMove.ForceStopImmediate();
        }

        if (dialogueManager == null)
        {
            FinishEnding();
            yield break;
        }

        EnsureBlackoutOverlay(dialogueManager);

        bool hasFirstDialogue = HasDialogue(firstDialogues);
        bool hasSecondDialogue = HasDialogue(endingDialogues);

        if (!hasFirstDialogue && !hasSecondDialogue)
        {
            FinishEnding();
            yield break;
        }

        isPlayerFadeComplete = false;
        isPlayerFadeStarted = false;
        pendingPlayerObject = playerObject;

        if (hasFirstDialogue)
        {
            if (firstDialogueImage != null && firstDialogueImageStartLine <= 1)
            {
                RevealFirstDialogueImage(dialogueManager);
            }
            else if (firstDialogueImage != null)
            {
                dialogueManager.HideDialogueImage();
                RegisterFirstDialogueImageTrigger();
            }

            if (playerFadeStartLine <= 1)
            {
                StartPlayerFadeIfNeeded();
            }
            else
            {
                RegisterPlayerFadeTrigger();
            }

            dialogueManager.ShowDialogue(firstDialogues, null, null, () => StartCoroutine(BeginSecondPhase(dialogueManager)));
            yield break;
        }

        StartPlayerFadeIfNeeded();
        yield return BeginSecondPhase(dialogueManager);
    }

    // Starts the blackout transition and then opens the second ending dialogue segment.
    private IEnumerator BeginSecondPhase(DialogueManager dialogueManager)
    {
        UnregisterFirstDialogueImageTrigger();
        UnregisterFirstDialogueImageHideTrigger();
        UnregisterPlayerFadeTrigger();
        yield return WaitForFirstDialogueImageClose(dialogueManager);
        StartPlayerFadeIfNeeded();

        if (dialogueManager != null)
        {
            dialogueManager.LockPlayer(true);
        }

        while (!isPlayerFadeComplete)
        {
            yield return null;
        }

        if (postFadeDelay > 0f)
        {
            yield return new WaitForSeconds(postFadeDelay);
        }

        yield return FadeBlackout(1f);

        if (dialogueManager == null)
        {
            FinishEnding();
            yield break;
        }

        if (!HasDialogue(endingDialogues))
        {
            FinishEnding();
            yield break;
        }

        if (endingImage != null)
        {
            dialogueManager.ShowDialogueImage(endingImage);
        }

        dialogueManager.ShowDialogue(endingDialogues, null, null, FinishEnding, endingDialogueAudioSettings);
    }

    // Starts waiting for the configured first-phase line before revealing the illustration.
    private void RegisterFirstDialogueImageTrigger()
    {
        if (firstDialogueImage == null)
        {
            return;
        }

        UnregisterFirstDialogueImageTrigger();
        isWaitingToShowFirstDialogueImage = true;
        DialogueManager.DialogueLineShown += HandleFirstDialogueLineShown;
    }

    // Stops listening for the first-phase image trigger once it is no longer needed.
    private void UnregisterFirstDialogueImageTrigger()
    {
        if (!isWaitingToShowFirstDialogueImage)
        {
            return;
        }

        DialogueManager.DialogueLineShown -= HandleFirstDialogueLineShown;
        isWaitingToShowFirstDialogueImage = false;
    }

    // Reveals the first dialogue image once the configured line index is reached.
    private void HandleFirstDialogueLineShown(string lineText, int lineIndex, int totalLines)
    {
        if (!isWaitingToShowFirstDialogueImage || firstDialogueImage == null)
        {
            return;
        }

        int targetLineIndex = Mathf.Max(0, firstDialogueImageStartLine - 1);
        if (lineIndex < targetLineIndex)
        {
            return;
        }

        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            RevealFirstDialogueImage(dialogueManager);
        }

        UnregisterFirstDialogueImageTrigger();
    }

    // Starts waiting for the configured line before hiding the first illustration.
    private void RegisterFirstDialogueImageHideTrigger()
    {
        if (firstDialogueImage == null)
        {
            return;
        }

        UnregisterFirstDialogueImageHideTrigger();
        isWaitingToHideFirstDialogueImage = true;
        DialogueManager.DialogueLineShown += HandleFirstDialogueImageHideLineShown;
    }

    // Stops listening for the first illustration hide line.
    private void UnregisterFirstDialogueImageHideTrigger()
    {
        if (!isWaitingToHideFirstDialogueImage)
        {
            return;
        }

        DialogueManager.DialogueLineShown -= HandleFirstDialogueImageHideLineShown;
        isWaitingToHideFirstDialogueImage = false;
    }

    // Hides the first illustration once its configured ending line is reached.
    private void HandleFirstDialogueImageHideLineShown(string lineText, int lineIndex, int totalLines)
    {
        if (!isWaitingToHideFirstDialogueImage)
        {
            return;
        }

        int startLineIndex = Mathf.Max(0, firstDialogueImageStartLine - 1);
        int hideLineIndex = Mathf.Max(startLineIndex + 1, firstDialogueImageHideLine - 1);
        if (hideLineIndex >= totalLines)
        {
            return;
        }

        if (lineIndex < hideLineIndex)
        {
            return;
        }

        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            StartFirstDialogueImageClose(dialogueManager);
        }
    }

    // Darkens the background before showing the first illustration.
    private void RevealFirstDialogueImage(DialogueManager dialogueManager)
    {
        if (dialogueManager == null || firstDialogueImage == null)
        {
            return;
        }

        StopFirstDialogueImageRevealRoutine();
        StopFirstDialogueImageCloseRoutine();
        RegisterFirstDialogueImageHideTrigger();
        firstDialogueImageRevealRoutine = StartCoroutine(RevealFirstDialogueImageRoutine(dialogueManager));
    }

    // Uses the existing blackout layer below dialogue UI so only the scene background fades.
    private IEnumerator RevealFirstDialogueImageRoutine(DialogueManager dialogueManager)
    {
        yield return FadeBlackout(firstImageBackgroundDarkAlpha, firstImageDarkenDuration);

        if (dialogueManager != null && firstDialogueImage != null)
        {
            dialogueManager.ShowDialogueImage(firstDialogueImage);
            isFirstDialogueImageVisible = true;
        }

        firstDialogueImageRevealRoutine = null;
    }

    // Starts the first illustration close transition if it is not already running.
    private void StartFirstDialogueImageClose(DialogueManager dialogueManager)
    {
        if (firstDialogueImageCloseRoutine != null)
        {
            return;
        }

        firstDialogueImageCloseRoutine = StartCoroutine(CloseFirstDialogueImageIfNeeded(dialogueManager));
    }

    // Waits for any active close transition, or runs one at first dialogue end.
    private IEnumerator WaitForFirstDialogueImageClose(DialogueManager dialogueManager)
    {
        if (firstDialogueImageCloseRoutine != null)
        {
            yield return firstDialogueImageCloseRoutine;
            yield break;
        }

        yield return CloseFirstDialogueImageIfNeeded(dialogueManager);
    }

    // Hides the first illustration, waits for its exit animation, then restores the background.
    private IEnumerator CloseFirstDialogueImageIfNeeded(DialogueManager dialogueManager)
    {
        UnregisterFirstDialogueImageHideTrigger();
        StopFirstDialogueImageRevealRoutine();

        if (dialogueManager != null && isFirstDialogueImageVisible)
        {
            dialogueManager.HideDialogueImage();
            isFirstDialogueImageVisible = false;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, dialogueManager.DialogueImageAnimDuration));
        }

        if (blackoutCanvasGroup != null && blackoutCanvasGroup.alpha > 0f)
        {
            yield return FadeBlackout(0f, firstImageBrightenDuration);
        }

        firstDialogueImageCloseRoutine = null;
    }

    // Prevents the first-image reveal fade from fighting the ending blackout.
    private void StopFirstDialogueImageRevealRoutine()
    {
        if (firstDialogueImageRevealRoutine == null)
        {
            return;
        }

        StopCoroutine(firstDialogueImageRevealRoutine);
        firstDialogueImageRevealRoutine = null;
    }

    // Stops any active first-image close transition during teardown or a fresh reveal.
    private void StopFirstDialogueImageCloseRoutine()
    {
        if (firstDialogueImageCloseRoutine == null)
        {
            return;
        }

        StopCoroutine(firstDialogueImageCloseRoutine);
        firstDialogueImageCloseRoutine = null;
    }

    // Starts waiting for the configured first-phase line before beginning the player fade.
    private void RegisterPlayerFadeTrigger()
    {
        if (pendingPlayerObject == null)
        {
            isPlayerFadeComplete = true;
            return;
        }

        UnregisterPlayerFadeTrigger();
        isWaitingToStartPlayerFade = true;
        DialogueManager.DialogueLineShown += HandlePlayerFadeLineShown;
    }

    // Stops listening for the player-fade trigger once it has fired or is no longer needed.
    private void UnregisterPlayerFadeTrigger()
    {
        if (!isWaitingToStartPlayerFade)
        {
            return;
        }

        DialogueManager.DialogueLineShown -= HandlePlayerFadeLineShown;
        isWaitingToStartPlayerFade = false;
    }

    // Starts the player fade once the configured first dialogue line is shown.
    private void HandlePlayerFadeLineShown(string lineText, int lineIndex, int totalLines)
    {
        if (!isWaitingToStartPlayerFade)
        {
            return;
        }

        int targetLineIndex = Mathf.Max(0, playerFadeStartLine - 1);
        if (targetLineIndex >= totalLines)
        {
            return;
        }

        if (lineIndex < targetLineIndex)
        {
            return;
        }

        UnregisterPlayerFadeTrigger();
        StartPlayerFadeIfNeeded();
    }

    // Ensures the player fade coroutine is launched only once.
    private void StartPlayerFadeIfNeeded()
    {
        if (isPlayerFadeStarted)
        {
            return;
        }

        isPlayerFadeStarted = true;
        StartCoroutine(FadeOutPlayer(pendingPlayerObject));
    }

    // Fades out the player presentation before the ending finishes.
    private IEnumerator FadeOutPlayer(GameObject playerObject)
    {
        if (playerObject == null)
        {
            isPlayerFadeComplete = true;
            yield break;
        }

        SpriteRenderer[] renderers = playerObject.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            playerObject.SetActive(false);
            isPlayerFadeComplete = true;
            yield break;
        }

        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
        }

        float duration = Mathf.Max(0.01f, playerFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - t;

            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = originalColors[i];
                color.a = originalColors[i].a * alpha;
                renderers[i].color = color;
            }

            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = originalColors[i];
            color.a = 0f;
            renderers[i].color = color;
        }

        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }

        isPlayerFadeComplete = true;
    }

    // Checks whether the supplied dialogue array contains at least one usable line.
    private bool HasDialogue(string[] dialogues)
    {
        return dialogues != null && dialogues.Length > 0;
    }

    // Creates a blackout overlay that sits below the dialogue UI but above the scene.
    private void EnsureBlackoutOverlay(DialogueManager dialogueManager)
    {
        if (blackoutCanvasGroup != null) return;
        if (dialogueManager == null) return;

        Transform uiParent = null;
        if (dialogueManager.dialoguePanel != null)
        {
            uiParent = dialogueManager.dialoguePanel.transform.parent;
        }
        else if (dialogueManager.dialogueImagePanel != null)
        {
            uiParent = dialogueManager.dialogueImagePanel.transform.parent;
        }

        if (uiParent == null) return;

        blackoutOverlay = new GameObject("EndingBlackoutOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        blackoutOverlay.transform.SetParent(uiParent, false);

        RectTransform rectTransform = blackoutOverlay.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        blackoutCanvasGroup = blackoutOverlay.GetComponent<CanvasGroup>();
        blackoutCanvasGroup.alpha = 0f;
        blackoutCanvasGroup.interactable = false;
        blackoutCanvasGroup.blocksRaycasts = false;

        Image blackoutImage = blackoutOverlay.GetComponent<Image>();
        blackoutImage.color = blackoutColor;
        blackoutImage.raycastTarget = false;

        int overlayIndex = uiParent.childCount - 1;
        if (dialogueManager.dialogueImagePanel != null && dialogueManager.dialogueImagePanel.transform.parent == uiParent)
        {
            overlayIndex = Mathf.Min(overlayIndex, dialogueManager.dialogueImagePanel.transform.GetSiblingIndex());
        }
        if (dialogueManager.dialoguePanel != null && dialogueManager.dialoguePanel.transform.parent == uiParent)
        {
            overlayIndex = Mathf.Min(overlayIndex, dialogueManager.dialoguePanel.transform.GetSiblingIndex());
        }

        blackoutOverlay.transform.SetSiblingIndex(Mathf.Max(0, overlayIndex));
    }

    // Fades the scene blackout layer while keeping the dialogue UI visible above it.
    private IEnumerator FadeBlackout(float targetAlpha)
    {
        yield return FadeBlackout(targetAlpha, blackoutFadeDuration);
    }

    // Fades the scene blackout layer over a caller-specified duration.
    private IEnumerator FadeBlackout(float targetAlpha, float duration)
    {
        if (blackoutCanvasGroup == null) yield break;

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        float startAlpha = blackoutCanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            blackoutCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        blackoutCanvasGroup.alpha = targetAlpha;
    }

    // Completes the ending flow and triggers the final scene transition.
    protected virtual void FinishEnding()
    {
        if (clearInventoryOnFinish)
        {
            Inventory.Clear();
        }

        SceneTransition.LoadScene(mainMenuSceneName);
    }

    // Cleans up runtime overlay objects if this sequence is destroyed early.
    protected virtual void OnDestroy()
    {
        UnregisterFirstDialogueImageTrigger();
        UnregisterFirstDialogueImageHideTrigger();
        UnregisterPlayerFadeTrigger();
        StopFirstDialogueImageRevealRoutine();
        StopFirstDialogueImageCloseRoutine();

        if (blackoutOverlay != null)
        {
            Destroy(blackoutOverlay);
        }
    }
}
