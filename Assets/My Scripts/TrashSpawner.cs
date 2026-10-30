using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Drops floating rubbish for the fish to dodge.
// Also in charge of making the WHOLE game harder as the run goes on: the
// longer you last, the faster the cave moves and the more rubbish comes.
[RequireComponent(typeof(CaveSpawner))]
public class TrashSpawner : MonoBehaviour
{
    [Header("Debris")]
    [Tooltip("The bits of rubbish to pick from. If one is missing a collider we add one.")]
    [FormerlySerializedAs("trashPrefabs")]
    [SerializeField] private GameObject[] trashPrefabsArray;

    [Tooltip("How big a piece is. Keep it smaller than the fish, which is about 1.33.")]
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 0.9f;

    [Tooltip("How fast a piece spins, in degrees a second. Which way round is random.")]
    [SerializeField] private float minSpin = 10f;
    [SerializeField] private float maxSpin = 45f;

    [Header("Drift")]
    [Tooltip("How far a piece is allowed to float up or down from where it was dropped.")]
    [SerializeField] private float driftY = 0.4f;

    [Tooltip("How fast rubbish floats about, in units a second.")]
    [SerializeField] private float driftMinSpeed = 0.15f;
    [SerializeField] private float driftMaxSpeed = 0.5f;

    [Header("Difficulty ramp")]
    [Tooltip("How many seconds of play before the game is as hard as it gets. Nothing gets " +
             "harder after this.")]
    [SerializeField] private float rampSeconds = 90f;

    // This also sets how long you swim through empty water before the
    // first rocks: about 8.8 units of space divided by this speed.
    [Tooltip("How fast things slide past at the START of a run, as a multiple of the rocks' own " +
             "speed of 5. Turning this up means less empty swimming before the first rocks.")]
    [SerializeField] private float startSpeedMultiplier = 1.15f;

    [Tooltip("How fast things slide past once it's as hard as it gets. 1.6 turns the rocks' " +
             "speed of 5 into 8.")]
    [SerializeField] private float maxSpeedMultiplier = 1.6f;

    [Tooltip("Seconds between pieces of rubbish at the start of a run.")]
    [SerializeField] private float startInterval = 3f;

    [Tooltip("Seconds between pieces once it's as hard as it gets. Never quicker than this.")]
    [SerializeField] private float minInterval = 0.9f;

    [Tooltip("Most pieces allowed on screen at once, no matter how long the run lasts.")]
    [SerializeField] private int maxOnScreen = 8;

    [Header("Currents")]
    // Careful above 1.4. By full difficulty the cave already moves at 8
    // a second, and a surge cuts your thinking time as much as it adds.
    [Tooltip("How strong a surge gets at FULL difficulty, as a multiple of the current speed. " +
             "It builds up from 1 over Ramp Seconds, so early surges are gentler. 1 = no currents.")]
    [SerializeField] private float surgeStrength = 1.3f;

    [Tooltip("How many seconds one surge takes to build up and fade away again.")]
    [SerializeField] private float surgeSeconds = 2.5f;

    [Tooltip("Calm seconds between surges. Always the same, never random, so you can learn " +
             "the rhythm and plan around it.")]
    [SerializeField] private float surgeInterval = 9f;

    [Tooltip("How much faster than the cave the rubbish drifts. 0 locks it to the rocks.")]
    [SerializeField] private float debrisCurrentPush = 1.2f;

    // Still water. Both are multipliers, and multiplying by 1 changes
    // nothing, so they are fixed rather than something to tune.
    private const float CALM_PACE = 1f;
    private const float CALM_SURGE = 1f;

    // How much of the full Surge Strength the earliest surges get. Half,
    // so you feel them but the deep cave is still twice as rough.
    private const float SURGE_AT_START = 0.5f;

    // Rubbish is made this far past the right edge, so you never see a
    // piece pop into existence.
    private const float SPAWN_MARGIN = 1f;

    private readonly List<GameObject> livePieces = new List<GameObject>();
    private CaveSpawner rockSpawner;
    private float elapsed;
    private float nextSpawn;
    private int lastIndex = -1;
    private bool spawning;

    // Finds the rock spawner, which owns the gap rubbish lands in.
    private void Awake()
    {
        rockSpawner = GetComponent<CaveSpawner>();
    }

    // Starts dropping rubbish, with the difficulty back at the start.
    public void StartSpawning()
    {
        ResetRun();

        if (!HasValidTrash()) return;

        spawning = true;
    }

    // Stops dropping rubbish and puts the cave back to calm.
    // The speed is shared by the whole game, so without this it would
    // survive into your next run.
    public void StopSpawning()
    {
        spawning = false;

        ObstacleMovement.SetPace(CALM_PACE, CALM_SURGE);
    }

    // Wipes the run's progress and resets the cave's speed.
    private void ResetRun()
    {
        livePieces.Clear();
        elapsed = 0f;
        lastIndex = -1;
        nextSpawn = startInterval;

        // The surge is reset as well as the speed. Update() would
        // overwrite it next frame, but an old value sitting in something
        // called "the current surge" makes both numbers untrustworthy.
        ObstacleMovement.SetPace(startSpeedMultiplier, CALM_SURGE);
    }

    // Runs the whole difficulty curve: winds the cave's speed up, adds a
    // surge on top, and drops rubbish with a shrinking gap between it.
    // Publishes ONE shared speed, so rocks, rubbish and coins stay in step.
    private void Update()
    {
        if (!spawning) return;

        elapsed += Time.deltaTime;

        // 0 to 1 over rampSeconds, then stays at 1.
        float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
        float ramped = Mathf.Lerp(startSpeedMultiplier, maxSpeedMultiplier, progress);

        // The surge sits on top, published separately so the fish feels it.
        float surge = Surge(progress);
        ObstacleMovement.SetPace(ramped * surge, surge);

        livePieces.RemoveAll(piece => piece == null);

        if (elapsed < nextSpawn) return;

        nextSpawn = elapsed + Mathf.Lerp(startInterval, minInterval, progress);

        if (livePieces.Count >= maxOnScreen) return;

        SpawnPiece();
    }

    // How strong the current is right now: a wave passing through.
    // Worked out purely from the clock, so nothing is remembered between
    // frames, and it multiplies the one shared speed.
    private float Surge(float progress)
    {
        if (surgeStrength <= 1f || surgeSeconds <= 0f || surgeInterval <= 0f) return 1f;

        float phase = elapsed % (surgeInterval + surgeSeconds);
        if (phase < surgeInterval) return 1f;

        // Grows on the same difficulty counter as the speed: shallow
        // water shoves a bit, the deep cave shoves twice as hard.
        // The rubbish gets this free, because ObstacleDrift multiplies
        // its own push by the surge.
        float strength = Mathf.Lerp(1f + (surgeStrength - 1f) * SURGE_AT_START, surgeStrength, progress);

        // Sin() draws a smooth arch, so the surge swells and fades
        // instead of snapping on and off like a switch.
        float t = (phase - surgeInterval) / surgeSeconds;
        return 1f + (strength - 1f) * Mathf.Sin(t * Mathf.PI);
    }

    // Drops one piece just off the right edge, inside the clear gap.
    // Gives up if the rocks are not built that far yet, or the gap is
    // too narrow for the piece plus room to spin and float.
    private void SpawnPiece()
    {
        int index = PickTrash(lastIndex);
        GameObject prefab = trashPrefabsArray[index];

        Vector2 spriteSize = prefab.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float scale = Random.Range(minSize, maxSize) / Mathf.Max(spriteSize.x, spriteSize.y);

        // A piece spins while it floats, so it needs its corner-to-corner
        // length, not its width.
        float radius = new Vector2(spriteSize.x, spriteSize.y).magnitude * scale * 0.5f;

        // The piece plus how far it may wander, so it never ends up
        // inside a rock.
        float clearance = radius + driftY;

        float x = CameraEdges.Right + SPAWN_MARGIN;
        if (!rockSpawner.TryGetGapAt(x, out float low, out float high)) return;
        if (high - low < clearance * 2f) return;

        GameObject piece = Instantiate(prefab, transform);
        piece.transform.position = new Vector3(x, Random.Range(low + clearance, high - clearance), 0f);
        piece.transform.localScale = new Vector3(scale, scale, 1f);

        // In front of the rocks and the fish, so it floats in front of
        // the wall rather than looking stuck inside it.
        piece.GetComponent<SpriteRenderer>().sortingOrder = 2;

        // Every piece already has a fitted collider. This is a safety net
        // for a new one added without.
        if (!piece.TryGetComponent(out Collider2D _))
        {
            piece.AddComponent<PolygonCollider2D>();
        }

        // Makes it slide with the cave, and lets restart clean-up find it.
        piece.AddComponent<ObstacleMovement>();

        float spin = Random.Range(minSpin, maxSpin) * (Random.value < 0.5f ? -1f : 1f);
        piece.AddComponent<TrashSpin>().SetSpin(spin);

        ObstacleDrift drift = piece.AddComponent<ObstacleDrift>();
        drift.Configure(driftY, driftMinSpeed, driftMaxSpeed);
        drift.ConfigureCurrent(rockSpawner, debrisCurrentPush, radius);

        livePieces.Add(piece);
        lastIndex = index;
    }

    // Picks a random piece, never the same one twice in a row.
    // Picks from a list one shorter and skips past the last one, so
    // every other piece is equally likely with no re-rolling.
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

    // Checks every piece is usable before the run starts.
    // The pictures too, because SpawnPiece measures them: without this
    // it would crash on the first piece instead of saying what is wrong.
    private bool HasValidTrash()
    {
        if (trashPrefabsArray == null || trashPrefabsArray.Length == 0)
        {
            Debug.LogError("TrashSpawner: Trash Prefabs is empty. Drag the trash prefabs into it on SpawnObstacles.", this);
            return false;
        }

        foreach (GameObject prefab in trashPrefabsArray)
        {
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
