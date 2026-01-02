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

    protected override void Chase(float speed)
    {
        float dir = playerTransform.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * speed * 2.5f, rb.linearVelocity.y);
        if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
    }

}