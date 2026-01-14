using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(CinemachineImpulseSource))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Ground Movement")]
    public float acceleration = 90f;
    public float maxSpeed = 20f;
    public float friction = 70f;
    public float backpedalSpeed = 18f; // Fast enough to not feel "stalled"
    public float baseRunSpeed = 8f;

    [Header("Air Physics (Tuned to Tiles)")]
    [Tooltip("Clears ~2.2 tiles from standstill")]
    public float baseJumpForce = 17f;
    [Tooltip("Adds height based on horizontal speed. Total ~3.5 tiles at max speed")]
    public float momentumJumpBonus = 5f;
    public float normalGravity = 4.5f;
    public float quickFallGravity = 10f;
    public float airAcceleration = 35f;
    public float coyoteTime = 0.15f;
    private float coyoteCounter;
    private float jumpCooldownTimer;

    [Header("Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;
    [SerializeField] private bool isGrounded;

    [Header("Abilities")]
    public Transform stiffArmPoint;
    public float stiffArmRange = 2.0f;
    public float stiffArmBaseForce = 25f;
    public float speedPushMultiplier = 1.0f;

    [Header("Juke Settings")]
    public float jukeDuration = 0.3f;
    public float jukeCooldown = 1f;
    public Color jukeColor = new(1, 1, 1, 0.5f);
    private float jukeTimer;

    [Header("States & References")]
    [Header("Package")]
    public bool hasPackage = true;
    public float baseFumbleChance = 0.05f;
    public float fumblePickupDelay = 1.2f; // Time before player can pick up a loose ball
    public GameObject packageObject;
    public Transform attachmentPoint;
    public int attachmentCount = 0;
    private readonly List<GameObject> attachedEnemies = new();

    public bool isStiffArming, isSpinning, isJuking, isProne;
    private float proneTimer;
    private float tackleDebuffTimer;
    private float pickupTimer; // Counter to track the delay

    [Header("Audio Clips")]
    public AudioClip jumpSfx;
    public AudioClip jukeSfx;
    public AudioClip spinSfx;
    public AudioClip stiffArmSfx;
    public AudioClip fumbleSfx;
    public AudioClip impactSfx;

    private Rigidbody2D rb;
    private CapsuleCollider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private CinemachineImpulseSource impulseSource;
    private AudioSource audioSource;
    private Animator anim;
    private float horizontalInput;
    private Color normalColor = Color.white;

    // Set this to false when using the real GameManager
    public bool ignoreGameManagerForTesting = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (!ignoreGameManagerForTesting && (GameManager.Instance == null || GameManager.Instance.isIntroSequence))
        {
            horizontalInput = 0;
            return;
        }

        if (isProne)
        {
            HandleProneState();
            UpdateAnimations();
            return;
        }

        CheckGrounded();
        HandleInput();
        ApplyCustomGravity();
        UpdateAnimations();

        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;
        if (jukeTimer > 0) jukeTimer -= Time.deltaTime;
        if (pickupTimer > 0) pickupTimer -= Time.deltaTime; // Decrement pickup delay
    }

    void FixedUpdate()
    {
        if (isProne || isSpinning) return;
        HandleMovement();
        if (tackleDebuffTimer > 0) tackleDebuffTimer -= Time.fixedDeltaTime;
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && coyoteCounter > 0 && jumpCooldownTimer <= 0)
        {
            PerformJump();
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isSpinning) StartCoroutine(PerformSpinMove());
        if (Input.GetKeyDown(KeyCode.E) && !isStiffArming) PerformStiffArm();
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isJuking && jukeTimer <= 0) StartCoroutine(PerformJuke());
    }

    private void HandleMovement()
    {
        float penalty = (attachmentCount * 2.5f) + (tackleDebuffTimer > 0 ? 8f : 0f);
        float currentMax = (horizontalInput < 0) ? backpedalSpeed : maxSpeed;
        float targetSpeed = horizontalInput * Mathf.Max(2f, currentMax - penalty);

        float accelRate;
        if (isGrounded)
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.1f) ? acceleration : friction;
        }
        else
        {
            accelRate = airAcceleration;
        }

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        if (!isSpinning && horizontalInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        }
    }

    private void PerformJump()
    {
        float speedRatio = Mathf.Abs(rb.linearVelocity.x) / maxSpeed;
        float totalJumpForce = baseJumpForce + (momentumJumpBonus * speedRatio) - (attachmentCount * 1.5f);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * totalJumpForce, ForceMode2D.Impulse);

        coyoteCounter = 0;
        jumpCooldownTimer = 0.1f;
        PlaySound(jumpSfx);
        EffectManager.Instance?.PlayEffect(EffectManager.Instance.jumpDustPrefab, groundCheck.position);
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0 || (rb.linearVelocity.y > 0 && !Input.GetButton("Jump")))
            rb.gravityScale = quickFallGravity;
        else
            rb.gravityScale = normalGravity;
    }

    // --- ABILITIES ---

    private void PerformStiffArm()
    {
        StartCoroutine(StiffArmRoutine());
        anim.SetTrigger("StiffArmTrigger");
        PlaySound(stiffArmSfx);
    }

    private IEnumerator StiffArmRoutine()
    {
        isStiffArming = true;
        // Multi-scan for collision consistency
        CheckStiffArmHit();
        yield return new WaitForSeconds(0.1f);
        CheckStiffArmHit();
        yield return new WaitForSeconds(0.2f);
        isStiffArming = false;
    }

    private void CheckStiffArmHit()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(stiffArmPoint.position, stiffArmRange);
        foreach (var enemyCol in enemies)
        {
            if (enemyCol.CompareTag("Enemy"))
            {
                if (enemyCol.TryGetComponent<EnemyAI>(out var enemyScript) && !enemyScript.isKnockedBack)
                {
                    float momentumBonus = Mathf.Abs(rb.linearVelocity.x) * speedPushMultiplier;
                    Vector2 dir = new Vector2(transform.localScale.x, 0.2f).normalized;

                    enemyScript.TakeHit(stiffArmBaseForce + momentumBonus, dir, false);
                    EffectManager.Instance?.PlayEffect(EffectManager.Instance.stiffArmImpactPrefab, enemyCol.transform.position);
                    impulseSource.GenerateImpulse(0.5f);
                    PlaySound(impactSfx);
                }
            }
        }
    }

    private IEnumerator PerformJuke()
    {
        isJuking = true;
        jukeTimer = jukeCooldown;
        PlaySound(jukeSfx);
        EffectManager.Instance?.PlayEffect(EffectManager.Instance.jukeGhostPrefab, transform.position);
        spriteRenderer.color = jukeColor;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
        yield return new WaitForSeconds(jukeDuration);
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
        spriteRenderer.color = normalColor;
        isJuking = false;
    }

    private IEnumerator PerformSpinMove()
    {
        isSpinning = true;
        impulseSource.GenerateImpulse(0.3f);
        PlaySound(spinSfx);
        anim.SetTrigger("SpinTrigger");
        EffectManager.Instance?.PlayEffect(EffectManager.Instance.spinTrailPrefab, transform.position);

        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float spinSpeed = 12f;
        float facingDir = transform.localScale.x > 0 ? 1 : -1;
        bool hasInput = Mathf.Abs(horizontalInput) > 0.1f;

        if (attachmentCount == 0)
        {
            // Traverse or Spin
            if (hasInput) rb.linearVelocity = new Vector2(Mathf.Sign(horizontalInput) * spinSpeed, rb.linearVelocity.y);
            else rb.linearVelocity = Vector2.zero;

            // Clear path
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 2.5f);
            foreach (var col in nearby)
            {
                if (col.CompareTag("Enemy") && col.TryGetComponent<EnemyAI>(out var e))
                    e.TakeHit(10f, (col.transform.position - transform.position).normalized, true);
            }
        }
        else
        {
            // Eject stacked enemy
            rb.linearVelocity = Vector2.zero;
            GameObject drop = attachedEnemies[0];
            attachedEnemies.RemoveAt(0);
            attachmentCount--;
            UpdateUI();

            drop.transform.SetParent(null);
            Vector3 dropPos = drop.transform.position;
            dropPos.z = 0; // Fix vanishing
            drop.transform.position = dropPos;

            drop.GetComponent<Collider2D>().enabled = true;
            if (drop.TryGetComponent<Rigidbody2D>(out var erb)) erb.simulated = true;
            if (drop.TryGetComponent<EnemyAI>(out var script))
            {
                script.enabled = true;
                Vector2 ejectDir = new Vector2(Random.Range(-1f, 1f), 1f).normalized;
                script.TakeHit(15f, ejectDir, true);
            }
            Physics2D.IgnoreCollision(playerCollider, drop.GetComponent<Collider2D>(), false);
        }

        // Flip-Flop visuals
        float elapsed = 0;
        while (elapsed < 0.4f)
        {
            transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }

        transform.localScale = new Vector3(horizontalInput != 0 ? Mathf.Sign(horizontalInput) : facingDir, 1, 1);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        isSpinning = false;
    }

    // --- SYSTEMS ---

    private void OnCollisionEnter2D(Collision2D col)
    {
        // Solid Pickup Logic - Added pickupTimer check to prevent instant re-grab
        if (pickupTimer <= 0 && !hasPackage && col.gameObject.CompareTag("Package"))
        {
            if (col.gameObject.TryGetComponent<Package>(out var pkg))
            {
                hasPackage = true;
                packageObject = col.gameObject;
                pkg.SetHeld(true, attachmentPoint);
                Physics2D.IgnoreCollision(playerCollider, col.collider, true);
            }
        }

        // Combat Logic
        if (!isSpinning && col.gameObject.CompareTag("Enemy"))
        {
            if (col.gameObject.TryGetComponent<EnemyAI>(out var enemy))
            {
                if (isStiffArming)
                {
                    float f = stiffArmBaseForce + (Mathf.Abs(rb.linearVelocity.x) * speedPushMultiplier);
                    enemy.TakeHit(f, new Vector2(transform.localScale.x, 0.3f).normalized, false);
                    EffectManager.Instance?.PlayEffect(EffectManager.Instance.stiffArmImpactPrefab, col.contacts[0].point);
                    PlaySound(impactSfx);
                }
                else if (enemy is MinionEnemy)
                {
                    AddAttachment(col.gameObject);
                }
                else
                {
                    if (enemy is BruteEnemy) tackleDebuffTimer = 1.5f;
                    ProcessFumble(0.2f);
                    PlaySound(impactSfx, 1.2f);
                }
            }
        }
    }

    public void SetProne(float duration)
    {
        if (isJuking || isSpinning) return;
        isProne = true;
        proneTimer = duration;
        rb.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.Euler(0, 0, 90);
        impulseSource.GenerateImpulse(1.5f);
        PlaySound(impactSfx);
        EffectManager.Instance?.PlayEffect(EffectManager.Instance.tackleImpactPrefab, transform.position);
        ProcessFumble(0.4f);
    }

    private void HandleProneState()
    {
        if (Input.GetButtonDown("Jump")) proneTimer -= 0.2f;
        proneTimer -= Time.deltaTime;
        if (proneTimer <= 0)
        {
            isProne = false;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            transform.rotation = Quaternion.identity;
        }
    }

    public void AddAttachment(GameObject enemy)
    {
        if (isSpinning || isJuking || attachedEnemies.Contains(enemy)) return;
        attachmentCount++;
        attachedEnemies.Add(enemy);
        UpdateUI();
        Physics2D.IgnoreCollision(playerCollider, enemy.GetComponent<Collider2D>(), true);
        enemy.GetComponent<EnemyAI>().enabled = false;
        if (enemy.TryGetComponent<Rigidbody2D>(out var erb)) { erb.linearVelocity = Vector2.zero; erb.simulated = false; }
        enemy.transform.SetParent(attachmentPoint);
        enemy.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.2f, 0.3f), 0);
        EffectManager.Instance?.PlayEffect(EffectManager.Instance.attachPoofPrefab, enemy.transform.position);
    }

    public void ProcessFumble(float extraRisk = 0)
    {
        if (!hasPackage || isSpinning || isJuking) return;
        float chance = baseFumbleChance + (attachmentCount * 0.15f) + extraRisk;
        if (Random.value < chance)
        {
            hasPackage = false;

            // Set cooldown so the player doesn't immediately pick the ball back up
            pickupTimer = fumblePickupDelay;

            if (packageObject != null && packageObject.TryGetComponent<Package>(out var pkg))
                pkg.SetHeld(false, null);

            impulseSource.GenerateImpulse(1.5f);
            PlaySound(fumbleSfx);
            EffectManager.Instance?.PlayEffect(EffectManager.Instance.fumbleExplosionPrefab, transform.position);
            Debug.Log("FUMBLE!");
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        float s = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", s);
        anim.SetFloat("RunMultiplier", Mathf.Clamp(s / baseRunSpeed, 0.5f, 3f));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isProne", isProne);
    }

    private void CheckGrounded()
    {
        bool was = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            if (!was)
            {
                EffectManager.Instance?.PlayEffect(EffectManager.Instance.landDustPrefab, groundCheck.position);
                if (CameraController.Instance) CameraController.Instance.SetPlayerGrounded(true);
            }
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
            if (CameraController.Instance) CameraController.Instance.SetPlayerGrounded(false);
        }
    }

    private void UpdateUI() { if (GameManager.Instance) GameManager.Instance.UpdateAttachmentCount(attachmentCount); }
    private void PlaySound(AudioClip c, float v = 1f) { if (c && audioSource) audioSource.PlayOneShot(c, v); }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        if (stiffArmPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(stiffArmPoint.position, stiffArmRange);
        }
    }
}