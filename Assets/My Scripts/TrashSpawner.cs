using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Drops floating debris for the fish to dodge, and drives the difficulty ramp for the whole
// run: the longer it lasts, the faster everything scrolls and the more often debris appears.
// Both climb to a ceiling and stop. Lives on MyObstacleSpawner's GameObject, so
// ClearObstacles() sweeps debris too.
[RequireComponent(typeof(MyObstacleSpawner))]
public class TrashSpawner : MonoBehaviour
{
    [Header("Debris")]
    [Tooltip("Trash prefabs to pick from. A collider is added to any that lacks one.")]
    [FormerlySerializedAs("trashPrefabs")]
    [SerializeField] private GameObject[] trashPrefabsArray;

    [Tooltip("Largest dimension of a piece, in world units. Keep under the fish's 1.33.")]
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 0.9f;

    [Tooltip("Degrees per second a piece turns. Direction is random.")]
    [SerializeField] private float minSpin = 10f;
    [SerializeField] private float maxSpin = 45f;

    [Header("Drift")]
    [Tooltip("How far a piece may rise or sink. The same is kept clear of the cave walls.")]
    [SerializeField] private float driftY = 0.4f;

    [Tooltip("How quickly debris wanders, in world units per second.")]
    [SerializeField] private float driftMinSpeed = 0.15f;
    [SerializeField] private float driftMaxSpeed = 0.5f;

    [Header("Difficulty ramp")]
    [Tooltip("Seconds of play before full difficulty. Nothing gets harder after this.")]
    [SerializeField] private float rampSeconds = 90f;

    [Tooltip("Starting scroll speed, as a multiple. 0.7 turns the rocks' 5 into 3.5.")]
    [SerializeField] private float startSpeedMultiplier = 0.7f;

    [Tooltip("Scroll speed at full difficulty. 1.6 turns the rocks' 5 into 8.")]
    [SerializeField] private float maxSpeedMultiplier = 1.6f;

    [Tooltip("Seconds between pieces at the start of a run.")]
    [SerializeField] private float startInterval = 3f;

    [Tooltip("Seconds between pieces at full difficulty. Never faster than this.")]
    [SerializeField] private float minInterval = 0.9f;

    [Tooltip("Most pieces allowed on screen at once, however long the run lasts.")]
    [SerializeField] private int maxOnScreen = 8;

    [Header("Currents")]
    // Careful above 1.4: at full difficulty the cave is already at 8 units/sec, and a
    // surge cuts reaction time by the same proportion it adds speed.
    [Tooltip("Peak of a surge, as a multiple of the current speed. 1 turns currents off.")]
    [SerializeField] private float surgeStrength = 1.3f;

    [Tooltip("How long one surge takes to swell and fade, in seconds.")]
    [SerializeField] private float surgeSeconds = 2.5f;

    [Tooltip("Calm seconds between surges. Fixed, not random, so the rhythm is learnable.")]
    [SerializeField] private float surgeInterval = 9f;

    [Tooltip("Extra leftward speed for debris, in units per second. 0 locks it to the cave.")]
    [SerializeField] private float debrisCurrentPush = 1.2f;

    private readonly List<GameObject> livePiecesList = new List<GameObject>();
    private MyObstacleSpawner rockSpawner;
    private float elapsed;
    private float nextSpawn;
    private int lastIndex = -1;
    private bool spawning;

    private void Awake()
    {
        rockSpawner = GetComponent<MyObstacleSpawner>();
    }

    public void StartSpawning()
    {
        ResetRun();

        if (!HasValidTrash()) return;

        spawning = true;
    }

    public void StopSpawning()
    {
        spawning = false;

        // Statics, so they would otherwise survive into the next run
        ObstacleMovement.SpeedMultiplier = 1f;
        ObstacleMovement.Surge = 1f;
    }

    private void ResetRun()
    {
        livePiecesList.Clear();
        elapsed = 0f;
        lastIndex = -1;
        nextSpawn = startInterval;
        ObstacleMovement.SpeedMultiplier = startSpeedMultiplier;
    }

    private void Update()
    {
        if (!spawning) return;

        elapsed += Time.deltaTime;

        // Eases to the ceiling across rampSeconds, then Clamp01 holds it there
        float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
        float ramped = Mathf.Lerp(startSpeedMultiplier, maxSpeedMultiplier, progress);

        // The current rides on top of the ramp, and is published so the fish feels it too
        float surge = Surge();
        ObstacleMovement.Surge = surge;
        ObstacleMovement.SpeedMultiplier = ramped * surge;

        livePiecesList.RemoveAll(piece => piece == null);

        if (elapsed < nextSpawn) return;

        nextSpawn = elapsed + Mathf.Lerp(startInterval, minInterval, progress);

        if (livePiecesList.Count >= maxOnScreen) return;

        SpawnPiece();
    }

    // A swell passing through the cave. Multiplying the one shared speed keeps rocks, debris
    // and coins in lockstep. Derived from elapsed, so it needs no state of its own.
    private float Surge()
    {
        if (surgeStrength <= 1f || surgeSeconds <= 0f || surgeInterval <= 0f) return 1f;

        float phase = elapsed % (surgeInterval + surgeSeconds);
        if (phase < surgeInterval) return 1f;

        // A sine arch, so the swell eases in and out instead of snapping on
        float t = (phase - surgeInterval) / surgeSeconds;
        return 1f + (surgeStrength - 1f) * Mathf.Sin(t * Mathf.PI);
    }

    private void SpawnPiece()
    {
        int index = PickTrash(lastIndex);
        GameObject prefab = trashPrefabsArray[index];

        Vector2 spriteSize = prefab.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float scale = Random.Range(minSize, maxSize) / Mathf.Max(spriteSize.x, spriteSize.y);

        // A piece turns as it drifts, so the room it needs is its diagonal
        float radius = new Vector2(spriteSize.x, spriteSize.y).magnitude * scale * 0.5f;

        // Its own reach plus however far it may wander, so it can never end up inside a rock
        float clearance = radius + driftY;

        float x = CameraRightEdge() + 1f;
        if (!rockSpawner.TryGetGapAt(x, out float low, out float high)) return;
        if (high - low < clearance * 2f) return;

        GameObject piece = Instantiate(prefab, transform);
        piece.transform.position = new Vector3(x, Random.Range(low + clearance, high - clearance), 0f);
        piece.transform.localScale = new Vector3(scale, scale, 1f);

        // Above the rocks and the fish, so debris reads as floating in front of the wall
        piece.GetComponent<SpriteRenderer>().sortingOrder = 2;

        // All nine prefabs ship with a fitted collider, so this is a net for any added without one
        if (!piece.TryGetComponent(out Collider2D _))
        {
            piece.AddComponent<PolygonCollider2D>();
        }

        // Baseline cave speed, and lets ClearObstacles() find the piece on restart
        piece.AddComponent<ObstacleMovement>();

        float spin = Random.Range(minSpin, maxSpin) * (Random.value < 0.5f ? -1f : 1f);
        piece.AddComponent<TrashSpin>().SetSpin(spin);

        ObstacleDrift drift = piece.AddComponent<ObstacleDrift>();
        drift.Configure(driftY, driftMinSpeed, driftMaxSpeed);
        drift.ConfigureCurrent(rockSpawner, debrisCurrentPush, radius);

        livePiecesList.Add(piece);
        lastIndex = index;
    }

    // Any piece except the one used last time
    private int PickTrash(int lastUsed)
    {
        if (trashPrefabsArray.Length == 1 || lastUsed < 0)
        {
            return Random.Range(0, trashPrefabsArray.Length);
        }

        int index = Random.Range(0, trashPrefabsArray.Length - 1);
        if (index >= lastUsed) index++;
        return index;
    }

    private bool HasValidTrash()
    {
        if (trashPrefabsArray == null || trashPrefabsArray.Length == 0)
        {
            Debug.LogError("TrashSpawner: Trash Prefabs is empty. Drag the trash prefabs into it on SpawnObstacles.", this);
            return false;
        }

        foreach (GameObject prefab in trashPrefabsArray)
        {
            // The sprite is checked too: SpawnPiece() reads sprite.bounds, so an empty
            // Sprite Renderer would throw on the first spawn rather than be caught here
            SpriteRenderer renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null)
            {
                Debug.LogError("TrashSpawner: a Trash Prefabs slot is empty, has no Sprite Renderer, or has no sprite in it.", this);
                return false;
            }
        }

        return true;
    }

    private float CameraRightEdge()
    {
        Camera cam = Camera.main;
        return cam.transform.position.x + cam.orthographicSize * cam.aspect;
    }
}
