using UnityEngine;

/// <summary>
/// Subclass for a fast-moving 'Interceptor' enemy.
/// Focuses on chasing the player when in range.
/// </summary>
public class InterceptorEnemy : EnemyAI
{
    [Header("Detection Settings")]
    //[SerializeField] private float chaseSpeedMultiplier = 2f;

    [Header("Edge Safety")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    private bool hasAttacked = false;

    protected override void Chase()
    {
        if (hasAttacked)
        {
            // Flee Logic
            rb.linearVelocity = new Vector2((movingRight ? 1 : -1) * moveSpeed * 2f, rb.linearVelocity.y);
            return;
        }

        if (currentTarget == null) return;

        float dir = currentTarget.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed * 2.5f, rb.linearVelocity.y);
        if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
    }

    // FIX: Use TryGetComponent
    protected override void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && !hasAttacked && !isKnockedBack)
        {
            if (col.gameObject.TryGetComponent<PlayerController>(out var player))
            {
                hasAttacked = true;
                player.ProcessFumble(0.2f); // Attempt strip
                Flip(); // Turn around to run away
            }
        }
        base.OnCollisionEnter2D(col);
    }

}