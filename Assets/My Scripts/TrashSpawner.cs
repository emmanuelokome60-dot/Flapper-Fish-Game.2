using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Drops floating debris for the fish to dodge, and drives the difficulty ramp for the whole
// run: the longer it lasts, the faster everything scrolls and the more often debris appears.
// Both climb to a ceiling and stop. Lives on ObstacleSpawner's GameObject, so
// ClearObstacles() sweeps debris too.
[RequireComponent(typeof(ObstacleSpawner))]
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

    // Also sets how long the fish swims through empty water before the first rocks arrive: the
    // dead distance is about 8.8 units with the fish at mid-screen, so the wait is that
    // divided by this speed: 1.54s at 16:9, 1.87s on a 19.5:9 phone.
    [Tooltip("Scroll speed at the start of a run, as a multiple of the rocks' own 5 units/sec. " +
             "Raising it shortens the empty swim before the first rocks reach the fish.")]
    [SerializeField] private float startSpeedMultiplier = 1.15f;

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
    [Tooltip("Peak of a surge at FULL difficulty, as a multiple of the current speed. Ramps up " +
             "from 1 across Ramp Seconds, so early surges are gentler. 1 turns currents off.")]
    [SerializeField] private float surgeStrength = 1.3f;

    [Tooltip("How long one surge takes to swell and fade, in seconds.")]
    [SerializeField] private float surgeSeconds = 2.5f;

    [Tooltip("Calm seconds between surges. Fixed, not random, so the rhythm is learnable.")]
    [SerializeField] private float surgeInterval = 9f;

    [Tooltip("Extra leftward speed for debris, in units per second. 0 locks it to the cave.")]
    [SerializeField] private float debrisCurrentPush = 1.2f;

    // Still water: the cave at its own speed with no swell on top. Both are multipliers, so 1
    // is the identity rather than a tuned value, which is why they are consts and not fields.
    private const float CALM_PACE = 1f;
    private const float CALM_SURGE = 1f;

    // How much of Surge Strength the first surges carry, before the ramp has built. Half, so the
    // opening currents are felt but the deep cave is still twice as rough.
    // ponytail: a const, not a field. A new SerializeField on a component already saved in the
    // scene comes back as 0 in the Inspector, and 0 here silently restores the glassy opening.
    private const float SURGE_AT_START = 0.5f;

    // Spawned this far past the right edge so a piece is never seen appearing
    private const float SPAWN_MARGIN = 1f;

    private readonly List<GameObject> livePieces = new List<GameObject>();
    private ObstacleSpawner rockSpawner;
    private float elapsed;
    private float nextSpawn;
    private int lastIndex = -1;
    private bool spawning;

    // - Finds the rock spawner on this same object, which owns the corridor debris must land in.
    private void Awake()
    {
        rockSpawner = GetComponent<ObstacleSpawner>();
    }

    // - Opens the debris tap for a new run after resetting the difficulty ramp.
    public void StartSpawning()
    {
        ResetRun();

        if (!HasValidTrash()) return;

        spawning = true;
    }

    // - Closes the tap and puts the cave's shared speed back to calm.
    // - Those are statics, so without this they would survive into the next run.
    public void StopSpawning()
    {
        spawning = false;

        // Statics, so they would otherwise survive into the next run
        ObstacleMovement.SetPace(CALM_PACE, CALM_SURGE);
    }

    // - Clears the run's state and puts the cave back to its opening speed.
    // - Sets the surge too, not just the multiplier: leaving a stale swell behind is how a
    //   reader ends up mistrusting both numbers.
    private void ResetRun()
    {
        livePieces.Clear();
        elapsed = 0f;
        lastIndex = -1;
        nextSpawn = startInterval;

        // Surge is set here too, not just the multiplier. Update() overwrites it on the next
        // frame either way, but leaving a stale swell in a field called "the current surge" is
        // how a reader ends up mistrusting both of them.
        ObstacleMovement.SetPace(startSpeedMultiplier, CALM_SURGE);
    }

    // - Drives the whole run's difficulty: eases the cave's speed up to its ceiling, rides a
    //   surge on top, and drops debris at a tightening interval.
    // - Publishes one shared speed, so rocks, debris and coins cannot drift out of step.
    private void Update()
    {
        if (!spawning) return;

        elapsed += Time.deltaTime;

        // Eases to the ceiling across rampSeconds, then Clamp01 holds it there
        float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
        float ramped = Mathf.Lerp(startSpeedMultiplier, maxSpeedMultiplier, progress);

        // The current rides on top of the ramp, and is published so the fish feels it too
        float surge = Surge(progress);
        ObstacleMovement.SetPace(ramped * surge, surge);

        livePieces.RemoveAll(piece => piece == null);

        if (elapsed < nextSpawn) return;

        nextSpawn = elapsed + Mathf.Lerp(startInterval, minInterval, progress);

        if (livePieces.Count >= maxOnScreen) return;

        SpawnPiece();
    }

    // A swell passing through the cave. Multiplying the one shared speed keeps rocks, debris
    // and coins in lockstep. Derived from elapsed, so it needs no state of its own.
    // - Returns the swell passing through the cave right now, 1 in calm water.
    // - Derived from elapsed so it needs no state of its own, and ramped with depth so shallow
    //   water shoves gently and the deep cave shoves hard.
    private float Surge(float progress)
    {
        if (surgeStrength <= 1f || surgeSeconds <= 0f || surgeInterval <= 0f) return 1f;

        float phase = elapsed % (surgeInterval + surgeSeconds);
        if (phase < surgeInterval) return 1f;

        // The swell grows with the same ramp that drives speed: shallow water already shoves, the
        // deep cave shoves twice as hard. Debris rides this for free -- ObstacleDrift multiplies
        // its own push by Surge, so the deeper current carries the trash faster with no second
        // number to keep in step.
        //
        // Starts at a fraction of full rather than at 1, so the very first surge is felt. Lerping
        // from 1 made the opening minute glassy, which read as the currents being broken.
        float strength = Mathf.Lerp(1f + (surgeStrength - 1f) * SURGE_AT_START, surgeStrength, progress);

        // A sine arch, so the swell eases in and out instead of snapping on
        float t = (phase - surgeInterval) / surgeSeconds;
        return 1f + (strength - 1f) * Mathf.Sin(t * Mathf.PI);
    }

    // - Drops one piece of debris just past the right edge, inside the clear corridor.
    // - Gives up if the rocks have not reached that far, or the gap is too tight for the piece
    //   plus the room it needs to turn and wander.
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

        float x = CameraEdges.Right + SPAWN_MARGIN;
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

        livePieces.Add(piece);
        lastIndex = index;
    }

    // Any piece except the one used last time
    // - Picks a trash prefab that is not the one used last time, so two never arrive in a row.
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

    // - Checks every trash prefab can be spawned before the run starts.
    // - The sprite is checked too, since SpawnPiece reads sprite.bounds and would otherwise
    //   throw on the first drop rather than report a clear error here.
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

}
