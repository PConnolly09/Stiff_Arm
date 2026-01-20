using UnityEngine;
using System.Collections;

public class BruteEnemy : EnemyAI
{
    [Header("Tackle")]
    public float tackleRange = 5f;
    public float tackleForce = 18f;
    private bool canTackle = true;
    private bool isRetreating = false;

    protected override void ResetState()
    {
        canTackle = true;
        isRetreating = false;
    }

    protected override void Chase()
    {
        if (currentTarget == null) return;

        // Retreat Logic
        if (isRetreating)
        {
            float dir = (transform.position.x > currentTarget.position.x) ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * moveSpeed * 0.8f, rb.linearVelocity.y);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentTarget.position);

        if (canTackle && dist < tackleRange)
        {
            StartCoroutine(Tackle());
        }
        else if (canTackle)
        {
            float dir = currentTarget.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * moveSpeed * 0.8f, rb.linearVelocity.y);
            if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
        }
    }

    private IEnumerator Tackle()
    {
        canTackle = false;
        rb.linearVelocity = Vector2.zero; // Wind up
        yield return new WaitForSeconds(0.4f);

        if (currentTarget != null)
        {
            float dir = currentTarget.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * tackleForce, 1f);
        }

        yield return new WaitForSeconds(1.5f);
        canTackle = true;
    }

    protected override void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && !isKnockedBack)
        {
            if (col.gameObject.TryGetComponent<PlayerController>(out var player))
            {
                player.SetProne(1.0f);
                StartCoroutine(RetreatRoutine());
            }
        }
        base.OnCollisionEnter2D(col);
    }

    private IEnumerator RetreatRoutine()
    {
        isRetreating = true;
        yield return new WaitForSeconds(2.0f);
        isRetreating = false;
    }
}