using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles tactile multi-state visual transitions (Default / Hover / Active Press)
/// using unscaled time so it works both in Menu and when Time.timeScale == 0.
/// Kept in its own matching .cs file so Unity generates a valid MonoScript GUID.
/// </summary>
public class TactilePixelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image bgImage;
    public RectTransform contentRt;
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;
    public Text titleText;
    public Text subText;
    public Image iconImage;
    public Text keycapText;
    public Color normalTitleColor = Color.white;
    public Color hoverTitleColor = Color.white;
    public Color pressedTitleColor = Color.white;

    private bool _hovered;
    private bool _pressed;
    private Vector3 _targetScale = Vector3.one;

    private void OnEnable()
    {
        _hovered = false;
        _pressed = false;
        transform.localScale = Vector3.one;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
        ApplyVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        ApplyVisualState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (bgImage != null)
        {
            if (_pressed && pressedSprite != null) bgImage.sprite = pressedSprite;
            else if (_hovered && hoverSprite != null) bgImage.sprite = hoverSprite;
            else if (normalSprite != null) bgImage.sprite = normalSprite;
        }

        if (titleText != null)
        {
            titleText.color = _pressed ? pressedTitleColor : (_hovered ? hoverTitleColor : normalTitleColor);
        }

        if (contentRt != null)
        {
            float yShift = _pressed ? -3f : 0f;
            contentRt.anchoredPosition = new Vector2(0f, yShift);
        }

        _targetScale = _pressed ? new Vector3(0.985f, 0.985f, 1f) : (_hovered ? new Vector3(1.025f, 1.025f, 1f) : Vector3.one);
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, dt * 18f);
    }
}
