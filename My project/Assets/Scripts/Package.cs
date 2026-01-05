using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))] // Changed to Circle for better bouncing
public class Package : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D coll;
    private bool isHeld = true;

    [Header("Fumble Physics")]
    public float enemyKnockForce = 15f;
    public float timePenaltyPerHit = 2.0f;
    public float torqueAmount = 10f; // Spin amount

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
    }

    public void SetHeld(bool held, Transform parent = null)
    {
        isHeld = held;

        if (isHeld)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
            coll.enabled = false;

            if (parent != null)
            {
                transform.SetParent(parent);
                transform.localPosition = Vector3.zero;
            }

            // Tell manager we got it back
            if (GameManager.Instance) GameManager.Instance.RecoverFumble();
        }
        else
        {
            transform.SetParent(null);
            rb.simulated = true;
            coll.enabled = true;

            // Initial Pop
            rb.AddForce(new Vector2(Random.Range(-5f, 5f), 8f), ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-torqueAmount, torqueAmount), ForceMode2D.Impulse);

            // Tell manager we lost it
            if (GameManager.Instance) GameManager.Instance.StartFumbleEvent(transform);
        }
    }

    void FixedUpdate()
    {
        if (isHeld)
        {
            if (transform.localPosition != Vector3.zero) transform.localPosition = Vector3.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If an enemy touches the ball, they kick it away!
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Calculate a chaotic direction: mostly up, random side
            Vector2 kickDir = new Vector2(Random.Range(-1f, 1f), 0.5f).normalized;

            // Apply Force
            rb.linearVelocity = Vector2.zero; // Reset current momentum for snappy direction change
            rb.AddForce(kickDir * enemyKnockForce, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-torqueAmount, torqueAmount) * 2f, ForceMode2D.Impulse);

            // Penalty
            if (GameManager.Instance) GameManager.Instance.PenalizeFumbleTime(timePenaltyPerHit);

            Debug.Log("Enemy kicked the ball! Time Penalty!");
        }
    }
}