using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Debug & Package")]
    public bool ignoreGameManagerForTesting = true;
    public bool hasPackage = true;
    public float baseFumbleChance = 0.05f;
    public GameObject packageObject; // Assign the actual package child here
    private Package currentPackage;

    [Header("Movement & Momentum")]
    public float acceleration = 90f;
    public float maxSpeed = 18f;
    public float backpedalSpeed = 10f;
    public float friction = 60f;
    public float airResistance = 10f;

    [Header("Jump Physics")]
    public float baseJumpForce = 16f;
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
    public bool isSpinning = false;
    public float dashForce = 25f;
    public float stiffArmBaseForce = 15f;
    public float speedPushMultiplier = 0.8f;
    private bool isDashing = false;

    [Header("Attachments")]
    public int attachmentCount = 0;
    public Transform attachmentPoint;
    private List<GameObject> attachedEnemies = new List<GameObject>();
    private float tackleDebuffTimer = 0f;

    private Rigidbody2D rb;
    private CapsuleCollider2D playerCollider;
    private float horizontalInput;
    private float coyoteCounter;
    private float jumpBufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CapsuleCollider2D>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // PACKAGE FIX: If a package is already present, ignore its collision immediately
        if (packageObject != null)
        {
            Collider2D packColl = packageObject.GetComponent<Collider2D>();
            if (packColl != null)
            {
                Physics2D.IgnoreCollision(playerCollider, packColl, true);
            }
        }

        currentPackage = GetComponentInChildren<Package>();
        if (currentPackage != null)
        {
            currentPackage.SetHeld(true, attachmentPoint); // Use your carry point
        }
    }

    void Update()
    {
        if (!ignoreGameManagerForTesting && (GameManager.Instance == null || GameManager.Instance.isIntroSequence))
        {
            horizontalInput = 0;
            return;
        }

        CheckGroundedStatus();
        HandleInput();
    }

    void FixedUpdate()
    {
        if (isDashing || isSpinning) return;

        ApplyMovement();
        ApplyCustomGravity();

        if (tackleDebuffTimer > 0) tackleDebuffTimer -= Time.fixedDeltaTime;
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

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            float jumpPenalty = attachmentCount * 1.5f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, baseJumpForce - jumpPenalty);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift)) StartCoroutine(PerformDash());
        if (Input.GetKeyDown(KeyCode.E)) StartCoroutine(PerformStiffArm());
        if (Input.GetKeyDown(KeyCode.Q)) StartCoroutine(PerformSpinMove());
    }

    private void ApplyMovement()
    {
        float penalty = (attachmentCount * 2.5f) + (tackleDebuffTimer > 0 ? 8f : 0f);
        float targetSpeed = 0;

        if (horizontalInput > 0.1f)
        {
            targetSpeed = Mathf.Max(4f, maxSpeed - penalty);
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (horizontalInput < -0.1f)
        {
            targetSpeed = -backpedalSpeed;
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;
        float lerpFactor = isMoving ? acceleration : (isGrounded ? friction : airResistance);

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, lerpFactor * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
    }

    public void AddAttachment(GameObject enemy)
    {
        if (isSpinning || isDashing || attachedEnemies.Contains(enemy)) return;

        attachmentCount++;
        attachedEnemies.Add(enemy);

        Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
        if (playerCollider != null && enemyCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, enemyCollider, true);
        }

        EnemyAI script = enemy.GetComponent<EnemyAI>();
        if (script) script.enabled = false;

        Rigidbody2D erb = enemy.GetComponent<Rigidbody2D>();
        if (erb)
        {
            erb.linearVelocity = Vector2.zero;
            erb.simulated = false;
        }

        enemy.transform.SetParent(attachmentPoint != null ? attachmentPoint : transform);
        enemy.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.2f, 0.3f), 0);
    }

    public void ProcessFumble(float extraRisk = 0)
    {
        if (!hasPackage || isSpinning) return;

        float chance = baseFumbleChance + (attachmentCount * 0.15f) + extraRisk;
        if (Random.value < chance)
        {
            hasPackage = false;

            if (currentPackage != null)
            {
                currentPackage.SetHeld(false);
                currentPackage = null;
            }

            Debug.Log("FUMBLE!");
        }
    }

    private IEnumerator PerformSpinMove()
    {
        isSpinning = true;
        if (attachedEnemies.Count > 0)
        {
            GameObject enemyToDrop = attachedEnemies[0];
            attachedEnemies.RemoveAt(0);
            attachmentCount--;

            Collider2D enemyCollider = enemyToDrop.GetComponent<Collider2D>();
            if (playerCollider != null && enemyCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, enemyCollider, false);
            }

            enemyToDrop.transform.SetParent(null);
            enemyToDrop.GetComponent<Collider2D>().enabled = true;
            Rigidbody2D erb = enemyToDrop.GetComponent<Rigidbody2D>();
            if (erb) erb.simulated = true;

            EnemyAI script = enemyToDrop.GetComponent<EnemyAI>();
            if (script)
            {
                script.enabled = true;
                script.TakeHit(10f, Vector2.up, false, false);
            }
        }
        yield return new WaitForSeconds(0.4f);
        isSpinning = false;
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        float dir = transform.localScale.x;
        rb.linearVelocity = new Vector2(dir * dashForce, 0f);
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
        // Logic to pick up the package after a fumble
        if (!hasPackage && col.gameObject.CompareTag("Package"))
        {
            Package pkg = col.gameObject.GetComponent<Package>();
            if (pkg != null)
            {
                hasPackage = true;
                currentPackage = pkg;
                currentPackage.SetHeld(true, attachmentPoint);
                Debug.Log("Package Recovered!");
            }
        }

        if (col.gameObject.CompareTag("Enemy") && !isSpinning)
        {
            EnemyAI enemy = col.gameObject.GetComponent<EnemyAI>();
            if (enemy == null) return;

            if (isStiffArming)
            {
                float force = stiffArmBaseForce + (Mathf.Abs(rb.linearVelocity.x) * speedPushMultiplier);
                Vector2 pushDir = new Vector2(transform.localScale.x, 0.3f).normalized;
                enemy.TakeHit(force, pushDir, isDashing, isDashing);
            }
            else
            {
                if (enemy is InterceptorEnemy) ProcessFumble();
                else if (enemy is BruteEnemy)
                {
                    tackleDebuffTimer = 1.5f;
                    ProcessFumble(0.4f);
                }
                else if (enemy is MinionEnemy) AddAttachment(col.gameObject);
            }
        }
    }
}