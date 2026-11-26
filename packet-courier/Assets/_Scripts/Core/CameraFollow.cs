using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(1.8f, 1.2f, -10);
    public float smoothSpeed = 5f;

    [Header("Zoom")]
    public bool enableZoom = true;
    public float orthoSize = 4.5f;
    public float minZoom = 3f;
    public float maxZoom = 12f;
    public float zoomSpeed = 2f;

    private Camera cam;
    private PlayerController player;
    private Vector3 followVelocity;
    private Vector3 _basePosition;
    private bool _basePosInitialized;
    private float lookAhead, lookVelocity;

    private bool _arenaLocked;
    private float _arenaMinX;
    private float _arenaMaxX;
    private float _arenaFocusY;
    private float _arenaOrthoSize = 6.2f;

    public bool IsArenaLocked => _arenaLocked;

    public void SetArenaBounds(float minX, float maxX, float focusY, float orthoSize = 6.2f)
    {
        _arenaLocked = true;
        _arenaMinX = Mathf.Min(minX, maxX);
        _arenaMaxX = Mathf.Max(minX, maxX);
        _arenaFocusY = focusY;
        _arenaOrthoSize = Mathf.Clamp(orthoSize, minZoom, maxZoom);
    }

    public void ClearArenaBounds()
    {
        _arenaLocked = false;
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam) cam.orthographicSize = orthoSize;
        if (GetComponent<AudioListener>() == null && FindAnyObjectByType<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
        }
        if (!target) target = FindAnyObjectByType<PlayerController>()?.transform;
        if (target)
        {
            player = target.GetComponent<PlayerController>();
            lookAhead = offset.x * (player ? player.FacingDir : 1);
            _basePosition = target.position + new Vector3(lookAhead, offset.y, offset.z);
            _basePosInitialized = true;
            transform.position = _basePosition;
        }
    }

    private void LateUpdate()
    {
        bool hitstopping = JuiceManager.Instance != null && JuiceManager.Instance.IsHitstopping;
        if (Time.timeScale == 0 && !hitstopping) return;

        if (hitstopping)
        {
            // Keep crisp micro-vibration alive during hitstop freeze-frames without drifting base camera position
            if (_basePosInitialized)
            {
                Vector3 hitShake = JuiceManager.Instance.CurrentShakeOffset;
                transform.position = _basePosition + hitShake;
            }
            return;
        }

        if (target == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player) target = player.transform;
            else return;
        }

        if (!player) player = target.GetComponent<PlayerController>();
        if (!_basePosInitialized)
        {
            _basePosition = transform.position;
            _basePosInitialized = true;
        }

        float desiredOrtho = _arenaLocked ? _arenaOrthoSize : orthoSize;
        if (cam)
        {
            if (!_arenaLocked && enableZoom && Input.mouseScrollDelta.y != 0f)
            {
                orthoSize = Mathf.Clamp(orthoSize - Input.mouseScrollDelta.y * zoomSpeed, minZoom, maxZoom);
                desiredOrtho = orthoSize;
            }
            float standardAspect = 16f / 9f;
            if (cam.aspect > 0.1f && cam.aspect < standardAspect)
            {
                // Preserve horizontal field-of-view when tiled in split-screen or narrow window
                desiredOrtho *= (standardAspect / cam.aspect);
                desiredOrtho = Mathf.Clamp(desiredOrtho, minZoom, maxZoom * 1.5f);
            }
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredOrtho, smoothSpeed * Time.deltaTime);
        }

        Vector3 desiredPos;
        if (_arenaLocked && cam != null)
        {
            float halfViewW = cam.orthographicSize * Mathf.Max(0.5f, cam.aspect);
            float arenaCenterX = (_arenaMinX + _arenaMaxX) * 0.5f;
            float targetX = Mathf.Lerp(target.position.x, arenaCenterX, 0.35f);
            float arenaSpan = (_arenaMaxX - _arenaMinX) * 0.5f;
            if (arenaSpan <= halfViewW + 0.5f)
            {
                targetX = arenaCenterX;
            }
            else
            {
                float clampMin = _arenaMinX + halfViewW - 1.5f;
                float clampMax = _arenaMaxX - halfViewW + 1.5f;
                if (clampMin <= clampMax) targetX = Mathf.Clamp(targetX, clampMin, clampMax);
                else targetX = arenaCenterX;
            }
            float targetY = Mathf.Lerp(target.position.y + offset.y * 0.6f, _arenaFocusY + 2.2f, 0.5f);
            desiredPos = new Vector3(targetX, targetY, offset.z);
        }
        else
        {
            lookAhead = Mathf.SmoothDamp(lookAhead, offset.x * (player ? player.FacingDir : 1), ref lookVelocity, 0.25f);
            desiredPos = target.position + new Vector3(lookAhead, offset.y, offset.z);
        }

        _basePosition = Vector3.SmoothDamp(_basePosition, desiredPos, ref followVelocity, 0.12f);
        Vector3 shake = JuiceManager.Instance != null ? JuiceManager.Instance.CurrentShakeOffset : Vector3.zero;
        transform.position = _basePosition + shake;
    }
}
