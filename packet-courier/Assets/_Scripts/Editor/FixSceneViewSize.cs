#if UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FixSceneViewSize
{
    private const int CheckFrames = 300;
    private static int framesLeft = CheckFrames;

    static FixSceneViewSize()
    {
        EditorApplication.update += AutoFix;
    }

    private static void AutoFix()
    {
        if (framesLeft <= 0)
        {
            EditorApplication.update -= AutoFix;
            return;
        }

        framesLeft--;

        var views = Resources.FindObjectsOfTypeAll<SceneView>();
        foreach (var view in views)
        {
            if (view == null) continue;
            if (TryFixView(view))
            {
                Debug.Log("FixSceneViewSize: auto-fixed a scene view to " + (int)view.position.width + "x" + (int)view.position.height + " at (" + (int)view.position.x + "," + (int)view.position.y + ")");
            }
        }
    }

    [MenuItem("Tools/Fix SceneView Size")]
    public static void Fix()
    {
        var views = Resources.FindObjectsOfTypeAll<SceneView>();
        if (views.Length == 0)
        {
            SceneView.lastActiveSceneView?.Show();
            return;
        }

        int fixedCount = 0;
        foreach (var view in views)
        {
            if (view == null) continue;
            view.Show();
            if (TryFixView(view)) fixedCount++;
        }

        Debug.Log("FixSceneViewSize: " + views.Length + " scene view(s), " + fixedCount + " resized.");
    }

    private static bool TryFixView(SceneView view)
    {
        if (view == null) return false;

        var rect = view.position;

        GetVirtualDesktop(out int vx, out int vy, out int vw, out int vh);

        bool offScreen = (rect.x + rect.width < vx + 1f) ||
                         (rect.y + rect.height < vy + 1f) ||
                         (rect.x > vx + vw - 1f) ||
                         (rect.y > vy + vh - 1f);

        bool tooSmall = rect.width < 200f || rect.height < 200f;

        if (!offScreen && !tooSmall) return false;

        float w = Mathf.Max(rect.width, 1000f);
        float h = Mathf.Max(rect.height, 700f);
        w = Mathf.Min(w, vw - 200f);
        h = Mathf.Min(h, vh - 150f);

        float x = rect.x;
        float y = rect.y;
        if (offScreen || x < vx || y < vy)
        {
            x = vx + 100f;
            y = vy + 100f;
        }

        rect.x = x;
        rect.y = y;
        rect.width = w;
        rect.height = h;
        view.position = rect;
        return true;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    private static void GetVirtualDesktop(out int x, out int y, out int width, out int height)
    {
        x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        if (width <= 0) { width = 1920; }
        if (height <= 0) { height = 1080; }
    }
}
#endif
