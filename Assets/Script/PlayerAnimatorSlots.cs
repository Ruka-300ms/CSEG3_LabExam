using UnityEngine;

public class PlayerAnimatorSlots : MonoBehaviour
{
    [Header("Drag Animation Clips Here")]
    [Tooltip("Drag your Idle animation clip here")]
    public AnimationClip idleClip;
    
    [Tooltip("Drag your Walk animation clip here")]
    public AnimationClip walkClip;
    
    [Tooltip("Drag your Run animation clip here")]
    public AnimationClip runClip;
    
    [Tooltip("Drag your Jump animation clip here")]
    public AnimationClip jumpClip;

    [Header("Component References (Auto-found if empty)")]
    public Animator animator;
    public Animation legacyAnimation;

    private AnimatorOverrideController overrideController;

    void Awake()
    {
        ConfigureClips();
    }

    void Start()
    {
        ConfigureClips();

        // Auto-find components on this object or child meshes (e.g. humanf_model)
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (legacyAnimation == null) legacyAnimation = GetComponentInChildren<Animation>();

        if (animator != null)
        {
            animator.applyRootMotion = false;

            // Automatically create dynamic AnimatorOverrideController if Controller is assigned
            if (animator.runtimeAnimatorController != null)
            {
                overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = overrideController;

                // Substitute state clips with your dragged inspector clips
                if (idleClip != null) overrideController["idle"] = idleClip;
                if (walkClip != null) overrideController["Walking"] = walkClip;
                if (runClip != null) overrideController["Run"] = runClip;
                if (jumpClip != null) overrideController["Jump"] = jumpClip;
            }

            foreach (var p in animator.parameters)
            {
                if (p.name == "isMoving") hasIsMoving = true;
                if (p.name == "isWalking") hasIsWalking = true;
                if (p.name == "isRunning") hasIsRunning = true;
                if (p.name == "isGrounded") hasIsGrounded = true;
            }
        }

        // Configure Legacy Animation component if used
        if (legacyAnimation != null)
        {
            if (idleClip != null) legacyAnimation.AddClip(idleClip, "Idle");
            if (walkClip != null) legacyAnimation.AddClip(walkClip, "Walk");
            if (runClip != null) legacyAnimation.AddClip(runClip, "Run");
            if (jumpClip != null) legacyAnimation.AddClip(jumpClip, "Jump");
        }
    }

    private bool hasIsMoving;
    private bool hasIsWalking;
    private bool hasIsRunning;
    private bool hasIsGrounded;

    private void ConfigureClips()
    {
        // Enforce looping on Idle, Walk, Run clips automatically
        SetClipLooping(idleClip, true);
        SetClipLooping(walkClip, true);
        SetClipLooping(runClip, true);
        SetClipLooping(jumpClip, false);
    }

    private void SetClipLooping(AnimationClip clip, bool shouldLoop)
    {
        if (clip != null)
        {
            clip.wrapMode = shouldLoop ? WrapMode.Loop : WrapMode.Once;
        }
    }

    public void UpdateAnimatorParameters(bool isMoving, bool isWalking, bool isRunning, bool isGrounded)
    {
        // 1. Update Mecanim Animator parameters
        if (animator != null)
        {
            if (hasIsMoving) animator.SetBool("isMoving", isMoving);
            if (hasIsWalking) animator.SetBool("isWalking", isWalking);
            if (hasIsRunning) animator.SetBool("isRunning", isRunning);
            if (hasIsGrounded) animator.SetBool("isGrounded", isGrounded);
        }

        // 2. Legacy Animation Fallback
        if (legacyAnimation != null)
        {
            if (!isGrounded)
            {
                if (jumpClip != null) legacyAnimation.CrossFade("Jump", 0.1f);
            }
            else if (isMoving)
            {
                if (isRunning && runClip != null) legacyAnimation.CrossFade("Run", 0.15f);
                else if (walkClip != null) legacyAnimation.CrossFade("Walk", 0.15f);
            }
            else
            {
                if (idleClip != null) legacyAnimation.CrossFade("Idle", 0.15f);
            }
        }
    }

    public void TriggerJump()
    {
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
        if (legacyAnimation != null && jumpClip != null)
        {
            legacyAnimation.Play("Jump");
        }
    }
}
