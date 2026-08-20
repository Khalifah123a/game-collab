using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Config")]
    public int worldIndex;
    public int segmentIndex;
    public Transform playerSpawn;
    public Transform serverBPoint;

    [Header("TTL Config")]
    public float levelDrainRate = 1f;

    [Header("State")]
    public bool isComplete;

    private PlayerHealth _health;

    private void Start()
    {
        _health = FindObjectOfType<PlayerHealth>();
        if (_health)
        {
            _health.drainRate = levelDrainRate;
            _health.RefreshStats();
        }

        if (playerSpawn)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player)
                player.transform.position = playerSpawn.position;
        }

        var gm = GameManager.Ensure();
        gm.currentWorldIndex = worldIndex;
        gm.currentSegmentIndex = segmentIndex;
    }

    public void CompleteSegment()
    {
        if (isComplete) return;
        isComplete = true;

        GameManager.Instance?.OnSegmentComplete();
    }

    private void OnDrawGizmos()
    {
        if (playerSpawn)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(playerSpawn.position, Vector3.one * 0.5f);
        }
        if (serverBPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(serverBPoint.position, Vector3.one * 0.5f);
        }
    }
}
