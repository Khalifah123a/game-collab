using UnityEngine;

public class Checkpoint_TCP : MonoBehaviour
{
    [Header("Checkpoint")]
    public bool isActivated;
    public GameObject visualActive;
    public GameObject visualInactive;

    [Header("Bonus")]
    public float ttlBonus = 10f;
    public float activationTime = 0.5f;

    private float _holdTimer;
    private bool _playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            _holdTimer = 0;
        }
    }

    private void Update()
    {
        if (isActivated || !_playerInRange) return;

        if ((InputManager.Instance != null && InputManager.Instance.interactPressed) || Input.GetButton("Submit"))
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= activationTime)
                Activate();
        }
        else
        {
            _holdTimer = 0;
        }
    }

    private void Activate()
    {
        isActivated = true;

        var health = FindObjectOfType<PlayerHealth>();
        if (health) health.SetCheckpointBonus();

        if (visualActive) visualActive.SetActive(true);
        if (visualInactive) visualInactive.SetActive(false);

        if (InputManager.Instance != null) InputManager.Instance.ConsumeInteract();
    }
}
