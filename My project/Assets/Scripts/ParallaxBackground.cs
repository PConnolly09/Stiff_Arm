using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private float length, startpos;
    public GameObject cam;
    public float parallaxEffect; // 1 = Still (Sky), 0 = Moves with Player (Foreground)

    void Start()
    {
        if (cam == null) cam = Camera.main.gameObject;
        startpos = transform.position.x;

        // Auto-detect sprite width for looping
        if (GetComponent<SpriteRenderer>() != null)
            length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        // 1. Calculate how far we have moved relative to the cam
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        float dist = (cam.transform.position.x * parallaxEffect);

        // 2. Move the background
        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // 3. Infinite Loop: If camera moved past the sprite's edge, snap it forward
        if (temp > startpos + length) startpos += length;
        else if (temp < startpos - length) startpos -= length;
    }
}