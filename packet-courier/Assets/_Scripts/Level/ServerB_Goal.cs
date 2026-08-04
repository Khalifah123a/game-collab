using UnityEngine;

public class ServerB_Goal : MonoBehaviour
{
    [Header("Config")]
    public LevelManager levelManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();

        levelManager?.CompleteSegment();
    }
}
