using System.Collections.Generic;
using UnityEngine;

// Drops coins into the gap between the rocks for the fish to collect.
// It does NOT speed coins up: they carry an ObstacleMovement that reads
// the one shared cave speed. Only how OFTEN they appear changes here.
// Sits on the same object as CaveSpawner, so clearing the rocks clears
// the coins at the same time.
[RequireComponent(typeof(CaveSpawner))]
public class CoinSpawner : MonoBehaviour
{
    [Header("Coin")]
    [Tooltip("The coin to copy. Its collider MUST stay set to Is Trigger, or collecting a coin " +
             "will kill the fish instead.")]
    [SerializeField] private GameObject coinPrefab;

    [Tooltip("How big a coin is. Keep it smaller than the fish, which is about 1.33.")]
    [SerializeField] private float coinSize = 0.7f;

    [Tooltip("How fast a coin spins on the spot, in degrees a second. 0 for no spin.")]
    [SerializeField] private float spinDegrees = 25f;

    [Header("Drift")]
    [Tooltip("How far a coin is allowed to float up or down from where it was dropped.")]
    [SerializeField] private float driftY = 0.4f;

    [Tooltip("How much clear space to leave between a coin and the rocks.")]
    [SerializeField] private float wallGap = 0.35f;

    [Tooltip("How fast coins float about, in units a second.")]
    [SerializeField] private float driftMinSpeed = 0.15f;
    [SerializeField] private float driftMaxSpeed = 0.5f;

    [Header("How often")]
    [Tooltip("How many seconds of play before coins come as fast as they ever will. Should " +
             "match TrashSpawner.")]
    [SerializeField] private float rampSeconds = 90f;

    [Tooltip("Seconds between coins at the start of a run.")]
    [SerializeField] private float startInterval = 2.5f;

    [Tooltip("Seconds between coins once it's as fast as it gets. 1 means one a second at most.")]
    [SerializeField] private float minInterval = 1f;

    [Tooltip("Most coins allowed on screen at once. A second limit, on top of the timer above.")]
    [SerializeField] private int maxOnScreen = 5;

    [Tooltip("How much faster than the cave coins drift. 0 locks them to the rocks.")]
    [SerializeField] private float coinCurrentPush = 1.2f;

    // Lets the coin counter use the same picture with no extra slot.
    public Sprite CoinSprite
    {
        get
        {
            if (coinPrefab == null) return null;

            SpriteRenderer renderer = coinPrefab.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }
    }

    // Coins are made this far past the right edge, so you never see one
    // pop into existence.
    private const float SPAWN_MARGIN = 1f;

    private readonly List<GameObject> liveCoins = new List<GameObject>();
    private CaveSpawner rockSpawner;
    private float elapsed;
    private float nextSpawn;
    private bool spawning;

    // Finds the rock spawner, the only thing that knows where the gap
    // between the walls is, which is where coins have to go.
    private void Awake()
    {
        rockSpawner = GetComponent<CaveSpawner>();
    }

    // Starts dropping coins for a new run.
    // A coin set up wrong just does not start, so you get a run with no
    // coins instead of an error every frame.
    public void StartSpawning()
    {
        liveCoins.Clear();
        elapsed = 0f;
        nextSpawn = startInterval;

        if (!HasValidCoin()) return;

        spawning = true;
    }

    // Stops dropping new coins. GameManager clears the rest.
    public void StopSpawning()
    {
        spawning = false;
    }

    // Counts down to the next coin and drops one when the timer runs
    // out. The wait gets shorter as the run goes on.
    private void Update()
    {
        if (!spawning) return;

        elapsed += Time.deltaTime;

        // Collected coins leave holes in the list, so clear them.
        liveCoins.RemoveAll(coin => coin == null);

        if (elapsed < nextSpawn) return;

        // 0 to 1 over rampSeconds, then stays at 1.
        float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
        nextSpawn = elapsed + Mathf.Lerp(startInterval, minInterval, progress);

        if (liveCoins.Count >= maxOnScreen) return;

        SpawnCoin();
    }

    // Puts one coin just off the right edge, inside the clear gap.
    // Gives up quietly if the rocks are not built that far yet, or the
    // gap is too narrow for the coin plus its room to float.
    private void SpawnCoin()
    {
        Vector2 spriteSize = coinPrefab.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float scale = coinSize / Mathf.Max(spriteSize.x, spriteSize.y);

        // A coin spins while it floats, so it needs its corner-to-corner
        // length, not its width.
        float radius = new Vector2(spriteSize.x, spriteSize.y).magnitude * scale * 0.5f;

        // The coin, plus how far it may wander, plus the gap to keep.
        float clearance = radius + driftY + wallGap;

        float x = CameraEdges.Right + SPAWN_MARGIN;
        if (!rockSpawner.TryGetGapAt(x, out float low, out float high)) return;
        if (high - low < clearance * 2f) return;

        GameObject coin = Instantiate(coinPrefab, transform);
        coin.transform.position = new Vector3(x, Random.Range(low + clearance, high - clearance), 0f);
        coin.transform.localScale = new Vector3(scale, scale, 1f);

        // Makes it slide with the cave, and lets restart clean-up find it.
        coin.AddComponent<ObstacleMovement>();

        if (spinDegrees > 0f)
        {
            coin.AddComponent<TrashSpin>().SetSpin(spinDegrees * (Random.value < 0.5f ? -1f : 1f));
        }

        ObstacleDrift drift = coin.AddComponent<ObstacleDrift>();
        drift.Configure(driftY, driftMinSpeed, driftMaxSpeed);
        drift.ConfigureCurrent(rockSpawner, coinCurrentPush, radius, wallGap);
        coin.AddComponent<CoinPickup>();

        liveCoins.Add(coin);
    }

    // Checks the coin is usable before the run starts.
    // The picture too, because SpawnCoin measures it: without this it
    // would crash on the first coin instead of saying what is wrong.
    private bool HasValidCoin()
    {
        if (coinPrefab == null)
        {
            Debug.LogError("CoinSpawner: no Coin Prefab assigned. Drag the coin prefab into the slot on SpawnObstacles.", this);
            return false;
        }

        SpriteRenderer renderer = coinPrefab.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            Debug.LogError("CoinSpawner: the Coin Prefab has no Sprite Renderer, or no sprite in it.", this);
            return false;
        }

        return true;
    }

}
