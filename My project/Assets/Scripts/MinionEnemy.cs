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

        float dir = currentTarget.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed * 1.2f, rb.linearVelocity.y);
        if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();

        // Jump Logic
        if (jumpTimer > 0) jumpTimer -= Time.fixedDeltaTime;

        bool targetIsAbove = currentTarget.position.y > transform.position.y + 1.5f;
        // Simple wall check or just pure vertical difference
        if (targetIsAbove && jumpTimer <= 0)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpTimer = jumpCooldown;
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
