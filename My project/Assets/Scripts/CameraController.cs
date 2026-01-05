using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public CinemachineCamera virtualCamera;
    private CinemachinePositionComposer positionComposer;
    private CinemachineImpulseSource globalImpulseSource;
    private Rigidbody2D playerRb;

    [Header("Smooth Framing")]
    public float screenXBias = -0.2f; // Left-third
    public float screenYBias = 0.35f; // Slightly below center
    public Vector3 defaultDamping = new Vector3(0.8f, 0.5f, 0); // Smooth follow

    [Header("Dynamic Floor Preview")]
    [Tooltip("How far down to look when falling to see the ground.")]
    public float fallLookDownAmount = -4f;
    [Tooltip("How fast the camera shifts focus down/up.")]
    public float lookShiftSpeed = 3f;

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
            // Apply base settings immediately
            ApplyCameraSettings();
        }
    }

    void LateUpdate()
    {
        if (playerRb == null || positionComposer == null) return;

        // DYNAMIC LOOK DOWN:
        // If falling fast, look down. If running/jumping, look neutral/up.
        float targetYOffset = (playerRb.linearVelocity.y < -4f) ? fallLookDownAmount : 0f;

        // Smoothly interpolate the offset so it doesn't snap
        currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, Time.deltaTime * lookShiftSpeed);

        // Apply to TargetOffset (This moves the camera focus, not the screen position)
        positionComposer.TargetOffset = new Vector3(0, currentYOffset, 0);
    }

    public void ApplyCameraSettings()
    {
        if (positionComposer == null) return;
        var comp = positionComposer.Composition;

        comp.ScreenPosition = new Vector2(screenXBias, screenYBias);
        comp.DeadZone.Size = new Vector2(0.1f, 0.1f); // Small deadzone for responsiveness
        comp.HardLimits.Size = new Vector2(0.8f, 0.8f);

        positionComposer.Composition = comp;
        positionComposer.Damping = defaultDamping;
    }

    public void Shake(float force = 1f)
    {
        if (globalImpulseSource) globalImpulseSource.GenerateImpulse(Vector3.one * force);
    }
}