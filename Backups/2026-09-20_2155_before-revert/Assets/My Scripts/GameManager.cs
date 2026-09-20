using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton: only one GameManager exists, and any script can reach it with GameManager.Instance
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject player;
    [SerializeField] private MyObstacleSpawner obstacleSpawner;
    [SerializeField] private TMP_Text scoreTextCounter;

    [Header("Screens")]
    [Tooltip("The home screen: dark overlay, title and Play button.")]
    [SerializeField] private GameObject mainMenuPanel;
    [Tooltip("Shown when the fish dies: Game Over text and the Retry button.")]
    [SerializeField] private GameObject gameOverPanel;

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // The game boots into the menu instead of playing straight away
        BackToMenu();
    }

    public void GameOver()
    {
        Time.timeScale = 0f;

        obstacleSpawner.StopSpawning();

        gameOverPanel.SetActive(true);
    }

    // Called by the Play button on the menu and the Retry button on the game over screen
    public void StartGame()
    {
        mainMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        Time.timeScale = 1f;

        // Reset the fish position, stop its fall and let it swim again
        player.GetComponent<PlayerControllerScript>().ResetPlayer(new Vector3(-7f, 0f, 0f));

        //Clear Obstacles
        ClearObstacles();

        // Every run starts from zero
        Score = 0;
        scoreTextCounter.text = "0";

        // Obstacles only start once the game is actually running
        obstacleSpawner.StartSpawning();
    }

    // Called once when the game first loads, and later by a back to menu button
    public void BackToMenu()
    {
        obstacleSpawner.StopSpawning();
        ClearObstacles();

        gameOverPanel.SetActive(false);

        player.GetComponent<PlayerControllerScript>().ResetPlayer(new Vector3(-7f, 0f, 0f));
        Score = 0;
        scoreTextCounter.text = "0";

        // Show the menu and freeze everything behind it
        mainMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void ClearObstacles()
    {
        // Obstacles are spawned as children of the spawner, so delete every one of them
        foreach (ObstacleMovement obstacle in obstacleSpawner.GetComponentsInChildren<ObstacleMovement>())
        {
            Destroy(obstacle.gameObject);
        }
    }

    public void AddScore()
    {
        Score++;
        scoreTextCounter.text = Score.ToString();
    }
}
