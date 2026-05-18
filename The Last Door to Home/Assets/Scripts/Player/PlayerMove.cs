using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 5f;
    [Header("松键后动画缓冲时间（秒）")]
    public float stopFreezeDelay = 0.08f;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 lastMoveDir = Vector2.down; // 默认朝下（正面）
    private bool isMoving;
    private float stopTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        // 对话锁定
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlayerControlLocked)
        {
            rb.velocity = Vector2.zero;
            stopTimer = 0f;
            FreezeAtCurrentDirection();
            return;
        }

        // 输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 🔴 禁止斜走：优先上下，再左右
        Vector2 moveDir = Vector2.zero;

        if (Mathf.Abs(v) > 0.1f)
        {
            moveDir = new Vector2(0, v);
        }
        else if (Mathf.Abs(h) > 0.1f)
        {
            moveDir = new Vector2(h, 0);
        }

        // 移动
        rb.velocity = moveDir * moveSpeed;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            // 有输入：更新朝向并正常播放动画
            lastMoveDir = moveDir;
            anim.SetFloat("MoveX", moveDir.x);
            anim.SetFloat("MoveY", moveDir.y);
            anim.speed = 1f;
            stopTimer = stopFreezeDelay;

            // 让短按也能在本帧立刻评估方向切换，避免“点一下来不及转向”
            anim.Update(0f);
            isMoving = true;
        }
        else
        {
            // 无输入：先给一个很短的播放缓冲，避免短按只看到平移
            if (stopTimer > 0f)
            {
                stopTimer -= Time.deltaTime;
                rb.velocity = Vector2.zero;
                anim.SetFloat("MoveX", lastMoveDir.x);
                anim.SetFloat("MoveY", lastMoveDir.y);
                anim.speed = 1f;
            }
            else
            {
                // 缓冲结束后，停在最后朝向的第1帧
                FreezeAtCurrentDirection();
            }
        }
    }

    private void FreezeAtCurrentDirection()
    {
        rb.velocity = Vector2.zero;
        anim.SetFloat("MoveX", lastMoveDir.x);
        anim.SetFloat("MoveY", lastMoveDir.y);

        // 仅在“移动 -> 静止”切换时重置到首帧，避免每帧强制重置
        if (isMoving)
        {
            int stateHash;
            if (anim.IsInTransition(0))
            {
                // 如果正在切状态，优先锁定到目标状态的第1帧
                stateHash = anim.GetNextAnimatorStateInfo(0).fullPathHash;
            }
            else
            {
                stateHash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            }

            anim.Play(stateHash, 0, 0f);
        }

        anim.speed = 0f;
        isMoving = false;
    }
}
