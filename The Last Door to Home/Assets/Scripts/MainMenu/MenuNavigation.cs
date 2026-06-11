using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
/*
Purpose: Controls keyboard-only navigation for the main menu buttons.
Attached GameObject: Main menu Canvas or menu controller GameObject.
Main responsibilities: Selects menu buttons, moves selection with keyboard input, invokes the selected button, and disables pointer raycasts.
Inputs: Configured Button list, W/S or arrow-key input, Return input, and the Continue load-panel state.
Outputs or effects: Updates button highlight alpha, UGUI selection, and button OnClick invocation.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify keyboard navigation wraps correctly, Return triggers the right action, and mouse clicks remain disabled.
*/

public class MenuNavigation : MonoBehaviour
{
    [Header("菜单按钮列表 / Menu Button List")]
    public List<Button> menuButtons; // Menu buttons in display order, such as Start, Continue, and Quit.
    private int currentSelectedIndex = 0; // Index of the currently selected button.
    private const float SelectedAlpha = 1f;
    private const float UnselectedAlpha = 66f / 255f;

    // Prepares runtime state after the scene has finished its initial setup.
    void Start()
    {
        DisableMouseInteraction();

        // Select the first button by default.
        if (menuButtons.Count > 0)
        {
            SelectButton(currentSelectedIndex);
        }
    }

    // Reads per-frame input and updates frame-dependent runtime state.
    void Update()
    {
        if (MainMenuLoadPanelController.IsOpen) return;

        // W/Up moves upward; S/Down moves downward.
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            Navigate(-1);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            Navigate(1);
        }

        // Return activates the current option.
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TriggerCurrentButton();
        }
    }

    // Handles the navigate step for this script.
    void Navigate(int direction)
    {
        currentSelectedIndex += direction;
        // Wrap around when the selection moves past either end.
        if (currentSelectedIndex < 0)
        {
            currentSelectedIndex = menuButtons.Count - 1;
        }
        else if (currentSelectedIndex >= menuButtons.Count)
        {
            currentSelectedIndex = 0;
        }
        // Refresh the visual state for the current selection.
        SelectButton(currentSelectedIndex);
    }

    // Handles the select button step for this script.
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
        selectedBtn.Select(); // Focus the selected UGUI button so Return can trigger it.
    }

    // Handles the trigger current button step for this script.
    void TriggerCurrentButton()
    {
        if (menuButtons.Count > 0)
        {
            menuButtons[currentSelectedIndex].onClick.Invoke();
        }
    }

    // Handles the disable mouse interaction step for this script.
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
