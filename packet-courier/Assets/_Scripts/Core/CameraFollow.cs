using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(4, 2, -10);
    public float smoothSpeed = 5f;

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
    }
}
