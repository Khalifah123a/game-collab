using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cipher Puzzle Terminal and interactive relay nodes.
/// Solving the sequence puzzle awards bonus DataBits, heals TTL, and reports to optional quest progress.
/// </summary>
public class TerminalPuzzle : MonoBehaviour
{
    [Header("Puzzle Config")]
    public string puzzleId = "puzzle_01";
    public int bonusBits = 150;
    public float ttlReward = 30f;
    public bool isSolved;

    [Header("References")]
    public PuzzleRelay[] relays;

    private readonly List<int> _currentInput = new List<int>();
    private readonly int[] _targetSequence = new[] { 0, 1, 2 }; // Alpha -> Beta -> Gamma
    private TextMesh _screenText;
    private SpriteRenderer _sr;

    private string Id => "puzzle:" + puzzleId;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null && (_sr.sprite == null || _sr.sprite.name.Contains("placeholder")))
        {
            var s = Resources.Load<Sprite>("GeneratedArt/puzzle_terminal");
            if (s != null) { _sr.sprite = s; _sr.color = Color.white; }
        }
    }

    private void Start()
    {
        var gm = GameManager.Instance;
        isSolved = gm != null && gm.ledger.IsCollected(Id);

        if (_sr == null)
            _sr = GetComponent<SpriteRenderer>();
        if (_sr != null && (_sr.sprite == null || _sr.sprite.name.Contains("placeholder")))
        {
            var s = Resources.Load<Sprite>("GeneratedArt/puzzle_terminal");
            if (s != null) { _sr.sprite = s; _sr.color = Color.white; }
        }

        BuildScreenText();
        UpdateScreen();

        if (relays != null)
        {
            for (int i = 0; i < relays.Length; i++)
            {
                if (relays[i] != null)
                    relays[i].Setup(this, i);
            }
        }
    }

    private void BuildScreenText()
    {
        if (_screenText != null) return;
        var go = new GameObject("ScreenText", typeof(TextMesh));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        _screenText = go.GetComponent<TextMesh>();
        _screenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _screenText.fontSize = 32;
        _screenText.characterSize = 0.038f;
        _screenText.anchor = TextAnchor.MiddleCenter;
        _screenText.alignment = TextAlignment.Center;
        _screenText.GetComponent<MeshRenderer>().sortingOrder = 20;
    }

    private void UpdateScreen()
    {
        if (_screenText == null) return;
        if (isSolved)
        {
            _screenText.text = "CIPHER\nDECRYPTED";
            _screenText.color = new Color(0.2f, 1f, 0.5f);
        }
        else
        {
            string seq = "";
            for (int i = 0; i < _currentInput.Count; i++)
                seq += (_currentInput[i] + 1) + " ";
            _screenText.text = "URUTAN: 1-2-3\nINPUT: " + (seq.Length > 0 ? seq : "_ _ _");
            _screenText.color = new Color(0.4f, 0.9f, 1f);
        }
    }

    public void OnRelayActivated(int relayIndex)
    {
        if (isSolved) return;

        _currentInput.Add(relayIndex);
        UpdateScreen();

        int currentStep = _currentInput.Count - 1;
        if (_currentInput[currentStep] != _targetSequence[currentStep])
        {
            // Wrong sequence, reset!
            _currentInput.Clear();
            Invoke(nameof(UpdateScreen), 0.4f);
            if (_screenText != null)
            {
                _screenText.text = "SALAH URUTAN!\nRESET...";
                _screenText.color = Color.red;
            }
            return;
        }

        if (_currentInput.Count == _targetSequence.Length)
        {
            // Solved!
            isSolved = true;
            UpdateScreen();

            var gm = GameManager.Ensure();
            gm.ledger.Collect(Id, bonusBits);
            gm.dataBitsCollectedThisRun = gm.ledger.Bits;

            var health = FindAnyObjectByType<PlayerHealth>();
            if (health != null) health.Heal(ttlReward);

            OptionalQuestService.ReportCollect(gm.currentWorldIndex, "puzzle:cipher", Id);
        }
    }
}
