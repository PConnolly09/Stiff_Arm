using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyAI : MonoBehaviour
{
    [Header("Base AI Settings")]
    [Range(0.1f, 20f)] public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public LayerMask obstacleLayer;
    public GameObject bloodSplatterPrefab;

    [Header("Package Logic")]
    public bool carriesPackage = false; // Restored
    public Transform packageHoldPoint;

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
    }

    protected virtual void FixedUpdate()
    {
        if (isKnockedBack || !enabled) return;

        // If carrying the ball, RUN AWAY (Base behavior)
        if (carriesPackage)
        {
            RunAway();
            return;
        }

        DetermineTarget();

        if (isChasing) Chase();
        else Patrol();
    }

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
            currentTarget = playerTransform;
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            isChasing = dist < detectionRange;
        }
    }

    protected virtual void Patrol()
    {
        rb.linearVelocity = new Vector2(movingRight ? moveSpeed : -moveSpeed, rb.linearVelocity.y);
        Vector2 origin = (Vector2)transform.position + new Vector2(movingRight ? 0.6f : -0.6f, 0);
        if (Physics2D.Raycast(origin, movingRight ? Vector2.right : Vector2.left, 0.5f, obstacleLayer)) Flip();
    }

    protected void RunAway()
    {
        if (playerTransform == null) return;
        float dir = (transform.position.x > playerTransform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed * 1.5f, rb.linearVelocity.y);
        if ((dir > 0 && !movingRight) || (dir < 0 && movingRight)) Flip();
    }

    protected abstract void Chase();

    // Fixes 'no suitable method found to override' error
    protected virtual void ResetState() { }

    public void TakeHit(float force, Vector2 direction, bool isStun)
    {
        if (carriesPackage) DropPackage();
        StopAllCoroutines();
        ResetState(); // Reset subclass state logic
        StartCoroutine(KnockbackRoutine(force, direction, isStun));
    }

    public void DropPackage()
    {
        carriesPackage = false;
        Package pkg = FindFirstObjectByType<Package>();
        if (pkg != null && pkg.currentHolder == this)
        {
            pkg.SetHeld(false, null, null);
        }
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
        // Pick up package if we don't have it and it's loose
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
            if (bloodSplatterPrefab)
            {
                Instantiate(bloodSplatterPrefab, new Vector3(collision.contacts[0].point.x, collision.contacts[0].point.y, -5), Quaternion.identity);
            }
            if (carriesPackage) DropPackage();
            Destroy(gameObject);
        }
    }
}