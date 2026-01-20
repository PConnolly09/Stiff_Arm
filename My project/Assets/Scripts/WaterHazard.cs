using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WaterHazard : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject splashEffect; // Drag your water splash particle prefab here if you have one

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Visual Feedback
        if (splashEffect != null)
        {
            // Spawn splash at the object's entry point
            Vector3 spawnPos = other.transform.position;
            spawnPos.z = -5f; // Ensure it renders in front of the background
            Instantiate(splashEffect, spawnPos, Quaternion.identity);
        }

        // 2. Logic based on what fell in
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player fell into water!");

            // Immediate penalty: Lose a Down and Restart
            if (GameManager.Instance)
            {
                GameManager.Instance.UseDown();
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            // Enemies drown instantly
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Package"))
        {
            // If the ball falls in, it's a turnover/loss of down
            if (GameManager.Instance)
            {
                GameManager.Instance.UseDown();
            }
        }
        else if (other.CompareTag("Grabbable")) // For Crane containers or debris
        {
            // Heavy objects sink and are destroyed
            Destroy(other.gameObject, 0.5f);
        }
    }
}