using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(4, 2, -10);
    public float smoothSpeed = 5f;

    [Header("Zoom")]
    public bool enableZoom = true;
    public float orthoSize = 6f;
    public float minZoom = 3f;
    public float maxZoom = 12f;
    public float zoomSpeed = 2f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam) cam.orthographicSize = orthoSize;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player) target = player.transform;
            else return;
        }

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        if (enableZoom && cam)
        {
            if (Input.mouseScrollDelta.y != 0f)
            {
                orthoSize = Mathf.Clamp(orthoSize - Input.mouseScrollDelta.y * zoomSpeed, minZoom, maxZoom);
            }
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthoSize, smoothSpeed * Time.deltaTime);
        }
    }
}
