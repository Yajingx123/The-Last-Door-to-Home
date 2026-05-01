using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AutoSortingPlayer : MonoBehaviour
{
    [Header("玩家默认层级")]
    public int defaultOrder = 100;

    private SpriteRenderer playerSR;

    void Awake()
    {
        playerSR = GetComponent<SpriteRenderer>();
        playerSR.sortingOrder = defaultOrder;
    }

    void Update()
    {
        // 找到场景里所有带碰撞体 + SpriteRenderer 的物体
        Collider2D[] colliders = Physics2D.OverlapAreaAll(new Vector2(-100, -100), new Vector2(100, 100));

        foreach (var col in colliders)
        {
            SpriteRenderer objSR = col.GetComponent<SpriteRenderer>();
            if (objSR == null) continue; // 没有图片就跳过

            float objY = col.transform.position.y;
            float playerY = transform.position.y;

            // 你要的规则：
            if (playerY > objY)
            {
                // 玩家在物体后面 → 层级 -1
                playerSR.sortingOrder = objSR.sortingOrder - 1;
            }
            else
            {
                // 玩家在物体前面 → 层级 +2
                playerSR.sortingOrder = objSR.sortingOrder + 2;
            }
        }
    }
}