using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public int score;
}

[System.Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public class LeaderboardManager : MonoBehaviour
{
    private string filePath;

    void Awake()
    {
        filePath = Application.persistentDataPath + "/leaderboard.json";
    }

    public void SubmitScore(string name, int score)
    {
        LeaderboardData data = LoadData();

        // Only keep the best score for this player
        var existing = data.entries.FirstOrDefault(e => e.playerName == name);
        if (existing != null)
        {
            if (score > existing.score) existing.score = score;
        }
        else
        {
            data.entries.Add(new LeaderboardEntry { playerName = name, score = score });
        }

        // Sort and limit to top 10
        data.entries = data.entries.OrderByDescending(e => e.score).Take(10).ToList();
        SaveData(data);
    }

    private void SaveData(LeaderboardData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(filePath, json);
    }

    public LeaderboardData LoadData()
    {
        if (!File.Exists(filePath)) return new LeaderboardData();
        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<LeaderboardData>(json);
    }
}