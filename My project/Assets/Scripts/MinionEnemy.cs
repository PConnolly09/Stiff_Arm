using UnityEngine;

public class MinionEnemy : EnemyAI
{
    [Header("Minion Patrol Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float edgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    protected override void Chase()
    {
        if (currentTarget == null) return;

        float dir = currentTarget.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed * 1.2f, rb.linearVelocity.y);
        if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
    }

    // FIX: Use TryGetComponent to avoid null propagation on Unity Objects
    protected override void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && !isKnockedBack)
        {
            if (col.gameObject.TryGetComponent<PlayerController>(out var player))
            {
                player.AddAttachment(gameObject);
            }
        }
        base.OnCollisionEnter2D(col);
    }
}
