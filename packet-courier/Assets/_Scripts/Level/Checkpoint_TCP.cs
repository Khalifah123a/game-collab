using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint_TCP : MonoBehaviour
{
    [Header("Checkpoint")]
    public bool isActivated;
    public GameObject visualActive;
    public GameObject visualInactive;

    [Header("Bonus")]
    public float ttlBonus = 10f;
    public float activationTime = 0.35f;

    private float _holdTimer;
    private bool _playerInRange;
    private InputManager _input;
    private InteractKeyPrompt _keyPrompt;
    private string Id => "checkpoint:" + gameObject.name;
    private Vector3 _originalScale = Vector3.one;
    private Coroutine _activationCoroutine;

    private void Start()
    {
        _input = InputManager.Instance;
        _originalScale = transform.localScale;
        isActivated = GameManager.Instance != null && GameManager.Instance.ledger.IsCollected(Id);

        EnsureVisualSprites();

        if (visualActive) visualActive.SetActive(isActivated);
        if (visualInactive) visualInactive.SetActive(!isActivated);

        BuildPromptObject();
    }

    private void Awake()
    {
        EnsureVisualSprites();
    }

    private void EnsureVisualSprites()
    {
        if (visualInactive == null)
        {
            var t = transform.Find("Visual_Inactive");
            if (t != null) visualInactive = t.gameObject;
            else
            {
                visualInactive = new GameObject("Visual_Inactive", typeof(SpriteRenderer));
                visualInactive.transform.SetParent(transform, false);
                visualInactive.transform.localPosition = Vector3.zero;
            }
        }
        if (visualActive == null)
        {
            var t = transform.Find("Visual_Active");
            if (t != null) visualActive = t.gameObject;
            else
            {
                visualActive = new GameObject("Visual_Active", typeof(SpriteRenderer));
                visualActive.transform.SetParent(transform, false);
                visualActive.transform.localPosition = Vector3.zero;
            }
        }

        if (visualInactive != null)
        {
            var sr = visualInactive.GetComponent<SpriteRenderer>();
            if (sr)
            {
                var s = Resources.Load<Sprite>("GeneratedArt/checkpoint_off");
                if (s != null) { sr.sprite = s; sr.color = Color.white; sr.sortingOrder = 10; }
            }
        }
        if (visualActive != null)
        {
            var sr = visualActive.GetComponent<SpriteRenderer>();
            if (sr)
            {
                var s = Resources.Load<Sprite>("GeneratedArt/checkpoint_on");
                if (s != null) { sr.sprite = s; sr.color = Color.white; sr.sortingOrder = 10; }
            }
        }
    }

    private void BuildPromptObject()
    {
        if (_keyPrompt != null) return;
        var promptGo = new GameObject("InteractPrompt");
        promptGo.transform.SetParent(transform, false);
        promptGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        _keyPrompt = promptGo.AddComponent<InteractKeyPrompt>();
        _keyPrompt.SetLabel("[E] SYNC TCP");
        _keyPrompt.Hide();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
        {
            _playerInRange = true;
            if (!isActivated && _keyPrompt != null) _keyPrompt.Show();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
        {
            _playerInRange = false;
            _holdTimer = 0;
            if (_keyPrompt != null) _keyPrompt.Hide();
        }
    }

    private void Update()
    {
        if (isActivated || !_playerInRange) return;

        bool interactTap = Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Submit");
        bool interactHold = Input.GetKey(KeyCode.E) || Input.GetButton("Submit") || (_input != null && _input.interactPressed);

        if (interactTap)
        {
            Activate();
            return;
        }

        if (interactHold)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= activationTime)
            {
                Activate();
            }
        }
        else
        {
            _holdTimer = 0;
        }
    }

    private void Activate()
    {
        if (isActivated) return;
        isActivated = true;
        if (_keyPrompt != null)
        {
            _keyPrompt.TriggerClick(true);
            _keyPrompt.Hide();
        }

        AudioManager.Play("sfx_collect", 1f, 1.25f);
        AudioManager.PlayInteract();

        var health = FindAnyObjectByType<PlayerHealth>();
        if (health)
        {
            health.Heal(ttlBonus);
            var gm = GameManager.Ensure();
            gm.ledger.Collect(Id, 0);
            gm.SaveCheckpoint(Id, transform.position + Vector3.up * 0.1f, health.ttlCurrent);
        }

        if (visualActive) visualActive.SetActive(true);
        if (visualInactive) visualInactive.SetActive(false);

        _input?.ConsumeInteract();

        // Game Feel & VFX Juice
        JuiceManager.Shake(0.25f);
        JuiceManager.SpawnImpactSparks(transform.position + Vector3.up * 1.0f, new Color(0f, 0.95f, 1f));

        if (_activationCoroutine != null) StopCoroutine(_activationCoroutine);
        _activationCoroutine = StartCoroutine(AnimateActivationRoutine());
    }

    private IEnumerator AnimateActivationRoutine()
    {
        Vector3 basePos = transform.position;

        // 1. Squash & Stretch Scale Punch
        float elapsed = 0f;
        float punchDuration = 0.45f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            // Elastic bounce
            float scaleY = 1f + 0.35f * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
            float scaleX = 1f - 0.2f * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
            transform.localScale = new Vector3(_originalScale.x * scaleX, _originalScale.y * scaleY, _originalScale.z);
            yield return null;
        }
        transform.localScale = _originalScale;

        // 2. Vertical Uplink Laser Beam
        var beamGo = new GameObject("Checkpoint_LaserBeam", typeof(SpriteRenderer));
        beamGo.transform.position = basePos + new Vector3(0f, 12f, -0.1f);
        var beamSr = beamGo.GetComponent<SpriteRenderer>();
        var boxSprite = NPC_Base.BoxSprite();
        beamSr.sprite = boxSprite;
        beamSr.sortingOrder = 22;
        var beamUnit = boxSprite != null ? boxSprite.bounds.size : Vector3.one;

        // 3. Expanding Holographic Shockwave Ring
        var ringGo = new GameObject("Checkpoint_Shockwave", typeof(SpriteRenderer));
        ringGo.transform.position = basePos + new Vector3(0f, 0.8f, -0.15f);
        var ringSr = ringGo.GetComponent<SpriteRenderer>();
        ringSr.sprite = Resources.Load<Sprite>("GeneratedArt/key_e_ring");
        ringSr.sortingOrder = 23;

        // 4. Floating Holographic Message
        var msgGo = new GameObject("Checkpoint_HoloText", typeof(TextMesh));
        msgGo.transform.position = basePos + new Vector3(0f, 2.2f, -0.2f);
        var tm = msgGo.GetComponent<TextMesh>();
        tm.text = "[TCP SYN-ACK // CHECKPOINT SYNCED]\n+10s TTL RESTORED";
        tm.fontSize = 24;
        tm.characterSize = 0.08f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = new Color(0.2f, 0.95f, 1f, 1f);

        // 5. Burst of Procedural Cyber Particle Bits
        int bitCount = 12;
        var particleBits = new Transform[bitCount];
        var bitVelocities = new Vector3[bitCount];
        for (int i = 0; i < bitCount; i++)
        {
            var pGo = new GameObject($"CyberBit_{i}", typeof(SpriteRenderer));
            pGo.transform.position = basePos + new Vector3(Random.Range(-0.3f, 0.3f), 0.8f + Random.Range(0f, 0.5f), -0.1f);
            var pSr = pGo.GetComponent<SpriteRenderer>();
            pSr.sprite = boxSprite;
            pSr.color = Color.Lerp(new Color(0f, 0.95f, 1f, 1f), Color.white, Random.value * 0.6f);
            pSr.sortingOrder = 24;
            pGo.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
            particleBits[i] = pGo.transform;

            float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
            float spd = Random.Range(2.5f, 6.0f);
            bitVelocities[i] = new Vector3(Mathf.Cos(angle) * spd, Mathf.Sin(angle) * spd, 0f);
        }

        // Animate particles, beam, ring, and text over 0.75s
        float vfxDuration = 0.75f;
        float vfxTimer = 0f;
        while (vfxTimer < vfxDuration)
        {
            vfxTimer += Time.deltaTime;
            float p = vfxTimer / vfxDuration;

            // Beam fade and width pulse
            if (beamGo != null)
            {
                float beamW = Mathf.Lerp(0.85f, 0.0f, p);
                beamGo.transform.localScale = new Vector3(beamW / beamUnit.x, 24f / beamUnit.y, 1f);
                beamSr.color = new Color(0.1f, 0.95f, 1f, (1f - p) * 0.9f);
            }

            // Ring expansion
            if (ringGo != null)
            {
                float ringScale = Mathf.Lerp(0.5f, 4.0f, p);
                ringGo.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                ringSr.color = new Color(0.2f, 0.95f, 1f, (1f - p) * 0.85f);
            }

            // Holographic text float up & fade
            if (msgGo != null)
            {
                msgGo.transform.position = basePos + new Vector3(0f, 2.2f + p * 0.8f, -0.2f);
                tm.color = new Color(0.2f, 0.95f, 1f, 1f - p * p);
            }

            // Particles fly out and fall
            for (int i = 0; i < bitCount; i++)
            {
                if (particleBits[i] != null)
                {
                    particleBits[i].position += bitVelocities[i] * Time.deltaTime;
                    bitVelocities[i].y -= 4f * Time.deltaTime; // gravity
                    particleBits[i].localScale = Vector3.Lerp(particleBits[i].localScale, Vector3.zero, Time.deltaTime * 3.5f);
                }
            }

            yield return null;
        }

        // Cleanup VFX objects
        if (beamGo != null) Destroy(beamGo);
        if (ringGo != null) Destroy(ringGo);
        if (msgGo != null) Destroy(msgGo);
        for (int i = 0; i < bitCount; i++)
        {
            if (particleBits[i] != null) Destroy(particleBits[i].gameObject);
        }
    }
}
