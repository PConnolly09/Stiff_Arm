using UnityEngine;
using System.Collections;

public class BruteEnemy : EnemyAI
{
    [Header("Tackle Settings")]
    public float tackleRange = 5f;
    public float tackleForce = 18f;
    public float tackleCooldown = 3f;

    private bool canTackle = true;
    private bool isRetreating = false;

    protected override void Chase()
    {
        if (currentTarget == null) return;

        // If we just hit the player, back off
        if (isRetreating)
        {
            float retreatDir = (transform.position.x > currentTarget.position.x) ? 1 : -1;
            rb.linearVelocity = new Vector2(retreatDir * moveSpeed * 0.8f, rb.linearVelocity.y);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentTarget.position);

        if (canTackle && dist < tackleRange)
        {
            StartCoroutine(TackleSequence());
        }
        else if (canTackle)
        {
            float dir = currentTarget.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * moveSpeed * 0.8f, rb.linearVelocity.y);
            if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
        }
    }

    private IEnumerator TackleSequence()
    {
        canTackle = false;
        rb.linearVelocity = Vector2.zero; // Wind up
        yield return new WaitForSeconds(0.4f);

        if (currentTarget != null)
        {
            float dir = currentTarget.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * tackleForce, 1f);
        }

        yield return new WaitForSeconds(1.5f); // Recovery time
        canTackle = true;
    }

    private IEnumerator RetreatRoutine()
    {
        isRetreating = true;
        yield return new WaitForSeconds(2.0f); // Walk away for 2 seconds
        isRetreating = false;
    }

    protected override void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && !isKnockedBack)
        {
            if (col.gameObject.TryGetComponent<PlayerController>(out var player))
            {
                player.SetProne(1.0f);
                StartCoroutine(RetreatRoutine()); // Back off after hit
            }
        }
        base.OnCollisionEnter2D(col);
    }
}