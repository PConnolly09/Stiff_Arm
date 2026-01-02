using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public abstract class EnemyAI : MonoBehaviour
{
    [Header("Base AI Settings")]
    [Range(0.1f, 20f)] public float moveSpeed = 3f;
    public float detectionRange = 8f;
    public LayerMask obstacleLayer;
    public LayerMask enemyLayer; // Assign the Enemy layer here

    [Header("Variety Settings")]
    [SerializeField] protected float speedVariance = 0.5f;
    protected float personalSpeed;

    [Header("Detection Offsets")]
    public float wallCheckDistance = 0.5f;
    public Vector2 checkOffset = new Vector2(0.6f, 0f);

    protected bool movingRight = true;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected bool isChasing = false;
    protected bool isKnockedBack = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerTransform = p.transform;

        // Variety: Randomize speed slightly so they don't move in sync
        personalSpeed = moveSpeed + Random.Range(-speedVariance, speedVariance);

        // Allow enemies to pass through each other physically
        int layer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(layer, layer, true);

        Physics2D.queriesStartInColliders = false;
    }

    protected virtual void FixedUpdate()
    {
        if (isKnockedBack || !enabled || personalSpeed <= 0) return;

        CheckForPlayer();

        // Anti-Stacking: Check if another enemy is right in front of us
        float currentTargetSpeed = personalSpeed;
        Vector2 checkDir = movingRight ? Vector2.right : Vector2.left;
        RaycastHit2D enemyHit = Physics2D.Raycast(transform.position, checkDir, 1.0f, 1 << gameObject.layer);

        if (enemyHit.collider != null)
        {
            // Slow down or stagger slightly to avoid standing on top
            currentTargetSpeed *= 0.5f;
        }

        if (isChasing) Chase(currentTargetSpeed);
        else Patrol(currentTargetSpeed);
    }

    protected void CheckForPlayer()
    {
        if (playerTransform == null) return;
        isChasing = Vector2.Distance(transform.position, playerTransform.position) < detectionRange;
    }

    protected virtual void Patrol(float speed)
    {
        rb.linearVelocity = new Vector2(movingRight ? speed : -speed, rb.linearVelocity.y);

        Vector2 origin = (Vector2)transform.position + new Vector2(movingRight ? checkOffset.x : -checkOffset.x, checkOffset.y);
        if (Physics2D.Raycast(origin, movingRight ? Vector2.right : Vector2.left, wallCheckDistance, obstacleLayer))
        {
            Flip();
        }
    }

    protected abstract void Chase(float speed);

    public void TakeHit(float force, Vector2 direction, bool isStun, bool isSuperHit)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(force, direction, isStun, isSuperHit));
    }

    private IEnumerator KnockbackRoutine(float force, Vector2 direction, bool isStun, bool isSuperHit)
    {
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        yield return new WaitForSeconds(isStun ? 2.0f : 0.6f);
        isKnockedBack = false;
    }

    protected void Flip()
    {
        movingRight = !movingRight;
        transform.localScale = new Vector3(movingRight ? Mathf.Abs(transform.localScale.x) : -Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isKnockedBack && ((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}