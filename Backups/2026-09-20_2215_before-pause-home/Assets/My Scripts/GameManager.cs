using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton: only one GameManager exists, and any script can reach it with GameManager.Instance
    public static GameManager Instance { get; private set; }

    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject player;
    [SerializeField] private MyObstacleSpawner obstacleSpawner;

    [Tooltip("The home screen. Shown at boot, hidden once Play is pressed.")]
    [SerializeField] private GameObject mainMenu;

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    [SerializeField] private TMP_Text scoreTextCounter;

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
        // Nothing moves until Play is pressed: timeScale 0 freezes the fish,
        // the background and the obstacles all at once.
        mainMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    public void GameOver()
    {
        Time.timeScale = 0f;

        obstacleSpawner.StopSpawning();

        gameOverText.gameObject.SetActive(true);
        playButton.SetActive(true);
    }

    public void StartGame()
    {
        mainMenu.SetActive(false);

        Time.timeScale = 1f;

        gameOverText.gameObject.SetActive(false);
        playButton.SetActive(false);

        // Reset the fish position, stop its fall and let it swim again
        player.GetComponent<PlayerControllerScript>().ResetPlayer(new Vector3(-7f, 0f, 0f));

        //Clear Obstacles
        ClearObstacles();

        // Obstacles only start coming once the game is actually running
        obstacleSpawner.StartSpawning();
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
