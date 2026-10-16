using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Autonomous base class for AI enemies.
/// Completely separated from GameManager - functions as an independent entity.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Target player transform. Auto-found at runtime if not assigned.")]
    public Transform player;

    [Header("Melee Combat")]
    [Tooltip("Distance at which melee damage can be dealt to the player.")]
    public float meleeRange = 1.8f;

    [Tooltip("Damage dealt per melee hit.")]
    public float meleeDamage = 15f;

    [Tooltip("Cooldown between melee attacks in seconds.")]
    public float attackCooldown = 1.0f;

    [Header("Perception")]
    [Tooltip("Eye height above enemy origin for line of sight and cone checks.")]
    public float eyeHeight = 0.6f;

    [Tooltip("Layer mask used for line of sight obstacle raycasts.")]
    public LayerMask obstacleMask = ~0;

    [Header("Health Bar Setup")]
    [Tooltip("Vertical height for the procedural floating health bar above the cube.")]
    public float healthBarHeight = 1.5f;

    // Component references
    protected Rigidbody rb;
    protected BoxCollider boxCollider;
    protected EnemyHealth enemyHealth;
    protected PlayerHealth playerHealth;
    protected Renderer enemyRenderer;

    // Combat state
    protected float nextAttackTime = 0f;
    protected Vector3 spawnPosition;
    private Color originalColor = Color.white;
    private bool isFlashing = false;

    protected virtual void Awake()
    {
        spawnPosition = transform.position;

        // Configure Rigidbody for reliable physics and no tumbling
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Configure BoxCollider
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider>();

        // Renderer
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null && enemyRenderer.material != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        // Tag as Enemy so bullet and collision systems identify it properly
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
        {
            gameObject.tag = "Enemy";
        }
    }

    protected virtual void Start()
    {
        EnsurePlayerReference();
        EnsureHealthComponent();
    }

    /// <summary>
    /// Finds the player independently without any dependency on GameManager.
    /// </summary>
    public void EnsurePlayerReference()
    {
        if (player != null && playerHealth != null) return;

        // 1. Try finding by Player tag
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerHealth = pObj.GetComponentInParent<PlayerHealth>() ?? pObj.GetComponentInChildren<PlayerHealth>();
        }

        // 2. Fallback to PlayerController
        if (player == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
                playerHealth = pc.GetComponentInParent<PlayerHealth>() ?? pc.GetComponentInChildren<PlayerHealth>();
            }
        }

        // 3. Fallback to PlayerHealth directly
        if (player == null)
        {
            PlayerHealth ph = FindObjectOfType<PlayerHealth>();
            if (ph != null)
            {
                player = ph.transform;
                playerHealth = ph;
            }
        }
    }

    /// <summary>
    /// Ensures EnemyHealth is attached and creates a floating health bar if missing.
    /// </summary>
    private void EnsureHealthComponent()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = gameObject.AddComponent<EnemyHealth>();
        }

        // Check if a health slider is already wired
        if (enemyHealth.healthBar == null)
        {
            Slider existingSlider = GetComponentInChildren<Slider>(true);
            if (existingSlider != null)
            {
                enemyHealth.healthBar = existingSlider;
            }
            else
            {
                // Create procedural world-space billboard health bar
                CreateProceduralHealthBar();
            }
        }
    }

    private void CreateProceduralHealthBar()
    {
        GameObject pivotObj = new GameObject("HealthBarPivot");
        pivotObj.transform.SetParent(transform);
        pivotObj.transform.localPosition = new Vector3(0f, healthBarHeight, 0f);

        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(pivotObj.transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 15f);
        canvasRect.localScale = Vector3.one * 0.01f;
        canvasRect.localPosition = Vector3.zero;

        // Dark background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Foreground fill slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(sliderObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = Color.green;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.targetGraphic = fillImg;
        slider.fillRect = fillRect;
        slider.maxValue = enemyHealth.maxHealth;
        slider.value = enemyHealth.currentHealth;

        enemyHealth.healthBar = slider;
        enemyHealth.billboardHealthBar = true;
    }

    /// <summary>
    /// Checks unobstructed line of sight from enemy eye position to target.
    /// </summary>
    public bool HasLineOfSight(Vector3 targetCenter, float maxDistance)
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toTarget = targetCenter - eyePos;
        float distance = toTarget.magnitude;

        if (distance > maxDistance || distance < 0.01f) return false;

        Ray ray = new Ray(eyePos, toTarget.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Reached player directly
            if (player != null && (hit.transform == player || hit.transform.IsChildOf(player)))
            {
                return true;
            }

            // Hit PlayerHealth in hierarchy
            PlayerHealth ph = hit.collider.GetComponentInParent<PlayerHealth>();
            if (ph != null) return true;

            // Hit self/child
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                return true;
            }

            // Obstructed by obstacle/wall/floor
            return false;
        }

        // No obstruction hit within distance
        return true;
    }

    /// <summary>
    /// Attempts to deal melee damage to the player if within meleeRange and cooldown has elapsed.
    /// </summary>
    public virtual bool TryMeleeAttack()
    {
        if (player == null) EnsurePlayerReference();
        if (player == null) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > meleeRange) return false;

        if (Time.time < nextAttackTime) return false;

        nextAttackTime = Time.time + attackCooldown;

        if (playerHealth == null)
        {
            playerHealth = player.GetComponentInParent<PlayerHealth>() ?? player.GetComponentInChildren<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(meleeDamage);
            StartCoroutine(FlashAttackFeedback());
            return true;
        }

        return false;
    }

    /// <summary>
    /// Visual feedback for attacks: flashes white/bright red momentarily.
    /// </summary>
    protected IEnumerator FlashAttackFeedback()
    {
        if (isFlashing || enemyRenderer == null) yield break;
        isFlashing = true;

        Color flashColor = Color.white;
        enemyRenderer.material.color = flashColor;

        yield return new WaitForSeconds(0.12f);

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = originalColor;
        }
        isFlashing = false;
    }

    /// <summary>
    /// Turns smoothly towards the target position on the horizontal XZ plane.
    /// </summary>
    protected void RotateTowards(Vector3 targetPos, float turnSpeed)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    /// <summary>
    /// Moves forward using physics (linearVelocity) while preserving gravity.
    /// </summary>
    protected void MoveForward(float speed)
    {
        if (rb == null) return;
        Vector3 forwardVelocity = transform.forward * speed;
        forwardVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = forwardVelocity;
    }

    /// <summary>
    /// Halts horizontal movement while maintaining gravity.
    /// </summary>
    protected void StopHorizontalMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }
}
