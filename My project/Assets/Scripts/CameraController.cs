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

    [Header("Framing Settings")]
    [Tooltip("Keep player in left third (-0.2). Set Y to ~0.25 for a platformer baseline.")]
    public Vector2 screenBias = new Vector2(-0.2f, 0.25f);

    [Tooltip("Dead zone allows jumping without moving camera. 0.25 Y is usually good.")]
    public Vector2 deadZoneSize = new Vector2(0.1f, 0.25f);

    [Header("Look Down / Fall Preview")]
    [Tooltip("How far (in Unity Units) to pan down when falling or holding Down.")]
    public float lookDownOffset = -5f;
    [Tooltip("How fast the camera shifts focus.")]
    public float lookShiftSpeed = 2f;

    private float currentYOffset = 0f;
    private bool isPlayerGrounded = false;

    void Awake()
    {
        Instance = this;
        globalImpulseSource = GetComponent<CinemachineImpulseSource>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerRb = player.GetComponent<Rigidbody2D>();

        if (virtualCamera != null)
        {
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
            ApplyCameraSettings();
        }
    }

    void LateUpdate()
    {
        if (playerRb == null || positionComposer == null) return;

        // --- LOOK DOWN LOGIC ---
        // 1. Manual Look Down (Input)
        bool manualLookDown = Input.GetAxisRaw("Vertical") < -0.5f;

        // 2. Automatic Fall Look Down (Velocity)
        bool falling = playerRb.linearVelocity.y < -8f; // Only trigger on fast falls

        float targetOffset = 0f;

        if (manualLookDown || falling)
        {
            targetOffset = lookDownOffset;
        }
        else if (isPlayerGrounded && Mathf.Abs(playerRb.linearVelocity.y) < 0.1f)
        {
            // If grounded and still, reset offset to 0 (Snap back to normal framing)
            targetOffset = 0f;
        }

        // Smoothly interpolate the offset
        currentYOffset = Mathf.Lerp(currentYOffset, targetOffset, Time.deltaTime * lookShiftSpeed);

        // Apply to TargetOffset (Moves the camera focus point without changing screen bounds)
        positionComposer.TargetOffset = new Vector3(0, currentYOffset, 0);
    }

    public void SetPlayerGrounded(bool grounded)
    {
        isPlayerGrounded = grounded;
    }

    public void ApplyCameraSettings()
    {
        if (positionComposer == null) return;
        var comp = positionComposer.Composition;

        comp.ScreenPosition = screenBias;
        comp.DeadZone.Size = deadZoneSize;
        comp.HardLimits.Size = new Vector2(0.9f, 0.9f); // Keep player on screen

        positionComposer.Composition = comp;

        // Damping: X is smooth (0.5), Y is tighter (0.2) to track landings, but deadzone handles the jitter
        positionComposer.Damping = new Vector3(0.5f, 0.2f, 0f);
    }

    public void Shake(float force = 1f)
    {
        if (globalImpulseSource) globalImpulseSource.GenerateImpulse(Vector3.one * force);
    }

    private void OnValidate()
    {
        if (virtualCamera != null && positionComposer == null)
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();

        if (positionComposer != null) ApplyCameraSettings();
    }
}