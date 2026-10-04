using System.Collections.Generic;
using UnityEngine;

// Drops coins into the cave's opening for the fish to collect. Deliberately has no speed
// ramp of its own: coins carry ObstacleMovement and ride the shared SpeedMultiplier that
// TrashSpawner drives, so two scripts never fight over one static. Only the spawn rate
// ramps here. Lives on MyObstacleSpawner's GameObject, so ClearObstacles() sweeps coins too.
[RequireComponent(typeof(MyObstacleSpawner))]
public class CoinSpawner : MonoBehaviour
{
    [Header("Coin")]
    [Tooltip("The coin prefab. Its collider must stay Is Trigger, or pickup kills the fish.")]
    [SerializeField] private GameObject coinPrefab;

    [Tooltip("Largest dimension of a coin, in world units. Keep it under the fish's 1.33.")]
    [SerializeField] private float coinSize = 0.7f;

    [Tooltip("Degrees per second a coin turns on the spot. Direction is random, 0 for none.")]
    [SerializeField] private float spinDegrees = 25f;

    [Header("Drift")]
    [Tooltip("How far a coin may rise or sink. The same is kept clear of the cave walls.")]
    [SerializeField] private float driftY = 0.4f;

    [Tooltip("Clear space between a coin and the rock face, beyond the coin's own radius.")]
    [SerializeField] private float wallGap = 0.35f;

    [Tooltip("How quickly coins wander, in world units per second.")]
    [SerializeField] private float driftMinSpeed = 0.15f;
    [SerializeField] private float driftMaxSpeed = 0.5f;

    [Header("How often")]
    [Tooltip("Seconds of play before coins come at their fastest. Matches TrashSpawner.")]
    [SerializeField] private float rampSeconds = 90f;

    [Tooltip("Seconds between coins at the start of a run.")]
    [SerializeField] private float startInterval = 2.5f;

    [Tooltip("Seconds between coins at full speed. 1 means at most one a second, ever.")]
    [SerializeField] private float minInterval = 1f;

    [Tooltip("Most coins allowed on screen at once. A second cap, on top of the interval.")]
    [SerializeField] private int maxOnScreen = 5;

    [Tooltip("Extra leftward speed for coins, in units per second. 0 locks them to the cave.")]
    [SerializeField] private float coinCurrentPush = 1.2f;

    // Lets the HUD show the same coin art without a second Inspector slot
    public Sprite CoinSprite
    {
        get
        {
            if (coinPrefab == null) return null;

            SpriteRenderer renderer = coinPrefab.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }
    }

    private readonly List<GameObject> liveCoinsList = new List<GameObject>();
    private MyObstacleSpawner rockSpawner;
    private float elapsed;
    private float nextSpawn;
    private bool spawning;

    private void Awake()
    {
        rockSpawner = GetComponent<MyObstacleSpawner>();
    }

    public void StartSpawning()
    {
        liveCoinsList.Clear();
        elapsed = 0f;
        nextSpawn = startInterval;

        if (!HasValidCoin()) return;

        spawning = true;
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    private void Update()
    {
        if (!spawning) return;

        elapsed += Time.deltaTime;

        // Collected and despawned coins leave nulls behind
        liveCoinsList.RemoveAll(coin => coin == null);

        if (elapsed < nextSpawn) return;

        // Eases to minInterval, then Clamp01 holds it at the cap
        float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
        nextSpawn = elapsed + Mathf.Lerp(startInterval, minInterval, progress);

        if (liveCoinsList.Count >= maxOnScreen) return;

        SpawnCoin();
    }

    private void SpawnCoin()
    {
        Vector2 spriteSize = coinPrefab.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float scale = coinSize / Mathf.Max(spriteSize.x, spriteSize.y);

        // A coin turns as it drifts, so the room it needs is its diagonal
        float radius = new Vector2(spriteSize.x, spriteSize.y).magnitude * scale * 0.5f;

        // Its own reach, however far it may wander, plus the gap that should still show
        float clearance = radius + driftY + wallGap;

        float x = CameraRightEdge() + 1f;
        if (!rockSpawner.TryGetGapAt(x, out float low, out float high)) return;
        if (high - low < clearance * 2f) return;

        GameObject coin = Instantiate(coinPrefab, transform);
        coin.transform.position = new Vector3(x, Random.Range(low + clearance, high - clearance), 0f);
        coin.transform.localScale = new Vector3(scale, scale, 1f);

        // The prefab ships with its trigger collider and sorting order 2, so neither is set here
        // Holds the coin at the cave's speed, and lets ClearObstacles() find it on restart
        coin.AddComponent<ObstacleMovement>();

        if (spinDegrees > 0f)
        {
            coin.AddComponent<TrashSpin>().SetSpin(spinDegrees * (Random.value < 0.5f ? -1f : 1f));
        }

        ObstacleDrift drift = coin.AddComponent<ObstacleDrift>();
        drift.Configure(driftY, driftMinSpeed, driftMaxSpeed);
        drift.ConfigureCurrent(rockSpawner, coinCurrentPush, radius, wallGap);
        coin.AddComponent<CoinPickup>();

        liveCoinsList.Add(coin);
    }

    private bool HasValidCoin()
    {
        if (coinPrefab == null)
        {
            Debug.LogError("CoinSpawner: no Coin Prefab assigned. Drag the coin prefab into the slot on SpawnObstacles.", this);
            return false;
        }

        // The sprite is checked too: SpawnCoin() reads sprite.bounds, so an empty
        // Sprite Renderer would throw on the first spawn rather than be caught here
        SpriteRenderer renderer = coinPrefab.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            Debug.LogError("CoinSpawner: the Coin Prefab has no Sprite Renderer, or no sprite in it.", this);
            return false;
        }

        return true;
    }

    private float CameraRightEdge()
    {
        Camera cam = Camera.main;
        return cam.transform.position.x + cam.orthographicSize * cam.aspect;
    }
}
