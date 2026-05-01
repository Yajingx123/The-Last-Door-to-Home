using UnityEngine;

[ExecuteInEditMode]
public class PixelSnap : MonoBehaviour
{
    private float pixelsPerUnit = 32; // 你的贴图 PPU = 32

    void Update()
    {
        Vector3 pos = transform.position;

        // 核心：把位置对齐到 1像素 的精度
        pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
        pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;

        transform.position = pos;
    }
}