using UnityEngine;
using UnityEngine.UI;

public class HUD_Cooldown : MonoBehaviour
{
    [Header("References")]
    public Image cooldownFillBar;
    public Text cooldownText;
    public Image powerUpIcon;
    public Text powerUpText;

    [Header("Layout")]
    public RectTransform barContainer;
    public float barHeight = 100f;
    public float barWidth = 16f;

    [Header("Color")]
    public Color readyColor = Color.cyan;
    public Color cooldownColor = Color.gray;

    private PlayerController _player;
    private PlayerMode _mode;

    private void Start()
    {
        _player = FindAnyObjectByType<PlayerController>();
        _mode = FindAnyObjectByType<PlayerMode>();

        if (barContainer && cooldownFillBar)
            barContainer.sizeDelta = new Vector2(barWidth, barHeight);
    }

    private void Update()
    {
        if (_player == null || _mode == null) return;

        float maxCD = _mode.GetDashCooldown();
        float timer = _player.dashCooldownTimer;
        float fill = maxCD > 0 ? Mathf.Clamp01(1f - (timer / maxCD)) : 1f;

        if (cooldownFillBar)
        {
            cooldownFillBar.fillAmount = fill;
            cooldownFillBar.color = fill >= 1f ? readyColor : cooldownColor;
        }

        if (cooldownText)
        {
            bool ready = fill >= 1f;
            cooldownText.text = ready ? "DASH" : "···";
            cooldownText.color = ready ? readyColor : cooldownColor;
        }
    }
}
