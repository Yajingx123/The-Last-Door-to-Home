using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionMenu : MonoBehaviour
{
    public static OptionMenu Instance;

    [Header("UI")]
    public GameObject optionPanel;
    public TextMeshProUGUI[] options;
    public Image cursor;

    [Header("玩家物体（拖Player）")]
    public GameObject player;

    private int currentSelect;
    private PickableItem currentItem;
    private RectTransform cursorRect;
    private float cursorFixedX;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }

        Instance = this;
        if (optionPanel != null) optionPanel.SetActive(false);

        cursorRect = cursor != null ? cursor.GetComponent<RectTransform>() : null;
        if (cursorRect != null)
        {
            cursorFixedX = cursorRect.anchoredPosition.x;
        }
    }

    void Update()
    {
        if (!optionPanel.activeSelf) return;

        // 上下选选项
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            currentSelect = Mathf.Max(0, currentSelect - 1);
            UpdateCursor();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            currentSelect = Mathf.Min(1, currentSelect + 1);
            UpdateCursor();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelect();
        }
    }

    public void ShowOptions(PickableItem item)
    {
        if (optionPanel == null)
        {
            Debug.LogWarning("OptionMenu: optionPanel 引用为空或已销毁，无法显示选项。", this);
            if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(false);
            return;
        }

        currentItem = item;
        optionPanel.SetActive(true);
        currentSelect = 0;
        UpdateCursor();
    }

    void UpdateCursor()
    {
        if (cursorRect == null || options == null || options.Length == 0 || options[currentSelect] == null) return;

        RectTransform optionRect = options[currentSelect].GetComponent<RectTransform>();
        if (optionRect == null) return;

        // 保留Inspector里设置的X，只跟随目标选项的Y，防止运行时X被覆盖
        Vector2 pos = cursorRect.anchoredPosition;
        pos.x = cursorFixedX;
        pos.y = optionRect.anchoredPosition.y;
        cursorRect.anchoredPosition = pos;
    }

    void ConfirmSelect()
    {
        if (currentSelect == 0) // 拾取
        {
            if (currentItem != null)
            {
                currentItem.PickUp();
            }
        }

        // 选项关闭 + 解锁玩家
        if (optionPanel != null) optionPanel.SetActive(false);
        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(false);
    }
}
