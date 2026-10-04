using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    // Singleton: any script reaches it with GameManager.Instance
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

    [Tooltip("Resumes a paused run. Only while paused, never on the game over screen.")]
    [SerializeField] private GameObject resumeButton;

    [Tooltip("How dark the screen goes behind the pause buttons. 0 = no dimming.")]
    [Range(0f, 1f)]
    [SerializeField] private float pauseDimAlpha = 0.6f;

    [Header("Coins")]
    [Tooltip("Shows the coin wallet: a running total that survives runs and sessions.")]
    [SerializeField] private TMP_Text coinText;

    [Tooltip("Height of the coin icon left of the counter, in UI units. The font is 90.")]
    [SerializeField] private float coinIconSize = 80f;

    // Inside the box on purpose, so it cannot fall off the canvas. Widen the box instead.
    [Tooltip("Inset of the icon from the left edge of the counter's box.")]
    [SerializeField] private float coinIconInset = 0f;

    [Tooltip("Space between the icon and the number.")]
    [SerializeField] private float coinIconTextGap = 8f;

    [Header("Distance")]
    // Shown on scoreTextCounter, which now carries the distance rather than the rock score.
    // The field keeps its name on purpose: renaming it would empty the Inspector slot, and the
    // scene has only the one numeric text object. A separate distanceTextCounter field was
    // wired to that same object, which is what made the counter flicker between two writers.
    // Deliberately NOT [FormerlySerializedAs("distancePerSecond")]. That field was saved as 0,
    // which reads as "start counting from 0" but actually means the number never moves. The
    // rename orphans it so this default applies. Min stops 0 being entered again.
    [Tooltip("How fast the number climbs, in world units per second. 5 matches the rocks.")]
    [Min(0.1f)]
    [SerializeField] private float unitsPerSecond = 5f;

    [Header("Player")]
    // Deliberately NOT [FormerlySerializedAs("startMarginFromLeft")]. That field held 1.9 WORLD
    // UNITS; this one holds a FRACTION, so 1.9 would start the fish 1.9 screen-widths across.
    // The rename is also what lets this default apply over the scene's saved value.
    [Tooltip("Where the fish starts, as a fraction of the screen width from the left edge.")]
    [Range(0f, 1f)]
    [SerializeField] private float startFractionFromLeft = 0.3333f;

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    // Collected this run. The HUD counter shows this, and it clears like the score.
    public int Coins { get; private set; }

    // Lifetime wallet, banked now so nothing is lost before the skin shop is built
    public int TotalCoins { get; private set; }

    // How far this run has travelled, in whole world units
    public int Distance { get; private set; }
    private float distanceTravelled;

    private const string COINS_KEY = "Coins";

    private bool isPaused;
    private bool isGameOver;

    // Both sit on the spawner's GameObject, so finding them here saves two Inspector slots
    private TrashSpawner trashSpawner;
    private CoinSpawner coinSpawner;

    // Full-screen dim behind the pause buttons, built on first pause so there is no slot to wire
    private GameObject pauseDim;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        trashSpawner = obstacleSpawner.GetComponent<TrashSpawner>();

        if (trashSpawner == null)
        {
            Debug.LogError("GameManager: no TrashSpawner on the spawner object. Add the Trash Spawner component to SpawnObstacles. The game still runs, just with no debris.", this);
        }

        coinSpawner = obstacleSpawner.GetComponent<CoinSpawner>();

        if (coinSpawner == null)
        {
            Debug.LogError("GameManager: no CoinSpawner on the spawner object. Add the Coin Spawner component to SpawnObstacles. The game still runs, just with no coins.", this);
        }

        // A counter is not a button. Any Graphic with Raycast Target left on reports as
        // "pointer over UI", and SwimPressed() refuses to swim while that is true, so a tap
        // landing on a counter is silently swallowed. The run counter is 1018x141 at the top
        // of the screen, which is a lot of dead area. Done here rather than by unticking the
        // box in the Inspector, so a new counter cannot reintroduce it.
        foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
        {
            if (counter != null) counter.raycastTarget = false;
        }

        TotalCoins = PlayerPrefs.GetInt(COINS_KEY, 0);
        BuildCoinIcon();
        ShowCoins();
    }

    private void Start()
    {
        // timeScale 0 in here freezes the fish, the background and the obstacles at once
        BackToMenu();
    }

    private void Update()
    {
        // Time.deltaTime is 0 whenever timeScale is, so the menu, pause and game over all
        // stop the counter without needing a flag of their own
        distanceTravelled += unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime;

        int whole = (int)distanceTravelled;
        if (whole == Distance) return;

        Distance = whole;
        ShowDistance();
    }

    public void GameOver()
    {
        // The fish can touch two things in one frame, so only the first hit counts
        if (isGameOver) return;

        isGameOver = true;
        isPaused = false;
        Time.timeScale = 0f;

        obstacleSpawner.StopSpawning();
        if (trashSpawner != null) trashSpawner.StopSpawning();
        if (coinSpawner != null) coinSpawner.StopSpawning();

        // SetInt only writes to memory, and a phone can be killed without a clean quit
        PlayerPrefs.Save();

        gameOverText.gameObject.SetActive(true);
        ShowRunButtons(true);
    }

    // Called by the Play button on the menu and the Restart button
    public void StartGame()
    {
        mainMenu.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        ShowRunButtons(false);

        isPaused = false;
        isGameOver = false;
        Time.timeScale = 1f;

        pauseButton.SetActive(true);

        player.GetComponent<PlayerControllerScript>().ResetPlayer(PlayerStart());
        ClearObstacles();

        // Every run starts from zero
        ResetScore();
        ResetCoins();
        ResetDistance();
        ShowRunCounters(true);

        // The run starts on PLAY. Breathing room is the spawner's lead-in plus the swim in
        // from the right edge.
        obstacleSpawner.StartSpawning();
        if (trashSpawner != null) trashSpawner.StartSpawning();
        if (coinSpawner != null) coinSpawner.StartSpawning();
    }

    // Called by the Pause button to pause, and by the Resume button to continue. Pause stays
    // on screen but sits under the dim, so its taps are swallowed: on a phone the two were
    // close enough that tapping Pause again to resume was easy to do by accident.
    public void TogglePause()
    {
        // Nothing to pause once the run is over
        if (isGameOver) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        ShowRunButtons(isPaused, true);
    }

    // Called by the Home button, and once when the game first loads
    public void BackToMenu()
    {
        obstacleSpawner.StopSpawning();
        if (trashSpawner != null) trashSpawner.StopSpawning();
        if (coinSpawner != null) coinSpawner.StopSpawning();
        PlayerPrefs.Save();
        ClearObstacles();

        isPaused = false;
        isGameOver = false;
        ResetScore();
        ResetCoins();
        ResetDistance();

        // The menu has its own artwork, so the counters are out of the way there
        ShowRunCounters(false);

        gameOverText.gameObject.SetActive(false);
        pauseButton.SetActive(false);
        ShowRunButtons(false);

        player.GetComponent<PlayerControllerScript>().ResetPlayer(PlayerStart());

        mainMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    // Measured as a share of the screen, so it lands in the same place on any device: a fixed
    // 1.9 units is 10.7% across at 16:9 but only 8.8% on a 19.5:9 phone. Height is locked at
    // 10 units and width follows the aspect.
    private Vector3 PlayerStart()
    {
        Camera cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        float leftEdge = cam.transform.position.x - halfWidth;
        return new Vector3(leftEdge + halfWidth * 2f * startFractionFromLeft, 0f, 0f);
    }

    // Restart and Home appear together, on game over and on pause. Resume joins them only
    // while paused, since TogglePause() refuses once the game is over.
    private void ShowRunButtons(bool visible, bool canResume = false)
    {
        restartButton.SetActive(visible);
        homeButton.SetActive(visible);

        // Tolerates an empty slot, since Start() calls straight through here
        if (resumeButton != null)
        {
            resumeButton.SetActive(visible && canResume);
        }

        ShowPauseDim(visible);
    }

    // Darkens the frozen game behind the buttons and swallows taps aimed at it
    private void ShowPauseDim(bool visible)
    {
        if (pauseDim == null)
        {
            if (!visible) return;

            pauseDim = new GameObject("PauseDim", typeof(RectTransform), typeof(Image));
            pauseDim.transform.SetParent(pauseButton.transform.parent, false);

            // Stretch to fill the Canvas whatever shape the device is
            RectTransform rect = pauseDim.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // A Graphic is a raycast target by default, which is what blocks the taps
            pauseDim.GetComponent<Image>().color = new Color(0f, 0f, 0f, pauseDimAlpha);
        }

        pauseDim.SetActive(visible);

        if (!visible) return;

        // UI draws in sibling order: the dim above the run, these above the dim. Anything not
        // raised here stays under it, which is how Pause and the score end up untappable.
        pauseDim.transform.SetAsLastSibling();
        gameOverText.transform.SetAsLastSibling();
        restartButton.transform.SetAsLastSibling();
        homeButton.transform.SetAsLastSibling();
        if (resumeButton != null) resumeButton.transform.SetAsLastSibling();
    }

    private void ClearObstacles()
    {
        // Obstacles are children of the spawner, so delete every one of them
        foreach (ObstacleMovement obstacle in obstacleSpawner.GetComponentsInChildren<ObstacleMovement>())
        {
            Destroy(obstacle.gameObject);
        }
    }

    public void AddScore()
    {
        // A gap can be cleared in the same frame the fish dies, so ignore late points
        if (isGameOver) return;

        Score++;
    }

    // Called by CoinPickup when the fish swims into a coin
    public void AddCoin()
    {
        if (isGameOver) return;

        Coins++;
        TotalCoins++;
        PlayerPrefs.SetInt(COINS_KEY, TotalCoins);
        ShowCoins();
    }

    private void ResetDistance()
    {
        distanceTravelled = 0f;
        Distance = 0;
        ShowDistance();
    }

    // The only place this counter's text is written. Two writers on one TMP_Text is what
    // made it flicker. Tolerates an empty slot, like the coin counter.
    private void ShowDistance()
    {
        if (scoreTextCounter == null) return;

        scoreTextCounter.text = "Distance: " + Distance;
    }

    // Only the run count clears. TotalCoins is the wallet and keeps climbing.
    private void ResetCoins()
    {
        Coins = 0;
        ShowCoins();
    }

    private void ShowCoins()
    {
        // Tolerates an empty slot until the counter has been made and dragged in
        if (coinText != null)
        {
            coinText.text = ": " + Coins;
        }
    }

    // Draws the coin art left of the counter, so the number reads as coins rather than a second
    // score. Parented to the counter, so it follows wherever the counter is placed. The sprite
    // comes from the spawner's coin prefab, so the two can never disagree.
    private void BuildCoinIcon()
    {
        if (coinText == null || coinSpawner == null) return;

        Sprite sprite = coinSpawner.CoinSprite;
        if (sprite == null) return;

        GameObject icon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(coinText.transform, false);

        // Anchored inside the counter's left edge, left-hand pivot, so it cannot end up off
        // the canvas the way a right-hand pivot outside the box did
        RectTransform rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(coinIconSize, coinIconSize);
        rect.anchoredPosition = new Vector2(coinIconInset, 0f);

        // Reserve the icon's strip so the number is laid out clear of it, inside the same box
        coinText.margin = new Vector4(coinIconInset + coinIconSize + coinIconTextGap, 0f, 0f, 0f);

        Image image = icon.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        // Never eat a tap meant for the game
        image.raycastTarget = false;
    }

    // Both counters sit on the Canvas next to the menu, so nothing shows them unless we do.
    // The distance counter is saved disabled in the scene and the coin counter saved enabled,
    // so this is what gets them agreeing: both come on with the run and go with the menu.
    // Game over leaves them up on purpose, so the final numbers can be read.
    private void ShowRunCounters(bool visible)
    {
        foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
        {
            if (counter != null) counter.gameObject.SetActive(visible);
        }
    }

    private void ResetScore()
    {
        Score = 0;
    }
}
