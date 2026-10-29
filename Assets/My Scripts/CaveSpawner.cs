using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Builds the cave: two never-ending rows of rocks, one hanging down from the ceiling and one
// standing up from the floor. It always leaves an opening big enough for the fish to swim through.
public class CaveSpawner : MonoBehaviour
{
    [Header("Rocks")]
    [Tooltip("The rocks to pick from. Each one is drawn hanging downwards; the floor rocks are " +
             "just the same ones turned upside down.")]
    [FormerlySerializedAs("rockPrefabs")]
    [SerializeField] private GameObject[] rockPrefabsArray;

    [Header("Space for the fish")]
    [Tooltip("How big the opening is at the mouth of the cave, where every run begins. The fish " +
             "is 1.33 tall, and the part that can actually hit things is 1.11.")]
    [SerializeField] private float minGap = 3f;

    [Tooltip("How big the opening is once the cave is at its darkest. Must be SMALLER than Min " +
             "Gap or the cave never closes in. GameManager's Darkest At Distance sets when.")]
    [SerializeField] private float deepGap = 2.6f;

    [Tooltip("Every rock sticks out at least this far, so none of them is a useless little sliver.")]
    [SerializeField] private float minDepth = 2f;

    [Tooltip("How much bigger or smaller a rock can be than the one before it. Lower = smoother, " +
             "flowing walls. Higher = jagged, jumpy walls.")]
    [SerializeField] private float maxDepthChange = 1.2f;

    [Tooltip("The height of the top of the Ground. Floor rocks stand on this line.")]
    [SerializeField] private float floorY = -4f;

    [Header("Size limits")]
    [Tooltip("Rocks are stretched to reach the depth they were given. These stop that going silly.")]
    [SerializeField] private float minScale = 0.35f;
    [SerializeField] private float maxScale = 1.2f;

    [Tooltip("The widest a single rock is allowed to be. The screen is about 17.8 wide.")]
    [SerializeField] private float maxWidth = 8f;

    [Tooltip("How far each rock tucks underneath its neighbour, so you never see a join.")]
    [SerializeField] private float overlap = 0.2f;

    // One rock we've placed. We remember it so the opposite row knows how much space is left.
    private class Rock
    {
        public Transform rockTransform;
        public float halfWidth;
        public float depth;

        public float Left => rockTransform.position.x - halfWidth;
        public float Right => rockTransform.position.x + halfWidth;
    }

    // One row of rocks, ceiling or floor. It only needs to remember its last rock to know where
    // the next one goes.
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

    // The smallest opening we will ever allow, no matter what the settings say.
    //
    // Below roughly twice the fish's size of 1.11, getting through stops being about skill and
    // becomes pure luck — especially on a phone, where your own thumb is covering the fish.
    private const float ABSOLUTE_MIN_GAP = 2.2f;

    // The rows begin just past the right edge, so the first rocks slide into view rather than
    // appearing out of nowhere
    private const float ROW_START_MARGIN = 0.5f;

    // How far behind the camera a rock gets before we stop keeping track of it. It only needs to
    // be further back than anything we still measure against. Deleting the actual rock object is
    // ObstacleMovement's job, not ours.
    private const float FORGET_MARGIN = 5f;

    private readonly Row topRow = new Row(true);
    private readonly Row bottomRow = new Row(false);
    private bool spawning;

    // - Starts building rocks for a new run, parking both rows just past the right edge.
    // - GameManager decides when this happens, which is why nothing appears when the scene loads.
    public void StartSpawning()
    {
        if (!HasValidRocks()) return;

        // We warn rather than silently fixing it. CurrentGap() would quietly rescue a bad number,
        // and that's how you end up shipping a cave nobody can fit through that looks deliberate.
        if (deepGap < ABSOLUTE_MIN_GAP)
        {
            Debug.LogWarning("CaveSpawner: Deep Gap " + deepGap + " is below the "
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

    // - Stops building new rocks. The ones already out there get cleared by GameManager.
    public void StopSpawning()
    {
        spawning = false;
    }

    // - Keeps both rows built out to just past the right edge, and forgets what's gone past.
    // - Rows are built one rock at a time, each one measured against the opposite row so the two
    //   never close up on each other.
    private void Update()
    {
        if (!spawning) return;

        Forget(topRow);
        Forget(bottomRow);

        float fillTo = CameraEdges.Right + maxWidth;
        FillRow(topRow, bottomRow, fillTo);
        FillRow(bottomRow, topRow, fillTo);
    }

    // - Throws away our notes about rocks that are long gone, so the list doesn't grow forever.
    // - This is only our notes. The rock object itself gets deleted by ObstacleMovement.
    // - Looped backwards by hand rather than RemoveAll. RemoveAll needs a throwaway object every
    //   call to carry `behind` in, and this runs twice a frame -- about 120 bits of rubbish a
    //   second for the garbage collector to clear up, which is what causes a sudden stutter.
    private void Forget(Row row)
    {
        float behind = CameraEdges.Left - FORGET_MARGIN;

        // Backwards, so removing one doesn't shuffle the ones we haven't checked yet
        for (int i = row.rocks.Count - 1; i >= 0; i--)
        {
            Rock rock = row.rocks[i];
            if (rock.rockTransform == null || rock.Right < behind) row.rocks.RemoveAt(i);
        }
    }

    // - Adds rocks to one row until it reaches far enough to the right.
    // - The limit of 10 is a safety net in case a rock somehow has no width, which would make
    //   this loop run forever. Normally one rock is enough.
    private void FillRow(Row row, Row otherRow, float fillTo)
    {
        for (int i = 0; i < 10 && row.RightEdge < fillTo; i++)
        {
            SpawnRock(row, otherRow);
        }
    }

    // - Works out how big the opening should be right now.
    // - It shrinks using the SAME 0-to-1 "how deep are we" number that dims the light, so the
    //   walls close in as the cave gets darker. One thing for the player to notice, not two
    //   separate systems to balance against each other.
    // - It also spaces out the difficulty for free: the depth number maxes out at 400m, roughly
    //   halfway through the speed build-up, so the gap finishes shrinking while the speed is
    //   still climbing.
    private float CurrentGap()
    {
        float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
        return Mathf.Max(Mathf.Lerp(minGap, deepGap, depth), ABSOLUTE_MIN_GAP);
    }

    // - Places one rock right up against the last one in its row, stretched to stick out as far
    //   as we decided it should.
    // - It checks the opposite row FIRST, so the opening between the two can never get smaller
    //   than the gap we're allowing at this depth.
    private void SpawnRock(Row row, Row otherRow)
    {
        int index = PickRock(row.lastIndex);
        GameObject prefab = rockPrefabsArray[index];

        // Made a child of the spawner, so the restart clean-up can find it
        GameObject rock = Instantiate(prefab, transform);

        Vector2 spriteSize = rock.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float startX = row.RightEdge - overlap;

        // The total depth the two rows are allowed to share between them. Whatever is left over
        // after the opening is the "budget".
        float gap = CurrentGap();
        float budget = CameraEdges.Top - floorY - gap;

        // How much of that budget the other row has already used up here. Measured across the
        // widest this new rock could possibly end up being.
        float taken = DeepestBetween(otherRow, startX, startX + spriteSize.x * maxScale);
        float allowed = Mathf.Min(budget - taken, budget - minDepth);

        // Aim for roughly the same depth as the last rock, so the wall flows along instead of
        // jumping in and out
        float previous = row.last != null ? row.last.depth : minDepth;

        // How much it's allowed to jump shrinks along with the opening. A 1-unit step is only a
        // third of a 3.0 gap, but it's nearly half of a 2.6 one — and the fish has the same
        // climbing power either way. Without this the deep cave zig-zags faster than you can
        // physically swim it.
        float depthChange = maxDepthChange * (gap / minGap);
        float low = Mathf.Max(minDepth, previous - depthChange);
        float high = Mathf.Min(allowed, previous + depthChange);
        float wanted = Random.Range(Mathf.Min(low, high), Mathf.Max(low, high));

        // Stretch it to reach that depth, so it always touches its own edge of the screen
        float scale = (wanted + overlap) / spriteSize.y;
        scale = Mathf.Clamp(scale, minScale, maxScale);
        scale = Mathf.Min(scale, (allowed + overlap) / spriteSize.y);  // never eat into the opening
        scale = Mathf.Min(scale, maxWidth / spriteSize.x);             // never ridiculously wide

        float width = spriteSize.x * scale;
        float height = spriteSize.y * scale;
        float depth = height - overlap;

        // We flip the rock by using a negative size, which flips the hit-box too. The Sprite
        // Renderer's own "Flip Y" tickbox only flips the picture and leaves the hit-box the
        // right way up, which would mean invisible walls in the wrong places.
        float mirror = Random.value < 0.5f ? -1f : 1f;
        float upsideDown = row.isTop ? 1f : -1f;
        rock.transform.localScale = new Vector3(scale * mirror, scale * upsideDown, 1f);

        // Sits right after the previous rock, with its flat edge pressed against the screen edge
        float x = startX + width / 2f;
        float y = row.isTop
            ? CameraEdges.Top + overlap - height / 2f
            : floorY - overlap + height / 2f;
        rock.transform.position = new Vector3(x, y, 0f);

        row.last = new Rock { rockTransform = rock.transform, halfWidth = width / 2f, depth = depth };
        row.rocks.Add(row.last);
        row.lastIndex = index;
    }

    // - How far the other row sticks out, at its very deepest, anywhere along the stretch a new
    //   rock would cover.
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

    // - Tells you where the clear opening is at a given spot, so rubbish and coins land in the
    //   cave and not buried inside a rock.
    // - Answers false until both rows have actually been built that far ahead.
    public bool TryGetGapAt(float x, out float low, out float high)
    {
        low = floorY;
        high = CameraEdges.Top;

        if (!RowReaches(topRow, x) || !RowReaches(bottomRow, x)) return false;

        low += DeepestBetween(bottomRow, x, x);
        high -= DeepestBetween(topRow, x, x);

        return high > low;
    }

    // - True once this row has a rock covering that spot.
    private bool RowReaches(Row row, float x)
    {
        foreach (Rock rock in row.rocks)
        {
            if (rock.rockTransform == null) continue;
            if (rock.Right > x && rock.Left < x) return true;
        }

        return false;
    }

    // - Picks a random rock, but never the same one this row used last time.
    // - It picks from a list one shorter, then skips past the one used last time. That gives every
    //   other rock an equal chance without having to keep re-rolling until it gets a different one.
    private int PickRock(int lastIndex)
    {
        if (rockPrefabsArray.Length == 1 || lastIndex < 0)
        {
            return Random.Range(0, rockPrefabsArray.Length);
        }

        int index = Random.Range(0, rockPrefabsArray.Length - 1);
        if (index >= lastIndex) index++;
        return index;
    }

    // - Checks every rock is actually usable before the run starts.
    // - We check the pictures too, not just the objects, because SpawnRock measures the picture
    //   AFTER making the rock. An empty one would crash and leave a brand new broken rock behind
    //   every single frame.
    private bool HasValidRocks()
    {
        if (rockPrefabsArray == null || rockPrefabsArray.Length == 0)
        {
            Debug.LogError("CaveSpawner: Rock Prefabs is empty. Drag the rock prefabs into it on SpawnObstacles.", this);
            return false;
        }

        foreach (GameObject prefab in rockPrefabsArray)
        {
            SpriteRenderer renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null)
            {
                Debug.LogError("CaveSpawner: a Rock Prefabs slot is empty, has no Sprite Renderer, or has no sprite in it.", this);
                return false;
            }
        }

        return true;
    }
}
