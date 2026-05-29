using UnityEngine;

[CreateAssetMenu(fileName = "StoryBeatSet", menuName = "Story/Story Beat Set")]
public class StoryBeatSet : ScriptableObject
{
    [Header("按顺序触发的剧情条目")]
    public StoryBeat[] beats;
}
