using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Package : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D coll;
    private Transform targetAnchor;

    [Header("Status")]
    public bool isHeld = false;
    [Tooltip("Check this if the player should start the level already holding this ball.")]
    public bool startHeld = false;

    [Header("Physics Settings")]
    public float dropGravity = 4f;
    public float airDrag = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();

        gameObject.tag = "Package";

        // Ensure Rigidbody is set to be influenced by physics initially
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        if (startHeld)
        {
            // If starting held, we need a player reference. 
            // Usually handled by the Player script, but we set state here.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerController>(out var pc))
            {
                pc.hasPackage = true;
                pc.packageObject = gameObject;
                SetHeld(true, pc.attachmentPoint);
            }
        }
        else
        {
            // If it's a loose ball, ensure simulation is ON and it's awake
            isHeld = false;
            rb.simulated = true;
            rb.gravityScale = dropGravity;
            coll.enabled = true;
            rb.WakeUp();
        }
    }

    void Update()
    {
        // Only override position if explicitly held by an anchor
        if (isHeld && targetAnchor != null)
        {
            transform.position = targetAnchor.position;
            transform.rotation = targetAnchor.rotation;
        }
    }

    public void SetHeld(bool held, Transform anchor)
    {
        isHeld = held;
        targetAnchor = anchor;

        if (held)
        {
            // Physics OFF: Let the Update loop snap it to the player's hands
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            coll.enabled = false;

            if (GameManager.Instance)
                GameManager.Instance.RecoverFumble();
        }
        else
        {
            // Physics ON: Let it bounce and fall
            transform.SetParent(null);
            rb.simulated = true;
            coll.enabled = true;
            coll.isTrigger = false;

            rb.gravityScale = dropGravity;
            rb.linearDamping = airDrag;

            // Apply the fumble "pop"
            Vector2 fumbleDir = new Vector2(Random.Range(-0.4f, 0.4f), 1f).normalized;
            rb.AddForce(fumbleDir * 10f, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-25f, 25f), ForceMode2D.Impulse);

            if (GameManager.Instance)
                GameManager.Instance.StartFumbleEvent(this.transform);
        }
    }
}