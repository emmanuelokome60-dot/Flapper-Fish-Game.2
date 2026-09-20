using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    // Singleton: only one GameManager exists, and any script can reach it with GameManager.Instance
    public static GameManager Instance { get; private set; }

    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private GameObject player;
    [SerializeField] private MyObstacleSpawner obstacleSpawner;
    [SerializeField] private TMP_Text scoreTextCounter;

    [Header("Screens")]
    [Tooltip("The home screen. Shown at boot, hidden once Play is pressed.")]
    [SerializeField] private GameObject mainMenu;

    [Header("Buttons")]
    [Tooltip("Restarts the run. Shown on the game over screen and while paused.")]
    [FormerlySerializedAs("playButton")]
    [SerializeField] private GameObject restartButton;

    [Tooltip("Goes back to the main menu. Sits next to the Restart button.")]
    [SerializeField] private GameObject homeButton;

    [Tooltip("Pauses the run. Only visible while actually playing.")]
    [SerializeField] private GameObject pauseButton;

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    private bool isPaused;

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
        BackToMenu();
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        isPaused = false;

        obstacleSpawner.StopSpawning();

        gameOverText.gameObject.SetActive(true);
        pauseButton.SetActive(false);

        // Restart and Home are the same pair used by the pause screen
        ShowRunButtons(true);
    }

    // Called by the Play button on the menu and the Restart button
    public void StartGame()
    {
        mainMenu.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        ShowRunButtons(false);

        isPaused = false;
        Time.timeScale = 1f;

        pauseButton.SetActive(true);

        // Reset the fish position, stop its fall and let it swim again
        player.GetComponent<PlayerControllerScript>().ResetPlayer(new Vector3(-7f, 0f, 0f));

        //Clear Obstacles
        ClearObstacles();

        // Obstacles only start coming once the game is actually running
        obstacleSpawner.StartSpawning();
    }

    // Called by the Pause button. Pressing it again resumes.
    public void TogglePause()
    {
        isPaused = !isPaused;

        // Freezing time is what actually pauses the fish, background and pipes
        Time.timeScale = isPaused ? 0f : 1f;

        ShowRunButtons(isPaused);
    }

    // Called by the Home button, and once when the game first loads
    public void BackToMenu()
    {
        obstacleSpawner.StopSpawning();
        ClearObstacles();

        isPaused = false;

        gameOverText.gameObject.SetActive(false);
        pauseButton.SetActive(false);
        ShowRunButtons(false);

        player.GetComponent<PlayerControllerScript>().ResetPlayer(new Vector3(-7f, 0f, 0f));

        // Show the menu and freeze everything behind it
        mainMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    // Restart and Home always appear together, on game over and on pause
    private void ShowRunButtons(bool visible)
    {
        restartButton.SetActive(visible);
        homeButton.SetActive(visible);
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
