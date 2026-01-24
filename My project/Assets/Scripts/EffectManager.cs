using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    [Header("Movement VFX")]
    public GameObject jumpDustPrefab;
    public GameObject landDustPrefab;
    public GameObject footstepDustPrefab;
    public GameObject spinTrailPrefab;
    public GameObject jukeGhostPrefab;

    [Header("Combat VFX")]
    public GameObject bloodSplatterPrefab;
    public GameObject stiffArmImpactPrefab;
    public GameObject tackleImpactPrefab;
    public GameObject squishEffectPrefab;

    // This was the missing definition causing the error:
    public GameObject attachPoofPrefab;

    [Header("Telegraphs (Indicators)")]
    public GameObject tackleTelegraphPrefab;
    public GameObject stripTelegraphPrefab;

    // Fix: Changed from Property to Method so BruteEnemy can call it like TackleTelegraph(...)
    public void TackleTelegraph(Transform parent, float duration)
    {
        // Plays the telegraph slightly above the enemy (1.5 units up)
        PlayAttachedEffect(tackleTelegraphPrefab, parent, Vector3.up * 1.5f, duration);
    }

    [Header("Package VFX")]
    public GameObject packagePickupPrefab;
    public GameObject packageDropPrefab;
    public GameObject fumbleExplosionPrefab;

    [Header("UI & Game")]
    public GameObject touchdownConfettiPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayEffect(GameObject prefab, Vector3 position, float scale = 1f)
    {
        if (prefab == null) return;

        // Force Z to -5 so it renders in front of background elements
        Vector3 spawnPos = new Vector3(position.x, position.y, -5f);
        GameObject vfx = Instantiate(prefab, spawnPos, Quaternion.identity);
        vfx.transform.localScale *= scale;

        Destroy(vfx, 3f);
    }

    public void PlayAttachedEffect(GameObject prefab, Transform parent, Vector3 offset, float duration)
    {
        if (prefab == null) return;
        GameObject vfx = Instantiate(prefab, parent.position + offset, Quaternion.identity, parent);
        Destroy(vfx, duration);
    }
}