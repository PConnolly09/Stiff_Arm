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
    [Header("Movement & Momentum")]
    public float acceleration = 120f;
    public float maxSpeed = 20f;
    public float friction = 80f;
    public float backpedalSpeed = 10f;
    [Tooltip("Reference speed for animation playback speed (prevents skating effect)")]
    public float baseRunSpeed = 8f;

    [Header("Vault Jump Settings")]
    public float jumpForce = 24f;
    public float hurdleBoost = 5f;
    public float quickFallGravity = 10f;
    public float normalGravity = 5f;
    public float jumpCooldownTime = 0.1f;
    public float airControl = 2f;
    public float coyoteTime = 0.15f;
    private float coyoteCounter;
    private float jumpCooldownTimer;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    [SerializeField] private bool isGrounded;

    [Header("Abilities")]
    public Transform stiffArmPoint;
    public float stiffArmRange = 1.5f;
    public float stiffArmBaseForce = 20f;
    public float speedPushMultiplier = 0.8f;

    [Header("Juke Settings")]
    public float jukeDuration = 0.3f;
    public float jukeCooldown = 1f;
    public Color jukeColor = new(1, 1, 1, 0.5f);
    private float jukeTimer;

    [Header("States")]
    public bool isStiffArming = false;
    public bool isSpinning = false;
    public bool isJuking = false;
    public bool isProne = false;
    public bool isDashing = false;

    [Header("Package & Fumbles")]
    public bool hasPackage = true;
    public float baseFumbleChance = 0.05f;
    public GameObject packageObject;

    [Header("Attachments")]
    public int attachmentCount = 0;
    public Transform attachmentPoint;
    private readonly List<GameObject> attachedEnemies = new();
    private float tackleDebuffTimer = 0f;

    [Header("Visuals & Audio")]
    public GameObject bloodSplatterPrefab;
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
    private float proneTimer;
    private Color normalColor = Color.white;

    public bool ignoreGameManagerForTesting = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();

        rb.gravityScale = normalGravity;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (packageObject != null)
        {
            if (packageObject.TryGetComponent<Collider2D>(out var packColl))
            {
                Physics2D.IgnoreCollision(playerCollider, packColl, true);
            }
        }
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
    }

    void FixedUpdate()
    {
        if (isProne || isSpinning) return; // Physics controlled by abilities/state
        HandleMovement();
        if (tackleDebuffTimer > 0) tackleDebuffTimer -= Time.fixedDeltaTime;
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && coyoteCounter > 0 && jumpCooldownTimer <= 0)
        {
            PerformHurdleJump();
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isSpinning) StartCoroutine(PerformSpinMove());
        if (Input.GetKeyDown(KeyCode.E) && !isStiffArming) PerformStiffArm();
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isJuking && jukeTimer <= 0) StartCoroutine(PerformJuke());
    }

    private void HandleMovement()
    {
        float penalty = (attachmentCount * 2.5f) + (tackleDebuffTimer > 0 ? 8f : 0f);
        // Symmetric speed handling
        float currentMax = (horizontalInput < 0) ? backpedalSpeed : maxSpeed;
        float targetSpeed = horizontalInput * Mathf.Max(2f, currentMax - penalty);

        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate;

        if (isGrounded) accelRate = (Mathf.Abs(targetSpeed) > 0.1f) ? acceleration : friction;
        else accelRate = airControl;

        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        if (!isSpinning && horizontalInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        }
    }

    private void PerformHurdleJump()
    {
        float jumpPenalty = attachmentCount * 1.5f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * (jumpForce - jumpPenalty), ForceMode2D.Impulse);

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            rb.AddForce(Vector2.right * horizontalInput * hurdleBoost, ForceMode2D.Impulse);
        }

        coyoteCounter = 0;
        jumpCooldownTimer = jumpCooldownTime;
        PlaySound(jumpSfx);
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = quickFallGravity;
        else rb.gravityScale = normalGravity;
    }

    private void PerformStiffArm()
    {
        StartCoroutine(StiffArmRoutine());
        anim.SetTrigger("StiffArmTrigger");

        Collider2D[] enemies = Physics2D.OverlapCircleAll(stiffArmPoint.position, stiffArmRange);
        bool hitSomething = false;

        foreach (var enemyCol in enemies)
        {
            if (enemyCol.CompareTag("Enemy"))
            {
                if (enemyCol.TryGetComponent<EnemyAI>(out var enemyScript) && !enemyScript.isKnockedBack)
                {
                    float momentumBonus = Mathf.Abs(rb.linearVelocity.x) * speedPushMultiplier;
                    Vector2 dir = new Vector2(transform.localScale.x, 0.2f).normalized;

                    enemyScript.TakeHit(stiffArmBaseForce + momentumBonus, dir, false);
                    SpawnBloodSplatter(enemyCol.transform.position);
                    hitSomething = true;
                }
            }
        }

        if (hitSomething)
        {
            impulseSource.GenerateImpulse(0.5f);
            PlaySound(impactSfx);
        }
        else
        {
            PlaySound(stiffArmSfx);
        }
    }

    private IEnumerator StiffArmRoutine()
    {
        isStiffArming = true;
        yield return new WaitForSeconds(0.3f);
        isStiffArming = false;
    }

    private IEnumerator PerformJuke()
    {
        isJuking = true;
        jukeTimer = jukeCooldown;
        PlaySound(jukeSfx);
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

        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        // Invincibility ON
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        // Movement Speed & Direction
        float spinMoveForce = 12f; // Covers distance
        float facingDir = transform.localScale.x > 0 ? 1 : -1;

        if (attachmentCount == 0)
        {
            // Apply constant velocity for traversal
            rb.linearVelocity = new Vector2(facingDir * spinMoveForce, rb.linearVelocity.y);

            // AOE Push
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 2.5f);
            foreach (var col in nearby)
            {
                if (col.CompareTag("Enemy"))
                {
                    if (col.TryGetComponent<EnemyAI>(out var enemyScript))
                    {
                        enemyScript.TakeHit(10f, (col.transform.position - transform.position).normalized, true);
                    }
                }
            }
        }
        else
        {
            // Shed Enemy (Stop movement to emphasize the shed)
            rb.linearVelocity = Vector2.zero;

            GameObject drop = attachedEnemies[0];
            attachedEnemies.RemoveAt(0);
            attachmentCount--;
            UpdateUI();

            drop.transform.SetParent(null);

            // Fix Z-Depth so enemy doesn't vanish
            Vector3 dropPos = drop.transform.position;
            dropPos.z = 0;
            drop.transform.position = dropPos;

            drop.GetComponent<Collider2D>().enabled = true;
            if (drop.TryGetComponent<Rigidbody2D>(out var erb)) { erb.simulated = true; }
            if (drop.TryGetComponent<EnemyAI>(out var script))
            {
                script.enabled = true;
                script.TakeHit(15f, Vector2.up, true);
            }
        }

        // FLIP FLOP VISUALS
        float spinDuration = 0.4f;
        float flipInterval = 0.05f;
        float elapsed = 0;
        while (elapsed < spinDuration)
        {
            transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
            yield return new WaitForSeconds(flipInterval);
            elapsed += flipInterval;
        }

        // Restore Orientation
        if (horizontalInput != 0) transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        else transform.localScale = new Vector3(facingDir, 1, 1);

        // Invincibility OFF
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        isSpinning = false;
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
    }

    public void ProcessFumble(float extraRisk = 0)
    {
        if (!hasPackage || isSpinning || isJuking) return;
        float chance = baseFumbleChance + (attachmentCount * 0.15f) + extraRisk;
        if (Random.value < chance)
        {
            hasPackage = false;

            Package pkgScript = null;
            if (packageObject != null) packageObject.TryGetComponent(out pkgScript);
            else TryGetComponent(out pkgScript);

            if (pkgScript != null) pkgScript.SetHeld(false);

            impulseSource.GenerateImpulse(1.5f);
            PlaySound(fumbleSfx);
            Debug.Log("FUMBLE!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasPackage && other.CompareTag("Package"))
        {
            if (other.TryGetComponent<Package>(out var pkg))
            {
                hasPackage = true;
                pkg.SetHeld(true, attachmentPoint);
                PlaySound(jumpSfx);
            }
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", currentSpeed);
        float multiplier = Mathf.Clamp(currentSpeed / baseRunSpeed, 0.5f, 3f);
        anim.SetFloat("RunMultiplier", multiplier);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isProne", isProne);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (isGrounded && CameraController.Instance) CameraController.Instance.SetPlayerGrounded(true);
        if (!isGrounded && CameraController.Instance) CameraController.Instance.SetPlayerGrounded(false);
    }

    private void UpdateUI()
    {
        if (GameManager.Instance) GameManager.Instance.UpdateAttachmentCount(attachmentCount);
    }

    private void SpawnBloodSplatter(Vector2 pos)
    {
        if (bloodSplatterPrefab) Instantiate(bloodSplatterPrefab, new Vector3(pos.x, pos.y, 10), Quaternion.identity);
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip && audioSource) audioSource.PlayOneShot(clip, volume);
    }

    private void OnDrawGizmosSelected()
    {
        if (stiffArmPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(stiffArmPoint.position, stiffArmRange);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!isSpinning && col.gameObject.CompareTag("Enemy"))
        {
            if (col.gameObject.TryGetComponent<EnemyAI>(out var enemy))
            {
                if (isStiffArming)
                {
                    float force = stiffArmBaseForce + (Mathf.Abs(rb.linearVelocity.x) * speedPushMultiplier);
                    impulseSource.GenerateImpulse(force * 0.05f);
                    Vector2 pushDir = new Vector2(transform.localScale.x, 0.3f).normalized;
                    enemy.TakeHit(force, pushDir, isDashing);
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
    }
}