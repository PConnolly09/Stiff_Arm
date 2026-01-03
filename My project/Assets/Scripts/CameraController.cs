using UnityEngine;
using Unity.Cinemachine; // Correct namespace for Cinemachine v3

/// <summary>
/// Enhanced bridge for Cinemachine. 
/// Optimized for "Grounded" gameplay: stays low during small jumps,
/// follows high jumps, and settles with the floor at the bottom.
/// Updated for Cinemachine v3 using CinemachinePositionComposer based on specific API docs.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Cinemachine References")]
    public CinemachineCamera virtualCamera;
    private CinemachinePositionComposer positionComposer;
    private CinemachineImpulseSource globalImpulseSource;

    [Header("Grounded Vertical Logic")]
    [Tooltip("Set to 0 for instant, frame-perfect vertical tracking to eliminate all floatiness.")]
    public float verticalDamping = 0.0f;

    [Tooltip("Minimal deadzone ensures the camera starts dropping the instant you fall.")]
    public float verticalDeadZone = 0.05f;

    [Tooltip("Vertical screen position. 0 is center, 0.5 is bottom. ~0.25 gives more floor visibility than 0.3.")]
    public float screenYBias = 0.25f;

    [Header("Horizontal Framing")]
    [Tooltip("Horizontal screen position. 0 is center, -0.5 is left edge. ~-0.17 is left-third.")]
    public float screenXBias = -0.17f;
    [Tooltip("Damping on X keeps the camera smooth horizontally.")]
    public float horizontalDamping = 0.3f;

    void Awake()
    {
        Instance = this;
        globalImpulseSource = GetComponent<CinemachineImpulseSource>();

        if (virtualCamera != null)
        {
            // In Cinemachine v3, position and framing is handled by the PositionComposer component
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
            ApplyCameraSettings();
        }
    }

    /// <summary>
    /// Synchronizes the script settings with the Cinemachine Camera using modern v3 syntax.
    /// </summary>
    public void ApplyCameraSettings()
    {
        if (positionComposer == null) return;

        // In Cinemachine v3, framing settings are stored in the Composition struct.
        // Since it's a struct, we must modify a copy and re-assign it.
        var comp = positionComposer.Composition;

        // 1. Screen Position: 0 is center, -0.5 to 0.5 are edges.
        comp.ScreenPosition = new Vector2(screenXBias, screenYBias);

        // 2. Dead Zone: Size property takes a Vector2 (Width, Height).
        // Minimal height ensures the camera is snapped to your vertical position almost instantly.
        comp.DeadZone.Size = new Vector2(0.1f, verticalDeadZone);

        // 3. Hard Limits: Replaces the 'Soft' settings. Size property takes a Vector2.
        comp.HardLimits.Size = new Vector2(0.8f, 0.8f);

        // Re-assign the modified struct back to the composer
        positionComposer.Composition = comp;

        // 4. Damping: Direct property on the composer.
        // Setting Y damping to 0 means the camera position is calculated instantly without lag.
        positionComposer.Damping = new Vector3(horizontalDamping, verticalDamping, 0f);
    }

    /// <summary>
    /// Utility to trigger a shake from anywhere using Cinemachine's Impulse system.
    /// </summary>
    public void Shake(float force = 1f)
    {
        if (globalImpulseSource != null)
        {
            globalImpulseSource.GenerateImpulse(Vector3.one * force);
        }
    }

    private void OnValidate()
    {
        // Check for the component if virtualCamera is assigned but composer isn't cached yet
        if (virtualCamera != null && positionComposer == null)
        {
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
        }

        if (positionComposer != null)
            ApplyCameraSettings();
    }
}