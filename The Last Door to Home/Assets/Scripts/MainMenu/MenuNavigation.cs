using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/*
Purpose: Manages m en un av ig at io n behavior for this part of the game.
Attached GameObject: Main menu canvas or UI controller GameObject.
Main responsibilities: Process menu navigation input and drive scene or UI transitions.
Inputs: Inspector configuration, scene references, and runtime method calls.
Outputs or effects: Applies runtime side effects through component state, UI updates, or return values.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class MenuNavigation : MonoBehaviour
{
    [Header("菜单按钮列表")]
    public List<Button> menuButtons; // 按顺序存放菜单按钮（如Start、Quit）
    private int currentSelectedIndex = 0; // 当前选中的按钮索引
    private const float SelectedAlpha = 1f;
    private const float UnselectedAlpha = 66f / 255f;

    // Prepares runtime state after the scene finishes its initial setup.
    void Start()
    {
        DisableMouseInteraction();

        // 初始化：选中第一个按钮
        if (menuButtons.Count > 0)
        {
            SelectButton(currentSelectedIndex);
        }
    }

    // Processes per-frame input and keeps this behaviour responsive during gameplay.
    void Update()
    {
        if (MainMenuLoadPanelController.IsOpen) return;

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
    // Moves the current menu selection in the requested direction.
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
    // Updates the current button selection and visual highlight state.
    void SelectButton(int index)
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (menuButtons[i] == null) continue;

            Image buttonImage = menuButtons[i].GetComponent<Image>();
            if (buttonImage == null) continue;

            Color color = buttonImage.color;
            color.a = i == index ? SelectedAlpha : UnselectedAlpha;
            buttonImage.color = color;
        }

        Button selectedBtn = menuButtons[index];
        selectedBtn.Select(); // UGUI聚焦，保证按钮可触发
    }

    // 触发当前选中的按钮点击事件
    // Invokes the currently selected menu button action.
    void TriggerCurrentButton()
    {
        if (menuButtons.Count > 0)
        {
            menuButtons[currentSelectedIndex].onClick.Invoke();
        }
    }

    // Disables pointer raycasts on the configured main-menu buttons so keyboard navigation is the only input path.
    void DisableMouseInteraction()
    {
        foreach (var btn in menuButtons)
        {
            if (btn == null) continue;

            Graphic[] graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }
    }
}
