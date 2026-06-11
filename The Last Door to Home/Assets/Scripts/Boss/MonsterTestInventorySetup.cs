using System;
using UnityEngine;

/*
Purpose: Adds selected test items to Inventory before monster scripts read item state.
Attached GameObject: Any GameObject in a monster test scene.
Main responsibilities: Lets developers tick key items in the Inspector for single-scene monster testing.
Inputs: Three Inspector-configured item toggles.
Outputs or effects: Adds selected items to Inventory during Awake.
Authorship or assistance: Original project helper; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify the helper runs before EyeMonster, StalkerMonster, and PlayerMove read Inventory.
*/

[DefaultExecutionOrder(-10000)]
public class MonsterTestInventorySetup : MonoBehaviour
{
    [Serializable]
    public class TestItem
    {
        public bool giveItem;
        public string itemUniqueID = "";
        public string itemName = "Test Item";
        public ItemType itemType = ItemType.Tool;
        [TextArea] public string itemDescription = "";
        public string iconResourcePath = "";
    }

    [Header("开发测试开关 / Development Test Switch")]
    [Tooltip("只在Unity编辑器里生效，正式打包时不会加测试道具。")]
    [SerializeField] private bool enableInEditor = true;
    [Tooltip("测试前是否清空当前Inventory。只建议单独测试Monster场景时打开。")]
    [SerializeField] private bool clearInventoryBeforeApplying = false;
    [Tooltip("勾上后，只有当前场景存在MonsterController时才会加测试道具。")]
    [SerializeField] private bool requireMonsterControllerInScene = true;

    [Header("关键道具 / Key Items")]
    public TestItem itemA = new TestItem { itemName = "Test Item A", itemType = ItemType.Tool };
    public TestItem itemB = new TestItem { itemName = "Test Item B", itemType = ItemType.Tool };
    public TestItem itemC = new TestItem { itemName = "Test Item C", itemType = ItemType.Tool };

    // 在怪物脚本读取背包前，先把Inspector里勾选的测试道具加入Inventory。
    // Adds selected test items before monster behavior checks Inventory.
    private void Awake()
    {
#if UNITY_EDITOR
        if (!enableInEditor) return;
        if (requireMonsterControllerInScene && FindObjectOfType<MonsterController>() == null) return;

        if (clearInventoryBeforeApplying)
        {
            Inventory.Clear();
        }

        ApplyItem(itemA);
        ApplyItem(itemB);
        ApplyItem(itemC);
#endif
    }

    // 如果某个测试道具被勾选，就把它作为已获得道具加入背包。
    // Adds one configured test item when its checkbox is enabled.
    private void ApplyItem(TestItem item)
    {
        if (item == null || !item.giveItem) return;
        if (string.IsNullOrWhiteSpace(item.itemUniqueID)) return;

        Inventory.AddItem(
            item.itemName,
            item.itemType,
            item.itemUniqueID,
            item.itemDescription,
            item.iconResourcePath
        );
    }
}
