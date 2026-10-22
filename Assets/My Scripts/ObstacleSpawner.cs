using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Builds two endless rows of rocks, one hanging from the ceiling and one standing on the
// ground, always leaving an opening of at least minGap for the fish to swim through.
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Rocks")]
    [Tooltip("Rock prefabs to pick from. Each is drawn hanging down; bottom rocks are flipped.")]
    [FormerlySerializedAs("rockPrefabs")]
    [SerializeField] private GameObject[] rockPrefabsArray;

    [Header("Space for the fish")]
    [Tooltip("The opening at the mouth of the run, where every run begins. The fish is 1.33 " +
             "tall, its collider 1.11.")]
    [SerializeField] private float minGap = 3f;

    [Tooltip("The opening once the cave is at its darkest. Narrower than Min Gap, or the cave " +
             "never closes in. GameManager's Darkest At Distance decides when that is.")]
    [SerializeField] private float deepGap = 2.6f;

    [Tooltip("Every rock reaches at least this far in, so none is just a sliver.")]
    [SerializeField] private float minDepth = 2f;

    [Tooltip("How much deeper or shallower than the rock before it. Lower = smoother walls.")]
    [SerializeField] private float maxDepthChange = 1.2f;

    [Tooltip("World Y of the top of the Ground. Bottom rocks stand on it.")]
    [SerializeField] private float floorY = -4f;

    [Header("Size limits")]
    [Tooltip("Rocks are scaled to reach their given depth. These stop them becoming silly.")]
    [SerializeField] private float minScale = 0.35f;
    [SerializeField] private float maxScale = 1.2f;

    [Tooltip("Widest a single rock may be, in world units. The screen is about 17.8 wide.")]
    [SerializeField] private float maxWidth = 8f;

    [Tooltip("How far each rock tucks under its neighbour and past the edge, so no seams show.")]
    [SerializeField] private float overlap = 0.2f;

    // One placed rock, remembered so the other row knows how much space is left
    private class Rock
    {
        public Transform rockTransform;
        public float halfWidth;
        public float depth;

        public float Left => rockTransform.position.x - halfWidth;
        public float Right => rockTransform.position.x + halfWidth;
    }

    // One row. It only needs its last rock to know where the next one goes.
    private class Row
    {
        public readonly bool isTop;
        public readonly List<Rock> rocks = new List<Rock>();
        public Rock last;
        public int lastIndex = -1;
        public float startX;

        public Row(bool isTop)
        {
            this.isTop = isTop;
        }

        public float RightEdge => last != null && last.rockTransform != null ? last.Right : startX;
    }

    // Below about twice the fish's 1.11 collider the corridor stops being a test of timing and
    // becomes a coin flip -- more so on a phone, where a thumb covers the fish.
    private const float ABSOLUTE_MIN_GAP = 2.2f;

    // The rows start just past the right edge so the first rocks slide in rather than pop in
    private const float ROW_START_MARGIN = 0.5f;

    // How far behind the camera a rock is forgotten. Only wants to be past anything the
    // spawner still measures against; the rock itself is deleted by ObstacleMovement.
    private const float FORGET_MARGIN = 5f;

    private readonly Row topRow = new Row(true);
    private readonly Row bottomRow = new Row(false);
    private bool spawning;

    // The GameManager decides when spawning starts, so nothing spawns on scene load
    // - Opens the rock tap for a new run and parks both rows just past the right edge.
    public void StartSpawning()
    {
        // Rows start past the right edge, so the first rocks slide in. That swim is the whole
        // of the player's breathing room: about 1.5s at 16:9, 1.9s on a 19.5:9 phone.
        if (!HasValidRocks()) return;

        // The clamp in CurrentGap() would otherwise rescue a bad number in silence, which is how
        // a cave nobody can fit through ships looking deliberate.
        if (deepGap < ABSOLUTE_MIN_GAP)
        {
            Debug.LogWarning("ObstacleSpawner: Deep Gap " + deepGap + " is below the "
                             + ABSOLUTE_MIN_GAP + " the fish needs, and is being clamped to it. "
                             + "Lower TrashSpawner's Surge Strength instead.", this);
        }

        foreach (Row row in new[] { topRow, bottomRow })
        {
            row.rocks.Clear();
            row.last = null;
            row.lastIndex = -1;
            row.startX = CameraEdges.Right + ROW_START_MARGIN;
        }

        spawning = true;
    }

    // - Closes the tap. Rocks already placed are swept by GameManager.ClearObstacles.
    public void StopSpawning()
    {
        spawning = false;
    }

    // - Keeps both rows filled out to just past the right edge and forgets what has gone by.
    // - Rows are filled one rock at a time, each measured against the opposite row.
    private void Update()
    {
        if (!spawning) return;

        Forget(topRow);
        Forget(bottomRow);

        float fillTo = CameraEdges.Right + maxWidth;
        FillRow(topRow, bottomRow, fillTo);
        FillRow(bottomRow, topRow, fillTo);
    }

    // Drop rocks that have been deleted or scrolled well past the fish
    // - Drops rocks this row no longer needs to measure against, once they are well off screen.
    // - Only this bookkeeping; the rock object itself is deleted by ObstacleMovement.
    private void Forget(Row row)
    {
        float behind = CameraEdges.Left - FORGET_MARGIN;
        row.rocks.RemoveAll(rock => rock.rockTransform == null || rock.Right < behind);
    }

    // - Adds rocks to one row until it reaches the fill line.
    // - The loop limit is a safety net against a zero-width rock; one rock is normally enough.
    private void FillRow(Row row, Row otherRow, float fillTo)
    {
        // The limit is a safety net; one rock is normally enough
        for (int i = 0; i < 10 && row.RightEdge < fillTo; i++)
        {
            SpawnRock(row, otherRow);
        }
    }

    // Narrows on the same 0-1 that dims the light, so the walls close as the cave darkens: one
    // thing the player reads, not two systems to tune against each other. It also staggers the
    // difficulty for free -- CaveDepth saturates at 400m, roughly half way through the speed
    // ramp, so the gap is done closing while the speed is still climbing.
    private float CurrentGap()
    {
        float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
        return Mathf.Max(Mathf.Lerp(minGap, deepGap, depth), ABSOLUTE_MIN_GAP);
    }

    // - Places one rock hard against the last one in its row, scaled to reach a chosen depth.
    // - The opposite row is measured first, so the opening between them never drops below the
    //   current minimum gap.
    private void SpawnRock(Row row, Row otherRow)
    {
        int index = PickRock(row.lastIndex);
        GameObject prefab = rockPrefabsArray[index];

        // Parented to the spawner, so ClearObstacles() can find it
        GameObject rock = Instantiate(prefab, transform);

        Vector2 spriteSize = rock.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float startX = row.RightEdge - overlap;

        // Total depth the two rows may share, so the opening never drops below the gap this
        // deep into the cave
        float gap = CurrentGap();
        float budget = CameraEdges.Top - floorY - gap;

        // What the other row already took here, measured at this rock's widest possible reach
        float taken = DeepestBetween(otherRow, startX, startX + spriteSize.x * maxScale);
        float allowed = Mathf.Min(budget - taken, budget - minDepth);

        // Aim near the last depth, so the wall flows instead of jumping
        float previous = row.last != null ? row.last.depth : minDepth;

        // Scaled with the corridor: a 1-unit jog is a third of a 3.0 gap but nearly half of a
        // 2.6 one, and the fish has the same climb either way. Without this the deep cave
        // zig-zags faster than it can be swum through.
        float depthChange = maxDepthChange * (gap / minGap);
        float low = Mathf.Max(minDepth, previous - depthChange);
        float high = Mathf.Min(allowed, previous + depthChange);
        float wanted = Random.Range(Mathf.Min(low, high), Mathf.Max(low, high));

        // Scaled to reach that depth, so it always touches its edge of the screen
        float scale = (wanted + overlap) / spriteSize.y;
        scale = Mathf.Clamp(scale, minScale, maxScale);
        scale = Mathf.Min(scale, (allowed + overlap) / spriteSize.y);  // never eat into the gap
        scale = Mathf.Min(scale, maxWidth / spriteSize.x);             // never absurdly wide

        float width = spriteSize.x * scale;
        float height = spriteSize.y * scale;
        float depth = height - overlap;

        // A negative scale flips the collider too; the Sprite Renderer's Flip Y would not
        float mirror = Random.value < 0.5f ? -1f : 1f;
        float upsideDown = row.isTop ? 1f : -1f;
        rock.transform.localScale = new Vector3(scale * mirror, scale * upsideDown, 1f);

        // Right after the previous rock, flat edge pressed against the screen edge
        float x = startX + width / 2f;
        float y = row.isTop
            ? CameraEdges.Top + overlap - height / 2f
            : floorY - overlap + height / 2f;
        rock.transform.position = new Vector3(x, y, 0f);

        row.last = new Rock { rockTransform = rock.transform, halfWidth = width / 2f, depth = depth };
        row.rocks.Add(row.last);
        row.lastIndex = index;
    }

    // The deepest the other row reaches anywhere along this stretch
    // - How far the other row reaches in, anywhere along the stretch a new rock would cover.
    private float DeepestBetween(Row otherRow, float left, float right)
    {
        float deepest = 0f;

        foreach (Rock rock in otherRow.rocks)
        {
            if (rock.rockTransform == null) continue;
            if (rock.Right > left && rock.Left < right)
            {
                deepest = Mathf.Max(deepest, rock.depth);
            }
        }

        return deepest;
    }

    // The clear corridor at a given X, so debris lands in the cave and not inside a rock.
    // False until both rows have reached that far.
    public bool TryGetGapAt(float x, out float low, out float high)
    {
        low = floorY;
        high = CameraEdges.Top;

        if (!RowReaches(topRow, x) || !RowReaches(bottomRow, x)) return false;

        low += DeepestBetween(bottomRow, x, x);
        high -= DeepestBetween(topRow, x, x);

        return high > low;
    }

    // - True once this row has a rock covering that X.
    private bool RowReaches(Row row, float x)
    {
        foreach (Rock rock in row.rocks)
        {
            if (rock.rockTransform == null) continue;
            if (rock.Right > x && rock.Left < x) return true;
        }

        return false;
    }

    // Any rock except the one this row used last time
    // - Picks a rock prefab that is not the one this row used last, so no two repeat in a row.
    private int PickRock(int lastIndex)
    {
        if (rockPrefabsArray.Length == 1 || lastIndex < 0)
        {
            return Random.Range(0, rockPrefabsArray.Length);
        }

        // Pick from one fewer, then skip over the last one
        int index = Random.Range(0, rockPrefabsArray.Length - 1);
        if (index >= lastIndex) index++;
        return index;
    }

    // - Checks every rock prefab can be spawned before the run starts.
    // - The sprite is checked too, since SpawnRock reads sprite.bounds after instantiating and
    //   an empty renderer would throw and leak a rock every frame.
    private bool HasValidRocks()
    {
        if (rockPrefabsArray == null || rockPrefabsArray.Length == 0)
        {
            Debug.LogError("ObstacleSpawner: Rock Prefabs is empty. Drag the rock prefabs into it on SpawnObstacles.", this);
            return false;
        }

        foreach (GameObject prefab in rockPrefabsArray)
        {
            // The sprite is checked too: SpawnRock() reads sprite.bounds after instantiating,
            // so an empty Sprite Renderer would throw and leak a rock every frame
            SpriteRenderer renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null)
            {
                Debug.LogError("ObstacleSpawner: a Rock Prefabs slot is empty, has no Sprite Renderer, or has no sprite in it.", this);
                return false;
            }
        }

        return true;
    }
}
