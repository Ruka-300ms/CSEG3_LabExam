using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float jumpHeight = 2.0f;
    public float gravity = -19.62f;
    public float kickStrength = 8f;
    public float upwardLift = 2f;

    private CharacterController charController;
    private Animator animator;
    private PlayerAnimatorSlots animatorSlots;
    private Vector3 currentVelocity;
    private bool isGrounded;

    void Start()
    {
        charController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        animatorSlots = GetComponentInChildren<PlayerAnimatorSlots>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    void Update()
    {
        // 1. Ground Check (Airborne if moving upward)
        if (currentVelocity.y > 0.1f)
        {
            isGrounded = false;
        }
        else
        {
            isGrounded = (charController != null && charController.isGrounded);
            if (!isGrounded)
            {
                // Raycast fallback if CharacterController.isGrounded flickers
                Vector3 rayOrigin = transform.position + Vector3.up * 0.15f;
                isGrounded = Physics.Raycast(rayOrigin, Vector3.down, 0.25f, ~0, QueryTriggerInteraction.Ignore);
            }
        }

        if (isGrounded && currentVelocity.y < 0f)
        {
            currentVelocity.y = -2f; // Stickiness to ground
        }

        // 2. Jump Input Check (Space bar or Jump button)
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
        if (jumpPressed && isGrounded)
        {
            currentVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isGrounded = false;

            if (animatorSlots != null)
            {
                animatorSlots.TriggerJump();
            }
            else
            {
                if (animator == null) animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                    animator.SetTrigger("Jump");
                }
            }
        }

        // 3. Horizontal Movement
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;
        bool isMoving = moveDirection.sqrMagnitude > 0.01f;
        bool isRunning = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // Apply gravity to vertical velocity
        currentVelocity.y += gravity * Time.deltaTime;

        // Combine horizontal move and vertical velocity
        Vector3 finalVelocity = moveDirection * currentSpeed;
        finalVelocity.y = currentVelocity.y;

        if (charController != null)
        {
            charController.Move(finalVelocity * Time.deltaTime);
        }

        // 4. Update Animator Parameters on humanf_model / PlayerAnimatorSlots
        if (animatorSlots != null)
        {
            animatorSlots.UpdateAnimatorParameters(isMoving, isMoving && !isRunning, isMoving && isRunning, isGrounded);
        }
        else
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetBool("isWalking", isMoving);
                animator.SetBool("isRunning", isRunning);
                animator.SetBool("isMoving", isMoving);
                animator.SetBool("isGrounded", isGrounded);
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            if (hit.gameObject.CompareTag("Ball"))
            {
                Vector3 pushDirection = hit.gameObject.transform.position - transform.position;
                pushDirection.y = 0f;
                pushDirection.Normalize();
                pushDirection.y = upwardLift;

                rb.AddForce(pushDirection * kickStrength);
            }
        }
    }
}