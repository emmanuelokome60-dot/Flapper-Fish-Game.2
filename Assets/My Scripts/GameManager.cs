using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    // Singleton: any script reaches it with GameManager.Instance
    public static GameManager Instance { get; private set; }

    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private GameObject player;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private TMP_Text scoreTextCounter;

    [Header("Game over screen")]
    [Tooltip("The GameOverMenu panel. Shown on death, hidden by Restart and Home.")]
    [SerializeField] private GameObject gameOverMenu;

    [Tooltip("This run's distance, written as SCORE: 23m.")]
    [SerializeField] private TMP_Text gameOverScoreText;

    [Tooltip("The best distance ever reached, written as BEST SCORE: 98m.")]
    [SerializeField] private TMP_Text gameOverBestText;

    [Tooltip("Coins collected in the run that just ended.")]
    [SerializeField] private TMP_Text gameOverCoinText;

    [Header("Screens")]
    [Tooltip("The home screen. Shown at boot, hidden once Play is pressed.")]
    [SerializeField] private GameObject mainMenu;

    [Tooltip("The fish shop. Opened by CharactersButton, over the same dim the pause screen uses.")]
    [SerializeField] private GameObject charactersMenu;

    [Header("Buttons")]
    [Tooltip("Restarts the run. The pause screen's copy — the game over panel has its own.")]
    [FormerlySerializedAs("playButton")]
    [SerializeField] private GameObject restartButton;

    [Tooltip("Goes back to the main menu. The pause screen's copy, next to that Restart.")]
    [SerializeField] private GameObject homeButton;

    [Tooltip("Pauses the run. Only visible while actually playing.")]
    [SerializeField] private GameObject pauseButton;

    [Tooltip("Resumes a paused run. Only while paused, never on the game over screen.")]
    [SerializeField] private GameObject resumeButton;

    // Deliberately NOT [FormerlySerializedAs("pauseDimAlpha")]. The scene had that saved at
    // 0.6 and a saved value beats a script default, so the rename is what lets 0.78 apply
    // without editing the scene while Unity holds it open. Tune from the slider after this.
    [Tooltip("How dark the screen goes behind the pause and game over buttons. 0 = no dimming.")]
    [Range(0f, 1f)]
    [SerializeField] private float pauseDimDarkness = 0.78f;

    [Tooltip("How soft the frozen game goes behind the dim. Each step halves the resolution. 0 = sharp.")]
    [Range(0, 6)]
    [SerializeField] private int pauseBlurSteps = 4;

    [Header("Sound Button")]
    [Tooltip("The main menu's sound toggle. Both toggles show the one Muted state.")]
    [SerializeField] private Image menuSoundButton;

    [Tooltip("The pause screen's sound toggle. Hidden with the other pause buttons during a run.")]
    [SerializeField] private Image pauseSoundButton;

    [Tooltip("Shown while sound is on.")]
    [SerializeField] private Sprite soundOnSprite;

    [Tooltip("Shown while sound is muted.")]
    [SerializeField] private Sprite soundOffSprite;

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
    [Tooltip("How fast the number climbs, in world units per second. 5 matches the rocks.")]
    [Min(0.1f)]
    [SerializeField] private float unitsPerSecond = 5f;

    [Header("Cave depth")]
    // This takes ownership of the light's Intensity: once a run starts, whatever is typed on
    // the Light 2D itself is overwritten from here. That is the point -- the value has to
    // change over the run, so it cannot also be authored on the component. Start Light is the
    // number to tune; the light's own slider only decides what the Editor preview shows.
    [Tooltip("The Global Light 2D. Left empty, the cave simply never darkens.")]
    [SerializeField] private Light2D caveLight;

    [Tooltip("Light intensity at the mouth of the run, where every run begins.")]
    [Range(0f, 2f)]
    [SerializeField] private float startLight = 0.35f;

    [Tooltip("Light intensity once the fish is as deep into the cave as it gets.")]
    [Range(0f, 2f)]
    [SerializeField] private float deepLight = 0.08f;

    [Tooltip("Distance in metres at which the cave reaches its darkest.")]
    [Min(1f)]
    [SerializeField] private float darkestAtDistance = 400f;

    [Header("Player")]
    // Deliberately NOT [FormerlySerializedAs("startMarginFromLeft")]. That field held 1.9 WORLD
    // UNITS; this one holds a FRACTION, so 1.9 would start the fish 1.9 screen-widths across.
    [Tooltip("Where the fish starts, as a fraction of the screen width from the left edge.")]
    [Range(0f, 1f)]
    [SerializeField] private float startFractionFromLeft = 0.5f;

    public int HighScore { get; private set; }

    // Collected this run. The HUD counter shows this, and it clears like the score.
    public int Coins { get; private set; }

    // Lifetime wallet, banked now so nothing is lost before the skin shop is built
    public int TotalCoins { get; private set; }

    // How far this run has travelled, in whole world units
    public int Distance { get; private set; }
    private float distanceTravelled;

    private const string COINS_KEY = "Coins";

    // Best distance ever reached. HighScore existed as a property for a long time without
    // anything ever assigning it; this is what finally gives it a value and a home.
    private const string BEST_KEY = "BestDistance";
    private const string MUTE_KEY = "Muted";

    [Header("Sound")]
    // Renamed from clickClip to match the file. FormerlySerializedAs is what keeps the clip you
    // already dragged on: without it the rename reads as a new field and comes back empty.
    [Tooltip("Played by every button in the UI. Assets/Music/buttons click.")]
    [FormerlySerializedAs("clickClip")]
    [SerializeField] private AudioClip buttonClick;

    [Tooltip("Played when the fish collects a coin. Assets/Music/fish get coin.")]
    [SerializeField] private AudioClip coinClip;

    [Tooltip("Played on every swim stroke, the fish rising. Assets/Music/Pop sound.")]
    [SerializeField] private AudioClip movementPop;

    // PlayOneShot's own volumeScale argument, which is the only per-clip level there is without
    // an AudioMixer. AudioListener.volume is the global mute and cannot balance one clip against
    // the rest. This one fires on every stroke, so it sits under the others by default.
    [Tooltip("How loud the swim pop is, relative to the other sounds.")]
    [Range(0f, 1f)]
    [SerializeField] private float movementPopVolume = 0.35f;

    [Tooltip("Played once when the fish dies. Assets/Music/Game Over sound.")]
    [SerializeField] private AudioClip gameOverClip;

    [Tooltip("Looped under the whole game, menu included. Assets/Music/Bg Music.")]
    [SerializeField] private AudioClip bgMusic;

    // Read once, at Awake. To find the level, drag the runtime AudioSource's own Volume slider
    // during Play, then type that number in here in Edit mode.
    [Tooltip("How loud the music sits under the sound effects.")]
    [Range(0f, 1f)]
    [SerializeField] private float bgMusicVolume = 0.3f;

    private AudioSource sfx;

    // Its own source: sfx is a PlayOneShot bus, and one AudioSource cannot be both that and a
    // clip that loops.
    private AudioSource music;

    // One flag behind every sound button, so the menus cannot disagree
    public bool Muted { get; private set; }

    // Read by PlayerController, which idles the fish here and nowhere else. Pause and game over
    // freeze the game too, so "timeScale is 0" is not the same question as "are we on the menu".
    public bool OnMenu => mainMenu != null && mainMenu.activeSelf;

    // How far into the cave the run is, 0 at the mouth and 1 at its darkest. Published so the
    // fish's lure can brighten against the dimming cave off the same number, rather than a
    // second copy of the distance maths that could drift out of step with this one.
    public float CaveDepth { get; private set; }

    private bool isPaused;
    private bool isGameOver;

    // Both sit on the spawner's GameObject, so finding them here saves two Inspector slots
    private TrashSpawner trashSpawner;
    private CoinSpawner coinSpawner;

    // Built on first pause, so there is still no slot to wire. Owns the render textures.
    private PauseBlur pauseBlur;
    private LegalNotice legalNotice;

    // - Brings the game up: claims the singleton, finds the spawners, loads the save, builds
    //   the sound and the coin icon.
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
        HighScore = PlayerPrefs.GetInt(BEST_KEY, 0);
        Muted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
        BuildSound();
        ApplySound();
        BuildCoinIcon(coinText, coinIconSize);
        ShowCoins();
    }

    // - Opens on the menu, which freezes the fish, the background and the obstacles at once.
    private void Start()
    {
        // timeScale 0 in here freezes the fish, the background and the obstacles at once
        BackToMenu();

        // After BackToMenu, so the gate sits on top of a menu that is already laid out. Its own
        // opaque panel is what stops the menu being tapped through it.
        legalNotice = new LegalNotice(CanvasRoot());
        legalNotice.ShowGateIfNeeded();
        BuildLegalButton();
    }

    // - Adds the Terms and Privacy button to the menu, bottom-right, opposite the sound button.
    // - Built in code rather than placed in the scene, so it could go in with the Editor open.
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
        caption.alignment = TextAlignmentOptions.Center;
        caption.color = Color.white;

        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlayClick();
            legalNotice.OpenReader();
        });
    }

    // - Advances the distance counter and darkens the cave with it.
    // - deltaTime is 0 whenever timeScale is, so the menu, pause and game over all stop the
    //   counter without needing a flag of their own.
    private void Update()
    {
        // Time.deltaTime is 0 whenever timeScale is, so the menu, pause and game over all
        // stop the counter without needing a flag of their own
        distanceTravelled += unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime;

        int whole = (int)distanceTravelled;
        if (whole == Distance) return;

        Distance = whole;
        ShowDistance();
        ApplyCaveDepth();
    }

    // - Ends the run: freezes time, stops the spawners, banks a new best and shows the panel.
    // - Guarded against running twice, since the fish can touch two things in one frame.
    public void GameOver()
    {
        // The fish can touch two things in one frame, so only the first hit counts
        if (isGameOver) return;

        isGameOver = true;

        // Behind the isGameOver guard above, so touching two things on one frame still plays it
        // once. timeScale goes to 0 just below; PlayOneShot is not scaled by it.
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

        // SetInt only writes to memory, and a phone can be killed without a clean quit
        PlayerPrefs.Save();

        ShowGameOver();
        gameOverText.gameObject.SetActive(true);
        if (gameOverMenu != null) gameOverMenu.SetActive(true);

        // The panel carries its own Restart and Home, composed to sit with the score rows, so
        // the pause pair stays down and the two screens can be positioned independently. The
        // dim is still wanted behind the panel, which is why it is raised on its own here.
        ShowRunButtons(false);
        ShowPauseDim(true);
    }

    // Called by the Play button on the menu and the Restart button
    // - Starts a run from the menu or a restart: clears the last one, resets the counters and
    //   opens the spawners.
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

        // Every run starts from zero
        ResetCoins();
        ResetDistance();
        ShowRunCounters(true);

        // The run starts on PLAY. Breathing room is the spawner's lead-in plus the swim in
        // from the right edge.
        obstacleSpawner.StartSpawning();
        if (trashSpawner != null) trashSpawner.StartSpawning();
        if (coinSpawner != null) coinSpawner.StartSpawning();

        // Only if it is not already going: Restart comes back through here, and Play() on a
        // running source snaps the track back to its first bar, so a retry loop would never
        // hear past the opening seconds.
        if (music != null && bgMusic != null && !music.isPlaying) music.Play();
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
    // - Returns to the menu: stops the spawners, clears the cave, saves, and freezes time.
    // - Also the first thing Start calls, so the menu state is defined in exactly one place.
    public void BackToMenu()
    {
        obstacleSpawner.StopSpawning();
        if (trashSpawner != null) trashSpawner.StopSpawning();
        if (coinSpawner != null) coinSpawner.StopSpawning();

        // The menu is the only place the music is off. Pause and game over keep it, so a
        // retry carries straight on. Guarded because Start() calls BackToMenu(), and a
        // duplicate GameManager returns out of Awake() before BuildSound() has run.
        if (music != null) music.Stop();

        PlayerPrefs.Save();
        ClearObstacles();

        isPaused = false;
        isGameOver = false;
        ResetCoins();
        ResetDistance();

        // The menu has its own artwork, so the counters are out of the way there
        ShowRunCounters(false);

        gameOverText.gameObject.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        pauseButton.SetActive(false);
        ShowRunButtons(false);

        player.GetComponent<PlayerController>().ResetPlayer(PlayerStart());

        mainMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    // Measured as a share of the screen, so it lands in the same place on any device: a fixed
    // 1.9 units is 10.7% across at 16:9 but only 8.8% on a 19.5:9 phone. Height is locked at
    // 10 units and width follows the aspect.
    private Vector3 PlayerStart()
    {
        float screenWidth = CameraEdges.HalfWidth * 2f;
        return new Vector3(CameraEdges.Left + screenWidth * startFractionFromLeft, 0f, 0f);
    }

    // The pause screen's three buttons. Game over no longer comes through here: its panel
    // holds its own Restart and Home so the two layouts can be composed separately.
    private void ShowRunButtons(bool visible, bool canResume = false)
    {
        restartButton.SetActive(visible);
        homeButton.SetActive(visible);

        // Tolerates an empty slot, since Start() calls straight through here
        if (resumeButton != null)
        {
            resumeButton.SetActive(visible && canResume);
        }

        // Shares this group and is wanted on exactly the same screens, so it is toggled with the
        // rest: the sound button belongs to the menu and the pause, never to a run in progress.
        if (pauseSoundButton != null)
        {
            pauseSoundButton.gameObject.SetActive(visible);
        }

        ShowPauseDim(visible);
    }

    // The dim and blur live in PauseBlur; this decides only what has to stay readable on top
    // of them. The panel goes up before the GAME OVER text, or its background would cover it
    // whenever that text is left as a sibling rather than nested inside the panel.
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

    // One AudioSource for every effect, added at runtime rather than sat in the scene, so no
    // component had to be added to GameManager by hand. Muting needs no part in this:
    private void BuildSound()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        // Added at runtime for the same reason sfx is: nothing to put in the scene by hand.
        // Muting needs no part here either -- ApplySound() runs on the next line of Awake and
        // AudioListener.volume is upstream of every source, this one included.
        music = gameObject.AddComponent<AudioSource>();
        music.clip = bgMusic;
        music.loop = true;
        music.volume = bgMusicVolume;
        music.playOnAwake = false;

        // These are new serialized fields on a component the scene already had saved, so they
        // come back EMPTY however they are declared. The game runs silent until they are dragged
        // on; this says so out loud rather than leaving it to be noticed.
        if (buttonClick == null || coinClip == null || movementPop == null
            || gameOverClip == null || bgMusic == null)
        {
            Debug.LogWarning("GameManager: Button Click, Coin Clip, Movement Pop, Game Over Clip "
                             + "and/or Bg Music is empty. "
                             + "Drag the files from Assets/Music onto GameManager in the Inspector, "
                             + "in Edit mode -- a Play-mode drag is discarded when Play stops.", this);
        }

        // Pulled into memory now rather than on first play. Without this the very first time each
        // clip sounds, playback waits on the load -- which is the delay that reads as "the sound
        // is late". Does in code what the importer's Preload Audio Data does, so it holds whether
        // or not that box is ticked on four separate assets.
        PreloadClips(buttonClick, coinClip, movementPop, gameOverClip, bgMusic);

        // Every button under the Canvas, the inactive ones included -- the shop's shelf is
        // switched off at load, and its six badges are buttons. Done here rather than per button
        // in the Inspector, for the same reason the counters have their raycast cleared above:
        foreach (Button button in CanvasRoot().GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<ButtonPressSound>() == null)
            {
                button.gameObject.AddComponent<ButtonPressSound>();
            }
        }
    }

    // - Forces each clip to finish loading before play, so the first use does not hitch.
    private void PreloadClips(params AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip != null) clip.LoadAudioData();
        }
    }

    // Public so a button wired straight to it in the Inspector also works, and so
    // ButtonPressSound can reach it
    // - Plays the UI click. Public so a button wired straight to it in the Inspector works too.
    public void PlayClick()
    {
        if (sfx != null && buttonClick != null) sfx.PlayOneShot(buttonClick);
    }

    // Called from PlayerController the frame it swims, which is the one place the stroke happens
    // - Plays the little pop the fish makes on a swim.
    public void PlayMovementPop()
    {
        if (sfx != null && movementPop != null) sfx.PlayOneShot(movementPop, movementPopVolume);
    }

    // Called by the sound button on every menu that carries one. There is a single Muted flag
    // behind all of them, which is what keeps the menus agreeing, and it is in PlayerPrefs so
    // the choice survives the app being killed.
    public void ToggleSound()
    {
        Muted = !Muted;
        PlayerPrefs.SetInt(MUTE_KEY, Muted ? 1 : 0);

        // SetInt only writes to memory, and a phone can be killed without a clean quit
        PlayerPrefs.Save();

        ApplySound();
    }

    // AudioListener.volume is the one global tap: it silences every AudioSource at once,
    // including any added later. The game has no audio at all yet, so this currently mutes
    // nothing -- which is the point. Sounds added later are muted by it without being wired
    // to anything, and no AudioMixer has to exist for that to hold.
    private void ApplySound()
    {
        AudioListener.volume = Muted ? 0f : 1f;

        Sprite icon = Muted ? soundOffSprite : soundOnSprite;
        if (icon == null) return;

        // Both are set, not just the visible one: a hidden menu's button still has to be
        // right by the time that menu is shown
        if (menuSoundButton != null) menuSoundButton.sprite = icon;
        if (pauseSoundButton != null) pauseSoundButton.sprite = icon;
    }

    // The Canvas this UI lives on, found by walking up rather than being wired, so regrouping
    // the hierarchy cannot break it.
    private Transform CanvasRoot()
    {
        Canvas canvas = pauseButton.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform : pauseButton.transform.parent;
    }

    // PauseBlur holds a RenderTexture, which is native memory and is not collected with the
    // object that held it
    // - Releases the blur's RenderTexture, which is native memory and is not collected with
    //   the object that held it.
    private void OnDestroy()
    {
        if (pauseBlur != null) pauseBlur.Release();
    }

    // - Deletes every rock, coin and piece of debris still in the cave.
    // - They all carry ObstacleMovement and all parent to the spawner, which is what lets one
    //   sweep find the lot.
    private void ClearObstacles()
    {
        // Obstacles are children of the spawner, so delete every one of them
        foreach (ObstacleMovement obstacle in obstacleSpawner.GetComponentsInChildren<ObstacleMovement>())
        {
            Destroy(obstacle.gameObject);
        }
    }

    // Called by CoinPickup when the fish swims into a coin
    // - Banks one collected coin into both the run total and the saved wallet.
    public void AddCoin()
    {
        if (isGameOver) return;

        Coins++;
        TotalCoins++;
        PlayerPrefs.SetInt(COINS_KEY, TotalCoins);
        ShowCoins();

        // Here rather than in CoinPickup: every collected coin routes through this one method,
        // including the isGameOver guard above, so a coin touched on the fatal frame stays silent.
        if (sfx != null && coinClip != null) sfx.PlayOneShot(coinClip);
    }

    // The only way coins leave the wallet, sitting next to the only way they enter it. Checking
    // the balance and then subtracting it are one step on purpose: as two, every caller has to
    // remember to do both, and the one that forgets hands out a fish for free.
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || TotalCoins < amount) return false;

        TotalCoins -= amount;
        PlayerPrefs.SetInt(COINS_KEY, TotalCoins);

        // SetInt only writes to memory, and a phone can be killed without a clean quit. A lost
        // purchase is worse than a lost point, so this one is flushed immediately.
        PlayerPrefs.Save();

        ShowCoins();
        return true;
    }

    // Called by CharactersButton to open the shop, and by its close button to leave. Reuses the
    // pause screen's dim and blur rather than building a second one -- the backdrop is meant to
    // look identical, and PauseBlur already takes whatever should sit above it.
    public void ShowCharacters(bool visible)
    {
        if (charactersMenu == null) return;

        charactersMenu.SetActive(visible);
        mainMenu.SetActive(!visible);

        // Closing the shop goes through neither StartGame nor BackToMenu, so without this the
        // menu fish keeps the old skin until the next run. Called on open too: one line beats a
        // branch, and re-applying the skin the fish already wears costs a sprite assignment.
        player.GetComponent<PlayerController>().ApplySkin();

        if (pauseBlur == null)
        {
            if (!visible) return;

            pauseBlur = new PauseBlur(CanvasRoot(), pauseDimDarkness, pauseBlurSteps);
        }

        pauseBlur.Show(visible, charactersMenu);
    }

    // - Puts the distance counter back to zero and redraws it.
    private void ResetDistance()
    {
        distanceTravelled = 0f;
        Distance = 0;
        ShowDistance();

        // Back to the mouth of the run. StartGame() and BackToMenu() both come through here,
        // so a new run and the menu are lit the same as the last run's first metre.
        ApplyCaveDepth();
    }

    // The only place this counter's text is written. Two writers on one TMP_Text is what
    // made it flicker. Tolerates an empty slot, like the coin counter.
    private void ShowDistance()
    {
        if (scoreTextCounter == null) return;

        scoreTextCounter.text = "Distance: " + Distance;
    }

    // The cave closes in the deeper the run gets. Called beside ShowDistance() rather than
    // from inside it, so the text writer stays the only thing that writes the text -- the
    // rule that came out of the counter flicker.
    private void ApplyCaveDepth()
    {
        // Min(1) on the field stops a divide by zero here, and Clamp01 means anything past
        // darkestAtDistance simply stays at deepLight instead of overshooting into black.
        // Set before the null check, so the lure still tracks depth with no light wired.
        CaveDepth = Mathf.Clamp01(Distance / darkestAtDistance);

        if (caveLight == null) return;

        caveLight.intensity = Mathf.Lerp(startLight, deepLight, CaveDepth);
    }

    // Fills the game over panel. One writer per text, like ShowDistance() and ShowCoins():
    // two methods writing one TMP_Text is what made the HUD counter flicker.
    private void ShowGameOver()
    {
        if (gameOverScoreText != null) gameOverScoreText.text = "SCORE: " + Distance + "m";
        if (gameOverBestText != null) gameOverBestText.text = "BEST SCORE: " + HighScore + "m";

        // This run's haul, not the lifetime wallet. TotalCoins if you want the wallet instead.
        if (gameOverCoinText != null) gameOverCoinText.text = Coins.ToString();
    }

    // Only the run count clears. TotalCoins is the wallet and keeps climbing.
    // - Clears this run's coin count. The saved wallet is untouched.
    private void ResetCoins()
    {
        Coins = 0;
        ShowCoins();
    }

    // - The only place the coin counters are written.
    // - Writes TotalCoins, the saved wallet, not Coins, which resets every run: using the
    //   wrong one is why the balance never looked cumulative.
    private void ShowCoins()
    {
        // The wallet, not this run's haul: the player is saving towards a 300 skin, and a counter
        // that restarts at 0 every run can never show them getting there.
        // Tolerates an empty slot until the counter has been made and dragged in
        if (coinText != null)
        {
            coinText.text = TotalCoins.ToString();
        }
    }

    // Draws the coin art left of a counter, so the number reads as coins rather than a second
    // score. Parented to the counter, so it follows wherever the counter is placed. The sprite
    // comes from the spawner's coin prefab, so the two can never disagree.
    public void BuildCoinIcon(TMP_Text target, float size)
    {
        if (target == null || coinSpawner == null) return;

        Sprite sprite = coinSpawner.CoinSprite;
        if (sprite == null) return;

        GameObject icon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(target.transform, false);

        // Anchored inside the counter's left edge, left-hand pivot, so it cannot end up off
        // the canvas the way a right-hand pivot outside the box did
        RectTransform rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(coinIconInset, 0f);

        // Reserve the icon's strip so the number is laid out clear of it, inside the same box
        target.margin = new Vector4(coinIconInset + size + coinIconTextGap, 0f, 0f, 0f);

        // The icon is centred in the box (pivot 0.5 above), so the number has to be too or it
        // rides above the coin. The scene had this text saved as top-aligned.
        target.verticalAlignment = VerticalAlignmentOptions.Middle;

        Image image = icon.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        // Never eat a tap meant for the game
        image.raycastTarget = false;
    }

    // The shop's wallet and the run counter show the same number. Showing it at two sizes reads
    // as two different things, which is what 73.6/78 against 80/50 looked like. Both sizes come
    // from the run counter here, so tuning the HUD moves the shop with it.
    public void BuildWalletCounter(TMP_Text target)
    {
        if (target == null) return;

        if (coinText != null) target.fontSize = coinText.fontSize;
        BuildCoinIcon(target, coinIconSize);
    }

    // Both counters sit on the Canvas next to the menu, so nothing shows them unless we do.
    // The distance counter is saved disabled in the scene and the coin counter saved enabled,
    // so this is what gets them agreeing: both come on with the run and go with the menu.
    private void ShowRunCounters(bool visible)
    {
        foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
        {
            if (counter != null) counter.gameObject.SetActive(visible);
        }
    }

}
