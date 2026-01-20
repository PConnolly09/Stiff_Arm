using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HeavyObject : MonoBehaviour
{
    [Header("Crush Settings")]
    public float minCrushVelocity = 5f;
    public float damage = 50f;
    public GameObject crushEffect; // Particle system

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gameObject.tag = "Grabbable"; // Important for the claw!
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check relative velocity to see if this is a "Crush" impact
        if (collision.relativeVelocity.magnitude > minCrushVelocity)
        {
            // Check if we hit an enemy
            if (collision.gameObject.CompareTag("Enemy"))
            {
                // Try to find the EnemyAI script we wrote earlier
                // Using reflection or SendMessage is okay, but GetComponent is better
                var enemy = collision.gameObject.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    // Instant kill or heavy damage
                    enemy.TakeHit(damage, Vector2.down, true); // Stun them too

                    if (crushEffect)
                        Instantiate(crushEffect, transform.position, Quaternion.identity);

                    Debug.Log("CRUSHED " + collision.gameObject.name);
                }
            }
        }
    }
}