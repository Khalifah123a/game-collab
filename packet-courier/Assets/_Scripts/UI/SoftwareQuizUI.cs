using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime Modal UI for Software & CS Engineering Quiz Terminal.
/// When opened, presents an authentic software question with 4 choices.
/// Evaluates answers, provides educational explanation, and distributes random loot rewards upon success.
/// </summary>
public class SoftwareQuizUI : MonoBehaviour
{
    public static SoftwareQuizUI Instance { get; private set; }
    public bool IsOpen => _isOpen;
    public static bool IsQuizOpen => Instance != null && Instance._isOpen;

    [System.Serializable]
    public class QuizQuestion
    {
        public string category;
        public string question;
        public string[] options;
        public int correctIndex;
        public string explanation;
    }

    private static readonly QuizQuestion[] QuestionBank = new[]
    {
        new QuizQuestion
        {
            category = "DATA STRUCTURES",
            question = "Struktur data mana yang bekerja dengan prinsip LIFO (Last In, First Out)?",
            options = new[] { "Queue (Antrean)", "Stack (Tumpukan)", "Binary Search Tree", "Linked List" },
            correctIndex = 1,
            explanation = "Stack mengambil elemen yang paling terakhir dimasukkan terlebih dahulu (LIFO)."
        },
        new QuizQuestion
        {
            category = "VERSION CONTROL (GIT)",
            question = "Perintah Git apa yang dipakai untuk membuat branch baru dan langsung berpindah ke branch tersebut?",
            options = new[] { "git branch -d", "git checkout -b", "git commit -m", "git push --force" },
            correctIndex = 1,
            explanation = "git checkout -b <nama> secara otomatis membuat branch baru dan berpindah ke dalamnya."
        },
        new QuizQuestion
        {
            category = "NETWORKING & PROTOCOLS",
            question = "Protokol layer transport yang berorientasi koneksi dan menjamin keutuhan urutan paket data adalah?",
            options = new[] { "UDP (User Datagram Protocol)", "TCP (Transmission Control Protocol)", "ICMP (Control Message)", "ARP (Address Resolution)" },
            correctIndex = 1,
            explanation = "TCP menjamin data terkirim berurutan tanpa packet loss melalui 3-way handshake dan acknowledgement."
        },
        new QuizQuestion
        {
            category = "ALGORITHMS & COMPLEXITY",
            question = "Berapa kompleksitas waktu rata-rata (Average Time Complexity) operasi pencarian pada Hash Table?",
            options = new[] { "O(n)", "O(log n)", "O(1) Konstan", "O(n^2)" },
            correctIndex = 2,
            explanation = "Pencarian Hash Table dengan fungsi hash terdistribusi baik memiliki kompleksitas rata-rata O(1)."
        },
        new QuizQuestion
        {
            category = "OBJECT-ORIENTED PROGRAMMING",
            question = "Konsep OOP di mana subclass memberikan implementasi spesifik terhadap method yang sudah ada di superclass disebut?",
            options = new[] { "Method Overriding", "Method Overloading", "Encapsulation", "Polymorphic Casting" },
            correctIndex = 0,
            explanation = "Method Overriding mendefinisikan ulang perilaku method milik parent class pada child class."
        },
        new QuizQuestion
        {
            category = "DATABASE & SQL",
            question = "Constraint database yang menjamin setiap baris memiliki identitas unik dan tidak boleh bernilai NULL adalah?",
            options = new[] { "Foreign Key", "Primary Key", "Index Non-Clustered", "Check Constraint" },
            correctIndex = 1,
            explanation = "Primary Key menjadi pengenal unik record dan secara absolut tidak mengizinkan nilai NULL."
        },
        new QuizQuestion
        {
            category = "SOFTWARE ARCHITECTURE",
            question = "Design pattern yang memastikan sebuah class hanya memiliki satu instance tunggal di seluruh aplikasi adalah?",
            options = new[] { "Factory Pattern", "Singleton Pattern", "Observer Pattern", "Strategy Pattern" },
            correctIndex = 1,
            explanation = "Singleton Pattern membatasi instansiasi kelas menjadi satu objek global tunggal."
        },
        new QuizQuestion
        {
            category = "WEB ARCHITECTURE",
            question = "Kode status HTTP yang menandakan 'Akses Ditolak / Terlarang' (Forbidden) adalah?",
            options = new[] { "404 Not Found", "500 Internal Server Error", "403 Forbidden", "200 OK" },
            correctIndex = 2,
            explanation = "HTTP 403 Forbidden menandakan server mengenali identitas klien namun menolak memberikan akses."
        },
        new QuizQuestion
        {
            category = "OPERATING SYSTEMS",
            question = "Kondisi ketika dua atau lebih proses saling menunggu resource yang dipegang proses lain sehingga keduanya macet disebut?",
            options = new[] { "Deadlock", "Race Condition", "Memory Leak", "Starvation" },
            correctIndex = 0,
            explanation = "Deadlock terjadi saat circular dependency antar proses memblokir alokasi resource selamanya."
        },
        new QuizQuestion
        {
            category = "SOFTWARE TESTING",
            question = "Tingkatan pengujian software yang memvalidasi fungsi method/komponen terkecil secara terisolasi adalah?",
            options = new[] { "Integration Testing", "Unit Testing", "System Testing", "Smoke Testing" },
            correctIndex = 1,
            explanation = "Unit Testing memverifikasi logika unit kode terkecil secara mandiri dan cepat."
        }
    };

    private bool _isOpen;
    private object _activeOwner;
    private Canvas _canvas;
    private Text _categoryLabel;
    private Text _questionLabel;
    private Text _resultFeedback;
    private readonly List<Button> _optionButtons = new List<Button>();
    private readonly List<Text> _optionTexts = new List<Text>();
    private Button _closeButton;

    private QuizQuestion _currentQuestion;
    private bool _answered;
    private Action<bool, string> _onCompletedCallback;

    public static SoftwareQuizUI Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("SoftwareQuiz_Canvas");
        return go.AddComponent<SoftwareQuizUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildUI()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 90; // High priority above gameplay HUD

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        gameObject.AddComponent<GraphicRaycaster>();

        if (UnityEngine.EventSystems.EventSystem.current == null)
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        // Background Dimmer
        var dimmer = new GameObject("Dimmer", typeof(RectTransform));
        dimmer.transform.SetParent(transform, false);
        var dimRt = dimmer.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero; dimRt.offsetMax = Vector2.zero;
        var dimImg = dimmer.AddComponent<Image>();
        dimImg.color = new Color(0.02f, 0.04f, 0.08f, 0.75f);

        // Center Cyber Modal Panel
        var panel = new GameObject("QuizPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.22f, 0.12f);
        panelRt.anchorMax = new Vector2(0.78f, 0.88f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var pImg = panel.AddComponent<Image>();
        pImg.color = new Color(0.04f, 0.08f, 0.15f, 0.98f);

        // Panel Border Outline
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.15f, 0.85f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        var title = MakeLabel("Title", panel.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), 24, TextAnchor.MiddleCenter);
        title.text = "[ ! ] TERMINAL EVALUASI SOFTWARE [ ! ]";
        title.color = new Color(0.2f, 0.95f, 1f);

        // Category Subheader
        _categoryLabel = MakeLabel("Category", panel.transform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.88f), 15, TextAnchor.MiddleCenter);
        _categoryLabel.color = new Color(1f, 0.8f, 0.25f);

        // Question Body
        _questionLabel = MakeLabel("Question", panel.transform, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.82f), 19, TextAnchor.UpperLeft);
        _questionLabel.color = Color.white;

        // 4 Option Buttons (A, B, C, D)
        float topY = 0.56f;
        float slotHeight = 0.095f;
        for (int i = 0; i < 4; i++)
        {
            var btn = MakeOptionButton(i, panel.transform, topY - i * (slotHeight + 0.015f), slotHeight);
            _optionButtons.Add(btn);
        }

        // Result Feedback Message
        _resultFeedback = MakeLabel("Feedback", panel.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.16f), 16, TextAnchor.MiddleCenter);
        _resultFeedback.text = "";

        // Close / Batal Button
        var closeGo = new GameObject("CloseButton", typeof(RectTransform));
        closeGo.transform.SetParent(panel.transform, false);
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.35f, 0.015f);
        closeRt.anchorMax = new Vector2(0.65f, 0.075f);
        closeRt.offsetMin = Vector2.zero; closeRt.offsetMax = Vector2.zero;
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
        _closeButton = closeGo.AddComponent<Button>();
        _closeButton.targetGraphic = closeImg;
        _closeButton.onClick.AddListener(Close);
        var closeTxt = MakeLabel("CloseTxt", closeGo.transform, Vector2.zero, Vector2.one, 15, TextAnchor.MiddleCenter);
        closeTxt.text = "[ TUTUP / ESC ]";
        closeTxt.color = new Color(0.85f, 0.9f, 1f);
    }

    private Button MakeOptionButton(int index, Transform parent, float top, float height)
    {
        var go = new GameObject("Option_" + index, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.06f, top - height);
        rt.anchorMax = new Vector2(0.94f, top);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.16f, 0.28f, 0.95f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        int captured = index;
        btn.onClick.AddListener(() => OnOptionSelected(captured));

        char letter = (char)('A' + index);
        var txt = MakeLabel("Label", go.transform, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), 16, TextAnchor.MiddleLeft);
        txt.color = Color.white;
        _optionTexts.Add(txt);

        return btn;
    }

    private static Text MakeLabel(string name, Transform parent, Vector2 min, Vector2 max, int fontSize, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = fontSize >= 22 ? PixelUIKit.GetDisplayFont() : PixelUIKit.GetMonoFont();
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    public void Open(object owner, Action<bool, string> onCompleted = null)
    {
        if (_isOpen) return;
        _activeOwner = owner;
        _onCompletedCallback = onCompleted;
        _isOpen = true;
        _answered = false;
        gameObject.SetActive(true);

        PickRandomQuestion();
        DisplayQuestion();

        AudioManager.PlayUIOpen();
        AudioManager.SetMuffled(true);

        // Freeze player movement
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.isFrozen = true;
    }

    private void PickRandomQuestion()
    {
        int idx = UnityEngine.Random.Range(0, QuestionBank.Length);
        _currentQuestion = QuestionBank[idx];
    }

    private void DisplayQuestion()
    {
        if (_currentQuestion == null) return;
        _categoryLabel.text = $"KATEGORI: {_currentQuestion.category}";
        _questionLabel.text = _currentQuestion.question;
        _resultFeedback.text = "Pilih salah satu jawaban (1 - 4 atau klik):";
        _resultFeedback.color = new Color(0.7f, 0.85f, 1f);

        for (int i = 0; i < 4; i++)
        {
            if (i < _currentQuestion.options.Length)
            {
                char letter = (char)('A' + i);
                _optionTexts[i].text = $"[{letter}]  {_currentQuestion.options[i]}";
                _optionButtons[i].image.color = new Color(0.1f, 0.16f, 0.28f, 0.95f);
                _optionButtons[i].interactable = true;
            }
        }
    }

    private void OnOptionSelected(int choiceIndex)
    {
        if (_answered || _currentQuestion == null) return;
        _answered = true;

        bool isCorrect = (choiceIndex == _currentQuestion.correctIndex);
        AudioManager.PlayUIClick();

        // Highlight options
        for (int i = 0; i < 4; i++)
        {
            _optionButtons[i].interactable = false;
            if (i == _currentQuestion.correctIndex)
                _optionButtons[i].image.color = new Color(0.1f, 0.55f, 0.25f, 0.95f); // Green
            else if (i == choiceIndex && !isCorrect)
                _optionButtons[i].image.color = new Color(0.6f, 0.15f, 0.15f, 0.95f); // Red
        }

        if (isCorrect)
        {
            AudioManager.PlayLevelClear();
            string rewardInfo = DistributeRandomReward();
            _resultFeedback.text = $"[OK] BENAR! {rewardInfo}\n{_currentQuestion.explanation}";
            _resultFeedback.color = new Color(0.3f, 1f, 0.5f);
            _onCompletedCallback?.Invoke(true, rewardInfo);
        }
        else
        {
            AudioManager.PlayHurt();
            _resultFeedback.text = $"[ERR] SALAH! Tidak mendapatkan reward.\nJawaban benar: {_currentQuestion.options[_currentQuestion.correctIndex]}";
            _resultFeedback.color = new Color(1f, 0.4f, 0.4f);
            _onCompletedCallback?.Invoke(false, "Salah");
        }
    }

    private string DistributeRandomReward()
    {
        int roll = UnityEngine.Random.Range(0, 100);
        var gm = GameManager.Instance;
        var health = PlayerHealth.Instance;

        // 1. DataBits Loot (45% chance)
        if (roll < 45)
        {
            int bits = UnityEngine.Random.Range(30, 60);
            if (gm != null)
            {
                gm.AddDataBits(bits);
                if (gm.isInRun) gm.dataBitsCollectedThisRun += bits;
            }
            return $"+{bits} DATABITS!";
        }
        // 2. TTL Health Overclock (35% chance)
        else if (roll < 80)
        {
            float ttlBonus = 35f;
            if (health != null) health.Heal(ttlBonus);
            return $"+{ttlBonus:0}s TTL RECOVERY OVERCLOCK!";
        }
        // 3. Packet Integrity Repair (20% chance)
        else
        {
            var pim = PacketIntegrityManager.Instance;
            if (pim != null)
            {
                pim.Repair(30f);
                return "INTEGRITAS PAKET PULIH +30%!";
            }
            // Fallback to DataBits if no packet carried
            if (gm != null) gm.AddDataBits(40);
            return "+40 DATABITS BONUS!";
        }
    }

    private void Update()
    {
        if (!_isOpen) return;

        var input = InputManager.Instance;
        if (input != null)
        {
            input.ConsumeInteract();
            input.ConsumeAttack();
        }

        // Quick keypad 1 - 4 answers
        if (!_answered)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.A)) OnOptionSelected(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.B)) OnOptionSelected(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.C)) OnOptionSelected(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.D)) OnOptionSelected(3);
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Close();
        }
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _activeOwner = null;
        gameObject.SetActive(false);

        AudioManager.SetMuffled(false);

        // Unfreeze player
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.isFrozen = false;
    }
}
