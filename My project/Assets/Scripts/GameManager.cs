using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Requires TextMeshPro

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { MainMenu, Playing, GameOver, Victory }
    public GameState currentState;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject inGameHUD;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("In-Game UI Elements")]
    public TextMeshProUGUI yardsText;
    public TextMeshProUGUI fumbleWarningText;

    [Header("Win Condition")]
    public float endZoneX = 100f; // Reach this X to win
    public Transform playerTransform;

    private float startingX;
    private bool isGameActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetState(GameState.MainMenu);
        if (playerTransform != null) startingX = playerTransform.position.x;
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateScore();
            CheckWinCondition();
        }
    }

    private void SetState(GameState newState)
    {
        currentState = newState;

        // Toggle Panels
        mainMenuPanel.SetActive(newState == GameState.MainMenu);
        inGameHUD.SetActive(newState == GameState.Playing);
        gameOverPanel.SetActive(newState == GameState.GameOver);
        victoryPanel.SetActive(newState == GameState.Victory);

        Time.timeScale = (newState == GameState.Playing) ? 1f : 0f;
        isGameActive = (newState == GameState.Playing);
    }

    public void StartGame() => SetState(GameState.Playing);

    public void OnPackageLost() => SetState(GameState.GameOver);

    private void CheckWinCondition()
    {
        if (playerTransform.position.x >= endZoneX)
        {
            SetState(GameState.Victory);
        }
    }

    private void UpdateScore()
    {
        float yards = Mathf.Max(0, playerTransform.position.x - startingX);
        if (yardsText != null) yardsText.text = $"Yards: {Mathf.FloorToInt(yards)}";
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame() => Application.Quit();
}