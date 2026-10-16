using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerShooter playerShooter;
    public GunShooter gunShooter;
    public PlayerHealth playerHealth;
    public ObjectiveManager objectiveManager;

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI reloadingText;
    public TextMeshProUGUI objectivesText;
    public UnityEngine.UI.Image colorIndicator;
    public GameObject loseWindow;
    public GameObject winWindow;

    void Awake()
    {
        // Allowed to run in Training Level now
    }

    void Start()
    {
        // Auto-find references in Level1 scene if unassigned in Inspector
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (playerShooter == null) playerShooter = FindObjectOfType<PlayerShooter>();
        if (gunShooter == null) gunShooter = FindObjectOfType<GunShooter>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (objectiveManager == null) objectiveManager = FindObjectOfType<ObjectiveManager>();

        if (timerText == null) timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        if (ammoText == null) ammoText = GameObject.Find("AmmoText")?.GetComponent<TextMeshProUGUI>();
        if (healthText == null) healthText = GameObject.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
        if (reloadingText == null) reloadingText = GameObject.Find("ReloadingText")?.GetComponent<TextMeshProUGUI>();
        if (objectivesText == null) objectivesText = GameObject.Find("ObjectivesText")?.GetComponent<TextMeshProUGUI>();
        if (colorIndicator == null) colorIndicator = GameObject.Find("ColorIndicator")?.GetComponent<UnityEngine.UI.Image>();

        if (scoreText == null && timerText != null)
        {
            GameObject scoreGO = new GameObject("LiveScoreText");
            scoreGO.transform.SetParent(timerText.transform.parent, false);
            scoreText = scoreGO.AddComponent<TextMeshProUGUI>();
            RectTransform rt = scoreText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(400, 50);
            
            scoreText.fontSize = 32;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.color = Color.yellow;
        }
    }

    void Update()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + ScoreManager.CurrentScore;
        }

        // Standard Game Manager logic
        if (gameManager != null)
        {
            if (timerText != null)
                timerText.text = "Time: " + Mathf.CeilToInt(gameManager.TimeLeft).ToString() + "s";

            if (gameManager.CurrentState == GameState.Lost && loseWindow != null && !loseWindow.activeSelf)
            {
                loseWindow.SetActive(true);
            }
            if (gameManager.CurrentState == GameState.Won && winWindow != null && !winWindow.activeSelf)
            {
                winWindow.SetActive(true);
            }

            if (gameManager.CurrentState == GameState.Lost && Input.GetKeyDown(KeyCode.Return))
            {
                RetryLevel();
            }
            if (gameManager.CurrentState == GameState.Won && Input.GetKeyDown(KeyCode.Return))
            {
                StatsWindow sw = winWindow != null ? winWindow.GetComponent<StatsWindow>() : null;
                if (sw != null) sw.NextLevel();
            }
        }

        if (objectiveManager != null && objectivesText != null)
        {
            objectivesText.text = objectiveManager.GetObjectivesText();
        }
        else if (TrainingLevelManager.Instance != null && objectivesText != null)
        {
            // Fallback for Training Level kills if no objective manager is running
            objectivesText.text = $"<b>OBJECTIVES:</b>\nKill Enemies: {EnemyHealth.TotalEnemiesKilled} / {TrainingLevelManager.Instance.RequiredKills}";
        }



        // Common HUD updates (HP, Ammo)
        if (playerShooter == null) playerShooter = FindObjectOfType<PlayerShooter>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (objectiveManager == null) objectiveManager = FindObjectOfType<ObjectiveManager>();

        if (gameManager != null)
        {
            if (timerText != null)
                timerText.text = "Time: " + Mathf.CeilToInt(gameManager.TimeLeft).ToString() + "s";

            if (gameManager.CurrentState == GameState.Lost && loseWindow != null && !loseWindow.activeSelf)
            {
                loseWindow.SetActive(true);
            }
            if (gameManager.CurrentState == GameState.Won && winWindow != null && !winWindow.activeSelf)
            {
                winWindow.SetActive(true);
            }

            // Input handling for restarting / next level
            if (gameManager.CurrentState == GameState.Lost && Input.GetKeyDown(KeyCode.Return))
            {
                RetryLevel();
            }
            if (gameManager.CurrentState == GameState.Won && Input.GetKeyDown(KeyCode.Return))
            {
                StatsWindow sw = winWindow != null ? winWindow.GetComponent<StatsWindow>() : null;
                if (sw != null) sw.NextLevel();
            }
        }

        if (playerHealth != null)
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {Mathf.CeilToInt(playerHealth.currentHealth)} / {Mathf.CeilToInt(playerHealth.maxHealth)}";
            }
            if (playerHealth.hpSlider != null)
            {
                playerHealth.hpSlider.maxValue = playerHealth.maxHealth;
                playerHealth.hpSlider.value = playerHealth.currentHealth;
            }
        }

        if (objectiveManager != null && objectivesText != null)
        {
            objectivesText.text = objectiveManager.GetObjectivesText();
        }

        if (playerShooter != null)
        {
            Color selectedColor = Color.white;
            if (playerShooter.availableColors != null && playerShooter.availableColors.Length > 0)
            {
                int index = Mathf.Clamp(playerShooter.selectedColorIndex, 0, playerShooter.availableColors.Length - 1);
                selectedColor = playerShooter.availableColors[index];
            }

            if (ammoText != null)
            {
                ammoText.text = $"Ammo: {playerShooter.currentAmmo} / {playerShooter.totalAmmo}";
                ammoText.color = selectedColor;
            }

            if (reloadingText != null)
            {
                reloadingText.gameObject.SetActive(playerShooter.isReloading);
            }

            if (colorIndicator != null)
            {
                colorIndicator.color = selectedColor;
            }
        }
        else if (gunShooter != null)
        {
            Color selectedColor = gunShooter.availableColors != null && gunShooter.availableColors.Length > 0 ? gunShooter.availableColors[0] : Color.white;
            if (ammoText != null)
            {
                ammoText.color = selectedColor;
            }
            if (colorIndicator != null)
            {
                colorIndicator.color = selectedColor;
            }
        }
    }

    public void RetryLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
