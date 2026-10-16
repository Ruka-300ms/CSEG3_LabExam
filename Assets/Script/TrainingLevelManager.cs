using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingLevelManager : MonoBehaviour
{
    public static TrainingLevelManager Instance { get; private set; }

    // ── Stage progression (persists across scene reloads) ──
    public static int currentStage = 1;

    [Header("Stage Settings")]
    [Tooltip("Dummies to kill in Stage 1")]
    public int baseRequiredKills = 10;
    [Tooltip("Extra dummies added per stage")]
    public int killsPerStageIncrease = 3;
    [Tooltip("Seconds allowed per stage")]
    public float timeLimit = 60f;

    [Header("Ammo Settings")]
    [Tooltip("Bullets loaded in the clip at start")]
    public int startingClipAmmo = 25;
    [Tooltip("Reserve ammo at start")]
    public int startingReserveAmmo = 50;
    [Tooltip("Maximum reserve ammo capacity")]
    public int maxReserveAmmo = 50;

    public int RequiredKills => baseRequiredKills + (currentStage - 1) * killsPerStageIncrease;
    public int ShotsFired { get; private set; }
    public int AmmoPickedUp { get; private set; }
    public int DummiesKilled { get; private set; }

    // ────────────────────────────────────────────────────────
    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        // Reset runtime state
        ShotsFired = 0;
        AmmoPickedUp = 0;
        DummiesKilled = 0;

        // Configure player ammo for training mode
        PlayerShooter shooter = FindObjectOfType<PlayerShooter>();
        if (shooter != null)
        {
            shooter.clipSize = startingClipAmmo;
            shooter.currentAmmo = startingClipAmmo;
            shooter.totalAmmo = startingReserveAmmo;
            shooter.maxReserveAmmo = maxReserveAmmo;
        }
    }

    // ── Public callbacks ──

    public void OnShotFired()
    {
        ShotsFired++;
    }

    public void OnAmmoPickedUp()
    {
        AmmoPickedUp++;
    }

    public void OnDummyKilled()
    {
        DummiesKilled++;

        if (DummiesKilled >= RequiredKills)
        {
            // Tell GameManager we won
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.WinGame();
        }
    }

    // ── Navigation ──

    public void ProceedToNextStage()
    {
        currentStage++;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        currentStage = 1;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ── Helpers ──

    private void DisableExistingEnemies()
    {
        EnemyAI[] existingEnemies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI enemy in existingEnemies)
        {
            if (enemy.gameObject.name.Contains("Dummy") || enemy.gameObject.name.Contains("Template"))
                continue;
            enemy.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
