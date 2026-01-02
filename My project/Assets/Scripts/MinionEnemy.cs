using UnityEngine;

public class MinionEnemy : EnemyAI
{
    [Header("Minion Patrol Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float edgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    protected override void Patrol()
    {
        base.Patrol();

        // Edge Detection: Check if there's floor ahead
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(movingRight ? checkOffset.x : -checkOffset.x, checkOffset.y);
        RaycastHit2D floorHit = Physics2D.Raycast(rayOrigin, Vector2.down, edgeCheckDistance, obstacleLayer);

        if (floorHit.collider == null)
        {
            Flip();
        }
    }
}