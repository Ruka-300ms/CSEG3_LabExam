using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI References")]
    public Slider hpSlider;
    public Image hpFillImage;
    public Text hpText;

    private GameManager gameManager;

    void Start()
    {
        currentHealth = maxHealth;
        gameManager = FindObjectOfType<GameManager>();
        UpdateUI();
    }

    void Update()
    {
        // Interactive testing keybinds
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(10f);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Heal(10f);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateUI();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateUI();
    }

    public void Heal(int amount)
    {
        Heal((float)amount);
    }

    void UpdateUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = currentHealth;
        }

        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = currentHealth / maxHealth;
        }

        if (hpText != null)
        {
            hpText.text = $"HP: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    void Die()
    {
        Debug.Log("Player Died!");
        if (gameManager != null)
        {
            gameManager.LoseGame();
        }
    }
}
