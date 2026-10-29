using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Serialization;

// The boss of the whole game. It runs the menu, starts and ends runs, keeps the score and coins,
// plays the sounds, and dims the cave as you go deeper.
public class GameManager : MonoBehaviour
{
    // There's only ever one of these, and any script can reach it by typing GameManager.Instance
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

    // Deliberately NOT marked with [FormerlySerializedAs("pauseDimAlpha")].
    //
    // The scene had the old name saved as 0.6, and a saved value always beats whatever a script
    // says. Giving it a NEW name is what let 0.78 actually take effect without having to edit the
    // scene file while Unity had it open. Tune it from the slider from now on.
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

    // Kept INSIDE the text box on purpose, so it can never fall off the edge of the screen.
    // If you need more room, make the box wider instead of pushing the coin outwards.
    [Tooltip("How far in from the left edge of its box the coin picture sits.")]
    [SerializeField] private float coinIconInset = 0f;

    [Tooltip("Space between the coin picture and the number.")]
    [SerializeField] private float coinIconTextGap = 8f;

    [Header("Distance")]
    // This shows up on scoreTextCounter, which now holds the DISTANCE rather than a rock score.
    //
    // The field keeps its old name on purpose: renaming it would empty its Inspector slot, and
    // there's only one number-text object in the scene anyway. There used to be a separate
    // distanceTextCounter field pointing at that SAME object, and two things writing to one piece
    // of text is exactly what made the counter flicker.
    [Tooltip("How fast the number climbs, in units a second. 5 matches the rocks' speed.")]
    [Min(0.1f)]
    [SerializeField] private float unitsPerSecond = 5f;

    [Header("Cave depth")]
    // This takes over the light's Intensity completely. Once a run starts, whatever number is
    // typed on the Light 2D itself gets overwritten from here.
    //
    // That's the whole point: the brightness has to CHANGE during the run, so it can't also be
    // set on the component. Start Light is the number to tune. The light's own slider only
    // decides what you see in the Editor preview before pressing Play.
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
    // Deliberately NOT marked with [FormerlySerializedAs("startMarginFromLeft")]. That old field
    // held 1.9 as a DISTANCE; this one holds a FRACTION of the screen. Carrying 1.9 over would
    // have started the fish 1.9 whole screen-widths off to the right.
    [Tooltip("Where the fish starts, as a fraction of the way across the screen. 0.5 = middle.")]
    [Range(0f, 1f)]
    [SerializeField] private float startFractionFromLeft = 0.5f;

    public int HighScore { get; private set; }

    // Coins collected in THIS run. The game over screen shows this, and it clears every run.
    public int Coins { get; private set; }

    // Your total coins, saved and kept forever. This is what the shop spends.
    public int TotalCoins { get; private set; }

    // How far this run has got, as a whole number
    public int Distance { get; private set; }
    private float distanceTravelled;

    private const string COINS_KEY = "Coins";
    private const string BEST_KEY = "BestDistance";
    private const string MUTE_KEY = "Muted";

    [Header("Sound")]
    // Renamed from clickClip to match the actual filename. FormerlySerializedAs is what keeps the
    // sound file you already dragged in: without it, a rename looks like a brand new empty field.
    [Tooltip("Played by every button in the game. Assets/Music/buttons click.")]
    [FormerlySerializedAs("clickClip")]
    [SerializeField] private AudioClip buttonClick;

    [Tooltip("Played when the fish collects a coin. Assets/Music/fish get coin.")]
    [SerializeField] private AudioClip coinClip;

    [Tooltip("Played every time the fish swims upwards. Assets/Music/Pop sound.")]
    [SerializeField] private AudioClip movementPop;

    // This is the only way to make ONE sound quieter than the others without setting up a whole
    // AudioMixer. The global volume control turns everything down together, so it can't balance
    // one sound against the rest. This particular sound fires on every single tap, so it sits
    // lower than the others by default or it gets annoying fast.
    [Tooltip("How loud the swim pop is compared to the other sounds.")]
    [Range(0f, 1f)]
    [SerializeField] private float movementPopVolume = 0.35f;

    [Tooltip("Played once when the fish dies. Assets/Music/Game Over sound.")]
    [SerializeField] private AudioClip gameOverClip;

    [Tooltip("Loops underneath the whole game, menu included. Assets/Music/Bg Music.")]
    [SerializeField] private AudioClip bgMusic;

    // Read once when the game starts. To find a level you like: press Play, drag the AudioSource's
    // Volume slider until it sounds right, then stop and type that number in here.
    [Tooltip("How loud the music sits underneath the sound effects.")]
    [Range(0f, 1f)]
    [SerializeField] private float bgMusicVolume = 0.3f;

    private AudioSource sfx;

    // Music gets its very own player. The one above fires off short one-off sounds, and a single
    // player can't do that AND loop a long track at the same time.
    private AudioSource music;

    // One setting behind every sound button in the game, so two menus can never disagree
    public bool Muted { get; private set; }

    // Read by PlayerController, which floats the fish gently ONLY on the menu.
    //
    // We need this as its own question because pause and game over freeze the game too. So
    // "is the game frozen?" is NOT the same question as "are we on the menu?".
    public bool OnMenu => mainMenu != null && mainMenu.activeSelf;

    // How deep into the cave the run is: 0 at the mouth, 1 at its darkest.
    //
    // Shared publicly so the fish's head lamp can brighten against the darkening cave using the
    // SAME number, instead of having its own copy of the distance maths that could slowly drift
    // out of step with this one.
    public float CaveDepth { get; private set; }

    private bool isPaused;
    private bool isGameOver;

    // Both of these live on the spawner object, so finding them here saves two Inspector slots
    private TrashSpawner trashSpawner;
    private CoinSpawner coinSpawner;

    // Built the first time you pause, so again there's no slot to fill in
    private PauseBlur pauseBlur;
    private LegalNotice legalNotice;
    private GameObject legalButton;

    // - Gets the game going: claims the single-instance spot, finds the spawners, loads your
    //   save, and sets up the sound and the coin picture.
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

        // A score counter is not a button, so stop it behaving like one.
        //
        // Anything drawn in the UI soaks up taps by default. The fish refuses to swim when your
        // tap landed on the UI, so a tap that happens to hit the score counter does nothing at
        // all. The score counter is 1018x141 across the top of the screen — that's a big dead
        // zone. We switch it off here in code rather than unticking a box in the Inspector, so
        // somebody adding a new counter later can't accidentally bring the problem back.
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

    // - Opens the game on the menu, which freezes the fish, the background and the rocks at once.
    private void Start()
    {
        BackToMenu();

        // Done AFTER BackToMenu, so the terms screen sits on top of a menu that's already laid
        // out properly. Its own solid panel is what stops the menu being tapped through it.
        legalNotice = new LegalNotice(CanvasRoot());
        BuildLegalButton();

        // Hidden until the terms are accepted, so the terms screen is the only thing on screen
        // the very first time. If you already accepted, ShowGateIfNeeded calls us straight back
        // and the button appears immediately.
        //
        // The null check matters: BuildLegalButton gives up if the menu slot is empty, and
        // without this the crash would happen BEFORE the line below, so the terms would never
        // appear at all. A missing menu must not be a way to skip the terms.
        if (legalButton != null) legalButton.SetActive(false);
        legalNotice.ShowGateIfNeeded(() =>
        {
            if (legalButton != null) legalButton.SetActive(true);
        });
    }

    // - Adds the Terms & Privacy button to the menu, bottom-right, opposite the sound button.
    // - Built in code rather than placed in the scene, which is what let it be added while the
    //   Unity Editor was open.
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

    // - Pushes the distance counter up and darkens the cave as it climbs.
    // - Time.deltaTime is 0 whenever the game is frozen, so the menu, the pause screen and the
    //   game over screen all stop the counter without needing their own switch for it.
    private void Update()
    {
        distanceTravelled += unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime;

        int whole = (int)distanceTravelled;
        if (whole == Distance) return;

        Distance = whole;
        ShowDistance();
        ApplyCaveDepth();
    }

    // - Ends the run: freezes everything, stops the spawners, saves a new best score if you got
    //   one, and shows the game over panel.
    // - Guarded so it can't run twice, because the fish can touch two things in the same frame.
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        // Placed after the guard above, so touching two things in one frame still only plays this
        // once. Everything freezes a few lines below, but one-off sounds aren't affected by that.
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

        // Saving only puts it in memory until we say Save(), and a phone can be closed without
        // ever shutting down properly
        PlayerPrefs.Save();

        ShowGameOver();
        gameOverText.gameObject.SetActive(true);
        if (gameOverMenu != null) gameOverMenu.SetActive(true);

        // The game over panel has its very own Restart and Home buttons, laid out to sit with the
        // score rows. So we hide the PAUSE screen's pair here and let the two screens be
        // positioned independently. We still want the dark backdrop behind the panel though,
        // which is why that's switched on separately on the next line.
        ShowRunButtons(false);
        ShowPauseDim(true);
    }

    // - Starts a run. Called by Play on the menu and by both Restart buttons.
    // - Clears out the last run, puts every counter back to zero, and opens the spawners.
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

        obstacleSpawner.StartSpawning();
        if (trashSpawner != null) trashSpawner.StartSpawning();
        if (coinSpawner != null) coinSpawner.StartSpawning();

        // Only if it isn't already playing. Restart comes back through here, and telling music
        // that's already going to Play() snaps it back to the very beginning — so someone
        // retrying over and over would never hear past the opening few seconds of the track.
        if (music != null && bgMusic != null && !music.isPlaying) music.Play();
    }

    // - Pauses the run, or carries on from a pause. Both buttons call this one method.
    // - The Pause button stays on screen but ends up UNDER the dark backdrop, so its taps get
    //   soaked up. On a phone the two buttons were close enough together that tapping Pause
    //   again to resume was easy to do by accident.
    public void TogglePause()
    {
        // Nothing to pause once you're already dead
        if (isGameOver) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        ShowRunButtons(isPaused, true);
    }

    // - Goes back to the menu: stops the spawners, clears the cave, saves, and freezes everything.
    // - Called by the Home buttons, and also the very first thing Start() calls — so what the
    //   menu looks like is decided in exactly one place rather than two.
    public void BackToMenu()
    {
        obstacleSpawner.StopSpawning();
        if (trashSpawner != null) trashSpawner.StopSpawning();
        if (coinSpawner != null) coinSpawner.StopSpawning();

        // The menu is the ONLY place the music stops. Pause and game over keep it playing, so
        // retrying carries straight on without the track restarting. The null check is because
        // Start() calls this method, and a duplicate GameManager bails out of Awake() before
        // the music player has even been made.
        if (music != null) music.Stop();

        PlayerPrefs.Save();
        ClearObstacles();

        isPaused = false;
        isGameOver = false;
        ResetCoins();
        ResetDistance();

        // The menu has its own artwork, so the counters would just be in the way there
        ShowRunCounters(false);

        gameOverText.gameObject.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        pauseButton.SetActive(false);
        ShowRunButtons(false);

        player.GetComponent<PlayerController>().ResetPlayer(PlayerStart());

        mainMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    // - Works out where the fish should start.
    // - Measured as a FRACTION of the screen width, so it lands in the same visual spot on every
    //   phone. A fixed distance of 1.9 is 10.7% of the way across a normal screen but only 8.8%
    //   on a tall phone, which is why it used to look off-centre on some devices.
    private Vector3 PlayerStart()
    {
        float screenWidth = CameraEdges.HalfWidth * 2f;
        return new Vector3(CameraEdges.Left + screenWidth * startFractionFromLeft, 0f, 0f);
    }

    // - Shows or hides the pause screen's buttons.
    // - The game over screen does NOT come through here any more. Its panel has its own Restart
    //   and Home, so the two screens can be laid out separately without fighting.
    private void ShowRunButtons(bool visible, bool canResume = false)
    {
        restartButton.SetActive(visible);
        homeButton.SetActive(visible);

        // Copes with the slot being empty, because Start() calls straight through here
        if (resumeButton != null)
        {
            resumeButton.SetActive(visible && canResume);
        }

        // The sound button belongs with this group and is wanted on exactly the same screens, so
        // it gets switched along with the rest: it's for the menu and the pause screen, never for
        // a run in progress.
        if (pauseSoundButton != null)
        {
            pauseSoundButton.gameObject.SetActive(visible);
        }

        ShowPauseDim(visible);
    }

    // - Switches the dark, blurry backdrop on or off.
    // - The backdrop itself is PauseBlur's job. All this decides is WHICH things have to stay
    //   readable on top of it.
    // - The panel is listed before the GAME OVER text on purpose. The panel has a background, so
    //   listed the other way round it would cover that text up whenever the text is a sibling of
    //   the panel rather than tucked inside it.
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

    // - Sets up the two sound players and connects the click sound to every button.
    // - The players are made here in code rather than sitting in the scene, so nobody had to
    //   remember to add components to GameManager by hand.
    private void BuildSound()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        // Muting doesn't need handling here. ApplySound() runs on the very next line of Awake,
        // and the global volume it sets sits above every sound player in the game, these included.
        music = gameObject.AddComponent<AudioSource>();
        music.clip = bgMusic;
        music.loop = true;
        music.volume = bgMusicVolume;
        music.playOnAwake = false;

        // These sound slots were added to a component the scene had ALREADY saved, which means
        // they come back empty no matter how they're written in the code. The game just runs
        // silently until somebody drags the files in, so we say so out loud rather than leaving
        // it to be discovered.
        if (buttonClick == null || coinClip == null || movementPop == null
            || gameOverClip == null || bgMusic == null)
        {
            Debug.LogWarning("GameManager: Button Click, Coin Clip, Movement Pop, Game Over Clip "
                             + "and/or Bg Music is empty. "
                             + "Drag the files from Assets/Music onto GameManager in the Inspector, "
                             + "in Edit mode -- a Play-mode drag is discarded when Play stops.", this);
        }

        // Loaded into memory now rather than the first time each one plays. Without this, the
        // very first time you hear each sound it has to wait for the file to load first — and
        // that wait is exactly what feels like "the sound is late".
        //
        // This does in code what the Preload Audio Data tickbox does, so it works whether or not
        // somebody remembered to tick that box on all five separate files.
        PreloadClips(buttonClick, coinClip, movementPop, gameOverClip, bgMusic);

        // Every button in the game, including the switched-off ones — the shop's shelf is off
        // when the game loads, and its six badges are buttons.
        //
        // Done here in one go rather than button by button in the Inspector, for the same reason
        // the counters are fixed up in Awake: a button added later gets this automatically
        // instead of silently missing out.
        foreach (Button button in CanvasRoot().GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<ButtonPressSound>() == null)
            {
                button.gameObject.AddComponent<ButtonPressSound>();
            }
        }
    }

    // - Makes each sound finish loading up front, so the first time you hear it there's no stutter.
    private void PreloadClips(params AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip != null) clip.LoadAudioData();
        }
    }

    // - Plays the button click.
    // - Public so a button wired straight to it in the Inspector works too, and so
    //   ButtonPressSound can reach it.
    public void PlayClick()
    {
        if (sfx != null && buttonClick != null) sfx.PlayOneShot(buttonClick);
    }

    // - Plays the little pop the fish makes when it swims.
    // - Called from PlayerController on the exact frame it swims, which is the one and only place
    //   a swim actually happens.
    public void PlayMovementPop()
    {
        if (sfx != null && movementPop != null) sfx.PlayOneShot(movementPop, movementPopVolume);
    }

    // - Turns the sound on or off. Every sound button in the game calls this.
    // - There's a single on/off setting behind all of them, which is what keeps the menus
    //   agreeing with each other, and it's saved so your choice survives closing the app.
    public void ToggleSound()
    {
        Muted = !Muted;
        PlayerPrefs.SetInt(MUTE_KEY, Muted ? 1 : 0);

        // Saving only puts it in memory until we say Save(), and a phone can be closed without
        // ever shutting down properly
        PlayerPrefs.Save();

        ApplySound();
    }

    // - Actually applies the mute setting, and updates both sound buttons' pictures.
    // - AudioListener.volume is the one master tap for the whole game: it silences every sound
    //   player at once, including any added later. So a new sound added tomorrow gets muted
    //   correctly without being wired up to anything, and without needing an AudioMixer.
    private void ApplySound()
    {
        AudioListener.volume = Muted ? 0f : 1f;

        Sprite icon = Muted ? soundOffSprite : soundOnSprite;
        if (icon == null) return;

        // We update BOTH buttons, not just the one you can currently see. A hidden menu's button
        // still has to be showing the right picture by the time that menu appears.
        if (menuSoundButton != null) menuSoundButton.sprite = icon;
        if (pauseSoundButton != null) pauseSoundButton.sprite = icon;
    }

    // - Finds the Canvas all the UI sits on.
    // - It walks UP the family tree to find it rather than having an Inspector slot, so
    //   rearranging or regrouping the UI can't break it.
    private Transform CanvasRoot()
    {
        Canvas canvas = pauseButton.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform : pauseButton.transform.parent;
    }

    // - Hands back the blur's memory when the game shuts down.
    // - That kind of memory isn't cleaned up automatically along with the object holding it, so
    //   somebody has to say explicitly when it's finished with.
    private void OnDestroy()
    {
        if (pauseBlur != null) pauseBlur.Release();
    }

    // - Deletes every rock, coin and piece of rubbish still in the cave.
    // - They all carry an ObstacleMovement and they're all children of the spawner, which is
    //   what lets this one short loop find the whole lot of them.
    private void ClearObstacles()
    {
        foreach (ObstacleMovement obstacle in obstacleSpawner.GetComponentsInChildren<ObstacleMovement>())
        {
            Destroy(obstacle.gameObject);
        }
    }

    // - Adds one collected coin to both this run's count and your saved total.
    // - Called by CoinPickup when the fish swims into a coin.
    public void AddCoin()
    {
        if (isGameOver) return;

        Coins++;
        TotalCoins++;
        PlayerPrefs.SetInt(COINS_KEY, TotalCoins);
        ShowCoins();

        // The sound is played here rather than in CoinPickup, because every collected coin comes
        // through this one method — including the isGameOver check above. That means a coin you
        // happen to touch on the frame you die stays silent, which is what you'd expect.
        if (sfx != null && coinClip != null) sfx.PlayOneShot(coinClip);
    }

    // - Takes coins off you, and says whether it worked.
    // - This is the ONLY way coins leave your total, and it sits right next to the only way they
    //   go in. Checking you can afford it and subtracting it are deliberately ONE step: as two
    //   separate steps, every caller has to remember to do both, and the one that forgets hands
    //   out a fish for free.
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || TotalCoins < amount) return false;

        TotalCoins -= amount;
        PlayerPrefs.SetInt(COINS_KEY, TotalCoins);

        // Written to disk straight away. A lost purchase is far worse than a lost point, so this
        // one doesn't wait.
        PlayerPrefs.Save();

        ShowCoins();
        return true;
    }

    // - Opens and closes the fish shop. The Characters button opens it, its close button leaves.
    // - It reuses the pause screen's dark blurry backdrop rather than building a second one. The
    //   backdrop is MEANT to look identical, and PauseBlur already accepts whatever should sit
    //   on top of it.
    public void ShowCharacters(bool visible)
    {
        if (charactersMenu == null) return;

        charactersMenu.SetActive(visible);
        mainMenu.SetActive(!visible);

        // Closing the shop goes through neither StartGame nor BackToMenu, so without this line
        // the menu fish would keep its old look until your next run. We call it when OPENING
        // too: one line beats an if-statement, and re-applying the fish it's already wearing
        // costs nothing worth worrying about.
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

        // Back to the brightness at the mouth of the cave. StartGame() and BackToMenu() both come
        // through here, so a new run and the menu are lit exactly like the last run's first metre.
        ApplyCaveDepth();
    }

    // - The one and only place the distance counter's text gets written.
    // - Two different methods writing to one piece of text is what made it flicker before.
    private void ShowDistance()
    {
        if (scoreTextCounter == null) return;

        scoreTextCounter.text = "Distance: " + Distance;
    }

    // - Darkens the cave the deeper the run gets.
    // - Called NEXT TO ShowDistance() rather than from inside it, so the text-writing method
    //   stays the only thing that writes text. That's the rule that came out of the flicker bug.
    private void ApplyCaveDepth()
    {
        // The Min(1) on darkestAtDistance is what stops a divide-by-zero here, and Clamp01 means
        // going further than darkestAtDistance just stays at deepLight instead of carrying on
        // into pitch black. Set before the null check below, so the fish's head lamp still
        // tracks depth even with no cave light wired up.
        CaveDepth = Mathf.Clamp01(Distance / darkestAtDistance);

        if (caveLight == null) return;

        caveLight.intensity = Mathf.Lerp(startLight, deepLight, CaveDepth);
    }

    // - Fills in the game over panel's numbers.
    // - One writer per piece of text, same rule as ShowDistance() and ShowCoins().
    private void ShowGameOver()
    {
        if (gameOverScoreText != null) gameOverScoreText.text = "SCORE: " + Distance + "m";
        if (gameOverBestText != null) gameOverBestText.text = "BEST SCORE: " + HighScore + "m";

        // This run's haul, not your lifetime total. Use TotalCoins if you ever want the total.
        if (gameOverCoinText != null) gameOverCoinText.text = Coins.ToString();
    }

    // - Clears THIS run's coin count. Your saved total is left alone and keeps climbing.
    private void ResetCoins()
    {
        Coins = 0;
        ShowCoins();
    }

    // - The one and only place the coin counter is written.
    // - It shows TotalCoins, your saved total — NOT Coins, which resets every run. Using the
    //   wrong one of those two is why the number never looked like it was adding up.
    private void ShowCoins()
    {
        // You're saving towards a 300-coin fish, and a counter that goes back to 0 every run can
        // never show you getting closer to it.
        // Copes with the slot being empty, in case the counter hasn't been wired in yet.
        if (coinText != null)
        {
            coinText.text = TotalCoins.ToString();
        }
    }

    // - Draws the coin picture to the left of a counter, so the number reads as coins rather than
    //   as a second score.
    // - It's made a child of the counter, so it follows along wherever the counter is moved. The
    //   picture comes from the spawner's actual coin, so the two can never show different coins.
    public void BuildCoinIcon(TMP_Text target, float size)
    {
        if (target == null || coinSpawner == null) return;

        Sprite sprite = coinSpawner.CoinSprite;
        if (sprite == null) return;

        GameObject icon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(target.transform, false);

        // Pinned INSIDE the counter's left edge and hanging from its own left side, so it can't
        // end up off the screen the way it did when it hung from its right side outside the box
        RectTransform rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(coinIconInset, 0f);

        // Save the coin a strip of space, so the number is laid out clear of it inside the same box
        target.margin = new Vector4(coinIconInset + size + coinIconTextGap, 0f, 0f, 0f);

        // The coin sits in the middle of the box vertically, so the number has to as well or it
        // rides up above the coin. The scene had this text saved as top-aligned.
        target.verticalAlignment = VerticalAlignmentOptions.Middle;

        Image image = icon.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        // Never swallow a tap that was meant for the fish
        image.raycastTarget = false;
    }

    // - Sets up the shop's coin total to match the in-game one exactly.
    // - They show the SAME number, and showing it at two different sizes makes it look like two
    //   different things — which is what 73.6/78 next to 80/50 looked like. Both sizes are taken
    //   from the in-game counter here, so tuning that one moves the shop's to match.
    public void BuildWalletCounter(TMP_Text target)
    {
        if (target == null) return;

        if (coinText != null) target.fontSize = coinText.fontSize;
        BuildCoinIcon(target, coinIconSize);
    }

    // - Shows or hides both run counters together.
    // - They sit on the Canvas right next to the menu, so nothing hides them unless we do. One of
    //   them was saved switched OFF in the scene and the other saved switched ON, so this is what
    //   finally gets them agreeing: both appear with the run, both go away with the menu.
    private void ShowRunCounters(bool visible)
    {
        foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
        {
            if (counter != null) counter.gameObject.SetActive(visible);
        }
    }

}
