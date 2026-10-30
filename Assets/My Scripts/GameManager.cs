using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Serialization;

// The boss of the game. Runs the menu, starts and ends runs, keeps the
// score and coins, plays the sounds, and darkens the cave.
public class GameManager : MonoBehaviour
{
    // Only one of these ever exists. Any script can reach it.
    public static GameManager Instance { get; private set; }

    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private GameObject player;
    [SerializeField] private CaveSpawner obstacleSpawner;
    [SerializeField] private TMP_Text scoreTextCounter;

    [Header("Game over screen")]
    [Tooltip("The GameOverMenu panel. Shown when you die, hidden by Restart and Home.")]
    [SerializeField] private GameObject gameOverMenu;

    [Tooltip("How far you got this run, written as SCORE: 23m.")]
    [SerializeField] private TMP_Text gameOverScoreText;

    [Tooltip("The furthest you've ever got, written as BEST SCORE: 98m.")]
    [SerializeField] private TMP_Text gameOverBestText;

    [Tooltip("Coins collected in the run that just ended.")]
    [SerializeField] private TMP_Text gameOverCoinText;

    [Header("Screens")]
    [Tooltip("The home screen. Shown when the game opens, hidden once you press Play.")]
    [SerializeField] private GameObject mainMenu;

    [Tooltip("The fish shop. Opened by the Characters button, over the same dark backdrop the " +
             "pause screen uses.")]
    [SerializeField] private GameObject charactersMenu;

    [Header("Buttons")]
    [Tooltip("Restarts the run. This is the PAUSE screen's one -- the game over panel has its own.")]
    [FormerlySerializedAs("playButton")]
    [SerializeField] private GameObject restartButton;

    [Tooltip("Goes back to the main menu. The pause screen's one, next to that Restart.")]
    [SerializeField] private GameObject homeButton;

    [Tooltip("Pauses the run. Only visible while you're actually playing.")]
    [SerializeField] private GameObject pauseButton;

    [Tooltip("Carries on a paused run. Only while paused, never on the game over screen.")]
    [SerializeField] private GameObject resumeButton;

    // New name on purpose, with no [FormerlySerializedAs]. The scene had
    // the old name saved as 0.6, and a saved value beats the script.
    [Tooltip("How dark the screen goes behind the pause and game over buttons. 0 = no dimming.")]
    [Range(0f, 1f)]
    [SerializeField] private float pauseDimDarkness = 0.78f;

    [Tooltip("How blurry the frozen game goes behind the dark backdrop. Each step halves the " +
             "sharpness. 0 = not blurry at all.")]
    [Range(0, 6)]
    [SerializeField] private int pauseBlurSteps = 4;

    [Header("Sound Button")]
    [Tooltip("The main menu's sound on/off button. Both buttons show the same one setting.")]
    [SerializeField] private Image menuSoundButton;

    [Tooltip("The pause screen's sound button. Hidden along with the other pause buttons " +
             "while you're playing.")]
    [SerializeField] private Image pauseSoundButton;

    [Tooltip("Shown while sound is on.")]
    [SerializeField] private Sprite soundOnSprite;

    [Tooltip("Shown while sound is muted.")]
    [SerializeField] private Sprite soundOffSprite;

    [Header("Coins")]
    [Tooltip("Shows your coin total. This keeps climbing across runs and across sessions.")]
    [SerializeField] private TMP_Text coinText;

    [Tooltip("How big the coin picture next to the number is. The number itself is 90.")]
    [SerializeField] private float coinIconSize = 80f;

    // Kept inside the text box so it can never fall off the screen.
    [Tooltip("How far in from the left edge of its box the coin picture sits.")]
    [SerializeField] private float coinIconInset = 0f;

    [Tooltip("Space between the coin picture and the number.")]
    [SerializeField] private float coinIconTextGap = 8f;

    [Header("Distance")]
    // Old field name kept on purpose: renaming empties its Inspector slot.
    [Tooltip("How fast the number climbs, in units a second. 5 matches the rocks' speed.")]
    [Min(0.1f)]
    [SerializeField] private float unitsPerSecond = 5f;

    [Header("Cave depth")]
    // This owns the light's brightness once a run starts, so anything
    // typed on the Light 2D is overwritten. Tune Start Light instead.
    [Tooltip("The Global Light 2D. Leave it empty and the cave simply never gets dark.")]
    [SerializeField] private Light2D caveLight;

    [Tooltip("How bright it is at the mouth of the cave, where every run begins.")]
    [Range(0f, 2f)]
    [SerializeField] private float startLight = 0.35f;

    [Tooltip("How bright it is once you're as deep into the cave as it gets.")]
    [Range(0f, 2f)]
    [SerializeField] private float deepLight = 0.08f;

    [Tooltip("How many metres in before the cave is at its absolute darkest.")]
    [Min(1f)]
    [SerializeField] private float darkestAtDistance = 400f;

    [Header("Player")]
    // New name on purpose. The old field held 1.9 as a distance, this one
    // holds a fraction, so carrying it over threw the fish off screen.
    [Tooltip("Where the fish starts, as a fraction of the way across the screen. 0.5 = middle.")]
    [Range(0f, 1f)]
    [SerializeField] private float startFractionFromLeft = 0.5f;

    public int HighScore { get; private set; }

    // Coins from THIS run only. Clears at the start of every run.
    public int Coins { get; private set; }

    // Your saved total. This is what the shop spends.
    public int TotalCoins { get; private set; }

    // How far this run has got, as a whole number.
    public int Distance { get; private set; }
    private float distanceTravelled;

    private const string COINS_KEY = "Coins";
    private const string BEST_KEY = "BestDistance";
    private const string MUTE_KEY = "Muted";

    [Header("Sound")]
    // FormerlySerializedAs keeps the file you already dragged in.
    [Tooltip("Played by every button in the game. Assets/Music/buttons click.")]
    [FormerlySerializedAs("clickClip")]
    [SerializeField] private AudioClip buttonClick;

    [Tooltip("Played when the fish collects a coin. Assets/Music/fish get coin.")]
    [SerializeField] private AudioClip coinClip;

    [Tooltip("Played every time the fish swims upwards. Assets/Music/Pop sound.")]
    [SerializeField] private AudioClip movementPop;

    // Fires on every tap, so it sits lower than the rest. The global
    // volume moves everything together and cannot do this on its own.
    [Tooltip("How loud the swim pop is compared to the other sounds.")]
    [Range(0f, 1f)]
    [SerializeField] private float movementPopVolume = 0.35f;

    [Tooltip("Played once when the fish dies. Assets/Music/Game Over sound.")]
    [SerializeField] private AudioClip gameOverClip;

    [Tooltip("Loops underneath the whole game, menu included. Assets/Music/Bg Music.")]
    [SerializeField] private AudioClip bgMusic;

    [Tooltip("How loud the music sits underneath the sound effects.")]
    [Range(0f, 1f)]
    [SerializeField] private float bgMusicVolume = 0.3f;

    private AudioSource sfx;

    // Its own player: one cannot fire short sounds and loop a track.
    private AudioSource music;

    // One setting behind every sound button, so two menus never disagree.
    public bool Muted { get; private set; }

    // True only on the menu, where the fish floats gently.
    // Pause and game over freeze the game too, so frozen is not menu.
    public bool OnMenu => mainMenu != null && mainMenu.activeSelf;

    // How deep the run is: 0 at the mouth, 1 at the darkest.
    // Shared, so the fish's lamp cannot drift out of step with it.
    public float CaveDepth { get; private set; }

    private bool isPaused;
    private bool isGameOver;

    private TrashSpawner trashSpawner;
    private CoinSpawner coinSpawner;

    private PauseBlur pauseBlur;
    private LegalNotice legalNotice;
    private GameObject legalButton;

    // Starts the game up: spawners, your save, sound, coin picture.
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

        // A counter is not a button, so stop it eating taps. UI soaks up
        // taps and the fish will not swim, and this one is 1018x141.
        foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
        {
            if (counter != null) counter.raycastTarget = false;
        }

        TotalCoins = PlayerPrefs.GetInt(COINS_KEY, 0);
        HighScore = PlayerPrefs.GetInt(BEST_KEY, 0);
        Muted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
        BuildSound();
        ApplySound();
        BuildCoinIcon(coinText, coinIconSize);
        ShowCoins();
    }

    // Opens the game on the menu, with everything frozen.
    private void Start()
    {
        BackToMenu();

        // After BackToMenu, so the terms sit on a finished menu.
        legalNotice = new LegalNotice(CanvasRoot());
        BuildLegalButton();

        // Hidden until the terms are accepted. The null check matters:
        // a missing menu must not become a way to skip the terms.
        if (legalButton != null) legalButton.SetActive(false);
        legalNotice.ShowGateIfNeeded(() =>
        {
            if (legalButton != null) legalButton.SetActive(true);
        });
    }

    // Adds the Terms & Privacy button to the menu, bottom right.
    // Built in code, so it could be added with the Editor open.
    private void BuildLegalButton()
    {
        if (mainMenu == null) return;

        GameObject button = new GameObject("LegalButton", typeof(RectTransform),
                                           typeof(Image), typeof(Button));
        button.transform.SetParent(mainMenu.transform, false);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(330f, 70f);
        rect.anchoredPosition = new Vector2(-60f, 60f);

        button.GetComponent<Image>().color = new Color(0.10f, 0.42f, 0.44f, 0.85f);

        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(button.transform, false);

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI caption = label.AddComponent<TextMeshProUGUI>();
        caption.text = "Terms & Privacy";
        caption.fontSize = 28f;
        caption.fontStyle = FontStyles.Bold;
        caption.alignment = TextAlignmentOptions.Center;
        caption.color = Color.white;
        legalNotice.ApplyGameFont(caption);

        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlayClick();
            legalNotice.OpenReader();
        });

        legalButton = button;
    }

    // Counts up the metres and darkens the cave as they climb.
    // deltaTime is 0 while frozen, so every screen stops it for free.
    private void Update()
    {
        distanceTravelled += unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime;

        int whole = (int)distanceTravelled;
        if (whole == Distance) return;

        Distance = whole;
        ShowDistance();
        ApplyCaveDepth();
    }

    // The fish is dead. Freezes the cave, saves a new best score, and
    // shows the game over screen.
    // Guarded: the fish can hit two things in one frame.
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        // After the guard, so hitting two things still plays it once.
        if (sfx != null && gameOverClip != null) sfx.PlayOneShot(gameOverClip);

        isPaused = false;
        Time.timeScale = 0f;

        obstacleSpawner.StopSpawning();
        if (trashSpawner != null) trashSpawner.StopSpawning();
        if (coinSpawner != null) coinSpawner.StopSpawning();

        if (Distance > HighScore)
        {
            HighScore = Distance;
            PlayerPrefs.SetInt(BEST_KEY, HighScore);
        }

        // Save() writes it to disk. A phone can be closed without ever
        // shutting down properly.
        PlayerPrefs.Save();

        ShowGameOver();
        gameOverText.gameObject.SetActive(true);
        if (gameOverMenu != null) gameOverMenu.SetActive(true);

        // The game over panel has its own Restart and Home, so hide the
        // pause pair but keep the dark backdrop.
        ShowRunButtons(false);
        ShowPauseDim(true);
    }

    // Starts a run. Play and both Restart buttons call this.
    public void StartGame()
    {
        mainMenu.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        ShowRunButtons(false);

        isPaused = false;
        isGameOver = false;
        Time.timeScale = 1f;

        pauseButton.SetActive(true);

        player.GetComponent<PlayerController>().ResetPlayer(PlayerStart());
        ClearObstacles();

        // Every run starts from zero.
        ResetCoins();
        ResetDistance();
        ShowRunCounters(true);

        obstacleSpawner.StartSpawning();
        if (trashSpawner != null) trashSpawner.StartSpawning();
        if (coinSpawner != null) coinSpawner.StartSpawning();

        // Only if not already playing. Play() on running music snaps it
        // to the start, so a retry never hears past the intro.
        if (music != null && bgMusic != null && !music.isPlaying) music.Play();
    }

    // Pauses the run, or carries on. Both buttons call this.
    // Pause stays on screen but sits UNDER the backdrop, so its taps are
    // soaked up. On a phone it was easy to hit by accident.
    public void TogglePause()
    {
        // Nothing to pause once you are dead.
        if (isGameOver) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        ShowRunButtons(isPaused, true);
    }

    // Goes back to the menu and freezes everything.
    // Start() calls this too, so the menu is built in one place.
    public void BackToMenu()
    {
        obstacleSpawner.StopSpawning();
        if (trashSpawner != null) trashSpawner.StopSpawning();
        if (coinSpawner != null) coinSpawner.StopSpawning();

        // The ONLY place music stops, so a retry carries straight on.
        // Null check: Start() calls this before music exists.
        if (music != null) music.Stop();

        PlayerPrefs.Save();
        ClearObstacles();

        isPaused = false;
        isGameOver = false;
        ResetCoins();
        ResetDistance();

        // The menu has its own artwork, so the counters are in the way.
        ShowRunCounters(false);

        gameOverText.gameObject.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        pauseButton.SetActive(false);
        ShowRunButtons(false);

        player.GetComponent<PlayerController>().ResetPlayer(PlayerStart());

        mainMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    // Picks the spot the fish starts from.
    // Uses a share of the screen width, not a fixed number. A fixed one
    // sits off-centre on tall phones.
    private Vector3 PlayerStart()
    {
        float screenWidth = CameraEdges.HalfWidth * 2f;
        return new Vector3(CameraEdges.Left + screenWidth * startFractionFromLeft, 0f, 0f);
    }

    // Shows or hides the pause screen's buttons.
    // Game over has its own pair, so it does not come through here.
    private void ShowRunButtons(bool visible, bool canResume = false)
    {
        restartButton.SetActive(visible);
        homeButton.SetActive(visible);

        // Copes with an empty slot, because Start() calls straight here.
        if (resumeButton != null)
        {
            resumeButton.SetActive(visible && canResume);
        }

        // Menu and pause screen only, never during a run.
        if (pauseSoundButton != null)
        {
            pauseSoundButton.gameObject.SetActive(visible);
        }

        ShowPauseDim(visible);
    }

    // Switches the dark, blurry backdrop on or off.
    // The panel goes before the GAME OVER text on purpose, or its own
    // background covers that text up.
    private void ShowPauseDim(bool visible)
    {
        if (pauseBlur == null)
        {
            if (!visible) return;

            pauseBlur = new PauseBlur(CanvasRoot(), pauseDimDarkness, pauseBlurSteps);
        }

        pauseBlur.Show(visible, gameOverMenu, gameOverText.gameObject, restartButton, homeButton,
                       resumeButton);
    }

    // Sets up the two sound players and gives every button a click.
    // Made in code, so there are no components to remember.
    private void BuildSound()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        music = gameObject.AddComponent<AudioSource>();
        music.clip = bgMusic;
        music.loop = true;
        music.volume = bgMusicVolume;
        music.playOnAwake = false;

        // Added to a component the scene had already saved, so they come
        // back empty. Say so, or the game just runs silently.
        if (buttonClick == null || coinClip == null || movementPop == null
            || gameOverClip == null || bgMusic == null)
        {
            Debug.LogWarning("GameManager: Button Click, Coin Clip, Movement Pop, Game Over Clip "
                             + "and/or Bg Music is empty. "
                             + "Drag the files from Assets/Music onto GameManager in the Inspector, "
                             + "in Edit mode -- a Play-mode drag is discarded when Play stops.", this);
        }

        // Loaded now. That first-time wait feels like a late sound.
        PreloadClips(buttonClick, coinClip, movementPop, gameOverClip, bgMusic);

        // Switched-off buttons too: the shop shelf is off at load.
        // Done in one go, so a button added later gets it as well.
        foreach (Button button in CanvasRoot().GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<ButtonPressSound>() == null)
            {
                button.gameObject.AddComponent<ButtonPressSound>();
            }
        }
    }

    // Loads the sounds up front, so the first play has no stutter.
    private void PreloadClips(params AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip != null) clip.LoadAudioData();
        }
    }

    // Plays the button click.
    // Public so the Inspector and ButtonPressSound can reach it.
    public void PlayClick()
    {
        if (sfx != null && buttonClick != null) sfx.PlayOneShot(buttonClick);
    }

    // Plays the little pop the fish makes when it swims.
    public void PlayMovementPop()
    {
        if (sfx != null && movementPop != null) sfx.PlayOneShot(movementPop, movementPopVolume);
    }

    // Turns the sound on or off. Every sound button calls this.
    // One saved setting behind them all, so the menus agree.
    public void ToggleSound()
    {
        Muted = !Muted;
        PlayerPrefs.SetInt(MUTE_KEY, Muted ? 1 : 0);

        PlayerPrefs.Save();

        ApplySound();
    }

    // Applies mute and updates both sound buttons' pictures.
    // AudioListener.volume silences every player, even new ones.
    private void ApplySound()
    {
        AudioListener.volume = Muted ? 0f : 1f;

        Sprite icon = Muted ? soundOffSprite : soundOnSprite;
        if (icon == null) return;

        // Both, not just the visible one. A hidden menu must be ready.
        if (menuSoundButton != null) menuSoundButton.sprite = icon;
        if (pauseSoundButton != null) pauseSoundButton.sprite = icon;
    }

    // Finds the Canvas all the UI sits on.
    // Walks UP the tree, so rearranging the UI cannot break it.
    private Transform CanvasRoot()
    {
        Canvas canvas = pauseButton.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform : pauseButton.transform.parent;
    }

    // Hands back the blur's memory. It is not freed on its own.
    private void OnDestroy()
    {
        if (pauseBlur != null) pauseBlur.Release();
    }

    // Deletes every rock, coin and bit of rubbish left in the cave.
    private void ClearObstacles()
    {
        foreach (ObstacleMovement obstacle in obstacleSpawner.GetComponentsInChildren<ObstacleMovement>())
        {
            Destroy(obstacle.gameObject);
        }
    }

    // The fish swam into a coin. Adds it to this run and to your total.
    public void AddCoin()
    {
        if (isGameOver) return;

        Coins++;
        TotalCoins++;
        PlayerPrefs.SetInt(COINS_KEY, TotalCoins);
        ShowCoins();

        // Here, not in CoinPickup, so a coin touched on the frame you
        // die stays silent.
        if (sfx != null && coinClip != null) sfx.PlayOneShot(coinClip);
    }

    // Takes coins off you, and says whether it worked.
    // Checking and subtracting are ONE step on purpose: as two, the
    // caller that forgets one hands out a fish for free.
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || TotalCoins < amount) return false;

        TotalCoins -= amount;
        PlayerPrefs.SetInt(COINS_KEY, TotalCoins);

        // Straight to disk. A lost purchase is worse than a lost point.
        PlayerPrefs.Save();

        ShowCoins();
        return true;
    }

    // Opens and closes the fish shop.
    // Reuses the pause screen's backdrop; it should look identical.
    public void ShowCharacters(bool visible)
    {
        if (charactersMenu == null) return;

        charactersMenu.SetActive(visible);
        mainMenu.SetActive(!visible);

        // Closing the shop misses StartGame and BackToMenu, so without
        // this the menu fish keeps its old look.
        player.GetComponent<PlayerController>().ApplySkin();

        if (pauseBlur == null)
        {
            if (!visible) return;

            pauseBlur = new PauseBlur(CanvasRoot(), pauseDimDarkness, pauseBlurSteps);
        }

        pauseBlur.Show(visible, charactersMenu);
    }

    // Puts the distance counter back to zero and redraws it.
    private void ResetDistance()
    {
        distanceTravelled = 0f;
        Distance = 0;
        ShowDistance();

        // Back to the brightness at the cave mouth.
        ApplyCaveDepth();
    }

    // The one and only place the distance text gets written.
    // Two methods writing one text is what made it flicker.
    private void ShowDistance()
    {
        if (scoreTextCounter == null) return;

        scoreTextCounter.text = "Distance: " + Distance;
    }

    // The deeper you swim, the dimmer the cave gets.
    // Called NEXT TO ShowDistance(), so one method writes the text.
    private void ApplyCaveDepth()
    {
        // Set before the null check, so the fish's lamp still tracks
        // depth with no cave light wired up.
        CaveDepth = Mathf.Clamp01(Distance / darkestAtDistance);

        if (caveLight == null) return;

        caveLight.intensity = Mathf.Lerp(startLight, deepLight, CaveDepth);
    }

    // Fills in the game over screen's numbers.
    private void ShowGameOver()
    {
        if (gameOverScoreText != null) gameOverScoreText.text = "SCORE: " + Distance + "m";
        if (gameOverBestText != null) gameOverBestText.text = "BEST SCORE: " + HighScore + "m";

        // This run's haul, not your lifetime total.
        if (gameOverCoinText != null) gameOverCoinText.text = Coins.ToString();
    }

    // Clears THIS run's coins. Your saved total keeps climbing.
    private void ResetCoins()
    {
        Coins = 0;
        ShowCoins();
    }

    // The one and only place the coin counter is written.
    // Shows TotalCoins, NOT Coins, which resets every run.
    private void ShowCoins()
    {
        // You are saving for a 300-coin fish, and a counter that resets
        // can never show you getting closer.
        if (coinText != null)
        {
            coinText.text = TotalCoins.ToString();
        }
    }

    // Draws the coin picture left of a counter, so the number reads as
    // coins and not as a second score.
    public void BuildCoinIcon(TMP_Text target, float size)
    {
        if (target == null || coinSpawner == null) return;

        Sprite sprite = coinSpawner.CoinSprite;
        if (sprite == null) return;

        GameObject icon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(target.transform, false);

        // Pinned INSIDE the left edge, so it cannot go off screen.
        RectTransform rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(coinIconInset, 0f);

        // Saves the coin a strip of space, clear of the number.
        target.margin = new Vector4(coinIconInset + size + coinIconTextGap, 0f, 0f, 0f);

        // The number must be middle too, or it rides above the coin.
        target.verticalAlignment = VerticalAlignmentOptions.Middle;

        Image image = icon.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        // Never swallow a tap that was meant for the fish.
        image.raycastTarget = false;
    }

    // Makes the shop's coin total match the in-game one.
    // Same number at two sizes looks like two different things.
    public void BuildWalletCounter(TMP_Text target)
    {
        if (target == null) return;

        if (coinText != null) target.fontSize = coinText.fontSize;
        BuildCoinIcon(target, coinIconSize);
    }

    // Shows or hides both run counters together.
    // One was saved ON and the other OFF, so they disagreed.
    private void ShowRunCounters(bool visible)
    {
        foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
        {
            if (counter != null) counter.gameObject.SetActive(visible);
        }
    }

}
