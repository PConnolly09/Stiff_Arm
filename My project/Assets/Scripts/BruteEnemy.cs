using UnityEngine;

/// <summary>
/// Subclass for the 'Brute' enemy type.
/// Slow, heavy, and handles edges with a different logic.
/// </summary>
public class BruteEnemy : EnemyAI
{
    [Header("Brute Specifics")]
    [SerializeField] private float massWeight = 5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    protected override void Awake()
    {
        base.Awake(); // Crucial to call base.Awake() to setup physics from EnemyAI

        // Brutes are heavy and shouldn't be easily pushed
        rb.mass = massWeight;
    }

    protected override void Patrol() { /* Slow walk */ }
    protected override void Chase()
    {
        float direction = playerTransform.position.x > transform.position.x ? 1 : -1;
        // Brutes are slower but steady
        rb.linearVelocity = new Vector2(direction * (moveSpeed * 0.8f), rb.linearVelocity.y);

        if (direction > 0 && !movingRight) Flip();
        else if (direction < 0 && movingRight) Flip();
    }
}