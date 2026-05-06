using UnityEngine;

public class ObjectDialogue : MonoBehaviour, IInteractable
{
    [TextArea(2, 4)]
    public string dialogue = "这是一个摆件";

    // 一次性对话开关（可选开启）
    // public bool oneTimeOnly = false;
    // private bool hasTriggered = false;

    public void OnInteract()
    {
        // 一次性对话逻辑（可选启用）
        // if (oneTimeOnly && hasTriggered)
        // {
        //     return;
        // }
        Debug.Log("【对话】" + dialogue);

        // 调用对话管理器显示对话（替换原Debug.Log）
        DialogueManager.Instance.ShowDialogue(dialogue);

        // hasTriggered = true;

        // 可选：添加“按任意键关闭对话”的逻辑（需在Update中监听）
        // 示例：
        // StartCoroutine(WaitForCloseDialogue());
    }

    // 可选：协程等待按键关闭对话
    // IEnumerator WaitForCloseDialogue()
    // {
    //     yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));
    //     DialogueManager.Instance.HideDialogue();
    // }
}