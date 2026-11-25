using UnityEngine;

/// <summary>
/// Physical interactive Cyber Software Quiz Board prop placed in the stages.
/// Players can approach the billboard and press [E] or click on it to open the Software Engineering Quiz.
/// Answering correctly awards random resources (DataBits, TTL, or Packet repair).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SoftwareQuizBoard : MonoBehaviour
{
    [Header("Configuration")]
    public string quizBoardId = "w1_s1_quiz";
    public bool isSolved;

    private SpriteRenderer _sr;
    private TextMesh _titleText;
    private InteractKeyPrompt _prompt;
    private bool _playerInRange;
    private float _retryCooldown;

    private string SaveKey => "quiz:" + quizBoardId;

    private void Awake()
    {
        EnsureCollider();
        EnsureVisuals();
        EnsureTitleText();
        EnsurePrompt();
        SnapToGround();
    }

    private void Start()
    {
        SnapToGround();
        var gm = GameManager.Instance;
        if (gm != null && gm.ledger.IsCollected(SaveKey))
        {
            isSolved = true;
        }
        UpdateStatusDisplay();
    }

    private void EnsureCollider()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.8f, 2.5f);
        col.offset = new Vector2(0f, 1.25f);
    }

    private void EnsureVisuals()
    {
        var visualTr = transform.Find("Visual");
        GameObject visualGo;
        if (visualTr == null)
        {
            visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
        }
        else
        {
            visualGo = visualTr.gameObject;
        }

        _sr = visualGo.GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = visualGo.AddComponent<SpriteRenderer>();
        if (_sr.sprite == null || _sr.sprite.name.Contains("Box"))
        {
            _sr.sprite = Resources.Load<Sprite>("GeneratedArt/quiz_board");
        }
        _sr.sortingOrder = 6;

        // Position sprite so the base rests precisely at root y = 0
        if (_sr.sprite != null)
        {
            float pivotYInUnits = _sr.sprite.pivot.y / _sr.sprite.pixelsPerUnit;
            visualGo.transform.localPosition = new Vector3(0f, pivotYInUnits, 0f);
        }
        else
        {
            visualGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        }
    }

    private void EnsureTitleText()
    {
        if (_titleText != null) return;
        var tGo = new GameObject("TitleText", typeof(TextMesh));
        tGo.transform.SetParent(transform, false);
        tGo.transform.localPosition = new Vector3(0f, 2.55f, 0f);

        _titleText = tGo.GetComponent<TextMesh>();
        _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _titleText.fontSize = 36;
        _titleText.characterSize = 0.075f;
        _titleText.anchor = TextAnchor.MiddleCenter;
        _titleText.alignment = TextAlignment.Center;
        _titleText.GetComponent<MeshRenderer>().sortingOrder = 20;
    }

    private void EnsurePrompt()
    {
        if (_prompt != null) return;
        var pGo = new GameObject("QuizPrompt", typeof(InteractKeyPrompt));
        pGo.transform.SetParent(transform, false);
        pGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        _prompt = pGo.GetComponent<InteractKeyPrompt>();
        _prompt.actionLabel = "KUIS SOFTWARE";
        pGo.SetActive(false);
    }

    public void SnapToGround()
    {
        Physics2D.SyncTransforms();
        Vector2 origin = (Vector2)transform.position + Vector2.up * 10.0f;
        Collider2D bestGround = null;
        float highestY = float.MinValue;

        // 1. Raycast downward from above to find the highest solid ground under the board
        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 50f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.normal.y < 0.4f) continue;
            if (hit.collider.GetComponentInParent<NPC_Base>() != null) continue;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            string n = hit.collider.gameObject.name;
            if (n.Contains("Spike") || n.Contains("Hazard") || n.Contains("Laser")) continue;

            if (hit.point.y > highestY)
            {
                highestY = hit.point.y;
                bestGround = hit.collider;
            }
        }

        if (bestGround != null)
        {
            transform.position = new Vector3(transform.position.x, highestY, transform.position.z);
            Physics2D.SyncTransforms();
            return;
        }

        // 2. Direct bounds scan across all solid ground colliders
        float posX = transform.position.x;
        Collider2D nearestGround = null;
        float nearestDist = float.MaxValue;

        foreach (var col in Object.FindObjectsByType<Collider2D>())
        {
            if (col == null || col.isTrigger) continue;
            if (col.GetComponentInParent<PlayerController>() != null) continue;
            if (col.GetComponentInParent<NPC_Base>() != null) continue;
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            string n = col.gameObject.name;
            if (n.Contains("Spike") || n.Contains("Hazard") || n.Contains("Laser")) continue;
            if (n.Contains("Platform_Moving")) continue;

            var b = col.bounds;
            if (posX >= b.min.x - 0.2f && posX <= b.max.x + 0.2f)
            {
                if (b.max.y > highestY)
                {
                    highestY = b.max.y;
                    bestGround = col;
                }
            }

            float dist = Mathf.Abs(b.center.x - posX);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestGround = col;
            }
        }

        if (bestGround != null)
        {
            transform.position = new Vector3(transform.position.x, highestY, transform.position.z);
            Physics2D.SyncTransforms();
            return;
        }

        // 3. If positioned over an abyss/gap, relocate onto the nearest solid ground platform
        if (nearestGround != null)
        {
            transform.position = new Vector3(nearestGround.bounds.center.x, nearestGround.bounds.max.y, transform.position.z);
            Physics2D.SyncTransforms();
            return;
        }

        // 4. Deterministic fallback anchor
        var lm = FindAnyObjectByType<LevelManager>();
        if (lm != null && lm.worldIndex == 0)
        {
            transform.position = new Vector3(transform.position.x, -17.0f, transform.position.z);
            Physics2D.SyncTransforms();
        }
    }

    private void UpdateStatusDisplay()
    {
        if (_titleText == null) return;

        if (isSolved)
        {
            _titleText.text = "[ KUIS SOFTWARE ]\n<STATUS: SELESAI>";
            _titleText.color = new Color(0.2f, 1f, 0.5f);
        }
        else if (_retryCooldown > 0f)
        {
            _titleText.text = $"[ KUIS SOFTWARE ]\n<RELOAD: {_retryCooldown:0}s>";
            _titleText.color = new Color(1f, 0.4f, 0.4f);
        }
        else
        {
            _titleText.text = "[ KUIS SOFTWARE ]\n<E / KLIK UNTUK AKSES>";
            _titleText.color = new Color(0.2f, 0.9f, 1f);
        }
    }

    private void Update()
    {
        if (_retryCooldown > 0f)
        {
            _retryCooldown -= Time.deltaTime;
            if (_retryCooldown <= 0f)
            {
                _retryCooldown = 0f;
                UpdateStatusDisplay();
            }
        }

        if (!_playerInRange || isSolved || _retryCooldown > 0f) return;

        var quizUI = SoftwareQuizUI.Instance;
        if (quizUI != null && quizUI.IsOpen) return;

        bool pressE = Input.GetKeyDown(KeyCode.E);
        var input = InputManager.Instance;
        if (input != null && input.interactPressed)
        {
            pressE = true;
            input.ConsumeInteract();
        }

        if (pressE)
        {
            OpenQuiz();
        }
    }

    private void OnMouseDown()
    {
        if (_playerInRange && !isSolved && _retryCooldown <= 0f)
        {
            OpenQuiz();
        }
    }

    public void OpenQuiz()
    {
        if (isSolved || _retryCooldown > 0f) return;
        var quizUI = SoftwareQuizUI.Ensure();
        quizUI.Open(this, OnQuizFinished);
    }

    private void OnQuizFinished(bool success, string detail)
    {
        if (success)
        {
            isSolved = true;
            var gm = GameManager.Instance;
            if (gm != null) gm.ledger.CollectRun(SaveKey, 1);
            AchievementManager.Unlock("ACH_SYN_ACK");
            JuiceManager.SpawnImpactSparks(transform.position + Vector3.up * 1.2f, Color.yellow);
        }
        else
        {
            _retryCooldown = 5f; // 5s cooldown before retry
        }
        UpdateStatusDisplay();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            _playerInRange = true;
            if (_prompt != null && !isSolved && _retryCooldown <= 0f)
                _prompt.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            _playerInRange = false;
            if (_prompt != null)
                _prompt.gameObject.SetActive(false);
        }
    }
}
