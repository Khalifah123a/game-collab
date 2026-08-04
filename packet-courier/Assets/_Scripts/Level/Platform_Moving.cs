using UnityEngine;

public class Platform_Moving : MonoBehaviour
{
    [Header("Path")]
    public Vector2 pointA = new Vector2(-3, 0);
    public Vector2 pointB = new Vector2(3, 0);
    public float speed = 2f;

    private Vector2 _startPos;
    private Vector2 _target;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _startPos = transform.position;
        _target = _startPos + pointA;
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        if (Vector2.Distance(transform.position, _target) < 0.05f)
            _target = Vector2.Distance(_target, _startPos + pointA) < 0.1f
                ? _startPos + pointB
                : _startPos + pointA;

        Vector2 dir = ((Vector2)_target - (Vector2)transform.position).normalized;
        _rb.MovePosition((Vector2)transform.position + dir * speed * Time.fixedDeltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            _startPos = transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_startPos + pointA, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(_startPos + pointB, Vector3.one * 0.4f);
        Gizmos.DrawLine(_startPos + pointA, _startPos + pointB);
    }
}
