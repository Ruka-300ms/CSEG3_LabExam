using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Tooltip("Drag the Slider from the enemy's world-space Canvas here.")]
    public Slider healthBar;

    [Tooltip("Health bar always faces the camera.")]
    public bool billboardHealthBar = true;

    private Camera mainCamera;
    private bool isDead = false;

    public static int TotalEnemiesKilled = 0;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Ensure the enemy has a collider so it can be hit by bullets
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = Vector3.zero;
        }

        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }

        mainCamera = Camera.main;

        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<Slider>(true);
        }

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    void LateUpdate()
    {
        if (billboardHealthBar && healthBar != null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Transform barTarget = healthBar.transform.parent != null ? healthBar.transform.parent : healthBar.transform;
                barTarget.forward = mainCamera.transform.forward;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (currentHealth <= 0f && maxHealth > 0f)
        {
            currentHealth = maxHealth;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<Slider>(true);
            if (healthBar != null) healthBar.maxValue = maxHealth;
        }

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        TotalEnemiesKilled++;
        ScoreManager.AddScore(100);

        // Notify training level manager (if active) that a dummy was killed
        if (TrainingLevelManager.Instance != null)
        {
            TrainingLevelManager.Instance.OnDummyKilled();
        }

        Destroy(gameObject);
    }
}