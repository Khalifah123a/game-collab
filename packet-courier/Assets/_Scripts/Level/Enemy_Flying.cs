using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Enemy_Flying : MonoBehaviour, IEnemyDamageable
{
    public float patrolRadius = 2f;
    public float patrolSpeed = 1.4f;
    public float detectionRadius = 12f;
    public float chaseRadius = 14f;
    public float chaseSpeed = 5.2f;
    public float attackRange = 3f;
    public float windupTime = 0.45f;
    public float diveSpeed = 10f;
    public float diveDuration = 0.26f;
    public float recoveryTime = 0.9f;
    public int hitPoints = 2;
    public float damageAmount = 12f;

    public enum State { Patrol, Chase, Windup, Attack, Recovery, Stunned, Return, Dead }
    public State currentState;
    public Transform HitTransform => transform;
    public bool IsDead => currentState == State.Dead;

    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private TextMesh warning;
    private PlayerHealth player;
    private Vector2 home;
    private Vector2 patrolTarget;
    private Vector2 diveVelocity;
    private float timer;
    private bool damagedThisDive;
    private Color baseColor;
    private float _flashTimer;

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
                sprite.sprite = Resources.Load<Sprite>("GeneratedArt/enemy_flying");
            if (sprite.sortingOrder < 8) sprite.sortingOrder = 10;
            baseColor = sprite.color.a < 0.2f ? Color.white : sprite.color;
            sprite.color = baseColor;
        }
        patrolTarget = home + Vector2.right * patrolRadius;

        var marker = new GameObject("Dive warning");
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = Vector3.up * 0.65f;
        warning = marker.AddComponent<TextMesh>();
        warning.text = "!";
        warning.fontSize = 40;
        warning.characterSize = 0.07f;
        warning.anchor = TextAnchor.MiddleCenter;
        warning.color = Color.yellow;
        marker.GetComponent<MeshRenderer>().sortingOrder = 20;
        marker.SetActive(false);
    }

    public void SetHome(Vector2 newHome)
    {
        home = newHome;
        patrolTarget = home + Vector2.right * patrolRadius;
    }

    private void Start()
    {
        if (home == Vector2.zero && (Vector2)transform.position != Vector2.zero)
        {
            SetHome(transform.position);
        }
        foreach (var c in GetComponents<Collider2D>()) c.isTrigger = true;
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = gameObject.AddComponent<SpriteRenderer>();
        if (sprite != null)
        {
            if (sprite.sprite == null || sprite.sprite.name.Contains("Box"))
                sprite.sprite = Resources.Load<Sprite>("GeneratedArt/enemy_flying");
            if (sprite.sortingOrder < 8) sprite.sortingOrder = 10;
            if (baseColor.a < 0.2f) baseColor = Color.white;
            sprite.color = baseColor;
        }
        if (transform.localScale.sqrMagnitude < 0.1f) transform.localScale = Vector3.one;
    }

    private void FixedUpdate()
    {
        if (IsDead || Time.timeScale == 0) return;
        if (!player) player = PlayerHealth.Instance;
        if (!player || player.isDead) { body.linearVelocity = Vector2.zero; return; }

        float step = Time.fixedDeltaTime;
        Vector2 playerPosition = player.transform.position;
        Vector2 delta = playerPosition - body.position;
        bool safe = SafeZone.IsPlayerSafe;
        bool visible = !safe && delta.sqrMagnitude <= detectionRadius * detectionRadius && HasLineOfSight(playerPosition);
        bool beyondLeash = ((Vector2)body.position - home).sqrMagnitude > chaseRadius * chaseRadius;
        timer -= step;
        _flashTimer -= step;

        // Safe Zone cancels a committed dive so the courier cannot be hit mid-air.
        if (safe && (currentState == State.Windup || currentState == State.Attack))
        {
            damagedThisDive = true;
            currentState = State.Patrol;
            body.linearVelocity = Vector2.zero;
        }

        switch (currentState)
        {
            case State.Stunned:
                body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, 16f * step);
                if (timer <= 0) BeginRecovery();
                break;
            case State.Windup:
                body.linearVelocity = Vector2.zero;
                if (timer <= 0) BeginDive(playerPosition);
                break;
            case State.Attack:
                body.linearVelocity = diveVelocity;
                TryDamageDuringDive();
                if (timer <= 0 || HitsTerrain(diveVelocity.normalized, Mathf.Max(0.2f, diveVelocity.magnitude * step))) BeginRecovery();
                break;
            case State.Recovery:
                body.linearVelocity = Vector2.zero;
                if (timer <= 0) currentState = beyondLeash ? State.Return : State.Patrol;
                break;
            case State.Return:
                MoveTowards(home, chaseSpeed, step);
                if (((Vector2)body.position - home).sqrMagnitude < 0.15f) currentState = State.Patrol;
                break;
            default:
                if (beyondLeash) { currentState = State.Return; break; }
                if (visible)
                {
                    currentState = State.Chase;
                    MoveTowards(playerPosition, chaseSpeed, step);
                    if (delta.sqrMagnitude <= attackRange * attackRange) BeginWindup();
                }
                else
                {
                    currentState = State.Patrol;
                    Patrol(step);
                }
                break;
        }

        if (warning) warning.gameObject.SetActive(currentState == State.Windup);
        if (sprite)
        {
            if (body.linearVelocity.x != 0) sprite.flipX = body.linearVelocity.x < 0;
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

    private void Patrol(float step)
    {
        if (((Vector2)body.position - patrolTarget).sqrMagnitude < 0.08f)
            patrolTarget = patrolTarget.x > home.x ? home - Vector2.right * patrolRadius : home + Vector2.right * patrolRadius;
        MoveTowards(patrolTarget, patrolSpeed, step);
    }

    private void MoveTowards(Vector2 target, float speed, float step)
    {
        Vector2 direction = target - body.position;
        if (direction.sqrMagnitude < 0.001f) { body.linearVelocity = Vector2.zero; return; }
        Vector2 velocity = direction.normalized * speed;
        if (HitsTerrain(velocity.normalized, Mathf.Max(0.18f, speed * step)))
        {
            body.linearVelocity = Vector2.zero;
            if (currentState == State.Chase) currentState = State.Return;
            else if (currentState == State.Patrol)
            {
                patrolTarget = patrolTarget.x > home.x ? home - Vector2.right * patrolRadius : home + Vector2.right * patrolRadius;
            }
            return;
        }
        body.linearVelocity = velocity;
    }

    private bool HasLineOfSight(Vector2 target)
    {
        Vector2 start = body.position;
        Vector2 delta = target - start;
        if (delta.sqrMagnitude < 0.01f) return true;
        foreach (var hit in Physics2D.RaycastAll(start, delta.normalized, delta.magnitude))
            if (!hit.collider.isTrigger && hit.collider.attachedRigidbody != body && !hit.collider.GetComponent<PlayerController>()) return false;
        return true;
    }

    private bool HitsTerrain(Vector2 direction, float distance)
    {
        if (direction.sqrMagnitude < 0.001f) return false;
        foreach (var hit in Physics2D.RaycastAll(body.position, direction, distance))
            if (!hit.collider.isTrigger && hit.collider.attachedRigidbody != body && !hit.collider.GetComponent<PlayerController>()) return true;
        return false;
    }

    private void BeginWindup()
    {
        currentState = State.Windup;
        timer = windupTime;
        body.linearVelocity = Vector2.zero;
    }

    private void BeginDive(Vector2 target)
    {
        Vector2 direction = target - body.position;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.down;
        diveVelocity = direction.normalized * diveSpeed;
        currentState = State.Attack;
        timer = diveDuration;
        damagedThisDive = false;
    }

    private void TryDamageDuringDive()
    {
        if (damagedThisDive || !player || player.IsInvulnerable || SafeZone.IsPlayerSafe) return;
        if (((Vector2)player.transform.position - body.position).sqrMagnitude > 1.15f * 1.15f) return;
        damagedThisDive = true;
        player.TakeDamage(damageAmount, PlayerHealth.DamageType.TTLDamage);
        var controller = player.GetComponent<PlayerController>();
        if (controller) controller.Knockback(diveVelocity.normalized);
    }

    private void BeginRecovery()
    {
        currentState = State.Recovery;
        timer = recoveryTime;
        body.linearVelocity = Vector2.zero;
    }

    public void TakeHit(int damage)
    {
        if (damage <= 0 || IsDead) return;
        hitPoints = Mathf.Max(0, hitPoints - damage);
        _flashTimer = 0.09f;
        if (sprite) sprite.color = Color.white;

        float knockbackDir = 1f;
        if (player != null) knockbackDir = Mathf.Sign(transform.position.x - player.transform.position.x);
        else if (PlayerHealth.Instance != null) knockbackDir = Mathf.Sign(transform.position.x - PlayerHealth.Instance.transform.position.x);
        if (knockbackDir == 0f) knockbackDir = 1f;

        body.linearVelocity = new Vector2(knockbackDir * 4.2f, 1.8f);

        if (hitPoints == 0)
        {
            currentState = State.Dead;
            if (GameManager.Instance != null)
                OptionalQuestService.ReportKill(GameManager.Instance.currentWorldIndex, "enemy:flying", gameObject.GetHashCode().ToString());
            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
            JuiceManager.Hitstop(0.045f);
            JuiceManager.SpawnDataDissolve(transform.position, 14, new Color(0.2f, 0.9f, 1f, 1f));
            AudioManager.PlayEnemyDissolve();
            if (sprite) { sprite.color = new Color(1, 1, 1, 0.4f); transform.localScale *= 0.65f; }
            Destroy(gameObject, 0.15f);
            return;
        }
        currentState = State.Stunned;
        timer = 0.2f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SafeZone>() != null || other.GetComponent<WorldDoor>() != null)
        {
            currentState = State.Return;
            body.linearVelocity = -body.linearVelocity;
        }
    }
}
