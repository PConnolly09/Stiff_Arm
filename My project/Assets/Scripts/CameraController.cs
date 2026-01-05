using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public CinemachineCamera virtualCamera;
    private CinemachinePositionComposer positionComposer;
    private CinemachineImpulseSource globalImpulseSource;

    [Header("Improved Visibility")]
    // Lower screen bias means more of the ground is visible below the player
    public float screenYBias = 0.4f;
    // Tighter dead zone means camera follows drops faster
    public float verticalDeadZone = 0.2f;

    void Awake()
    {
        Instance = this;
        globalImpulseSource = GetComponent<CinemachineImpulseSource>();
        if (virtualCamera != null)
        {
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
            ApplyCameraSettings();
        }
    }

    public void ApplyCameraSettings()
    {
        if (positionComposer == null) return;
        var comp = positionComposer.Composition;

        // X = -0.3 puts player on left side
        comp.ScreenPosition = new Vector2(-0.3f, screenYBias);
        comp.DeadZone.Size = new Vector2(0.1f, verticalDeadZone);
        comp.HardLimits.Size = new Vector2(0.8f, 0.8f);

        positionComposer.Composition = comp;
        // Low Y damping prevents "stuck high" feeling
        positionComposer.Damping = new Vector3(0.5f, 0.1f, 0f);
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