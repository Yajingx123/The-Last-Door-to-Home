using UnityEngine;

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

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

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

        Bounds b = mapBounds.bounds;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = b.min.x + halfWidth;
        float maxX = b.max.x - halfWidth;
        float minY = b.min.y + halfHeight;
        float maxY = b.max.y - halfHeight;

        // 如果地图比相机可视范围还小，固定在地图中心，避免 Clamp 参数反转
        float clampedX = minX > maxX ? b.center.x : Mathf.Clamp(followPos.x, minX, maxX);
        float clampedY = minY > maxY ? b.center.y : Mathf.Clamp(followPos.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, followPos.z);
    }
}
