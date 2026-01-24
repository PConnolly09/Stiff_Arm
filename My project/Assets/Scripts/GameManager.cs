using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static int CurrentDown = 1;
    public static bool AutoStartNextLoad = false;

    public enum GameState { MainMenu, Playing, Fumble, GameOver, Victory }
    public GameState currentState;

    [Header("UI Panels (MUST BE ASSIGNED)")]
    public GameObject mainMenuPanel;
    public GameObject inGameHUD;
    public GameObject fumbleHUD;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject highScorePanel;

    [Header("HUD Elements")]
    public TextMeshProUGUI yardsText;
    public TextMeshProUGUI downsText;
    public TextMeshProUGUI attachedText;
    public TextMeshProUGUI fumbleTimerText;
    public TextMeshProUGUI gameOverReasonText;
    public TextMeshProUGUI victoryTimeText;
    public TMP_InputField nameInputField;

    [Header("Game Rules")]
    public int maxDowns = 4;
    public Transform playerTransform;
    public float endZoneX = 100f;
    public bool isIntroSequence = false;

    [Header("Fumble Settings")]
    public float maxFumbleTime = 10f;
    private float currentFumbleTimer;
    public Transform currentPackageTransform;

    private float startingX;
    private float startTime;
    private float finalTime;

    void Awake()
    {
        // FIX: Simple overwrite. 
        // We don't need to Destroy(Instance.gameObject) because the Scene Reload 
        // has already destroyed the previous instance. Accessing it caused the crash.
        Instance = this;

        // Ensure time is running so we can process logic
        Time.timeScale = 1f;

        // Hide everything immediately to prevent visual glitches
        ResetUI();
    }

    void OnEnable()
    {
        ResetUI();
    }

    void Start()
    {
        if (playerTransform != null) startingX = playerTransform.position.x;

        if (AutoStartNextLoad)
        {
            AutoStartNextLoad = false;
            StartGameLogic();
        }
        else
        {
            CurrentDown = 1;
            SetState(GameState.MainMenu);
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateHUD();
            CheckWinCondition();
        }
        else if (currentState == GameState.Fumble)
        {
            HandleFumbleMode();
        }
    }

    private void ResetUI()
    {
        // Forcefully hide all panels immediately
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (inGameHUD) inGameHUD.SetActive(false);
        if (fumbleHUD) fumbleHUD.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
        if (highScorePanel) highScorePanel.SetActive(false);
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
        ResetUI(); // Clear the screen

        switch (newState)
        {
            case GameState.MainMenu:
                if (mainMenuPanel) mainMenuPanel.SetActive(true);
                Time.timeScale = 0f;
                break;
            case GameState.Playing:
                if (inGameHUD) inGameHUD.SetActive(true);
                Time.timeScale = 1f;
                break;
            case GameState.Fumble:
                if (fumbleHUD) fumbleHUD.SetActive(true);
                Time.timeScale = 1f;
                break;
            case GameState.GameOver:
                if (gameOverPanel) gameOverPanel.SetActive(true);
                Time.timeScale = 0f;
                break;
            case GameState.Victory:
                HandleVictory();
                break;
        }

        UpdateDownsUI();
    }

    public void StartGame()
    {
        CurrentDown = 1;
        StartGameLogic();
    }

    private void StartGameLogic()
    {
        startTime = Time.time;
        UpdateDownsUI();
        SetState(GameState.Playing);
    }

    public void RestartLevel()
    {
        AutoStartNextLoad = true;
        Time.timeScale = 1f; // Unpause before reload
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void UseDown(string reason = "FUMBLE LOST")
    {
        CurrentDown++;
        if (CurrentDown > maxDowns)
        {
            if (gameOverReasonText) gameOverReasonText.text = "TURNOVER ON DOWNS";
            SetState(GameState.GameOver);
        }
        else RestartLevel();
    }

    public void StartFumbleEvent(Transform package)
    {
        if (currentState != GameState.Playing) return;

        currentPackageTransform = package;
        currentFumbleTimer = maxFumbleTime;
        SetState(GameState.Fumble);
    }

    public void RecoverFumble()
    {
        if (currentState == GameState.Fumble)
        {
            currentPackageTransform = null;
            SetState(GameState.Playing);
        }
    }

    public void PenalizeFumbleTime(float seconds)
    {
        currentFumbleTimer -= seconds;
    }

    private void HandleFumbleMode()
    {
        currentFumbleTimer -= Time.deltaTime;
        if (fumbleTimerText) fumbleTimerText.text = currentFumbleTimer.ToString("F1");

        if (currentFumbleTimer <= 0)
        {
            UseDown("RECOVERY FAILED");
        }
    }

    private void CheckWinCondition()
    {
        if (playerTransform != null && playerTransform.position.x >= endZoneX)
            SetState(GameState.Victory);
    }

    private void HandleVictory()
    {
        Time.timeScale = 0f;
        finalTime = Time.time - startTime;
        if (victoryTimeText) victoryTimeText.text = "TIME: " + finalTime.ToString("F2") + "s";

        if (LeaderboardManager.Instance != null && LeaderboardManager.Instance.IsHighScore(finalTime))
        {
            if (highScorePanel) highScorePanel.SetActive(true);
            else if (victoryPanel) victoryPanel.SetActive(true); // Fallback
        }
        else
        {
            if (victoryPanel) victoryPanel.SetActive(true);
        }
    }

    public void SubmitScore()
    {
        if (nameInputField != null && LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddScore(nameInputField.text, finalTime);
            if (highScorePanel) highScorePanel.SetActive(false);
            if (victoryPanel) victoryPanel.SetActive(true);
        }
    }

    private void UpdateHUD()
    {
        if (playerTransform == null || yardsText == null) return;
        float yards = Mathf.Max(0, playerTransform.position.x - startingX);
        yardsText.text = "YARDS: " + Mathf.FloorToInt(yards).ToString();
    }

    public void UpdateAttachmentCount(int count)
    {
        if (attachedText) attachedText.text = "WEIGHT: " + count.ToString();
    }

    private void UpdateDownsUI()
    {
        if (downsText)
        {
            string suffix = (CurrentDown == 1) ? "st" : (CurrentDown == 2) ? "nd" : (CurrentDown == 3) ? "rd" : "th";
            downsText.text = $"{CurrentDown}{suffix} & GOAL";
        }
    }

    public void QuitToDesktop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}