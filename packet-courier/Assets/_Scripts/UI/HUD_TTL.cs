using UnityEngine;
using UnityEngine.UI;

public class HUD_TTL : MonoBehaviour
{
    [Header("References")]
    public Image ttlFillBar;
    public Text ttlText;

    [Header("Color")]
    public Color highColor = Color.green;
    public Color midColor = Color.yellow;
    public Color lowColor = Color.red;
    public float lowThreshold = 0.3f;
    public float midThreshold = 0.6f;

    private PlayerHealth _health;

    private void Start()
    {
        _health = FindObjectOfType<PlayerHealth>();
        if (_health == null)
            enabled = false;
    }

    private void Update()
    {
        if (_health == null) return;

        float fill = _health.ttlCurrent / _health.ttlMax;
        ttlFillBar.fillAmount = fill;

        if (fill <= lowThreshold)
            ttlFillBar.color = lowColor;
        else if (fill <= midThreshold)
            ttlFillBar.color = midColor;
        else
            ttlFillBar.color = highColor;

        ttlText.text = $"{Mathf.CeilToInt(_health.ttlCurrent)}/{Mathf.CeilToInt(_health.ttlMax)}";
    }
}
