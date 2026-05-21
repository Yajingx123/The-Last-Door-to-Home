using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public static Vector2 SPAWN_POSITION;
    public static bool NEED_SPAWN = false;

    private void Awake()
    {
        if (NEED_SPAWN)
        {
            transform.position = SPAWN_POSITION;
            NEED_SPAWN = false;
        }
    }
}
