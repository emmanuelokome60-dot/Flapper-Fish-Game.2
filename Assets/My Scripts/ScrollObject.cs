using System.Collections.Generic;
using UnityEngine;

// Scrolls a background left and recycles each copy as it leaves the screen.
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollObject : MonoBehaviour
{
    [Tooltip("How fast this background slides left. Every copy must use the same speed.")]
    [SerializeField] private float scrollSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private float width;
    private float startX;
    private int copyCount = 1;

    // Where this copy sits in the row. Kept so Update() can rebuild its X from scratch every
    // frame rather than nudging the previous one along, which is what used to let the copies
    // drift a fraction of a unit apart and open a flickering seam.
    private float anchorX;
    private int index;

    // When the row started scrolling. Shared from the first copy rather than read per object:
    // clones can get their Start a frame later, and a per-copy start time would bake that frame
    // in as a permanent gap -- the exact bug this rewrite removes.
    private float startTime;

    // World X of the camera's left edge, read once in Start(). The camera never moves here.
    private float leftEdge;

    // Copies are laid a hair closer together than their own width. Deriving X from a shared clock
    // stopped the copies DRIFTING apart, but two quads that abut exactly still leave a hairline
    // wherever the join falls between pixels: neither covers that column fully, and the camera's
    // clear colour -- a blue-grey, much lighter than the cave -- flashes through it. Overlapping
    // is what ObstacleSpawner already does with rocks, for the same reason.
    // ponytail: 0.02 units is about 2px at 1080p. Raise it if a seam ever shows on a denser screen.
    private const float SEAM_OVERLAP = 0.02f;

    // How far apart the copies are placed. Everything that steps along the row uses this; only
    // the recycle test below uses the true width, because that asks where the sprite really ends.
    private float Step => width - SEAM_OVERLAP;

    // - Measures this copy's on-screen width and notes when it started scrolling.
    // - Both are replaced in Start() by the first copy's values; these are only a fallback for
    //   a copy that somehow runs alone.
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // On-screen width, including scale
        width = spriteRenderer.bounds.size.x;
        startX = transform.position.x;
        startTime = Time.time;
    }

    // - Lines this copy up in the row: finds its siblings, makes any that are missing, and
    //   takes width, speed and start time from the first copy so they cannot disagree.
    // - Pulls the row left to the screen edge, so a wider device shows no bare space.
    private void Start()
    {
        List<ScrollObject> copies = FindCopies();

        // One copy cannot scroll endlessly, so make the missing ones here rather than
        // rely on someone duplicating it in the Editor
        copies = AddMissingCopies(copies);

        copyCount = copies.Count;

        // Every copy uses the first one's width, so a differently scaled copy still joins cleanly
        ScrollObject first = copies[0];
        width = first.width;

        // Taken from the first copy rather than trusted per object: one copy set to a different
        // speed in the Inspector would tear the row apart, and the tooltip can only ask nicely.
        scrollSpeed = first.scrollSpeed;
        startTime = first.startTime;

        index = copies.IndexOf(this);

        // Pull the row left to the screen edge, so a wider device shows no bare space. Kept in
        // a field rather than dropped at the end of this method: Update() recycles against it.
        Camera cam = Camera.main;
        leftEdge = cam != null
            ? cam.transform.position.x - cam.orthographicSize * cam.aspect
            : -width;

        anchorX = Mathf.Min(first.startX, leftEdge + width / 2f);
        float x = anchorX + index * Step;
        transform.position = new Vector3(x, first.transform.position.y, transform.position.z);
        transform.localScale = first.transform.localScale;
    }

    // - Rebuilds this copy's X from a clock every frame rather than nudging it along, which is
    //   what stopped neighbours drifting apart and flashing a seam.
    // - Recycles the copy to the back of the row once its right edge has cleared the camera.
    private void Update()
    {
        float span = Step * copyCount;

        // Derived, never accumulated. Every copy reads the same clock, so neighbours stay exactly
        // `width` apart on every frame. Nudging each copy along by its own delta instead meant two
        // copies at different magnitudes did not preserve their difference in float, and each
        // recycle added another rounding error on top -- the gap drifted, and the sub-pixel seam
        // flashed in and out as it crossed the pixel grid.
        //
        // Time.time is scaled time, so this still stops dead when GameManager zeroes timeScale.
        float travelled = Mathf.Repeat((Time.time - startTime) * scrollSpeed, span);
        float x = anchorX + index * Step - travelled;

        // Recycled once this copy's RIGHT edge has cleared the camera, not at a fixed -width.
        // That threshold assumed the screen was no wider than one copy, so it sent a copy to
        // the back of the line with 1.9 units of it still on screen at 16:9 -- 3.8 on a 19.5:9
        // phone -- and left a bare strip at the left edge for about a second on every wrap.
        if (x + width / 2f <= leftEdge) x += span;

        Vector3 p = transform.position;
        transform.position = new Vector3(x, p.y, p.z);
    }

    // Enough copies to cover the screen, plus one waiting off the right edge
    // - Clones the first copy until the row covers the screen with one spare off the right.
    // - Here rather than in the Editor, so a wider device cannot open a gap in the background.
    private List<ScrollObject> AddMissingCopies(List<ScrollObject> copies)
    {
        ScrollObject first = copies[0];
        if (first.width <= 0f) return copies;   // nothing to measure

        Camera cam = Camera.main;
        float screenWidth = cam != null ? cam.orthographicSize * cam.aspect * 2f : 20f;
        int needed = Mathf.CeilToInt(screenWidth / first.width) + 1;

        if (copies.Count >= needed) return copies;

        for (int i = copies.Count; i < needed; i++)
        {
            Instantiate(first.gameObject, first.transform.parent);
        }

        // The new copies bring their own ScrollObject
        return FindCopies();
    }

    // Active siblings using ScrollObject and the same sprite
    // - Collects the active siblings that scroll with the same sprite as this one.
    // - Matching on the sprite is what keeps two different backgrounds in one parent apart.
    private List<ScrollObject> FindCopies()
    {
        var copies = new List<ScrollObject>();

        if (transform.parent == null)
        {
            copies.Add(this);
            return copies;
        }

        foreach (Transform child in transform.parent)
        {
            ScrollObject copy = child.GetComponent<ScrollObject>();
            if (copy != null && child.gameObject.activeInHierarchy && copy.spriteRenderer.sprite == spriteRenderer.sprite)
            {
                copies.Add(copy);
            }
        }

        return copies;
    }
}
