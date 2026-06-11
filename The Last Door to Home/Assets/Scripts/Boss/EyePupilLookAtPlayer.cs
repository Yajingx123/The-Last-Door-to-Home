using UnityEngine;
/*
Purpose: Makes an eye or pupil child transform look toward the player.
Attached GameObject: Eye Monster pupil/iris child GameObject.
Main responsibilities: Resolves the player target, rotates the pupil, and optionally offsets it inside the eye socket.
Inputs: Player position, optional MonsterController reference, look settings, and local offset limits.
Outputs or effects: Updates this transform's local position and/or rotation each frame.
Authorship or assistance: Implemented with OpenAI Codex assistance.
Testing notes: Verify the pupil faces the player in play mode and adjust angle offset/radius for the sprite art.
*/

public class EyePupilLookAtPlayer : MonoBehaviour
{
    private enum LookMode
    {
        RotateOnly,
        OffsetOnly,
        RotateAndOffset
    }

    [Header("目标 / Target")]
    [SerializeField] private MonsterController controller;
    [SerializeField] private Transform target;

    [Header("眼珠行为 / Pupil Look")]
    [SerializeField] private LookMode lookMode = LookMode.OffsetOnly;
    [SerializeField] private float maxLocalOffset = 0.08f;
    [SerializeField] private float rotationAngleOffset = 0f;
    [SerializeField] private float followSpeed = 12f;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (target == null)
        {
            return;
        }

        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float t = followSpeed <= 0f ? 1f : 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        if (lookMode == LookMode.OffsetOnly || lookMode == LookMode.RotateAndOffset)
        {
            Vector3 desiredLocalPosition = startLocalPosition + (Vector3)(direction.normalized * maxLocalOffset);
            transform.localPosition = Vector3.Lerp(transform.localPosition, desiredLocalPosition, t);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, startLocalPosition, t);
        }

        if (lookMode == LookMode.RotateOnly || lookMode == LookMode.RotateAndOffset)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationAngleOffset;
            Quaternion desiredRotation = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }
        else
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, startLocalRotation, t);
        }
    }

    private void ResolveReferences()
    {
        if (target != null)
        {
            return;
        }

        if (controller == null)
        {
            controller = GetComponentInParent<MonsterController>();
        }

        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }

        if (controller != null)
        {
            target = controller.Player;
        }

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
}
