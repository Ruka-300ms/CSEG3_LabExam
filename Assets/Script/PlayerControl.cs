using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    //speed, jump height, rigidbody, groundchecker
    public float speed = 5f;
    public float jumpforce = 10f;
    private Rigidbody rb;

    private bool isGrounded;

    void Awake()
    {
        // 1. Ensure the root object has a valid Collider (auto-add CapsuleCollider if missing)
        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider == null)
        {
            CapsuleCollider cap = gameObject.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1f, 0f);
            cap.height = 2f;
            cap.radius = 0.4f;
            rootCollider = cap;
        }
        rootCollider.enabled = true;

        // 2. Disable all colliders on child objects (3D model meshes/bones)
        // to prevent internal physics collisions that push the player upward or break physics.
        Collider[] childColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in childColliders)
        {
            if (col != rootCollider && col.gameObject != gameObject)
            {
                col.enabled = false;
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Prevent player capsule from tipping/rolling over
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            // Prevent tunneling through ground geometry
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        CheckGroundState();

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpforce, ForceMode.Impulse);
                isGrounded = false;
            }
        }
    }

    private void CheckGroundState()
    {
        // Downward spherecast to detect ground below player's feet reliably
        Vector3 origin = transform.position + Vector3.up * 0.3f;
        if (Physics.SphereCast(origin, 0.25f, Vector3.down, out RaycastHit hit, 0.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform))
            {
                isGrounded = true;
                return;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("ColorProp"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("ColorProp"))
        {
            isGrounded = true;
        }
    }
}