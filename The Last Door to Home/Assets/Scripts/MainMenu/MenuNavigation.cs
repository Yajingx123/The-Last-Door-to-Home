using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuNavigation : MonoBehaviour
{
    [Header("菜单按钮列表")]
    public List<Button> menuButtons; // 按顺序存放菜单按钮（如Start、Quit）
    private int currentSelectedIndex = 0; // 当前选中的按钮索引

    void Start()
    {
        // 初始化：选中第一个按钮
        if (menuButtons.Count > 0)
        {
            SelectButton(currentSelectedIndex);
        }
    }

    void Update()
    {
        // 监听上下键/WASD的上下（W=上，S=下；上箭头=上，下箭头=下）
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            Navigate(-1); // 向上选（索引-1）
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            Navigate(1); // 向下选（索引+1）
        }

        // 按回车触发当前选中的按钮
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TriggerCurrentButton();
        }
    }

    // 菜单导航逻辑
    void Navigate(int direction)
    {
        currentSelectedIndex += direction;
        // 循环选择（到顶/到底后绕回）
        if (currentSelectedIndex < 0)
        {
            currentSelectedIndex = menuButtons.Count - 1;
        }
        else if (currentSelectedIndex >= menuButtons.Count)
        {
            currentSelectedIndex = 0;
        }
        // 选中当前索引的按钮
        SelectButton(currentSelectedIndex);
    }

    // 选中指定按钮（视觉反馈+聚焦）
    void SelectButton(int index)
    {
        // 重置所有按钮的选中状态（可选：改颜色/缩放区分选中）
        foreach (var btn in menuButtons)
        {
            btn.GetComponent<Image>().color = Color.white; // 未选中为白色
        }
        // 设置当前按钮为选中状态
        Button selectedBtn = menuButtons[index];
        selectedBtn.GetComponent<Image>().color = Color.yellow; // 选中为黄色（可自定义）
        selectedBtn.Select(); // UGUI聚焦，保证按钮可触发
    }

    // 触发当前选中的按钮点击事件
    void TriggerCurrentButton()
    {
        if (menuButtons.Count > 0)
        {
            menuButtons[currentSelectedIndex].onClick.Invoke();
        }
    }
}