using UnityEngine;

public class MinionEnemy : EnemyAI
{

    [Header("Minion Jump")]
    public float jumpForce = 12f;
    public float jumpCooldown = 2f;
    private float jumpTimer;

    protected override void Chase()
    {
        if (currentTarget == null) return;

        float xDiff = currentTarget.position.x - transform.position.x;

        // FIX: Don't jitter if we are directly above/below target
        if (Mathf.Abs(xDiff) > 0.2f)
        {
            float dir = Mathf.Sign(xDiff);
            rb.linearVelocity = new Vector2(dir * moveSpeed * 1.2f, rb.linearVelocity.y);
            if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
        }
        else
        {
            // Calm down horizontal movement
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

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
