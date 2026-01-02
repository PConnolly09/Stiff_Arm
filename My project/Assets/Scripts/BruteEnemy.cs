using UnityEngine;
using System.Collections;

/// <summary>
/// Subclass for the 'Brute' enemy type.
/// Slow, heavy, and handles edges with a different logic.
/// </summary>
public class BruteEnemy : EnemyAI
{
    [Header("Tackle Stats")]
    public float tackleRange = 5f;
    public float tackleForce = 15f;
    private bool canTackle = true;

    protected override void Chase(float speed)
    {
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (canTackle && dist < tackleRange) StartCoroutine(Tackle());
        else if (canTackle)
        {
            float dir = playerTransform.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * speed * 0.8f, rb.linearVelocity.y);
            if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
        }
    }

    private IEnumerator Tackle()
    {
        canTackle = false;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f);
        float dir = playerTransform.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * tackleForce, 2f);
        yield return new WaitForSeconds(1f);
        canTackle = true;
    }
}