using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        // 上下左右输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 移动
        Vector2 moveDir = new Vector2(h, v).normalized;
        rb.velocity = moveDir * moveSpeed;

        // 转向
        if (h != 0)
            transform.localScale = new Vector3(h, 1, 1);

        // --------------------------
        // 动画控制：移动/待机 切换
        // --------------------------
        float moveMagnitude = moveDir.magnitude;

        if (anim != null)
        {
            // 移动 > 0.1 → 走路动画
            if (moveMagnitude > 0.1f)
                anim.SetBool("isWalking", true);
            // 不动 → 待机动画
            else
                anim.SetBool("isWalking", false);
        }
    }

    void Awake()
    {
        // DontDestroyOnLoad(gameObject);
    }
}