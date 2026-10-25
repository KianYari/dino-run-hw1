using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { MainMenu, Playing, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    const string HighScoreKey = "HighScore";

    // stays after a scene reload so Restart skips the menu
    static bool startImmediately;

    public static GameManager Instance { get; private set; }

    [SerializeField] PlayerController player;
    [SerializeField] UIManager ui;
    [SerializeField] float startSpeed = 8f;
    [SerializeField] float maxSpeed = 18f;
    [SerializeField] float acceleration = 0.15f;
    [SerializeField] float menuScrollSpeed = 2f;
    [SerializeField] float scorePerUnit = 2f;
    [SerializeField] float gameOverInputDelay = 1.2f;

    InputAction pauseAction;
    InputAction confirmAction;
    float score;
    float playTime;
    float gameOverTime;
    int savedHighScore;

    public GameState State { get; private set; }
    public int Score => (int)score;
    public int HighScore => Mathf.Max(savedHighScore, Score);

    float CurrentSpeed => Mathf.Min(maxSpeed, startSpeed + acceleration * playTime);

    public float Difficulty01 => Mathf.InverseLerp(startSpeed, maxSpeed, CurrentSpeed);

    public float ScrollSpeed
    {
        get
        {
            switch (State)
            {
                case GameState.Playing: return CurrentSpeed * player.DashMultiplier;
                case GameState.MainMenu: return menuScrollSpeed;
                default: return 0f;
            }
        }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        savedHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Keyboard>/p");
        pauseAction.AddBinding("<Gamepad>/start");

        confirmAction = new InputAction("Confirm", InputActionType.Button);
        confirmAction.AddBinding("<Keyboard>/space");
        confirmAction.AddBinding("<Keyboard>/enter");
        confirmAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable()
    {
        pauseAction.Enable();
        confirmAction.Enable();
    }

    void OnDisable()
    {
        pauseAction.Disable();
        confirmAction.Disable();
    }

    void Start()
    {
        if (startImmediately)
        {
            startImmediately = false;
            StartGame();
        }
        else
        {
            SetState(GameState.MainMenu);
        }
    }

    void Update()
    {
        switch (State)
        {
            case GameState.MainMenu:
                if (confirmAction.WasPressedThisFrame()) StartGame();
                break;

            case GameState.Playing:
                playTime += Time.deltaTime;
                score += CurrentSpeed * player.DashMultiplier * scorePerUnit * Time.deltaTime;
                if (pauseAction.WasPressedThisFrame()) Pause();
                break;

            case GameState.Paused:
                if (pauseAction.WasPressedThisFrame()) Resume();
                break;

            case GameState.GameOver:
                if (Time.unscaledTime - gameOverTime > gameOverInputDelay && confirmAction.WasPressedThisFrame())
                    Restart();
                break;
        }
    }

    void SetState(GameState newState)
    {
        State = newState;
        Time.timeScale = newState == GameState.Paused ? 0f : 1f;
        ui.Show(newState);
    }

    public void StartGame()
    {
        score = 0f;
        playTime = 0f;
        SetState(GameState.Playing);
    }

    public void Pause()
    {
        if (State == GameState.Playing) SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State == GameState.Paused) SetState(GameState.Playing);
    }

    public void Restart()
    {
        SaveHighScore();
        startImmediately = true;
        ReloadScene();
    }

    public void GoToMainMenu()
    {
        SaveHighScore();
        startImmediately = false;
        ReloadScene();
    }

    public void Quit()
    {
        SaveHighScore();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return;
        SaveHighScore();
        gameOverTime = Time.unscaledTime;
        SetState(GameState.GameOver);
    }

    public void AddScore(int points)
    {
        score += points;
    }

    void ReloadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void SaveHighScore()
    {
        if (Score <= savedHighScore) return;
        savedHighScore = Score;
        PlayerPrefs.SetInt(HighScoreKey, savedHighScore);
        PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        SaveHighScore();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
