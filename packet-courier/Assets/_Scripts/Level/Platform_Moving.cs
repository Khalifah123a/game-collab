using UnityEngine;

public class Platform_Moving : MonoBehaviour
{
    [Header("Path")]
    public Vector2 pointA = new Vector2(-3, 0);
    public Vector2 pointB = new Vector2(3, 0);
    public float speed = 2f;

    public Vector2 CurrentVelocity { get; private set; }
    public Vector2 LastDelta { get; private set; }

    private Vector2 _startPos;
    private Vector2 _target;
    private Rigidbody2D _rb;
    private Rigidbody2D _passenger;
    private Collider2D _col;
    private float _detachPassengerUntil;

    private void Awake()
    {
        _startPos = transform.position;
        _target = _startPos + pointA;
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
    }

    public void NotifyPassengerJumped()
    {
        _passenger = null;
        _detachPassengerUntil = Time.time + 0.16f;
    }

    private void FixedUpdate()
    {
        Vector2 currentPos = _rb != null ? _rb.position : (Vector2)transform.position;

        if (Vector2.Distance(currentPos, _target) < 0.05f)
            _target = Vector2.Distance(_target, _startPos + pointA) < 0.1f
                ? _startPos + pointB
                : _startPos + pointA;

        Vector2 delta = Vector2.ClampMagnitude(_target - currentPos, speed * Time.fixedDeltaTime);
        LastDelta = delta;
        CurrentVelocity = Time.fixedDeltaTime > 0.0001f ? (delta / Time.fixedDeltaTime) : Vector2.zero;

        if (_rb != null)
            _rb.MovePosition(currentPos + delta);
        else
            transform.position = currentPos + delta;

        if (_passenger != null)
        {
            if (Time.time < _detachPassengerUntil)
            {
                _passenger = null;
            }
            else
            {
                float maxHoriz = (_col != null ? _col.bounds.extents.x : 2.0f) + 0.75f;
                float topY = _col != null ? _col.bounds.max.y : (currentPos.y + 0.25f);
                bool withinX = Mathf.Abs(_passenger.position.x - currentPos.x) <= maxHoriz;
                bool withinY = _passenger.position.y >= topY - 0.35f && _passenger.position.y <= topY + 2.2f;

                if (withinX && withinY)
                {
                    _passenger.position += delta;
                }
                else
                {
                    _passenger = null;
                }
            }
        }
    }

    private void TryAttachPassenger(Collision2D collision)
    {
        if (Time.time < _detachPassengerUntil) return;
        if (collision == null || collision.collider == null || collision.rigidbody == null) return;
        bool isPlayer = collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<PlayerController>() != null;
        if (!isPlayer) return;

        float platCenterY = _col != null ? _col.bounds.center.y : transform.position.y;
        if (collision.collider.bounds.min.y >= platCenterY - 0.15f)
        {
            _passenger = collision.rigidbody;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryAttachPassenger(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryAttachPassenger(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody == _passenger)
        {
            if (Time.time < _detachPassengerUntil || _passenger == null)
            {
                _passenger = null;
                return;
            }
            // Keep passenger attached across 1-frame downward step separation if still right above the platform
            float topY = _col != null ? _col.bounds.max.y : (transform.position.y + 0.25f);
            float maxHoriz = (_col != null ? _col.bounds.extents.x : 2.0f) + 0.6f;
            if (Mathf.Abs(_passenger.position.x - transform.position.x) > maxHoriz ||
                _passenger.position.y > topY + 1.8f ||
                _passenger.position.y < topY - 0.35f)
            {
                _passenger = null;
            }
        }
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
