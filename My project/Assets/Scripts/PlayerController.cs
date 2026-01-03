using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(CinemachineImpulseSource))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement & Momentum")]
    public float acceleration = 120f;
    public float maxSpeed = 18f;
    public float friction = 80f;
    public float backpedalSpeed = 10f;

    [Header("Burst Jump Settings")]
    public float jumpForce = 22f;
    public float quickFallGravity = 8f;
    public float normalGravity = 4f;
    public float coyoteTime = 0.15f;
    public float jumpCooldownTime = 0.1f; // Prevents accidental double jumps
    private float coyoteCounter;
    private float jumpCooldownTimer;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    [SerializeField] private bool isGrounded;

    [Header("Abilities")]
    public bool isStiffArming = false;
    public bool isSpinning = false;
    public float dashForce = 30f;
    public float stiffArmBaseForce = 15f;
    public float speedPushMultiplier = 0.8f;
    private bool isDashing = false;

    [Header("Package & Fumbles")]
    public bool hasPackage = true;
    public float baseFumbleChance = 0.05f;
    public GameObject packageObject;

    [Header("Attachments")]
    public int attachmentCount = 0;
    public Transform attachmentPoint;
    private List<GameObject> attachedEnemies = new List<GameObject>();
    private float tackleDebuffTimer = 0f;

    [Header("Visuals & Juice")]
    public GameObject bloodSplatterPrefab;
    public ParticleSystem windEffect; // Optional for dash

    [Header("Audio Clips")]
    public AudioClip jumpSfx;
    public AudioClip dashSfx;
    public AudioClip spinSfx;
    public AudioClip stiffArmSfx;
    public AudioClip fumbleSfx;
    public AudioClip impactSfx;

    private Rigidbody2D rb;
    private CapsuleCollider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private CinemachineImpulseSource impulseSource;
    private AudioSource audioSource;
    private float horizontalInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();

        rb.gravityScale = normalGravity;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (packageObject != null)
        {
            Collider2D packColl = packageObject.GetComponent<Collider2D>();
            if (packColl != null) Physics2D.IgnoreCollision(playerCollider, packColl, true);
        }
    }

    void Update()
    {
        // Handle global game state (Pause/Menu) - Placeholder for GameManager
        // if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        CheckGrounded();
        HandleInput();
        ApplyCustomGravity();

        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (isDashing || isSpinning) return;

        HandleMovement();

        if (tackleDebuffTimer > 0) tackleDebuffTimer -= Time.fixedDeltaTime;
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && coyoteCounter > 0 && jumpCooldownTimer <= 0)
        {
            float jumpPenalty = attachmentCount * 1.5f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce - jumpPenalty);

            coyoteCounter = 0;
            jumpCooldownTimer = jumpCooldownTime; // Fix for accidental double jumps
            PlaySound(jumpSfx);
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isSpinning) StartCoroutine(PerformSpinMove());
        if (Input.GetKeyDown(KeyCode.E) && !isStiffArming) StartCoroutine(PerformStiffArm());
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing) StartCoroutine(PerformDash());
    }

    private void HandleMovement()
    {
        float penalty = (attachmentCount * 2.5f) + (tackleDebuffTimer > 0 ? 8f : 0f);
        float currentMax = (horizontalInput < 0) ? backpedalSpeed : maxSpeed;
        float targetSpeed = horizontalInput * Mathf.Max(4f, currentMax - penalty);

        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : friction;

        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        if (!isSpinning && horizontalInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        }
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = quickFallGravity;
        }
        else
        {
            rb.gravityScale = normalGravity;
        }
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

        PlaySound(impactSfx, 0.5f);
    }

    public void ProcessFumble(float extraRisk = 0)
    {
        if (!hasPackage || isSpinning) return;

        float chance = baseFumbleChance + (attachmentCount * 0.15f) + extraRisk;
        if (Random.value < chance)
        {
            hasPackage = false;
            impulseSource.GenerateImpulse(0.8f);
            PlaySound(fumbleSfx);
            Debug.Log("FUMBLE!");

            // Trigger UI/Game Over state via a GameManager
            // if (GameManager.Instance != null) GameManager.Instance.OnPackageLost();
        }
    }

    private IEnumerator PerformSpinMove()
    {
        isSpinning = true;
        impulseSource.GenerateImpulse(0.3f);
        PlaySound(spinSfx);

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
                script.TakeHit(12f, Vector2.up, false, false);
            }
        }

        float spinDuration = 0.4f;
        float flipInterval = 0.05f;
        float elapsed = 0;
        while (elapsed < spinDuration)
        {
            transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
            yield return new WaitForSeconds(flipInterval);
            elapsed += flipInterval;
        }

        if (horizontalInput != 0) transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        isSpinning = false;
    }

    private IEnumerator PerformStiffArm()
    {
        isStiffArming = true;
        PlaySound(stiffArmSfx);
        yield return new WaitForSeconds(0.3f);
        isStiffArming = false;
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        PlaySound(dashSfx);
        if (windEffect != null) windEffect.Play();

        float dir = transform.localScale.x;
        rb.linearVelocity = new Vector2(dir * dashForce, 0);
        yield return new WaitForSeconds(0.2f);
        isDashing = false;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Enemy") && !isSpinning)
        {
            EnemyAI enemy = col.gameObject.GetComponent<EnemyAI>();
            if (enemy == null) return;

            if (isStiffArming)
            {
                float force = stiffArmBaseForce + (Mathf.Abs(rb.linearVelocity.x) * speedPushMultiplier);
                impulseSource.GenerateImpulse(force * 0.05f);
                Vector2 pushDir = new Vector2(transform.localScale.x, 0.3f).normalized;
                enemy.TakeHit(force, pushDir, isDashing, isDashing);

                SpawnBloodSplatter(col.contacts[0].point);
                PlaySound(impactSfx);
            }
            else
            {
                if (enemy is InterceptorEnemy) ProcessFumble();
                else if (enemy is BruteEnemy)
                {
                    impulseSource.GenerateImpulse(1.2f);
                    tackleDebuffTimer = 1.5f;
                    ProcessFumble(0.4f);
                    PlaySound(impactSfx, 1.2f);
                }
                else if (enemy is MinionEnemy) AddAttachment(col.gameObject);
            }
        }
    }

    private void SpawnBloodSplatter(Vector2 position)
    {
        if (bloodSplatterPrefab != null)
        {
            Instantiate(bloodSplatterPrefab, position, Quaternion.identity);
        }
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}