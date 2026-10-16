using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class StatsWindow : MonoBehaviour
{
    [Header("Manager References (Auto-found if null)")]
    public ObjectiveManager objectiveManager;
    public GameManager gameManager;
    public PlayerShooter playerShooter;
    public GunShooter gunShooter;

    [Header("UI Text References")]
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI correctText;
    public TextMeshProUGUI incorrectText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI continueText;

    [Header("Level Navigation")]
    public string nextSceneName = "Level1 part2";

    void OnEnable()
    {
        UpdateStats();
    }

    public void UpdateStats()
    {
        // 1. Check if we are in the Training Level first
        if (TrainingLevelManager.Instance != null)
        {
            int kills = EnemyHealth.TotalEnemiesKilled;
            var mgr = TrainingLevelManager.Instance;
            
            float trainingTimeLeft = 0f;
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null) trainingTimeLeft = gm.TimeLeft;

            if (statsText != null)
            {
                statsText.text = $"--- TRAINING COMPLETED ---\n\n" +
                             $"[ ENEMY KILLS ]\n" +
                             $"{EnemyHealth.TotalEnemiesKilled} / {mgr.RequiredKills}\n\n" +
                             $"[ COMBAT STATS ]\n" +
                             $"Shots Fired: {mgr.ShotsFired}\n" +
                             $"Ammo Picked Up: {mgr.AmmoPickedUp}\n\n" +
                             $"Time Remaining: {Mathf.CeilToInt(trainingTimeLeft)}s\n\n" +
                             $"<size=85%><color=#AAAAAA>Press [ENTER] for Next Stage</color></size>";
            }
            return;
        }

        // 2. Otherwise, use the standard Level stats
        // Auto-find references in active scene
        if (objectiveManager == null) objectiveManager = FindObjectOfType<ObjectiveManager>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (playerShooter == null) playerShooter = FindObjectOfType<PlayerShooter>();
        if (gunShooter == null) gunShooter = FindObjectOfType<GunShooter>();

        int correct = 0;
        int totalObjs = 0;
        int incorrect = 0;

        if (objectiveManager != null)
        {
            objectiveManager.RecalculateScores();
            correct = objectiveManager.CorrectColors;
            totalObjs = objectiveManager.TotalObjectives;
            incorrect = objectiveManager.IncorrectColors;
        }

        float timeRemaining = gameManager != null ? Mathf.Max(0f, gameManager.TimeLeft) : 0f;
        int remainingAmmo = 0;

        if (playerShooter != null)
        {
            remainingAmmo = playerShooter.currentAmmo + playerShooter.totalAmmo;
        }
        else if (gunShooter != null)
        {
            remainingAmmo = 30;
        }

        // Score Breakdown
        int correctScore = correct * 200;
        int penaltyScore = incorrect * 50;
        int timeBonus = Mathf.CeilToInt(timeRemaining * 10f);
        int ammoBonus = remainingAmmo * 5;
        int finalScore = Mathf.Max(0, correctScore - penaltyScore + timeBonus + ammoBonus);

        // 1. Update Full Formatted Stats Card
        if (statsText != null)
        {
            statsText.text = $"<color=#FFD700><b>★ STAGE CLEAR ★</b></color>\n\n" +
                             $"<color=#4CAF50>✔ Correct Objects:</color> <b>{correct} / {totalObjs}</b>\n" +
                             $"<color=#FF5252>✖ Incorrect Objects:</color> <b>{incorrect}</b>\n" +
                             $"<color=#64B5F6>⏱ Time Remaining:</color> <b>{timeRemaining:F1}s</b>  <size=80%><i>(+{timeBonus} pts)</i></size>\n" +
                             $"<color=#FFB74D>💥 Ammo Remaining:</color> <b>{remainingAmmo}</b>  <size=80%><i>(+{ammoBonus} pts)</i></size>\n\n" +
                             $"-----------------------------------\n" +
                             $"<color=#FFD700><b>FINAL SCORE: {finalScore:N0} PTS</b></color>\n" +
                             $"-----------------------------------\n\n" +
                             $"<size=85%><color=#AAAAAA>Press [ENTER] for Next Stage</color></size>";
        }

        // 2. Update Individual Text Fields if using separated UI elements
        if (titleText != null) titleText.text = "STAGE CLEAR!";
        if (correctText != null) correctText.text = $"Correct Objects: {correct} / {totalObjs}";
        if (incorrectText != null) incorrectText.text = $"Incorrect Objects: {incorrect}";
        if (timeText != null) timeText.text = $"Time Remaining: {timeRemaining:F1}s (+{timeBonus})";
        if (ammoText != null) ammoText.text = $"Ammo Remaining: {remainingAmmo} (+{ammoBonus})";
        if (scoreText != null) scoreText.text = $"FINAL SCORE: {finalScore:N0} PTS";
        if (continueText != null) continueText.text = "Press [ENTER] for Next Stage";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            NextLevel();
        }
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        string activeScene = SceneManager.GetActiveScene().name;

        string targetScene = nextSceneName;
        if (activeScene.Equals("Level1", System.StringComparison.OrdinalIgnoreCase))
        {
            targetScene = "Level1 part2";
        }
        else if (activeScene.Equals("Level1 part2", System.StringComparison.OrdinalIgnoreCase))
        {
            targetScene = "Level2";
        }

        if (!string.IsNullOrEmpty(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
