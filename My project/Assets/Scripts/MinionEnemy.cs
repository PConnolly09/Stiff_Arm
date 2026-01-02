using UnityEngine;

public class MinionEnemy : EnemyAI
{
    [Header("Minion Patrol Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float edgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    protected override void Chase(float speed)
    {
        float dir = playerTransform.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * speed * 1.2f, rb.linearVelocity.y);
        if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
    }
}
