using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactRange = 1.5f; // 可交互的最大距离
    public float angleTolerance = 60f; // 面朝方向的容错角度（单位：度）

    private Vector2 faceDir = Vector2.right; // 玩家当前面朝方向

    void Update()
    {
        // 更新玩家面朝方向
        UpdateFaceDirection();

        // 按下 Enter 触发交互
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TryInteract();
        }
    }

    // 根据输入更新面朝方向
    void UpdateFaceDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
            faceDir = new Vector2(h, v).normalized;
        }
    }

    // 核心交互逻辑
    void TryInteract()
    {
        // 找到场景中所有实现了 IInteractable 接口的物体
        ObjectDialogue[] interactables = FindObjectsOfType<ObjectDialogue>();

        foreach (var item in interactables)
        {
            // 跳过未启用的交互物体
            if (!item.enabled) continue;

            // 计算玩家与物品的距离
            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance > interactRange) continue;

            // 计算玩家面朝方向到物品方向的夹角
            Vector2 dirToItem = (item.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(faceDir, dirToItem);

            // 如果距离够近，且面朝方向在容错角度内，则触发交互
            if (angle <= angleTolerance)
            {
                item.OnInteract();
                // 找到一个就退出，避免同时触发多个
                break;
            }
        }
    }
}