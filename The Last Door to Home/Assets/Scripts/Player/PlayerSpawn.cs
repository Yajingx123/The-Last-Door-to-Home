using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawn : MonoBehaviour
{
    public static Vector2 SPAWN_POSITION;
    public static bool NEED_SPAWN = false;

    private void Awake()
    {
        // 不需要 DontDestroyOnLoad，因为 Player 物体本身已经做了
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (NEED_SPAWN)
        {
            transform.position = SPAWN_POSITION;
            NEED_SPAWN = false;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}