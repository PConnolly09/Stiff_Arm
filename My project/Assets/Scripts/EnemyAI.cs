using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyAI : MonoBehaviour
{
    [Header("Base AI Settings")]
    [Range(0.1f, 20f)] public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public LayerMask obstacleLayer;

    [Header("Patrol")]
    public Transform[] patrolPoints; // Drag waypoints here
    private int currentPointIndex = 0;

    [Header("Chasing")]
    public float maxChaseTime = 5f;
    private float chaseTimer;

    [Header("Visuals")]
    protected bool movingRight = true;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected Transform currentTarget;
    protected bool isChasing = false;
    public bool isKnockedBack = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 4f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerTransform = p.transform;

        int layer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(layer, layer, true);
        Physics2D.queriesStartInColliders = false;

        SnapToGround();
    }

    private void SnapToGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 5f, obstacleLayer);
        if (hit.collider != null)
        {
            // Adjust based on pivot (assuming pivot is center, move up by half height)
            float halfHeight = GetComponent<CapsuleCollider2D>()?.size.y / 2f ?? 0.5f;
            transform.position = new Vector3(transform.position.x, hit.point.y + halfHeight, transform.position.z);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isKnockedBack || !enabled) return;

        DetermineTarget();

        if (isChasing)
        {
            Chase();
            chaseTimer += Time.fixedDeltaTime;
            if (chaseTimer > maxChaseTime)
            {
                isChasing = false; // Give up
                currentTarget = null;
            }
        }
        else
        {
            chaseTimer = 0;
            Patrol();
        }
    }

    protected void DetermineTarget()
    {
        // ... (Existing target logic for Package/Player) ...
        // Ensure to set isChasing = true if target found within range
        if (GameManager.Instance && GameManager.Instance.currentState == GameManager.GameState.Fumble)
        {
            if (GameManager.Instance.currentPackageTransform != null)
            {
                currentTarget = GameManager.Instance.currentPackageTransform;
                isChasing = true;
                return;
            }
        }

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < detectionRange)
            {
                currentTarget = playerTransform;
                isChasing = true;
            }
        }
    }

    protected virtual void Patrol()
    {
        if (patrolPoints.Length == 0) return; // Idle if no points

        Transform targetPoint = patrolPoints[currentPointIndex];
        float dist = Vector2.Distance(transform.position, targetPoint.position);

        if (dist < 0.5f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            return;
        }

        float dir = (targetPoint.position.x > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

        if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
    }

    protected abstract void Chase();

    // ... (Keep TakeHit, Flip, OnCollisionEnter2D) ...
    public void TakeHit(float force, Vector2 direction, bool isStun)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(force, direction, isStun));
    }

    private IEnumerator KnockbackRoutine(float force, Vector2 direction, bool isStun)
    {
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        yield return new WaitForSeconds(isStun ? 2.0f : 0.8f);
        isKnockedBack = false;
    }

    protected void Flip()
    {
        movingRight = !movingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isKnockedBack && ((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            EffectManager.Instance?.PlayEffect(EffectManager.Instance.bloodSplatterPrefab, collision.contacts[0].point);
            Destroy(gameObject);
        }
    }
}