using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    public string targetSceneName;
    public Vector2 spawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerSpawn.SPAWN_POSITION = spawnPosition;
            PlayerSpawn.NEED_SPAWN = true;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}