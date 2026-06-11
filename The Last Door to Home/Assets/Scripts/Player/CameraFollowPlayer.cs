using UnityEngine;

[RequireComponent(typeof(Camera))]
/*
Purpose: Smoothly follows the player while clamping the camera inside map bounds.
Attached GameObject: Main Camera GameObject.
Main responsibilities: Caches the Camera component, follows a target transform, smooths motion, and clamps the final camera position.
Inputs: Target transform, map bounds Collider2D, smoothing value, and camera aspect/orthographic size.
Outputs or effects: Updates the camera transform position every frame after player movement.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify camera behavior near every map edge and after enabling/disabling the camera object.
*/

public class CameraFollowPlayer : MonoBehaviour
{
    [Header("跟随目标 / Follow Target")]
    [SerializeField] private Transform target;

    [Header("地图边界（挂了 Collider2D 的物体） / Map Bounds (Object With Collider2D)")]
    [SerializeField] private Collider2D mapBounds;

    [Header("平滑跟随（0 表示直接跟随） / Smooth Follow (0 Means Instant)")]
    [SerializeField, Min(0f)] private float smoothTime = 0.12f;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    // Initializes component references and singleton ownership before Start runs.
    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // Prepares runtime state after the scene has finished its initial setup.
    private void Start()
    {
        SnapToTargetIfReady();
    }

    // Registers callbacks or resets transient state when the component becomes active.
    private void OnEnable()
    {
        velocity = Vector3.zero;
        SnapToTargetIfReady();
    }

    // Applies follow-up updates after other frame logic has completed.
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

    // Handles the snap to target if ready step for this script.
    private void SnapToTargetIfReady()
    {
        if (target == null || mapBounds == null || cam == null)
        {
            return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = ClampToBounds(desired);
    }

    // Handles the clamp to bounds step for this script.
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
