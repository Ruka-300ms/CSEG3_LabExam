using UnityEngine;

public class PlayerControlAnim : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpforce = 10f;
    private Rigidbody rb;

    private bool isGrounded;

    [Header("Animator (Mecanim) or Animation (Legacy)")]
    public Animator animator;
    public Animation anim; 

    [Header("Drag your Animation Clips here (If using Legacy Animation)")]
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip runClip;
    public AnimationClip jumpClip;

    private string currentAnim;

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
            rb.linearDamping = 2f; // Prevents slipping/sliding across floor when stopped
            // Prevent player capsule from tipping/rolling over
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            // Prevent tunneling through ground geometry
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Auto-find Animator if unassigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Auto-find legacy Animation if unassigned
        if (anim == null)
        {
            anim = GetComponentInChildren<Animation>();
        }

        // Load clips into legacy Animation component if present
        if (anim != null)
        {
            if (idleClip) anim.AddClip(idleClip, "Idle");
            if (walkClip) anim.AddClip(walkClip, "Walk");
            if (runClip) anim.AddClip(runClip, "Run");
            if (jumpClip) anim.AddClip(jumpClip, "Jump");
        }
    }

    void Update()
    {
        CheckGroundState();

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Check if movement keys are pressed
        bool isMoving = (x != 0 || z != 0);

        // Check if Shift is held for running/sprinting
        bool isRunning = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        // Choose move speed based on running state
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(move.x * currentSpeed, rb.linearVelocity.y, move.z * currentSpeed);
        }

        // Update Animator parameters (Mecanim)
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
            animator.SetBool("isWalking", isMoving && !isRunning);
            animator.SetBool("isRunning", isMoving && isRunning);
            animator.SetBool("isGrounded", isGrounded);
        }

        // Jump
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
        if (jumpPressed && isGrounded)
        {
            if (rb != null)
            {
                rb.AddForce(Vector3.up * jumpforce, ForceMode.Impulse);
            }
            isGrounded = false;
            PlayAnim("Jump");
        }
        else if (isGrounded) // Switch animations based on movement state
        {
            if (isMoving)
            {
                if (isRunning)
                    PlayAnim("Run");
                else
                    PlayAnim("Walk");
            }
            else
            {
                PlayAnim("Idle");
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

    // Helper function to switch animations smoothly without restarting every frame
    void PlayAnim(string animName)
    {
        if (animator != null)
        {
            if (animName == "Jump")
            {
                animator.SetTrigger("Jump");
            }
        }

        if (anim != null && currentAnim != animName)
        {
            anim.CrossFade(animName, 0.15f); // Smooth blend transition
            currentAnim = animName;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckGrounded(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        CheckGrounded(collision);
    }

    void CheckGrounded(Collision collision)
    {
        // Ground detection check
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("ColorProp") || collision.contacts.Length > 0)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.4f)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }
}