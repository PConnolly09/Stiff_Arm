using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    [Tooltip("How much this layer moves. Higher = Closer to camera.")]
    public float parallaxStrength = 10f;
    public bool invert = false;

    private Vector2 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        // Normalize mouse coordinates (0 to 1) -> (-0.5 to 0.5)
        float x = (mousePos.x / Screen.width) - 0.5f;
        float y = (mousePos.y / Screen.height) - 0.5f;

        if (invert) { x = -x; y = -y; }

        Vector2 offset = new Vector2(x, y) * parallaxStrength;

        // Smoothly move towards the target
        transform.position = Vector3.Lerp(transform.position, startPos + offset, Time.deltaTime * 5f);
    }
}