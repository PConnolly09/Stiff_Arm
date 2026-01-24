using UnityEngine;
using TMPro;
using System.Text;

public class MainMenuController : MonoBehaviour
{
    [Header("Sub-Panels")]
    public GameObject rootMenu;
    public GameObject leaderboardPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    [Header("Leaderboard UI")]
    public TextMeshProUGUI leaderboardText;

    // FIX: Force reset to Root whenever the Main Menu appears
    void OnEnable()
    {
        ShowRoot();
    }

    public void ShowRoot()
    {
        if (rootMenu) rootMenu.SetActive(true);
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
    }

    public void ShowLeaderboard()
    {
        rootMenu.SetActive(false);
        leaderboardPanel.SetActive(true);
        UpdateLeaderboardText();
    }

    public void ShowSettings()
    {
        rootMenu.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ShowCredits()
    {
        rootMenu.SetActive(false);
        creditsPanel.SetActive(true);
    }

    private void UpdateLeaderboardText()
    {
        if (LeaderboardManager.Instance == null || leaderboardText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>TOP RUSHERS</b>\n");

        var entries = LeaderboardManager.Instance.data.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {entries[i].name} - {entries[i].time:F2}s");
        }

        if (entries.Count == 0) sb.AppendLine("No Records Yet!");

        leaderboardText.text = sb.ToString();
    }
}