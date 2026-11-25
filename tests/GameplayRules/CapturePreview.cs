using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class CapturePreview
{
    public static async Task<string> Main()
    {
        var camera = Camera.main;
        var canvas = UnityEngine.Object.FindObjectOfType<GameplayHUD>().GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        int[] widths = { 1280, 1920, 2560 }, heights = { 720, 1080, 1080 };
        try
        {
            for (int i = 0; i < widths.Length; i++)
            {
                var target = new RenderTexture(widths[i], heights[i], 24);
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                await Task.Delay(80);
                Canvas.ForceUpdateCanvases();
                foreach (var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                {
                    if (string.IsNullOrEmpty(text.text)) continue;
                    var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                    var lo = camera.WorldToScreenPoint(corners[0]); var hi = camera.WorldToScreenPoint(corners[2]);
                    if (lo.x < -1 || lo.y < -1 || hi.x > widths[i]+1 || hi.y > heights[i]+1) throw new Exception("HUD clipping at " + widths[i]);
                }
                camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var image = new Texture2D(widths[i], heights[i], TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, widths[i], heights[i]), 0, 0); image.Apply();
                File.WriteAllBytes("D:/unity/game-collab/artifacts/preview-" + widths[i] + ".png", image.EncodeToPNG());
                RenderTexture.active = previous; camera.targetTexture = null;
                UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(target);
            }
            return "PASS: rendered HUD fits 1280x720, 1920x1080, 2560x1080";
        }
        finally { camera.targetTexture = null; canvas.renderMode = RenderMode.ScreenSpaceOverlay; }
    }
}
