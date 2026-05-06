using UnityEngine;

public class PickableItem : MonoBehaviour
{
    [Header("物品唯一ID")]
    public string itemUniqueID = "flower_01";

    [Header("物品名称")]
    public string itemName = "Flower";

    [Header("物品类型")]
    public ItemType itemType;

    [Header("拾取提示")]
    public string pickMessage = "你获得了物品！";

    void Start()
    {
        // 只在本次游戏里判断是否拾取
        if (Inventory.HasCollected(itemUniqueID))
        {
            gameObject.SetActive(false);
        }
    }

    public void PickUp()
    {
        if (Inventory.HasCollected(itemUniqueID)) return;

        Inventory.AddItem(itemName, itemType, itemUniqueID);
        gameObject.SetActive(false);

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new string[] { pickMessage });
        }
    }
}