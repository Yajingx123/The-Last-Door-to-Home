using UnityEngine;

[RequireComponent(typeof(PlayerMove))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private float stepInterval = 0.38f;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.55f;

    private PlayerMove playerMove;
    private float stepTimer;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
    }

    private void Update()
    {
        if (footstepClip == null || playerMove == null)
        {
            return;
        }

        if (!playerMove.IsCurrentlyMoving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f) return;

        AudioManager.EnsureInstance().PlaySfx(footstepClip, volume);
        stepTimer = Mathf.Max(0.05f, stepInterval);
    }
}
