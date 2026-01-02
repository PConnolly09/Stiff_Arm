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

    //private bool isRetreating = false;
  //  private float retreatTimer = 0f;

    protected override void Chase()
    {
        float direction = playerTransform.position.x > transform.position.x ? 1 : -1;
        // Interceptors move much faster
        rb.linearVelocity = new Vector2(direction * (moveSpeed * 2.5f), rb.linearVelocity.y);

        if (direction > 0 && !movingRight) Flip();
        else if (direction < 0 && movingRight) Flip();
    }

   // public void OnStripSuccess()
   // {
       // isRetreating = true;
       // retreatTimer = 2f;
   // }

    protected override void Patrol()
    {
        rb.linearVelocity = new Vector2(movingRight ? moveSpeed * 1.5f : -moveSpeed * 1.5f, rb.linearVelocity.y);
        // Simple flip logic...
    }

}