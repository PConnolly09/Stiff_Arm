using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool ignoreGameManagerForTesting = true;

    [Header("Package Settings")]
    public bool hasPackage = true;
    public float baseFumbleChance = 0.05f; // 5% base
    public GameObject packagePrefab; // Assign in inspector to spawn on fumble

    [Header("Movement & Momentum")]
    public float acceleration = 90f;
    public float maxSpeed = 18f;
    public float backpedalSpeed = 10f;
    public float friction = 60f;
    public float airResistance = 10f;
    public float currentSpeed = 0f;

    [Header("Jump Physics")]
    public float jumpForce = 16f;
    public float fallMultiplier = 4f;
    public float lowJumpMultiplier = 3f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    [SerializeField] private bool isGrounded;

    [Header("Abilities")]
    public bool isStiffArming = false;
    public float stiffArmBaseForce = 15f;
    public float dashForce = 25f;
    private bool isDashing = false;

    [Header("Attachments")]
    public int attachmentCount = 0;
    public Transform attachmentPoint;
    private List<GameObject> attachedEnemies = new List<GameObject>();

    private Rigidbody2D rb;
    private float horizontalInput;
    private float coyoteCounter;
    private float jumpBufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (!ignoreGameManagerForTesting && (GameManager.Instance == null || GameManager.Instance.isIntroSequence)) return;

        CheckGroundedStatus();
        HandleInput();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        currentSpeed = rb.linearVelocity.x;

        ApplyMovement();
        ApplyCustomGravity();

        // Snap speed to zero if reversing or pushing opposite
        bool isPushingOpposite = (horizontalInput > 0 && currentSpeed < -0.1f) || (horizontalInput < 0 && currentSpeed > 0.1f);
        if (isGrounded && isPushingOpposite)
        {
            currentSpeed = 0;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void CheckGroundedStatus()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        else
            isGrounded = rb.IsTouchingLayers(groundLayer);

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump Buffer logic
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Execute Jump with Coyote Time
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift)) StartCoroutine(PerformDash());
        if (Input.GetKeyDown(KeyCode.E)) StartCoroutine(PerformStiffArm());
    }

    private void ApplyMovement()
    {
        float speedPenalty = attachmentCount * 2.5f; // Minions slow you down
        float targetSpeed = 0;

        if (horizontalInput > 0)
            targetSpeed = Mathf.Max(4f, maxSpeed - speedPenalty);
        else if (horizontalInput < 0)
            targetSpeed = -backpedalSpeed;

        float lerpFactor = (horizontalInput != 0) ? acceleration : (isGrounded ? friction : airResistance);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, lerpFactor * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
    }

    private void ApplyCustomGravity()
    {
        // Custom gravity feel for jumping (snappier falling)
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
    }

    public void AddAttachment(GameObject enemy)
    {
        if (attachedEnemies.Contains(enemy)) return;

        attachmentCount++;
        attachedEnemies.Add(enemy);

        // Kill AI and Physics to stop stuttering
        EnemyAI script = enemy.GetComponent<EnemyAI>();
        if (script) script.enabled = false;

        enemy.GetComponent<Collider2D>().enabled = false;
        Rigidbody2D erb = enemy.GetComponent<Rigidbody2D>();
        if (erb)
        {
            erb.linearVelocity = Vector2.zero;
            erb.simulated = false;
        }

        enemy.transform.SetParent(attachmentPoint != null ? attachmentPoint : transform);
        enemy.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), 0);

        Debug.Log("Enemy Attached! Penalty Active.");
    }

    public void ProcessFumble()
    {
        if (!hasPackage) return;

        // Fumble chance increases with attachments
        float chance = baseFumbleChance + (attachmentCount * 0.15f);
        if (Random.value < chance)
        {
            hasPackage = false;
            Debug.Log("FUMBLE! Package dropped.");
            // Logic to instantiate packagePrefab and apply force...
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        float dir = horizontalInput >= 0 ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * dashForce, 0);
        yield return new WaitForSeconds(0.2f);
        isDashing = false;
    }

    private IEnumerator PerformStiffArm()
    {
        isStiffArming = true;
        yield return new WaitForSeconds(0.3f);
        isStiffArming = false;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            EnemyAI enemy = col.gameObject.GetComponent<EnemyAI>();
            if (enemy == null) return;

            if (isStiffArming)
            {
                // Flashy Stiff Arm: Force scales with player's current speed
                float speedFactor = Mathf.Abs(rb.linearVelocity.x) * 0.8f;
                float finalForce = stiffArmBaseForce + speedFactor;

                // Dash + Stiff Arm = Stun + Damage flag
                Vector2 pushDir = new Vector2(transform.localScale.x, 0.3f).normalized;
                enemy.TakeHit(finalForce, pushDir, isDashing, isDashing);

                Debug.Log("Stiff Arm Hit! Force: " + finalForce);
            }
            else
            {
                // Interceptor triggers fumble logic, others attach
                if (enemy is InterceptorEnemy)
                {
                    ProcessFumble();
                }
                else if (enemy is MinionEnemy || enemy is BruteEnemy)
                {
                    AddAttachment(col.gameObject);
                }
            }
        }
    }
}