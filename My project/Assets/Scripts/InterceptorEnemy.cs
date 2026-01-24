using UnityEngine;
using System.Collections;
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

    protected override void ResetState()
    {
        hasAttacked = false;
    }

    protected override void Chase()
    {
        if (hasAttacked)
        {
            // Run away from player after missing or hitting
            if (playerTransform != null)
            {
                float dir = (transform.position.x > playerTransform.position.x) ? 1 : -1;
                Move(dir, moveSpeed * 2.5f);
            }
            return;
        }

        if (currentTarget == null) return;

        // Chase Package or Player
        float chaseDir = Mathf.Sign(currentTarget.position.x - transform.position.x);
        Move(chaseDir, moveSpeed * 2.5f);
    }

    protected override void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && !hasAttacked && !isKnockedBack)
        {
            if (col.gameObject.TryGetComponent<PlayerController>(out var player))
            {
                hasAttacked = true;
                player.ProcessFumble(0.25f); // High strip chance
                Flip();
                StartCoroutine(ResetAttackCooldown());
            }
        }
        base.OnCollisionEnter2D(col);
    }

    private IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(3.0f);
        hasAttacked = false;
    }

}