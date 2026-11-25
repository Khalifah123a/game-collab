using UnityEngine;

/// <summary>
/// Interactive relay pylon for the cipher terminal puzzle.
/// Player touches or attacks the relay to trigger it in the sequence.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PuzzleRelay : MonoBehaviour
{
    public int relayIndex;
    public TerminalPuzzle puzzle;

    private SpriteRenderer _sr;
    private float _glowTimer;

    private static readonly Color[] RelayColors = new[]
    {
        new Color(0.2f, 0.9f, 1f),   // Node 1: Cyan
        new Color(0.2f, 1f, 0.5f),   // Node 2: Emerald
        new Color(1f, 0.8f, 0.2f)    // Node 3: Amber
    };

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        _sr = GetComponent<SpriteRenderer>();

        if (_sr != null && (_sr.sprite == null || _sr.sprite.name.Contains("placeholder")))
        {
            var s = Resources.Load<Sprite>("GeneratedArt/puzzle_relay");
            if (s != null)
            {
                _sr.sprite = s;
                _sr.color = Color.white;
            }
        }
    }

    public void Setup(TerminalPuzzle parentPuzzle, int index)
    {
        puzzle = parentPuzzle;
        relayIndex = index;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            ActivateRelay();
    }

    public void ActivateRelay()
    {
        _glowTimer = 0.5f;
        if (puzzle != null)
            puzzle.OnRelayActivated(relayIndex);
    }

    private void Update()
    {
        if (_sr == null) return;

        Color baseCol = relayIndex >= 0 && relayIndex < RelayColors.Length ? RelayColors[relayIndex] : Color.white;

        if (_glowTimer > 0f)
        {
            _glowTimer -= Time.deltaTime;
            _sr.color = Color.white;
        }
        else
        {
            _sr.color = baseCol;
        }
    }
}
