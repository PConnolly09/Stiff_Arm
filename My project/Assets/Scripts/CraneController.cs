using UnityEngine;

public class CraneController : MonoBehaviour
{
    public enum ControlMode { MoveStructure, OperateCart }

    [Header("Hierarchy References")]
    public Transform gantry;
    public Transform cart;
    public Transform grabber;
    public SpriteRenderer ropeLeft;
    public SpriteRenderer ropeRight;

    [Header("Settings")]
    public ControlMode currentMode = ControlMode.OperateCart;
    public bool isPlayerControlling = false;
    public float gantrySpeed = 4f;
    public float cartSpeed = 4f;
    public float hoistSpeed = 3f;

    [Header("Limits (Local Space)")]
    public float gantryTravelDist = 10f;
    public float cartMinLocalX = -4f;
    public float cartMaxLocalX = 4f;
    public float grabberMinLocalY = -5f;
    public float grabberMaxLocalY = -1f;

    [Header("Claw Logic")]
    public Transform grabPoint;
    public float grabRadius = 0.8f;
    public LayerMask grabbableLayer;
    private FixedJoint2D grabJoint;
    private GameObject currentObject;

    // Internal state
    private float gantryStartX;
    private float grabberInitialLocalX; // FIX: Stores initial X alignment
    private PlayerController playerRef;
    private Rigidbody2D playerRb;
    private RigidbodyType2D originalPlayerBodyType;

    void Awake()
    {
        if (gantry) gantryStartX = gantry.position.x;
        // FIX: Capture where the grabber is sitting relative to the cart
        if (grabber) grabberInitialLocalX = grabber.localPosition.x;

        SetupKinematic(gantry);
        SetupKinematic(cart);
        SetupKinematic(grabber);
    }

    private void SetupKinematic(Transform t)
    {
        if (!t) return;
        Rigidbody2D rb = t.GetComponent<Rigidbody2D>();
        if (!rb) rb = t.gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.mass = 1000f;
    }

    void Update()
    {
        UpdateRopes();

        if (!isPlayerControlling) return;

        // Mode Switching
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentMode = (currentMode == ControlMode.MoveStructure)
                ? ControlMode.OperateCart
                : ControlMode.MoveStructure;
        }

        HandleMovement();
    }

    void HandleMovement()
    {
        float hInput = Input.GetAxisRaw("Horizontal");
        float vInput = Input.GetAxisRaw("Vertical");

        if (currentMode == ControlMode.MoveStructure)
        {
            if (gantry != null && Mathf.Abs(hInput) > 0.01f)
            {
                float nextX = gantry.position.x + (hInput * gantrySpeed * Time.deltaTime);
                nextX = Mathf.Clamp(nextX, gantryStartX - gantryTravelDist, gantryStartX + gantryTravelDist);
                gantry.position = new Vector3(nextX, gantry.position.y, gantry.position.z);
            }
        }
        else
        {
            // Cart Movement
            if (cart != null && Mathf.Abs(hInput) > 0.01f)
            {
                float nextLocalX = cart.localPosition.x + (hInput * cartSpeed * Time.deltaTime);
                nextLocalX = Mathf.Clamp(nextLocalX, cartMinLocalX, cartMaxLocalX);
                cart.localPosition = new Vector3(nextLocalX, cart.localPosition.y, cart.localPosition.z);
            }

            // Grabber Movement
            if (grabber != null && Mathf.Abs(vInput) > 0.01f)
            {
                float nextLocalY = grabber.localPosition.y + (vInput * hoistSpeed * Time.deltaTime);
                nextLocalY = Mathf.Clamp(nextLocalY, grabberMinLocalY, grabberMaxLocalY);

                // FIX: Use the captured initial X instead of forcing 0
                grabber.localPosition = new Vector3(grabberInitialLocalX, nextLocalY, grabber.localPosition.z);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (currentObject) Release();
                else AttemptGrab();
            }
        }
    }

    void UpdateRopes()
    {
        if (!cart || !grabber || !ropeLeft || !ropeRight) return;
        float height = Mathf.Abs(Mathf.Abs(cart.position.y) - Mathf.Abs(grabber.position.y));
        Vector2 newSizeL = new (ropeLeft.size.x, height);
        Vector2 newSizeR = new (ropeRight.size.x, height);
        ropeLeft.size = newSizeL;
        ropeRight.size = newSizeR;
    }

    void AttemptGrab()
    {
        Collider2D hit = Physics2D.OverlapCircle(grabPoint.position, grabRadius, grabbableLayer);
        if (hit && hit.CompareTag("Grabbable"))
        {
            Debug.Log("Grabbed: " + hit.gameObject.name);
            currentObject = hit.gameObject;
            grabJoint = grabber.gameObject.AddComponent<FixedJoint2D>();
            grabJoint.connectedBody = hit.attachedRigidbody;
            grabJoint.dampingRatio = 1f;
            grabJoint.frequency = 0;
        }
    }

    void Release()
    {
        Debug.Log("Released: " + (currentObject ? currentObject.name : "null"));
        if (grabJoint) { Destroy(grabJoint); grabJoint = null; }
        if (currentObject)
        {
            Rigidbody2D objRb = currentObject.GetComponent<Rigidbody2D>();
            if (objRb) objRb.WakeUp();
        }
        currentObject = null;
    }

    public void EnterControl(PlayerController player)
    {
        isPlayerControlling = true;
        playerRef = player;
        player.enabled = false;

        if (player.TryGetComponent<Rigidbody2D>(out playerRb))
        {
            originalPlayerBodyType = playerRb.bodyType;
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
        }
        currentMode = ControlMode.OperateCart;
    }

    public void ExitControl()
    {
        isPlayerControlling = false;
        if (playerRef)
        {
            if (playerRb)
            {
                playerRb.bodyType = originalPlayerBodyType;
                playerRb.WakeUp();
            }
            playerRef.enabled = true;
            playerRef = null;
            playerRb = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (grabPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(grabPoint.position, grabRadius);
        }
    }
}