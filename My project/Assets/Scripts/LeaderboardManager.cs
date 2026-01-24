using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;
    private const string PREF_KEY = "LeaderboardData";

    [System.Serializable]
    public class ScoreEntry
    {
        public string name;
        public float time; // Lower is better
    }

    [System.Serializable]
    public class LeaderboardData
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    public LeaderboardData data;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            LoadScores();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddScore(string playerName, float timeTaken)
    {
        ScoreEntry newEntry = new ScoreEntry { name = playerName, time = timeTaken };
        data.entries.Add(newEntry);

        // Sort by fastest time (ascending)
        data.entries = data.entries.OrderBy(x => x.time).ToList();

        // Keep only top 10
        if (data.entries.Count > 10)
        {
            data.entries.RemoveAt(data.entries.Count - 1);
        }

        SaveScores();
    }

    public bool IsHighScore(float time)
    {
        if (data.entries.Count < 10) return true;
        // If time is lower (faster) than the slowest score on the board
        return time < data.entries[data.entries.Count - 1].time;
    }

    private void SaveScores()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadScores()
    {
        if (PlayerPrefs.HasKey(PREF_KEY))
        {
            string json = PlayerPrefs.GetString(PREF_KEY);
            data = JsonUtility.FromJson<LeaderboardData>(json);
        }
        else
        {
            data = new LeaderboardData();
        }
    }
}