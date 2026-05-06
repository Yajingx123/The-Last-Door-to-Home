using UnityEngine;

public class DontDestroyDialogueSystem : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}