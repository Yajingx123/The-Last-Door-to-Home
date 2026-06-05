using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
Purpose: Manages e nd in gs eq ue nc et ri gg er behavior for this part of the game.
Attached GameObject: Trigger collider or event-driving scene object.
Main responsibilities: Evaluate progression conditions, trigger story responses, and update narrative state.
Inputs: Event IDs, story flags, inventory state, and serialized story data.
Outputs or effects: Advances narrative state, launches dialogue, or gates gameplay actions.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class EndingSequenceTrigger : MonoBehaviour
{
    [Header("第一段对话")]
    [TextArea(2, 8)]
    public string[] firstDialogues;
    public Sprite firstDialogueImage;

    [Header("第二段结局对话")]
    [TextArea(2, 8)]
    public string[] endingDialogues;
    public Sprite endingImage;
    public DialogueAudioSettings endingDialogueAudioSettings;

    [Header("场景切换")]
    public string mainMenuSceneName = "MainMenu";
    public bool clearInventoryOnFinish = false;

    [Header("玩家消失效果")]
    [SerializeField] private float playerFadeDuration = 1.2f;
    [SerializeField] private float postFadeDelay = 0.15f;

    [Header("第二段前黑屏")]
    [SerializeField] private float blackoutFadeDuration = 0.8f;
    [SerializeField] private Color blackoutColor = Color.black;

    private bool triggered;
    private bool isPlayerFadeComplete;
    private GameObject blackoutOverlay;
    private CanvasGroup blackoutCanvasGroup;

    // Handles trigger entry events for this gameplay object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(PlayEndingSequence(other.gameObject));
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
        StartCoroutine(FadeOutPlayer(playerObject));

        if (hasFirstDialogue)
        {
            if (firstDialogueImage != null)
            {
                dialogueManager.ShowDialogueImage(firstDialogueImage);
            }

            dialogueManager.ShowDialogue(firstDialogues, null, null, () => StartCoroutine(BeginSecondPhase(dialogueManager)));
            yield break;
        }

        yield return BeginSecondPhase(dialogueManager);
    }

    // Starts the blackout transition and then opens the second ending dialogue segment.
    private IEnumerator BeginSecondPhase(DialogueManager dialogueManager)
    {
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
        if (blackoutCanvasGroup == null) yield break;

        float duration = Mathf.Max(0.01f, blackoutFadeDuration);
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
    private void FinishEnding()
    {
        if (clearInventoryOnFinish)
        {
            Inventory.Clear();
        }

        SceneTransition.LoadScene(mainMenuSceneName);
    }

    // Cleans up event hooks if this trigger is destroyed before the ending flow completes.
    private void OnDestroy()
    {
        if (blackoutOverlay != null)
        {
            Destroy(blackoutOverlay);
        }
    }
}
