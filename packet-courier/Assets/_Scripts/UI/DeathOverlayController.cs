using System.Collections;
using UnityEngine;

/// <summary>
/// Handles unscaled pop-up emergence animation and hotkeys (R / Enter / Space / T / Esc)
/// for the Die Scene (GameOverUI). Kept in its own matching .cs file so Unity generates a valid MonoScript GUID.
/// </summary>
public class DeathOverlayController : MonoBehaviour
{
    public RectTransform cardRt;
    public CanvasGroup canvasGroup;

    private void Start()
    {
        if (!Application.isPlaying)
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (cardRt != null) cardRt.localScale = Vector3.one;
            return;
        }

        if (cardRt != null && canvasGroup != null)
        {
            StartCoroutine(AnimatePopUp());
        }
    }

    private IEnumerator AnimatePopUp()
    {
        float duration = 0.25f;
        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        cardRt.localScale = new Vector3(0.6f, 0.6f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-out back curve with bounce
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float scale = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);
            cardRt.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        cardRt.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Hotkeys running during Time.timeScale == 0
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            GameOverUI.Retry();
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            GameOverUI.Restart();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameOverUI.ReturnToMenu();
        }
    }
}
