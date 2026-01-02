using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public abstract class EnemyAI : MonoBehaviour
{
    [Header("Base AI Settings")]
    [Range(0.1f, 20f)] public float moveSpeed = 3f; // Range helps prevent 0 speed in Inspector
    public float detectionRange = 8f;
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    [Header("Detection Offsets")]
    public float wallCheckDistance = 0.5f;
    public Vector2 checkOffset = new Vector2(0.6f, 0f);

    protected bool movingRight = true;
    protected Rigidbody2D rb;
    protected CapsuleCollider2D coll;
    protected Transform playerTransform;
    protected bool isChasing = false;
    protected bool isKnockedBack = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();

        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerTransform = p.transform;

        // CRITICAL: Prevent rays from hitting the enemy's own collider
        Physics2D.queriesStartInColliders = false;

        // Match initial movement bool to scale
        movingRight = transform.localScale.x > 0;
    }

    protected virtual void FixedUpdate()
    {
        // If speed is 0 in Inspector, they won't move! 
        // Always check the Inspector value.
        if (isKnockedBack || !enabled || moveSpeed <= 0) return;

        CheckForPlayer();

        if (isChasing) Chase();
        else Patrol();
    }

    protected virtual void CheckForPlayer()
    {
        if (playerTransform == null) return;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        isChasing = dist < detectionRange;
    }

    protected virtual void Patrol()
    {
        // Movement
        float velX = movingRight ? moveSpeed : -moveSpeed;
        rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);

        // Wall Detection
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(movingRight ? checkOffset.x : -checkOffset.x, checkOffset.y);
        RaycastHit2D wallHit = Physics2D.Raycast(rayOrigin, movingRight ? Vector2.right : Vector2.left, wallCheckDistance, obstacleLayer);

        if (wallHit.collider != null)
        {
            Flip();
        }
    }

    protected virtual void Chase()
    {
        if (playerTransform == null) return;

        float direction = playerTransform.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(direction * (moveSpeed * 1.3f), rb.linearVelocity.y);

        if (direction > 0 && !movingRight) Flip();
        else if (direction < 0 && movingRight) Flip();
    }

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
        Vector3 localScale = transform.localScale;
        localScale.x = movingRight ? Mathf.Abs(localScale.x) : -Mathf.Abs(localScale.x);
        transform.localScale = localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isKnockedBack && ((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Destroy(gameObject);
        }
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(movingRight ? checkOffset.x : -checkOffset.x, checkOffset.y);
        Gizmos.DrawRay(rayOrigin, (movingRight ? Vector2.right : Vector2.left) * wallCheckDistance);
    }
}