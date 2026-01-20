using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    [Header("Movement Effects")]
    public GameObject jumpDustPrefab;
    public GameObject landDustPrefab;
    public GameObject spinTrailPrefab; // Use a Trail Renderer or particles
    public GameObject jukeGhostPrefab; // Create a sprite fade effect

    [Header("Combat Effects")]
    public GameObject bloodSplatterPrefab;
    public GameObject stiffArmImpactPrefab; // Spark/Hit flash
    public GameObject tackleImpactPrefab;   // Dust explosion
    public GameObject attachPoofPrefab;     // Small smoke puff
    public GameObject telegraphPrefab;      // Exclamation mark or flash

    [Header("Game Effects")]
    public GameObject fumbleExplosionPrefab;
    public GameObject touchdownConfettiPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayEffect(GameObject prefab, Vector3 position, float scale = 1f)
    {
        if (prefab == null) return;

        // Force Z to -5 to ensure it renders in front of background
        Vector3 spawnPos = new (position.x, position.y, -5f);
        GameObject vfx = Instantiate(prefab, spawnPos, Quaternion.identity);
        vfx.transform.localScale *= scale;

        // Auto-destroy is handled by the ParticleSystem "Stop Action" setting
        // But we can add a failsafe:
        Destroy(vfx, 3f);
    }

    // Helper for telegraphing (needs to follow the enemy)
    public void ShowTelegraph(Transform target, float duration)
    {
        if (telegraphPrefab == null) return;
        GameObject icon = Instantiate(telegraphPrefab, target.position + Vector3.up, Quaternion.identity, target);
        Destroy(icon, duration);
    }
}