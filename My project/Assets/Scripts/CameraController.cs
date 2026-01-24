using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Cinemachine References")]
    public CinemachineCamera virtualCamera;
    private CinemachinePositionComposer positionComposer;
    private CinemachineImpulseSource globalImpulseSource;
    private Rigidbody2D playerRb;

    [Header("Zoom & Framing")]
    [Tooltip("Default Zoom")]
    public float orthographicSize = 6f;

    [Header("Directional Framing (Hysteresis)")]
    [Tooltip("Screen X when moving Right (Standard). E.g. -0.2 (Player on Left)")]
    public float forwardBias = -0.2f;
    [Tooltip("Screen X when moving Left (Backpedal). E.g. 0.2 (Player on Right)")]
    public float backwardBias = 0.2f;
    [Tooltip("Speed required to switch camera sides.")]
    public float switchThreshold = 8f;
    [Tooltip("Time required moving in new direction before camera shifts.")]
    public float turnDelay = 0.5f;
    [Tooltip("How fast the camera pans to the new side.")]
    public float biasShiftSpeed = .75f;

    // State tracking
    private float currentXBias;
    private bool isFacingRight = true;
    private float turnTimer = 0f;
    private float targetSize;
    private float defaultScreenXBias;
    private bool isCraneView = false;

    [Header("Grounded Vertical Logic")]
    public float verticalDamping = 0.0f;
    public float verticalDeadZone = 0.3f;
    public float screenYBias = 0.2f;
    public float horizontalDamping = 0.5f;

    [Header("Look Down Logic")]
    public float lookDownOffset = -4f;
    public float lookShiftSpeed = 2f;
    private float currentYOffset = 0f;

    void Awake()
    {
        Instance = this;
        globalImpulseSource = GetComponent<CinemachineImpulseSource>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerRb = player.GetComponent<Rigidbody2D>();

        if (virtualCamera != null)
        {
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
            currentXBias = forwardBias;
            defaultScreenXBias = forwardBias;
            targetSize = orthographicSize;
            ApplyCameraSettings();
        }
    }

    void Update()
    {
        if (virtualCamera != null)
        {
            virtualCamera.Lens.OrthographicSize = Mathf.Lerp(virtualCamera.Lens.OrthographicSize, targetSize, Time.deltaTime * 2f);
        }

        if (!isCraneView)
        {
            HandleDirectionalFraming();
            HandleLookDown();
        }
    }

    private void HandleDirectionalFraming()
    {
        if (playerRb == null || positionComposer == null) return;

        float velocityX = playerRb.linearVelocity.x;
        bool tryingToSwitch = false;

        // Check if moving against current facing
        if (isFacingRight && velocityX < -switchThreshold)
        {
            tryingToSwitch = true;
            turnTimer += Time.deltaTime;
            if (turnTimer > turnDelay)
            {
                isFacingRight = false;
                turnTimer = 0f;
            }
        }
        else if (!isFacingRight && velocityX > switchThreshold)
        {
            tryingToSwitch = true;
            turnTimer += Time.deltaTime;
            if (turnTimer > turnDelay)
            {
                isFacingRight = true;
                turnTimer = 0f;
            }
        }

        // Reset timer if not consistently moving in new direction
        if (!tryingToSwitch) turnTimer = 0f;

        float targetX = isFacingRight ? forwardBias : backwardBias;
        currentXBias = Mathf.MoveTowards(currentXBias, targetX, biasShiftSpeed * Time.deltaTime);

        var comp = positionComposer.Composition;
        comp.ScreenPosition = new Vector2(currentXBias, screenYBias);
        positionComposer.Composition = comp;
    }

    private void HandleLookDown()
    {
        if (playerRb == null || positionComposer == null) return;

        float targetOffset = 0f;
        bool manualLookDown = Input.GetAxisRaw("Vertical") < -0.5f;
        bool fallingFast = playerRb.linearVelocity.y < -12f;

        if (manualLookDown || fallingFast) targetOffset = lookDownOffset;

        currentYOffset = Mathf.Lerp(currentYOffset, targetOffset, Time.deltaTime * lookShiftSpeed);
        positionComposer.TargetOffset = new Vector3(0, currentYOffset, 0);
    }

    public void SetCraneView(bool active)
    {
        isCraneView = active;
        if (active)
        {
            targetSize = 10f;
            currentXBias = -0.4f;
            if (positionComposer)
            {
                var comp = positionComposer.Composition;
                comp.ScreenPosition = new Vector2(currentXBias, screenYBias);
                positionComposer.Composition = comp;
            }
        }
        else
        {
            targetSize = orthographicSize;
            isFacingRight = true;
            currentXBias = forwardBias;
        }
    }

    public void ApplyCameraSettings()
    {
        if (positionComposer == null) return;

        var comp = positionComposer.Composition;
        comp.ScreenPosition = new Vector2(currentXBias, screenYBias);
        comp.DeadZone.Size = new Vector2(0.1f, verticalDeadZone);
        comp.HardLimits.Size = new Vector2(0.8f, 0.8f);

        positionComposer.Composition = comp;
        positionComposer.Damping = new Vector3(horizontalDamping, verticalDamping, 0f);
    }

    public void SetPlayerGrounded(bool grounded) { }

    public void Shake(float force = 1f)
    {
        if (globalImpulseSource) globalImpulseSource.GenerateImpulse(Vector3.one * force);
    }

    private void OnValidate()
    {
        if (virtualCamera != null && positionComposer == null)
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();

        if (positionComposer != null)
            ApplyCameraSettings();
    }
}