using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Package : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D coll;
    private bool isHeld = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Call this when the player picks up the package or starts with it.
    /// It strips the package of all physics influence.
    /// </summary>
    public void SetHeld(bool held, Transform parent = null)
    {
        isHeld = held;

        if (isHeld)
        {
            // 1. Stop all movement
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // 2. Make it a "ghost" so it can't push the player
            rb.simulated = false;
            coll.enabled = false;

            // 3. Attach to player
            if (parent != null)
            {
                transform.SetParent(parent);
                transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // 4. Become a physical object again (Fumble)
            transform.SetParent(null);
            rb.simulated = true;
            coll.enabled = true;

            // Give it a little pop so it doesn't just drop straight down
            rb.AddForce(new Vector2(Random.Range(-2f, 2f), 5f), ForceMode2D.Impulse);
        }
    }

    void FixedUpdate()
    {
        // CRITICAL FIX: If the old script was setting position/velocity here,
        // it was causing the "pull." We ensure it does NOTHING while held.
        if (isHeld)
        {
            // Ensure local position stays snapped to the player's carry point
            // but DO NOT use Rigidbody forces or global positions.
            if (transform.localPosition != Vector3.zero)
                transform.localPosition = Vector3.zero;

            return;
        }
    }
}