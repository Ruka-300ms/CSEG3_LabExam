using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Config")]
    public int levelNumber = 1;
    public string nextSceneName = "Stage2";
    public int maxAmmo = 8;
    public float levelTimeLimit = 60f;

    [Header("Target Objectives")]
    public List<ColorTargetObject> targetObjects = new List<ColorTargetObject>();

    [Header("HUD References (TMP or Legacy Text)")]
    public TMP_Text timerText;
    public Text timerTextLegacy;

    public TMP_Text ammoText;
    public Text ammoTextLegacy;

    public TMP_Text objectivesHUDText;
    public Text objectivesHUDTextLegacy;

    public Image currentAmmoColorPreview;

    [Header("Stats Window (Win Overlay)")]
    public GameObject statsWindow;
    public TMP_Text statsCorrectText;
    public Text statsCorrectTextLegacy;

    public TMP_Text statsIncorrectText;
    public Text statsIncorrectTextLegacy;

    public TMP_Text statsAmmoText;
    public Text statsAmmoTextLegacy;

    public TMP_Text statsTimeText;
    public Text statsTimeTextLegacy;

    public TMP_Text statsScoreText;
    public Text statsScoreTextLegacy;

    public Button nextLevelButton;

    [Header("Lose Window Overlay")]
    public GameObject loseWindow;
    public TMP_Text loseReasonText;
    public Text loseReasonTextLegacy;

    public Button retryButton;

    // Runtime state
    private int currentAmmo;
    private float timeRemaining;
    private bool isGameActive = true;

    void Awake()
    {
        if (FindObjectOfType<TrainingLevelManager>() != null)
        {
            enabled = false;
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (FindObjectOfType<TrainingLevelManager>() != null)
        {
            enabled = false;
            return;
        }
        currentAmmo = maxAmmo;
        timeRemaining = levelTimeLimit;
        isGameActive = true;

        if (statsWindow != null) statsWindow.SetActive(false);
        if (loseWindow != null) loseWindow.SetActive(false);

        // Auto-find targets if unassigned
        if (targetObjects.Count == 0)
        {
            targetObjects.AddRange(FindObjectsOfType<ColorTargetObject>());
        }

        // Wire button clicks
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(NextLevel);
        if (retryButton != null) retryButton.onClick.AddListener(RetryLevel);

        UpdateHUD();
    }

    void Update()
    {
        if (!isGameActive) return;

        // Countdown timer
        if (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                UpdateHUD();
                TriggerGameOver("Time Expired! Objective Failed.");
            }
            else
            {
                UpdateHUD();
            }
        }
    }

    public bool CanShoot()
    {
        return isGameActive && currentAmmo > 0;
    }

    public void UseAmmo()
    {
        if (!isGameActive) return;

        currentAmmo--;
        UpdateHUD();

        // Check if out of ammo without completing all objectives
        if (currentAmmo <= 0)
        {
            CheckVictoryOrDefeat();
        }
    }

    public void OnObjectColored(ColorTargetObject target)
    {
        UpdateHUD();
        CheckVictoryOrDefeat();
    }

    public void UpdateAmmoColorPreview(Color c)
    {
        if (currentAmmoColorPreview != null)
        {
            currentAmmoColorPreview.color = c;
        }
    }

    private void CheckVictoryOrDefeat()
    {
        int correctCount = 0;
        foreach (var obj in targetObjects)
        {
            if (obj != null && obj.isCorrectlyColored)
            {
                correctCount++;
            }
        }

        // All objectives completed correctly
        if (correctCount >= targetObjects.Count && targetObjects.Count > 0)
        {
            TriggerLevelWin();
        }
        else if (currentAmmo <= 0)
        {
            // Out of ammo and not all objectives are correct
            TriggerGameOver("Out of Ammo! You used all available shots.");
        }
    }

    private void TriggerLevelWin()
    {
        isGameActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        int correctCount = 0;
        foreach (var obj in targetObjects)
        {
            if (obj != null && obj.isCorrectlyColored) correctCount++;
        }
        int incorrectCount = targetObjects.Count - correctCount;

        // Calculate score
        int score = (correctCount * 500) + (currentAmmo * 100) + Mathf.RoundToInt(timeRemaining * 10f);

        SetText(statsCorrectText, statsCorrectTextLegacy, "Correct Objects: " + correctCount + " / " + targetObjects.Count);
        SetText(statsIncorrectText, statsIncorrectTextLegacy, "Incorrect Objects: " + incorrectCount);
        SetText(statsAmmoText, statsAmmoTextLegacy, "Shots Remaining: " + currentAmmo);
        SetText(statsTimeText, statsTimeTextLegacy, "Time Remaining: " + Mathf.CeilToInt(timeRemaining) + "s");
        SetText(statsScoreText, statsScoreTextLegacy, "Final Score: " + score);

        if (statsWindow != null) statsWindow.SetActive(true);
    }

    private void TriggerGameOver(string reason)
    {
        isGameActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetText(loseReasonText, loseReasonTextLegacy, reason);
        if (loseWindow != null) loseWindow.SetActive(true);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateHUD()
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        SetText(timerText, timerTextLegacy, "Time: " + seconds + "s");
        SetText(ammoText, ammoTextLegacy, "Ammo: " + currentAmmo + " / " + maxAmmo);

        string list = "<b>OBJECTIVES:</b>\n";
        foreach (var obj in targetObjects)
        {
            if (obj == null) continue;
            string status = obj.isCorrectlyColored ? "<color=green>[✓]</color>" : "<color=red>[X]</color>";
            list += status + " " + obj.objectName + " -> " + GetColorName(obj.requiredColor) + "\n";
        }
        SetText(objectivesHUDText, objectivesHUDTextLegacy, list);
    }

    private void SetText(TMP_Text tmp, Text legacy, string content)
    {
        if (tmp != null) tmp.text = content;
        if (legacy != null) legacy.text = content;
    }

    private string GetColorName(Color c)
    {
        if (c == Color.red) return "Red";
        if (c == Color.blue) return "Blue";
        if (c == Color.green) return "Green";
        if (c == Color.yellow) return "Yellow";
        if (c == Color.cyan) return "Cyan";
        if (c == Color.magenta) return "Magenta";
        return "Custom Color";
    }
}
