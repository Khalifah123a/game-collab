using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy_Patrol : MonoBehaviour, IEnemyDamageable
{
    public Vector2 pointA = new Vector2(-2, 0), pointB = new Vector2(2, 0);
    public float patrolSpeed = 2, detectionRadius = 10, chaseSpeed = 4.5f;
    public float chaseRadius = 16;
    public int hitPoints = 2;
    public float damageAmount = 15, damageCooldown = 0.8f, damageTriggerRadius = 1.5f;
    public bool stationary;
    public enum State { Patrol, Chase, Windup, Attack, Recovery, Stunned, Dead }
    public State currentState;
    public Transform HitTransform => transform;
    public bool IsDead => currentState == State.Dead;
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private Vector2 home;
    private PlayerHealth player;
    private float direction = 1, lockedDirection, timer;
    private bool hit;
    private Color baseColor;
    private TextMesh warning;
    private bool returning;
    private float lostSight;
    private float _flashTimer;
    private SpriteRenderer _slashTelegraph;
    private static Sprite _telegraphSprite;

    private static Sprite GetTelegraphSprite()
    {
        if (_telegraphSprite == null)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _telegraphSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
        return _telegraphSprite;
    }

    private float ResolveHalfHeight()
    {
        if (sprite != null && sprite.sprite != null)
        {
            return sprite.sprite.rect.height / (2f * sprite.sprite.pixelsPerUnit);
        }
        return 0.6f;
    }

    private float? FindGroundSurfaceY()
    {
        Vector2 footOrigin = body.position + Vector2.up * 0.4f;
        foreach (var hit in Physics2D.RaycastAll(footOrigin, Vector2.down, 1.4f))
        {
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject
                && !hit.collider.transform.IsChildOf(transform) && hit.normal.y > 0.5f)
            {
                return hit.point.y;
            }
        }
        return null;
    }

    private void Awake()
    {
        home = transform.position;
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        foreach (var collider in GetComponents<Collider2D>()) collider.isTrigger = true;
        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = gameObject.AddComponent<SpriteRenderer>();
        if (sprite != null)
        {
            if (sprite.sprite == null || sprite.sprite.name.Contains("Box"))
                sprite.sprite = Resources.Load<Sprite>("GeneratedArt/enemy");
            if (sprite.sortingOrder < 8) sprite.sortingOrder = 10;
            baseColor = sprite.color.a < 0.2f ? Color.white : sprite.color;
            sprite.color = baseColor;
        }
        var marker = new GameObject("Attack warning");
        marker.transform.SetParent(transform, false); marker.transform.localPosition = new Vector3(0, 1, 0);
        warning = marker.AddComponent<TextMesh>(); warning.text = "!"; warning.fontSize = 40; warning.characterSize = 0.07f;
        warning.anchor = TextAnchor.MiddleCenter; warning.color = Color.yellow;
        marker.GetComponent<MeshRenderer>().sortingOrder = 20; marker.SetActive(false);

        var telegraphGo = new GameObject("SlashTelegraph");
        telegraphGo.transform.SetParent(transform, false);
        telegraphGo.transform.localPosition = new Vector3(0.85f, 0.05f, 0f);
        _slashTelegraph = telegraphGo.AddComponent<SpriteRenderer>();
        _slashTelegraph.sprite = GetTelegraphSprite();
        _slashTelegraph.sortingOrder = 12;
        _slashTelegraph.color = new Color(1f, 0.85f, 0.15f, 0.55f);
        telegraphGo.SetActive(false);
    }

    public void SetHome(Vector2 newHome)
    {
        home = newHome;
    }

    public void SnapToGroundSurface()
    {
        float halfHeight = 0.6f;
        if (sprite != null && sprite.sprite != null)
        {
            halfHeight = sprite.sprite.rect.height / (2f * sprite.sprite.pixelsPerUnit);
        }
        else
        {
            var col = GetComponent<Collider2D>();
            if (col != null) halfHeight = col.bounds.extents.y;
        }

        Vector2 origin = (Vector2)transform.position + Vector2.up * 1.5f;
        RaycastHit2D bestHit = default;
        float bestDist = float.MaxValue;
        foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, 6.0f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.normal.y < 0.5f) continue;
            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                bestHit = hit;
            }
        }

        if (bestHit.collider != null)
        {
            float targetY = bestHit.point.y + halfHeight;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            home = new Vector2(home.x, targetY);
            if (body != null) body.position = new Vector2(body.position.x, targetY);
        }
    }

    private void Start()
    {
        if (home == Vector2.zero && (Vector2)transform.position != Vector2.zero)
        {
            home = transform.position;
        }
        foreach (var c in GetComponents<Collider2D>()) c.isTrigger = true;
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = gameObject.AddComponent<SpriteRenderer>();
        if (sprite != null)
        {
            if (sprite.sprite == null || sprite.sprite.name.Contains("Box"))
                sprite.sprite = Resources.Load<Sprite>("GeneratedArt/enemy");
            if (sprite.sortingOrder < 8) sprite.sortingOrder = 10;
            if (baseColor.a < 0.2f) baseColor = Color.white;
            sprite.color = baseColor;
        }
        if (transform.localScale.sqrMagnitude < 0.1f) transform.localScale = Vector3.one;

        SnapToGroundSurface();
    }

    private void FixedUpdate()
    {
        if (currentState == State.Dead) return;
        _flashTimer -= Time.fixedDeltaTime;
        float halfHeight = ResolveHalfHeight();
        float? groundY = FindGroundSurfaceY();

        if (currentState == State.Stunned)
        {
            timer -= Time.fixedDeltaTime;
            float vx = Mathf.MoveTowards(body.linearVelocity.x, 0f, 18f * Time.fixedDeltaTime);
            float vy = body.linearVelocity.y - 22f * Time.fixedDeltaTime;
            if (groundY.HasValue)
            {
                float targetY = groundY.Value + halfHeight;
                if (body.position.y <= targetY + 0.03f && vy <= 0f)
                {
                    body.position = new Vector2(body.position.x, targetY);
                    vy = 0f;
                }
            }
            body.linearVelocity = new Vector2(vx, vy);
            if (_slashTelegraph != null) _slashTelegraph.gameObject.SetActive(false);
            if (timer <= 0) { currentState = State.Recovery; timer = 0.15f; }
            return;
        }
        if (!player) player = PlayerHealth.Instance;
        if (!player || player.isDead)
        {
            body.linearVelocity = Vector2.zero;
            if (_slashTelegraph != null) _slashTelegraph.gameObject.SetActive(false);
            return;
        }

        // Continuous ground elevation hugging so feet stay pixel-perfect on terrain
        if (groundY.HasValue)
        {
            float targetY = groundY.Value + halfHeight;
            if (Mathf.Abs(body.position.y - targetY) > 0.015f)
            {
                body.position = new Vector2(body.position.x, Mathf.Lerp(body.position.y, targetY, 0.45f));
            }
        }

        Vector2 delta = player.transform.position - transform.position;
        timer -= Time.fixedDeltaTime;
        bool safe = SafeZone.IsPlayerSafe;
        bool visible = !safe && delta.magnitude < detectionRadius && Mathf.Abs(delta.y) < 4;
        foreach (var wall in Physics2D.RaycastAll(transform.position, delta.normalized, delta.magnitude))
            if (!wall.collider.isTrigger && !wall.collider.GetComponent<PlayerController>() && wall.collider.gameObject != gameObject) visible = false;
        // Safe Zone drops the telegraph mid-swing as well as blocking a fresh one.
        if (safe && (currentState == State.Windup || currentState == State.Attack)) { currentState = State.Patrol; hit = false; }
        if (currentState == State.Windup && timer <= 0) { currentState = State.Attack; timer = 0.15f; hit = false; }
        else if (currentState == State.Attack)
        {
            if (!hit && delta.x * lockedDirection > 0 && Mathf.Abs(delta.x) < 1.7f && Mathf.Abs(delta.y) < 1.2f)
            {
                hit = true;
                var combat = player.GetComponent<PlayerCombat>();
                if (combat != null && combat.IsParrying)
                {
                    combat.OnParrySuccess(damageAmount, PlayerHealth.DamageType.TTLDamage);
                    currentState = State.Stunned;
                    timer = 0.65f;
                    body.linearVelocity = new Vector2(-lockedDirection * 3.5f, 1.2f);
                    warning.gameObject.SetActive(false);
                    if (_slashTelegraph != null) _slashTelegraph.gameObject.SetActive(false);
                    return;
                }
                else if (!player.IsInvulnerable)
                {
                    player.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
                    player.GetComponent<PlayerController>().Knockback(new Vector2(lockedDirection, 0));
                }
            }
            if (timer <= 0) { currentState = State.Recovery; timer = damageCooldown; }
        }
        else if (currentState == State.Recovery && timer <= 0) currentState = State.Patrol;
        if (currentState == State.Patrol || currentState == State.Chase)
        {
            lostSight = visible ? 0 : lostSight + Time.fixedDeltaTime;
            if (safe) returning = true;
            if (Mathf.Abs(body.position.x - home.x) >= chaseRadius) returning = true;
            if (returning && Mathf.Abs(body.position.x - home.x) < 0.5f) returning = false;
            bool pursuing = !returning && (visible || currentState == State.Chase && lostSight < 0.6f);
            currentState = pursuing ? State.Chase : State.Patrol;
            if (visible && !returning && Mathf.Abs(delta.x) <= 1.6f && Mathf.Abs(delta.y) < 1.2f)
            { currentState = State.Windup; lockedDirection = Mathf.Sign(delta.x); timer = 0.4f; }
        }
        float velocity = 0;
        if (currentState == State.Patrol || currentState == State.Chase)
        {
            float x = body.position.x;
            if (x <= home.x + pointA.x) direction = 1;
            if (x >= home.x + pointB.x) direction = -1;
            if (currentState == State.Chase) direction = Mathf.Sign(delta.x);
            else if (returning || x < home.x + pointA.x || x > home.x + pointB.x) direction = Mathf.Sign(home.x - x);
            bool inside = Mathf.Abs(x + direction * 0.3f - home.x) <= chaseRadius + 0.5f;
            
            bool groundAhead = false;
            foreach (var groundHit in Physics2D.RaycastAll(body.position + new Vector2(direction * 0.65f, 0.2f), Vector2.down, 1.4f))
                if (!groundHit.collider.isTrigger && !groundHit.collider.GetComponent<PlayerController>()) groundAhead = true;
            
            bool blocked = false;
            foreach (var wall in Physics2D.RaycastAll(body.position + Vector2.up * 0.15f, Vector2.right * direction, 0.75f))
            {
                if (!wall.collider.isTrigger && !wall.collider.GetComponent<PlayerController>() && wall.collider.gameObject != gameObject)
                {
                    if (Mathf.Abs(wall.normal.x) > 0.4f || wall.point.y > body.position.y - halfHeight + 0.15f)
                    {
                        blocked = true;
                    }
                }
            }

            if (currentState == State.Patrol)
            {
                // If facing a wall or edge in patrol, turn around immediately so it never freezes stuck against terrain
                if (blocked || !groundAhead)
                {
                    direction = -direction;
                    if (direction > 0 && home.x + pointA.x < x) pointA = new Vector2(x - home.x, pointA.y);
                    else if (direction < 0 && home.x + pointB.x > x) pointB = new Vector2(x - home.x, pointB.y);
                }
                else if (inside && !stationary)
                {
                    velocity = direction * patrolSpeed;
                }
            }
            else // Chase
            {
                if (inside && groundAhead && !blocked && !stationary)
                {
                    velocity = direction * chaseSpeed;
                }
                else if (blocked || !groundAhead)
                {
                    returning = true;
                }
            }
        }
        body.linearVelocity = new Vector2(velocity, 0);
        bool showTelegraph = currentState == State.Windup || currentState == State.Attack;
        warning.gameObject.SetActive(showTelegraph);
        if (_slashTelegraph != null)
        {
            _slashTelegraph.gameObject.SetActive(showTelegraph);
            if (showTelegraph)
            {
                float dir = lockedDirection == 0f ? direction : lockedDirection;
                _slashTelegraph.transform.localPosition = new Vector3(dir * 0.9f, 0.05f, 0f);
                _slashTelegraph.transform.localScale = new Vector3(1.35f, currentState == State.Attack ? 0.36f : 0.14f, 1f);
                _slashTelegraph.color = currentState == State.Attack
                    ? new Color(1f, 0.22f, 0.18f, 0.85f)
                    : new Color(1f, 0.88f, 0.18f, 0.45f + 0.25f * Mathf.PingPong(Time.time * 8f, 1f));
            }
        }
        if (sprite)
        {
            sprite.flipX = direction < 0;
            if (_flashTimer > 0f)
            {
                sprite.color = Color.white;
            }
            else
            {
                sprite.color = currentState == State.Windup ? Color.yellow : currentState == State.Attack ? Color.red : baseColor;
            }
        }
    }

    public void TakeHit(int damage)
    {
        if (damage <= 0 || currentState == State.Dead) return;
        hitPoints = Mathf.Max(0, hitPoints - damage);
        _flashTimer = 0.09f;
        if (sprite) sprite.color = Color.white;

        float knockbackDir = 1f;
        if (player != null) knockbackDir = Mathf.Sign(transform.position.x - player.transform.position.x);
        else if (PlayerHealth.Instance != null) knockbackDir = Mathf.Sign(transform.position.x - PlayerHealth.Instance.transform.position.x);
        if (knockbackDir == 0f) knockbackDir = -direction;

        body.linearVelocity = new Vector2(knockbackDir * 4.2f, 1.8f);
        warning.gameObject.SetActive(false);
        if (_slashTelegraph != null) _slashTelegraph.gameObject.SetActive(false);

        if (hitPoints == 0)
        {
            currentState = State.Dead;
            if (GameManager.Instance != null)
                OptionalQuestService.ReportKill(GameManager.Instance.currentWorldIndex, "enemy:patrol", gameObject.GetHashCode().ToString());
            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
            JuiceManager.Hitstop(0.045f);
            JuiceManager.SpawnDataDissolve(transform.position, 12, new Color(1f, 0.35f, 0.2f, 1f));
            AudioManager.PlayEnemyDissolve();
            if (sprite) { sprite.color = new Color(1, 1, 1, 0.4f); transform.localScale *= 0.6f; }
            Destroy(gameObject, 0.15f);
        }
        else { currentState = State.Stunned; timer = 0.2f; }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SafeZone>() != null || other.GetComponent<WorldDoor>() != null)
        {
            direction = -direction;
            currentState = State.Patrol;
        }
    }
}
