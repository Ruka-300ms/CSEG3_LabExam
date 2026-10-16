using UnityEngine;

public static class ScoreManager
{
    public static int CurrentScore
    {
        get { return PlayerPrefs.GetInt("GlobalScore", 0); }
        private set { PlayerPrefs.SetInt("GlobalScore", value); }
    }

    public static void AddScore(int amount)
    {
        CurrentScore += amount;
        PlayerPrefs.Save();
    }

    public static void ResetScore()
    {
        CurrentScore = 0;
        PlayerPrefs.Save();
    }
}
