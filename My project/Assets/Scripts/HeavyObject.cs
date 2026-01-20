using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HeavyObject : MonoBehaviour
{
    [Header("Crush Settings")]
    public float minCrushVelocity = 5f; // Velocity needed to kill
    public GameObject crushEffect;

    private Rigidbody2D rb;
    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gameObject.tag = "Grabbable"; // Required for Crane

        // PHYSICS SETUP: Heavy and Stable
        rb.mass = 2204f; // High mass prevents player from pushing it easily
        rb.gravityScale = 50f; // Heavy fall
        rb.linearDamping = 30f; // High drag stops sliding quickly on ground
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

    }

    // Call this from CraneController when picking up
    public void OnGrab()
    {
        // Kinematic allows movement via Transform (Crane) but keeps collisions active
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    // Call this from CraneController when dropping
    public void OnRelease()
    {
        // Switch back to Dynamic so gravity takes over
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.WakeUp();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // CRUSH LOGIC: Only kill if we are falling (Dynamic) or moving fast
        if (rb.bodyType == RigidbodyType2D.Dynamic && collision.relativeVelocity.y > minCrushVelocity)
        {
            // Crush Logic
            if (collision.gameObject.CompareTag("Enemy"))
            {
                // Try to find ANY Enemy script (Base class covers all types)
                if (collision.gameObject.TryGetComponent<EnemyAI>(out var enemy))
                {
                    Debug.Log("CRUSHED " + collision.gameObject.name);

                    // Instantiate Effect
                    if (crushEffect)
                        Instantiate(crushEffect, transform.position, Quaternion.identity);

                    // Kill Enemy Instantly
                    Destroy(collision.gameObject);
                }
            }
        }
    }
}