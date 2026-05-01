using UnityEngine;

public class ObjectDialogue : MonoBehaviour, IInteractable
{
    [TextArea(2, 4)]
    public string dialogue = "这是一个摆件";

    // 一次性对话开关
    // public bool oneTimeOnly = false;
    // private bool hasTriggered = false;

    public void OnInteract()
    {
        // if (oneTimeOnly && hasTriggered)
        // if ( hasTriggered)
        // {
        //     Debug.Log("【对话】这个对话已经触发过了");
        //     return;
        // }

        // 输出到控制台
        Debug.Log("【对话】" + dialogue);

        // hasTriggered = true;

        // 未来加对话框时，只需要在这里替换代码
        // DialogueManager.Instance.ShowDialogue(dialogue);
    }
}