using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-world interactive Network Protocol & Packet Modifier Terminal.
/// Located in SafeChamber_Start, allows the player to configure the active
/// transport protocol (IPv4/TCP, Fragile UDP, Jumbo Frame, Volatile ICMP)
/// with real-time speed, jump, and reward modifiers.
/// </summary>
public class ProtocolTerminal : MonoBehaviour
{
    private SpriteRenderer _sr;
    private InteractKeyPrompt _prompt;
    private bool _playerInRange;
    private Transform _playerTransform;

    public const float InteractionDistance = 2.4f;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

        var s = Resources.Load<Sprite>("GeneratedArt/puzzle_terminal");
        if (s == null) s = Resources.Load<Sprite>("GeneratedArt/server_rack");
        _sr.sprite = s;
        _sr.color = new Color(0.2f, 0.95f, 0.85f, 1f);
        _sr.sortingOrder = 4;

        _prompt = InteractKeyPrompt.Attach(gameObject, "GANTI PROTOKOL", new Vector2(0f, 2.2f));
        SnapToGround();
    }

    private void Start()
    {
        SnapToGround();
    }

    public void SnapToGround()
    {
        float halfH = _sr != null && _sr.sprite != null ? _sr.sprite.bounds.extents.y : 0.67f;
        var lm = FindAnyObjectByType<LevelManager>();
        if (lm != null && lm.worldIndex == 0)
        {
            transform.position = new Vector3(transform.position.x, -17.0f + halfH, transform.position.z);
            Physics2D.SyncTransforms();
            return;
        }
        Physics2D.SyncTransforms();
        Vector2 origin = (Vector2)transform.position + Vector2.up * 2f;
        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 10f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.normal.y < 0.5f) continue;
            if (hit.collider.GetComponentInParent<ProtocolTerminal>() != null) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.collider.GetComponentInParent<NPC_Base>() != null) continue;
            transform.position = new Vector3(transform.position.x, hit.point.y + halfH, transform.position.z);
            return;
        }
    }

    private void Update()
    {
        FindPlayer();
        if (_playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, _playerTransform.position);
        bool inRange = dist <= InteractionDistance;

        if (inRange != _playerInRange)
        {
            _playerInRange = inRange;
            if (_prompt != null)
            {
                if (_playerInRange) _prompt.Show();
                else _prompt.Hide();
            }
        }

        // Pulse glow while player is in range
        if (_playerInRange)
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
            _sr.color = new Color(0.2f, 1f, 0.9f) * pulse;

            if (Input.GetKeyDown(KeyCode.E) || (InputManager.Instance != null && InputManager.Instance.interactPressed))
            {
                if (InputManager.Instance != null) InputManager.Instance.interactPressed = false;
                OpenProtocolMenu();
            }
        }
        else
        {
            _sr.color = new Color(0.2f, 0.85f, 0.75f, 0.95f);
        }
    }

    private void FindPlayer()
    {
        if (_playerTransform != null) return;
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) _playerTransform = pc.transform;
    }

    public void OpenProtocolMenu()
    {
        ProtocolSelectionUI.Show();
        AudioManager.PlayUIOpen();
    }
}
