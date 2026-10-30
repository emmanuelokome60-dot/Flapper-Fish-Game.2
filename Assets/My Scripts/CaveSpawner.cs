using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Builds the cave: two endless rows of rocks, one hanging from the
// ceiling and one standing on the floor, always leaving an opening the
// fish can swim through.
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

    // One rock we placed. Remembered so the opposite row knows the space left.
    private class Rock
    {
        public Transform rockTransform;
        public float halfWidth;
        public float depth;

        public float Left => rockTransform.position.x - halfWidth;
        public float Right => rockTransform.position.x + halfWidth;
    }

    // One row of rocks, ceiling or floor. It only needs its last rock to
    // know where the next one goes.
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

    // The smallest opening allowed, whatever the settings say. Below
    // twice the fish's 1.11, getting through is luck, not skill -- and
    // on a phone your own thumb is covering the fish.
    private const float ABSOLUTE_MIN_GAP = 2.2f;

    // The rows start just past the right edge, so the first rocks slide
    // into view instead of appearing from nowhere.
    private const float ROW_START_MARGIN = 0.5f;

    // How far back a rock goes before we stop tracking it. Deleting the
    // rock itself is ObstacleMovement's job, not ours.
    private const float FORGET_MARGIN = 5f;

    private readonly Row topRow = new Row(true);
    private readonly Row bottomRow = new Row(false);
    private bool spawning;

    // Starts building rocks for a new run, both rows parked off screen.
    // GameManager decides when, which is why nothing appears at load.
    public void StartSpawning()
    {
        if (!HasValidRocks()) return;

        // Warned, not silently fixed. A quiet rescue is how you ship a
        // cave nobody fits through that looks deliberate.
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

    // Stops building new rocks. GameManager clears the ones out there.
    public void StopSpawning()
    {
        spawning = false;
    }

    // Keeps both rows built past the right edge, and forgets what is gone.
    // Each rock is measured against the opposite row, so the two walls
    // can never close up on each other.
    private void Update()
    {
        if (!spawning) return;

        Forget(topRow);
        Forget(bottomRow);

        float fillTo = CameraEdges.Right + maxWidth;
        FillRow(topRow, bottomRow, fillTo);
        FillRow(bottomRow, topRow, fillTo);
    }

    // Throws away notes on rocks that are long gone.
    // Looped by hand, not RemoveAll: that needs a throwaway object every
    // call, and this runs twice a frame -- enough rubbish to stutter.
    private void Forget(Row row)
    {
        float behind = CameraEdges.Left - FORGET_MARGIN;

        // Backwards, so removing one does not shuffle the rest.
        for (int i = row.rocks.Count - 1; i >= 0; i--)
        {
            Rock rock = row.rocks[i];
            if (rock.rockTransform == null || rock.Right < behind) row.rocks.RemoveAt(i);
        }
    }

    // Adds rocks to one row until it reaches far enough right.
    // The 10 is a safety net: a rock with no width would loop forever.
    private void FillRow(Row row, Row otherRow, float fillTo)
    {
        for (int i = 0; i < 10 && row.RightEdge < fillTo; i++)
        {
            SpawnRock(row, otherRow);
        }
    }

    // How big the opening should be right now.
    // Shrinks on the SAME 0-to-1 depth number that dims the light, so
    // the walls close in as the cave darkens: one thing to notice,
    // not two systems to balance.
    private float CurrentGap()
    {
        float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
        return Mathf.Max(Mathf.Lerp(minGap, deepGap, depth), ABSOLUTE_MIN_GAP);
    }

    // Places one rock against the last one in its row, stretched to the
    // depth it was given.
    // Checks the opposite row FIRST, so the opening can never shrink
    // below the gap allowed at this depth.
    private void SpawnRock(Row row, Row otherRow)
    {
        int index = PickRock(row.lastIndex);
        GameObject prefab = rockPrefabsArray[index];

        // A child of the spawner, so restart clean-up can find it.
        GameObject rock = Instantiate(prefab, transform);

        Vector2 spriteSize = rock.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float startX = row.RightEdge - overlap;

        // What the two rows may share. Whatever is left after the
        // opening is the budget.
        float gap = CurrentGap();
        float budget = CameraEdges.Top - floorY - gap;

        // How much of it the other row already used here, measured
        // across the widest this rock could be.
        float taken = DeepestBetween(otherRow, startX, startX + spriteSize.x * maxScale);
        float allowed = Mathf.Min(budget - taken, budget - minDepth);

        // Aim near the last rock, so the wall flows instead of jumping.
        float previous = row.last != null ? row.last.depth : minDepth;

        // The allowed jump shrinks with the opening. A 1-unit step is a
        // third of a 3.0 gap but half of a 2.6 one, and the fish climbs
        // no faster, so without this the deep cave zig-zags too fast.
        float depthChange = maxDepthChange * (gap / minGap);
        float low = Mathf.Max(minDepth, previous - depthChange);
        float high = Mathf.Min(allowed, previous + depthChange);
        float wanted = Random.Range(Mathf.Min(low, high), Mathf.Max(low, high));

        // Stretched to reach that depth, so it always touches its edge.
        float scale = (wanted + overlap) / spriteSize.y;
        scale = Mathf.Clamp(scale, minScale, maxScale);
        scale = Mathf.Min(scale, (allowed + overlap) / spriteSize.y);  // never eat into the opening
        scale = Mathf.Min(scale, maxWidth / spriteSize.x);             // never ridiculously wide

        float width = spriteSize.x * scale;
        float height = spriteSize.y * scale;
        float depth = height - overlap;

        // Flipped with a negative size, which flips the hit-box too.
        // The Flip Y tickbox only flips the picture, which would leave
        // invisible walls in the wrong places.
        float mirror = Random.value < 0.5f ? -1f : 1f;
        float upsideDown = row.isTop ? 1f : -1f;
        rock.transform.localScale = new Vector3(scale * mirror, scale * upsideDown, 1f);

        // Right after the last rock, flat edge against the screen edge.
        float x = startX + width / 2f;
        float y = row.isTop
            ? CameraEdges.Top + overlap - height / 2f
            : floorY - overlap + height / 2f;
        rock.transform.position = new Vector3(x, y, 0f);

        row.last = new Rock { rockTransform = rock.transform, halfWidth = width / 2f, depth = depth };
        row.rocks.Add(row.last);
        row.lastIndex = index;
    }

    // How far the other row sticks out at its deepest, anywhere along
    // the stretch a new rock would cover.
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

    // Says where the clear opening is at a spot, so rubbish and coins
    // land in the cave and not inside a rock.
    // False until both rows are built that far ahead.
    public bool TryGetGapAt(float x, out float low, out float high)
    {
        low = floorY;
        high = CameraEdges.Top;

        if (!RowReaches(topRow, x) || !RowReaches(bottomRow, x)) return false;

        low += DeepestBetween(bottomRow, x, x);
        high -= DeepestBetween(topRow, x, x);

        return high > low;
    }

    // True once this row has a rock covering that spot.
    private bool RowReaches(Row row, float x)
    {
        foreach (Rock rock in row.rocks)
        {
            if (rock.rockTransform == null) continue;
            if (rock.Right > x && rock.Left < x) return true;
        }

        return false;
    }

    // Picks a random rock, never the one this row used last.
    // Picks from a list one shorter and skips past the last one, so
    // every other rock is equally likely with no re-rolling.
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

    // Checks every rock is usable before the run starts.
    // The pictures too, because SpawnRock measures the picture after
    // making the rock: an empty one would crash every single frame.
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
