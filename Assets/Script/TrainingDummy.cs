using UnityEngine;
using UnityEngine.UI;

public class TrainingDummy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Health Bar Layout")]
    [Tooltip("World-space height above the dummy pivot for the health bar")]
    public float healthBarHeight = 2.3f;
    public float healthBarWidth = 1.0f;
    public float healthBarThickness = 0.12f;

    private Image healthFillImage;
    private Transform healthBarPivot;
    private Camera mainCamera;

    void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        // Ensure the dummy has a collider for bullet hits
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.height = 2f;
            col.radius = 0.5f;
        }

        // Kinematic rigidbody so bullets can collide via OnCollisionEnter
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        CreateHealthBar();
    }

    // ── Procedural world-space health bar ──
    private void CreateHealthBar()
    {
        // Root pivot (billboarded in LateUpdate)
        GameObject pivotObj = new GameObject("HealthBarPivot");
        pivotObj.transform.SetParent(transform);
        pivotObj.transform.localPosition = new Vector3(0f, healthBarHeight, 0f);
        healthBarPivot = pivotObj.transform;

        // Canvas
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(pivotObj.transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        float pixelW = healthBarWidth * 100f;
        float pixelH = healthBarThickness * 100f;
        canvasRect.sizeDelta = new Vector2(pixelW, pixelH);
        canvasRect.localScale = Vector3.one * 0.01f;   // 1 pixel = 0.01 world units
        canvasRect.localPosition = Vector3.zero;

        // Dark background
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        StretchFill(bgRect);

        // Green → Red fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        healthFillImage = fillObj.AddComponent<Image>();
        healthFillImage.color = Color.green;
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        StretchFill(fillRect);
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── Billboard ──
    void LateUpdate()
    {
        if (healthBarPivot != null && mainCamera != null)
        {
            healthBarPivot.forward = mainCamera.transform.forward;
        }
    }

    // ── Damage ──
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0f) return; // already dead

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthBar();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage == null) return;
        float ratio = currentHealth / maxHealth;
        healthFillImage.fillAmount = ratio;
        healthFillImage.color = Color.Lerp(Color.red, Color.green, ratio);
    }

    private void Die()
    {
        ScoreManager.AddScore(50);
        if (TrainingLevelManager.Instance != null)
        {
            TrainingLevelManager.Instance.OnDummyKilled();
        }
        Destroy(gameObject);
    }
}
