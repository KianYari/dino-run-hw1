using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] PlayerController player;

    [Header("Panels")]
    [SerializeField] GameObject hudPanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject gameOverPanel;

    [Header("HUD")]
    [SerializeField] Text scoreText;
    [SerializeField] Text highScoreText;
    [SerializeField] Image healthFill;
    [SerializeField] Image dashFill;

    [Header("Other texts")]
    [SerializeField] Text menuHighScoreText;
    [SerializeField] Text finalScoreText;
    [SerializeField] Text finalHighScoreText;

    [SerializeField] float gameOverPanelDelay = 0.9f;

    static readonly Color Low = new Color(0.9f, 0.25f, 0.2f);
    static readonly Color High = new Color(0.35f, 0.85f, 0.4f);

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        string score = gm.Score.ToString("00000");
        string best = gm.HighScore.ToString("00000");

        scoreText.text = "SCORE  " + score;
        highScoreText.text = "HI  " + best;
        menuHighScoreText.text = "HIGH SCORE  " + best;
        finalScoreText.text = "SCORE  " + score;
        finalHighScoreText.text = "HIGH SCORE  " + best;

        float health = (float)player.Lives / player.MaxLives;
        healthFill.fillAmount = health;
        healthFill.color = Color.Lerp(Low, High, health);
        dashFill.fillAmount = player.DashReady01;
    }

    public void Show(GameState state)
    {
        StopAllCoroutines();

        hudPanel.SetActive(state != GameState.MainMenu);
        mainMenuPanel.SetActive(state == GameState.MainMenu);
        pausePanel.SetActive(state == GameState.Paused);
        gameOverPanel.SetActive(false);

        if (state == GameState.GameOver) StartCoroutine(ShowGameOverPanel());
    }

    IEnumerator ShowGameOverPanel()
    {
        // wait a bit so the death animation can play
        yield return new WaitForSecondsRealtime(gameOverPanelDelay);
        gameOverPanel.SetActive(true);
    }
}
