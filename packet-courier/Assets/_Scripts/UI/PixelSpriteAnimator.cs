using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cycles through pixel-art sprite frames using unscaled time.
/// Kept in its own matching .cs file so Unity generates a valid MonoScript GUID.
/// </summary>
public class PixelSpriteAnimator : MonoBehaviour
{
    public Image targetImage;
    public Sprite[] frames;
    public float framesPerSecond = 6f;

    private float _timer;
    private int _index;

    private void Update()
    {
        if (targetImage == null || frames == null || frames.Length == 0) return;
        _timer += Time.unscaledDeltaTime;
        float interval = 1f / Mathf.Max(1f, framesPerSecond);
        if (_timer >= interval)
        {
            _timer -= interval;
            _index = (_index + 1) % frames.Length;
            if (frames[_index] != null) targetImage.sprite = frames[_index];
        }
    }
}
