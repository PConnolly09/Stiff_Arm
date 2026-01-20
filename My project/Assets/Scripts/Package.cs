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
    [Tooltip("Check this if the player starts the level holding this.")]
    public bool startHeld = false;
    public MonoBehaviour currentHolder;

    [Header("Physics Settings")]
    public float dropGravity = 3f;
    public float airDrag = 0.5f;
    [Tooltip("Cap speed to prevent falling through the floor.")]
    public float maxFumbleSpeed = 15f;
    public PhysicsMaterial2D fumbleMaterial;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CircleCollider2D>();
        gameObject.tag = "Package";

        // CRITICAL: Continuous detection prevents floor tunneling
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (fumbleMaterial != null) coll.sharedMaterial = fumbleMaterial;
    }

    void Start()
    {
        if (startHeld)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerController>(out var pc))
            {
                pc.hasPackage = true;
                pc.packageObject = gameObject;
                SetHeld(true, pc.attachmentPoint, pc);
            }
        }
        else
        {
            // INITIALIZE LOOSE (Static):
            // We set the state manually here instead of calling SetHeld(false),
            // so we DO NOT apply the explosive fumble force on start.
            isHeld = false;
            transform.SetParent(null);

            rb.simulated = true;
            coll.enabled = true;
            coll.isTrigger = false; // Solid so it sits on floor

            rb.gravityScale = dropGravity;
            rb.linearDamping = airDrag;
            rb.linearVelocity = Vector2.zero; // Ensure it starts still
            rb.angularVelocity = 0f;
        }
    }

    void Update()
    {
        // Failsafe: If holder dies/vanishes, drop the ball
        if (isHeld && targetAnchor == null)
        {
            SetHeld(false, null, null);
            return;
        }

        if (isHeld && targetAnchor != null)
        {
            transform.SetPositionAndRotation(
                targetAnchor.position,
                targetAnchor.rotation
            );

        }
    }

    void FixedUpdate()
    {
        if (!isHeld)
        {
            // Anti-Tunneling: Clamp speed
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxFumbleSpeed);
        }
    }

    public void SetHeld(bool held, Transform anchor, MonoBehaviour holder)
    {
        isHeld = held;
        targetAnchor = anchor;
        currentHolder = held ? holder : null;

        if (held)
        {
            // Physics OFF
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            coll.enabled = false;

            if (holder is PlayerController && GameManager.Instance)
                GameManager.Instance.RecoverFumble();
        }
        else
        {
            // Physics ON (The Fumble Pop)
            transform.SetParent(null);
            rb.simulated = true;
            coll.enabled = true;
            coll.isTrigger = false;

            rb.gravityScale = dropGravity;
            rb.linearDamping = airDrag;
            rb.WakeUp();

            // Only apply force if this is an actual DROP/FUMBLE event
            // (Start() does not call this)
            float sideForce = Random.Range(-5f, 5f);
            float upForce = Random.Range(8f, 12f);
            Vector2 popVector = new (sideForce, upForce);

            rb.linearVelocity = Vector2.zero; // Reset momentum
            rb.AddForce(popVector, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-50f, 50f), ForceMode2D.Impulse);

            if (GameManager.Instance)
                GameManager.Instance.StartFumbleEvent(this.transform);
        }
    }
}