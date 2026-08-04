using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private bool _warned;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateState(bool isMoving, float speedMultiplier, float facing)
    {
        if (animator == null)
        {
            if (!_warned)
            {
                Debug.LogWarning("[PlayerAnimator] Animator belum di-set. Skip animasi.", this);
                _warned = true;
            }
            return;
        }

        animator.speed = speedMultiplier;
        animator.SetBool("isMoving", isMoving);
        animator.SetFloat("speedMultiplier", speedMultiplier);
        animator.SetFloat("facing", facing);

        if (spriteRenderer != null)
            spriteRenderer.flipX = facing < 0;
    }
}
