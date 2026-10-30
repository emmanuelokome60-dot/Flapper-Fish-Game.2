using System.Collections.Generic;
using UnityEngine;

// Slides a background picture leftwards forever. Several copies sit in a
// row, and one that slides off the left goes to the back of the queue.
// A few pictures then look endless.
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollObject : MonoBehaviour
{
    [Tooltip("How fast this background slides left. Every copy has to use the same number.")]
    [SerializeField] private float scrollSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private float width;
    private float startX;
    private int copyCount = 1;

    // This copy's place in the queue, and where the queue starts.
    // Update() rebuilds the position from these every frame instead of
    // nudging it along, which is what let the copies drift apart.
    private float anchorX;
    private int index;

    // Copied from the FIRST copy, never noted per copy. Copies can start
    // a frame apart, and that frame would bake in a permanent gap.
    private float startTime;

    // Where the left edge of the screen is. The camera never moves.
    private float leftEdge;

    // The copies overlap slightly instead of sitting edge to edge.
    // One clock stops them drifting, but a perfect join still leaves a
    // hairline crack on the pixel grid, and the pale camera background
    // flashes through it. CaveSpawner overlaps rocks for the same reason.
    // ponytail: 0.02 units is about 2px at 1080p. Raise it if a line ever shows on a sharper screen.
    private const float SEAM_OVERLAP = 0.02f;

    // How far apart the copies sit. Only the recycling check uses the
    // real width, because that one asks where the picture really ends.
    private float Step => width - SEAM_OVERLAP;

    // Measures how wide this copy is and notes when it started moving.
    // Start() replaces both with the first copy's values.
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        width = spriteRenderer.bounds.size.x;
        startX = transform.position.x;
        startTime = Time.time;
    }

    // Lines this copy up in the row and copies the width, speed and
    // start time off the first one, so they cannot disagree.
    private void Start()
    {
        List<ScrollObject> copies = FindCopies();

        // Made here, not in the Editor, so nobody has to remember.
        copies = AddMissingCopies(copies);

        copyCount = copies.Count;

        // The first copy's width, so one scaled by accident still fits.
        ScrollObject first = copies[0];
        width = first.width;

        // Speed and start time come from the first copy too. One copy
        // set to a different speed would rip the row apart.
        scrollSpeed = first.scrollSpeed;
        startTime = first.startTime;

        index = copies.IndexOf(this);

        // Pull the row left to the screen edge, so a wider phone shows
        // no bare patch. Kept, because Update() recycles against it.
        Camera cam = Camera.main;
        leftEdge = cam != null
            ? cam.transform.position.x - cam.orthographicSize * cam.aspect
            : -width;

        anchorX = Mathf.Min(first.startX, leftEdge + width / 2f);
        float x = anchorX + index * Step;
        transform.position = new Vector3(x, first.transform.position.y, transform.position.z);
        transform.localScale = first.transform.localScale;
    }

    // Works out where this copy belongs from the clock, every frame.
    // Sends it to the back of the queue once it is off screen.
    private void Update()
    {
        float span = Step * copyCount;

        // Worked out fresh, never added up. Every copy reads the same
        // clock, so neighbours stay exactly as far apart forever.
        // Nudging each copy along let rounding errors creep in, and the
        // gap grew a little more on every trip round the queue.
        // Time.time is game time, so a freeze stops this too.
        float travelled = Mathf.Repeat((Time.time - startTime) * scrollSpeed, span);
        float x = anchorX + index * Step - travelled;

        // Recycled once the RIGHT edge passes the screen, not at a fixed
        // distance. A fixed one wrapped a copy with 1.9 units still
        // showing, leaving a bare strip down the left edge.
        if (x + width / 2f <= leftEdge) x += span;

        Vector3 p = transform.position;
        transform.position = new Vector3(x, p.y, p.z);
    }

    // Makes copies until the row covers the screen, plus one spare.
    // Done in code, so a wider phone cannot open a gap.
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

        // The new copies bring their own ScrollObject, so look again.
        return FindCopies();
    }

    // Collects the copies next to this one that use the same picture.
    // Matching the picture keeps two different backgrounds apart.
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
