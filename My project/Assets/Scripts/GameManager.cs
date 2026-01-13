using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static bool AutoStartNextLoad = false;

    public enum GameState { MainMenu, Playing, Fumble, GameOver, Victory }
    public GameState currentState;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject inGameHUD;
    public GameObject fumbleHUD;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("HUD Elements")]
    public TextMeshProUGUI yardsText;
    public TextMeshProUGUI downsText;
    public TextMeshProUGUI attachedText;
    public TextMeshProUGUI fumbleTimerText;

    [Header("Game Rules")]
    public int maxDowns = 4;
    public int currentDown = 1;
    public Transform playerTransform;
    public float endZoneX = 100f;

    public bool isIntroSequence = false;

    [Header("Fumble Settings")]
    public float maxFumbleTime = 10f;
    private float currentFumbleTimer;
    public Transform currentPackageTransform;

    private float startingX;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Time.timeScale = 1f;
    }

    void Start()
    {
        if (playerTransform != null) startingX = playerTransform.position.x;
        UpdateDownsUI();

        if (AutoStartNextLoad)
        {
            AutoStartNextLoad = false;
            StartGame();
        }
        else
        {
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

    public void SetState(GameState newState)
    {
        currentState = newState;

        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (inGameHUD) inGameHUD.SetActive(false);
        if (fumbleHUD) fumbleHUD.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);

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
                if (victoryPanel) victoryPanel.SetActive(true);
                Time.timeScale = 0f;
                break;
        }
    }

    public void StartFumbleEvent(Transform package)
    {
        currentPackageTransform = package;
        currentFumbleTimer = maxFumbleTime;
        SetState(GameState.Fumble);
    }

    public void RecoverFumble()
    {
        currentPackageTransform = null;
        SetState(GameState.Playing);
    }

    public void PenalizeFumbleTime(float seconds)
    {
        currentFumbleTimer -= seconds;
    }

    private void HandleFumbleMode()
    {
        currentFumbleTimer -= Time.deltaTime;

        if (fumbleTimerText)
            fumbleTimerText.text = currentFumbleTimer.ToString("F1");

        if (currentFumbleTimer <= 0)
        {
            UseDown();
        }
    }

    public void StartGame()
    {
        currentDown = 1;
        UpdateDownsUI();
        SetState(GameState.Playing);
    }

    public void RestartLevel()
    {
        AutoStartNextLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void UseDown()
    {
        currentDown++;
        if (currentDown > maxDowns) SetState(GameState.GameOver);
        else RestartLevel();
    }

    public void QuitToDesktop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
            string suffix = (currentDown == 1) ? "st" : (currentDown == 2) ? "nd" : (currentDown == 3) ? "rd" : "th";
            downsText.text = $"{currentDown}{suffix} & GOAL";
        }
    }

    private void CheckWinCondition()
    {
        if (playerTransform != null && playerTransform.position.x >= endZoneX)
            SetState(GameState.Victory);
    }
}