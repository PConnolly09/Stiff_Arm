using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("0 = Static (Ground), 1 = Moves with Camera (Sky/Far away)")]
    [Range(0f, 1f)] public float parallaxFactor;

    [Tooltip("Check this if this layer should repeat infinitely (like sky or crowd)")]
    public bool infiniteLoop = true;

    private Transform cam;
    private Vector3 startPos;
    private float length;

    void Start()
    {
        cam = Camera.main.transform;
        startPos = transform.position;

        // Calculate the length of the sprite for looping
        if (GetComponent<SpriteRenderer>())
        {
            length = GetComponent<SpriteRenderer>().bounds.size.x;
        }
    }

    void LateUpdate() // Use LateUpdate to move AFTER the camera moves to prevent jitter
    {
        // 1. Distance: How far the camera has moved from the start
        float temp = (cam.position.x * (1 - parallaxFactor));
        float dist = (cam.position.x * parallaxFactor);

        // 2. Move the layer
        transform.position = new Vector3(startPos.x + dist, transform.position.y, transform.position.z);

        // 3. Infinite Loop Logic (The "Snap")
        if (infiniteLoop)
        {
            if (temp > startPos.x + length) startPos.x += length;
            else if (temp < startPos.x - length) startPos.x -= length;
        }
    }
}