using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public abstract class EnemyAI : MonoBehaviour
{
    // ... [Settings headers match previous] ...
    [Header("Base AI Settings")]
    [Range(0.1f, 20f)] public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public LayerMask obstacleLayer;
    // Removed local prefab reference to use Manager instead
    // public GameObject bloodSplatterPrefab; 

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    protected int currentPatrolIndex = 0;
    protected float patrolWaitTimer = 0f;
    public float waitAtWaypointTime = 1f;

    [Header("Package Logic")]
    public bool carriesPackage = false;
    public Transform packageHoldPoint;

    // ... [State Variables match previous] ...
    protected bool movingRight = true;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected Transform currentTarget;
    protected bool isChasing = false;
    public bool isKnockedBack = false;
    protected Vector3 startPos;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        startPos = transform.position;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerTransform = p.transform;

        int layer = gameObject.layer;
        Physics2D.IgnoreLayerCollision(layer, layer, true);
        Physics2D.queriesStartInColliders = false;
    }

    protected virtual void FixedUpdate()
    {
        if (isKnockedBack || !enabled) return;

        if (carriesPackage)
        {
            RunAway();
            return;
        }

        DetermineTarget();

        if (isChasing && currentTarget != null)
        {
            Chase();
        }
        else
        {
            Patrol();
        }
    }

    // ... [DetermineTarget, RunAway, Move, Chase, ResetState, TakeHit, DropPackage, KnockbackRoutine, Flip match previous] ...

    protected void DetermineTarget()
    {
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
                Vector2 dir = (playerTransform.position - transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, obstacleLayer);
                if (hit.collider == null)
                {
                    currentTarget = playerTransform;
                    isChasing = true;
                    return;
                }
            }
        }
        isChasing = false;
        currentTarget = null;
    }

    protected virtual void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        float dist = Mathf.Abs(transform.position.x - targetPoint.position.x);

        if (dist < 0.5f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            patrolWaitTimer += Time.fixedDeltaTime;
            if (patrolWaitTimer > waitAtWaypointTime)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                patrolWaitTimer = 0;
            }
        }
        else
        {
            float dir = Mathf.Sign(targetPoint.position.x - transform.position.x);
            Move(dir, moveSpeed * 0.8f);
        }
    }

    protected void RunAway()
    {
        if (playerTransform == null) return;
        float dir = (transform.position.x > playerTransform.position.x) ? 1 : -1;
        Move(dir, moveSpeed * 1.5f);
    }

    protected void Move(float direction, float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        if (direction > 0 && !movingRight) Flip();
        else if (direction < 0 && movingRight) Flip();
    }

    protected abstract void Chase();
    protected virtual void ResetState() { }

    public void TakeHit(float force, Vector2 direction, bool isStun)
    {
        if (carriesPackage) DropPackage();
        StopAllCoroutines();
        ResetState();
        StartCoroutine(KnockbackRoutine(force, direction, isStun));
    }

    public void DropPackage()
    {
        carriesPackage = false;
        if (GameManager.Instance && GameManager.Instance.currentPackageTransform)
        {
            Package pkg = GameManager.Instance.currentPackageTransform.GetComponent<Package>();
            if (pkg && pkg.currentHolder == this) pkg.SetHeld(false, null, null);
        }
    }

    private IEnumerator KnockbackRoutine(float force, Vector2 direction, bool isStun)
    {
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        Vector2 safeDirection = new Vector2(direction.x, Mathf.Max(direction.y, 0.4f)).normalized;
        rb.AddForce(safeDirection * force, ForceMode2D.Impulse);
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
        if (!carriesPackage && collision.gameObject.CompareTag("Package"))
        {
            if (collision.gameObject.TryGetComponent<Package>(out var pkg) && !pkg.isHeld)
            {
                carriesPackage = true;
                pkg.SetHeld(true, packageHoldPoint, this);
            }
        }
        if (isKnockedBack && ((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            // FIX: Use EffectManager instead of local prefab reference
            if (EffectManager.Instance)
            {
                // Spawn slightly in front (z = -5) to ensure visibility
                Vector3 spawnPos = new Vector3(collision.contacts[0].point.x, collision.contacts[0].point.y, -5f);
                EffectManager.Instance.PlayEffect(EffectManager.Instance.bloodSplatterPrefab, spawnPos);
            }
            if (carriesPackage) DropPackage();
            Destroy(gameObject);
        }
    }

    // --- NEW: Visual Debugging for Waypoints ---
    protected virtual void OnDrawGizmos()
    {
        // Draw the Detection Range
        Gizmos.color = new Color(1, 1, 0, 0.2f); // Yellow transparent
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw Patrol Path
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    // Draw the point
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);

                    // Draw line to next point
                    Transform nextPoint = patrolPoints[(i + 1) % patrolPoints.Length];
                    if (nextPoint != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, nextPoint.position);
                    }
                }
            }

            // Draw line from Enemy to current target point
            if (Application.isPlaying && patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, patrolPoints[currentPatrolIndex].position);
            }
        }
    }
}