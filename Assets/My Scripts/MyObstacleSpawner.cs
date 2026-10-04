using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Builds two endless rows of rocks, one hanging from the ceiling and one standing on the
// ground, always leaving an opening of at least minGap for the fish to swim through.
public class MyObstacleSpawner : MonoBehaviour
{
    [Header("Rocks")]
    [Tooltip("Rock prefabs to pick from. Each is drawn hanging down; bottom rocks are flipped.")]
    [FormerlySerializedAs("rockPrefabs")]
    [SerializeField] private GameObject[] rockPrefabsArray;

    [Header("Space for the fish")]
    [Tooltip("Smallest opening between the rows. The fish is 1.33 tall, its collider 1.11.")]
    [SerializeField] private float minGap = 3f;

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

    [Header("Start off run")]
    // Capped at 2.5s: CoinSpawner's first spawn attempt lands there and would be burnt
    [Tooltip("A beat of open water after PLAY, before the cave mouth slides in.")]
    [Range(0f, 2.5f)]
    [SerializeField] private float leadInSeconds = 1f;

    // One placed rock, remembered so the other row knows how much space is left
    private class Rock
    {
        public Transform transform;
        public float halfWidth;
        public float depth;

        public float Left => transform.position.x - halfWidth;
        public float Right => transform.position.x + halfWidth;
    }

    // One row. It only needs its last rock to know where the next one goes.
    private class Row
    {
        public readonly bool isTop;
        public readonly List<Rock> rocksList = new List<Rock>();
        public Rock last;
        public int lastIndex = -1;
        public float startX;

        public Row(bool isTop)
        {
            this.isTop = isTop;
        }

        public float RightEdge => last != null && last.transform != null ? last.Right : startX;
    }

    private readonly Row topRow = new Row(true);
    private readonly Row bottomRow = new Row(false);
    private bool spawning;
    private float elapsed;

    // The GameManager decides when spawning starts, so nothing spawns on scene load
    public void StartSpawning()
    {
        // Rows start past the right edge, so the first rocks slide in. That swim is the
        // player's breathing room: about 3.5s at 16:9, 4.3s on a 19.5:9 phone, plus leadInSeconds.
        elapsed = 0f;

        if (!HasValidRocks()) return;

        foreach (Row row in new[] { topRow, bottomRow })
        {
            row.rocksList.Clear();
            row.last = null;
            row.lastIndex = -1;
            row.startX = CameraRightEdge() + 0.5f;
        }

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
        if (elapsed < leadInSeconds) return;

        Forget(topRow);
        Forget(bottomRow);

        float fillTo = CameraRightEdge() + maxWidth;
        FillRow(topRow, bottomRow, fillTo);
        FillRow(bottomRow, topRow, fillTo);
    }

    // Drop rocks that have been deleted or scrolled well past the fish
    private void Forget(Row row)
    {
        float behind = CameraLeftEdge() - 5f;
        row.rocksList.RemoveAll(rock => rock.transform == null || rock.Right < behind);
    }

    private void FillRow(Row row, Row otherRow, float fillTo)
    {
        // The limit is a safety net; one rock is normally enough
        for (int i = 0; i < 10 && row.RightEdge < fillTo; i++)
        {
            SpawnRock(row, otherRow);
        }
    }

    private void SpawnRock(Row row, Row otherRow)
    {
        int index = PickRock(row.lastIndex);
        GameObject prefab = rockPrefabsArray[index];

        // Parented to the spawner, so ClearObstacles() can find it
        GameObject rock = Instantiate(prefab, transform);

        Vector2 spriteSize = rock.GetComponent<SpriteRenderer>().sprite.bounds.size;
        float startX = row.RightEdge - overlap;

        // Total depth the two rows may share, so the opening never drops below minGap
        float budget = CameraTopEdge() - floorY - minGap;

        // What the other row already took here, measured at this rock's widest possible reach
        float taken = DeepestBetween(otherRow, startX, startX + spriteSize.x * maxScale);
        float allowed = Mathf.Min(budget - taken, budget - minDepth);

        // Aim near the last depth, so the wall flows instead of jumping
        float previous = row.last != null ? row.last.depth : minDepth;
        float low = Mathf.Max(minDepth, previous - maxDepthChange);
        float high = Mathf.Min(allowed, previous + maxDepthChange);
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
            ? CameraTopEdge() + overlap - height / 2f
            : floorY - overlap + height / 2f;
        rock.transform.position = new Vector3(x, y, 0f);

        row.last = new Rock { transform = rock.transform, halfWidth = width / 2f, depth = depth };
        row.rocksList.Add(row.last);
        row.lastIndex = index;
    }

    // The deepest the other row reaches anywhere along this stretch
    private float DeepestBetween(Row otherRow, float left, float right)
    {
        float deepest = 0f;

        foreach (Rock rock in otherRow.rocksList)
        {
            if (rock.transform == null) continue;
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
        high = CameraTopEdge();

        if (!RowReaches(topRow, x) || !RowReaches(bottomRow, x)) return false;

        low += DeepestBetween(bottomRow, x, x);
        high -= DeepestBetween(topRow, x, x);

        return high > low;
    }

    private bool RowReaches(Row row, float x)
    {
        foreach (Rock rock in row.rocksList)
        {
            if (rock.transform == null) continue;
            if (rock.Right > x && rock.Left < x) return true;
        }

        return false;
    }

    // Any rock except the one this row used last time
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

    private bool HasValidRocks()
    {
        if (rockPrefabsArray == null || rockPrefabsArray.Length == 0)
        {
            Debug.LogError("MyObstacleSpawner: Rock Prefabs is empty. Drag the rock prefabs into it on SpawnObstacles.", this);
            return false;
        }

        foreach (GameObject prefab in rockPrefabsArray)
        {
            // The sprite is checked too: SpawnRock() reads sprite.bounds after instantiating,
            // so an empty Sprite Renderer would throw and leak a rock every frame
            SpriteRenderer renderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null)
            {
                Debug.LogError("MyObstacleSpawner: a Rock Prefabs slot is empty, has no Sprite Renderer, or has no sprite in it.", this);
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

    private float CameraLeftEdge()
    {
        Camera cam = Camera.main;
        return cam.transform.position.x - cam.orthographicSize * cam.aspect;
    }

    private float CameraTopEdge()
    {
        Camera cam = Camera.main;
        return cam.transform.position.y + cam.orthographicSize;
    }
}
