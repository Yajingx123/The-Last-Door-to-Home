using UnityEngine;

/*
Purpose: Smoothly follows the player while clamping the camera inside the map bounds.
Attached GameObject: Main Camera GameObject that follows the player.
Main responsibilities: Read player-facing state, coordinate related components, and apply movement or presentation updates.
Inputs: Inspector references, Unity input, and state from linked gameplay managers.
Outputs or effects: Moves the player or camera, updates animations, and changes immediate gameplay feel.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

[RequireComponent(typeof(Camera))]
public class CameraFollowPlayer : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform target;

    [Header("地图边界（挂了 Collider2D 的物体）")]
    [SerializeField] private Collider2D mapBounds;

    [Header("平滑跟随（0 表示直接跟随）")]
    [SerializeField, Min(0f)] private float smoothTime = 0.12f;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    // Initializes cached references and one-time component state before gameplay begins.
    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // Prepares runtime state after the scene finishes its initial setup.
    private void Start()
    {
        SnapToTargetIfReady();
    }

    // Resets transient state whenever this component becomes active again.
    private void OnEnable()
    {
        velocity = Vector3.zero;
        SnapToTargetIfReady();
    }

    // Applies follow-up updates after other frame logic has already run.
    private void LateUpdate()
    {
        if (target == null || mapBounds == null)
        {
            return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 followPos;

        if (smoothTime <= 0f)
        {
            followPos = desired;
        }
        else
        {
            followPos = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }

        transform.position = ClampToBounds(followPos);
    }

    // Snaps the camera directly to the target once the required references are available.
    private void SnapToTargetIfReady()
    {
        if (target == null || mapBounds == null || cam == null)
        {
            return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = ClampToBounds(desired);
    }

    // Constrains the supplied position so the camera stays inside the map limits.
    private Vector3 ClampToBounds(Vector3 position)
    {
        Bounds b = mapBounds.bounds;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = b.min.x + halfWidth;
        float maxX = b.max.x - halfWidth;
        float minY = b.min.y + halfHeight;
        float maxY = b.max.y - halfHeight;

        float clampedX = minX > maxX ? b.center.x : Mathf.Clamp(position.x, minX, maxX);
        float clampedY = minY > maxY ? b.center.y : Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(clampedX, clampedY, position.z);
    }
}
