using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Package : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform playerTransform;
    private bool isDropped = false;

    [Header("Fumble Settings")]
    public float bounceForce = 5f;
    public Vector3 carryOffset = new Vector3(0.5f, 0, 0);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Find player on start
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Start()
    {
        // Register this package with the GameManager automatically
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentPackage = this;
        }
    }

    void Update()
    {
        // If being carried, follow the player
        if (!isDropped && playerTransform != null)
        {
            transform.position = playerTransform.position + carryOffset;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Drop()
    {
        isDropped = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.enabled = true;

        // "Pop" the ball up and forward for the fumble
        Vector2 fumbleDir = new Vector2(Random.Range(-1f, 1f), 1f).normalized;
        rb.AddForce(fumbleDir * bounceForce, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Recover logic
        if (isDropped && other.CompareTag("Player"))
        {
            isDropped = false;
            rb.bodyType = RigidbodyType2D.Kinematic;
            col.enabled = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecoverPackage();
            }
        }
    }
}