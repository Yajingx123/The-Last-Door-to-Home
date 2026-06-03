using System.Collections;
using UnityEngine;

public class EndingSequenceTrigger : MonoBehaviour
{
    [Header("结局内容")]
    [TextArea(2, 8)]
    public string[] endingDialogues;
    public Sprite endingImage;

    [Header("场景切换")]
    public string mainMenuSceneName = "MainMenu";
    public bool clearInventoryOnFinish = false;

    [Header("玩家消失效果")]
    [SerializeField] private float playerFadeDuration = 1.2f;
    [SerializeField] private float postFadeDelay = 0.15f;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(PlayEndingSequence(other.gameObject));
    }

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

        yield return FadeOutPlayer(playerObject);

        if (postFadeDelay > 0f)
        {
            yield return new WaitForSeconds(postFadeDelay);
        }

        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }

        if (dialogueManager == null)
        {
            FinishEnding();
            yield break;
        }

        if (endingImage != null)
        {
            dialogueManager.ShowDialogueImage(endingImage);
        }

        if (endingDialogues == null || endingDialogues.Length == 0)
        {
            FinishEnding();
            yield break;
        }

        dialogueManager.ShowDialogue(endingDialogues, null, null, FinishEnding);
    }

    private IEnumerator FadeOutPlayer(GameObject playerObject)
    {
        if (playerObject == null) yield break;

        SpriteRenderer[] renderers = playerObject.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

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
    }

    private void FinishEnding()
    {
        if (clearInventoryOnFinish)
        {
            Inventory.Clear();
        }

        SceneTransition.LoadScene(mainMenuSceneName);
    }
}
