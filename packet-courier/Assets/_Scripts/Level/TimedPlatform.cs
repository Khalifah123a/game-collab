using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class TimedPlatform : MonoBehaviour
{
    public bool crumble;
    public float phase;
    private float timer, breakAt = -1;
    private BoxCollider2D surface;
    private SpriteRenderer sprite;
    private Color color;
    private void Awake() { surface = GetComponent<BoxCollider2D>(); sprite = GetComponent<SpriteRenderer>(); color = sprite.color; timer = phase; }
    private void Update()
    {
        timer += Time.deltaTime;
        bool solid;
        if (crumble)
        {
            solid = breakAt < 0 || Time.time < breakAt;
            if (breakAt > 0 && Time.time > breakAt + 2) breakAt = -1;
        }
        else solid = Mathf.Repeat(timer, 3.5f) >= 0.5f && Mathf.Repeat(timer, 3.5f) < 3;
        surface.enabled = solid;
        sprite.color = new Color(color.r, color.g, color.b, solid ? 1 : 0.22f);
        if (crumble && breakAt > Time.time) sprite.color = Color.Lerp(color, Color.red, Mathf.PingPong(Time.time * 8, 1));
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (crumble && breakAt < 0 && other.collider.CompareTag("Player")) breakAt = Time.time + 0.8f;
    }
}
