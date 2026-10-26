using System.Collections.Generic;
using UnityEngine;

// Slides a background picture leftwards forever. There are several copies lined up in a row, and
// whenever one slides off the left side it gets moved round to the back of the queue. That makes
// the background look endless using only a few pictures.
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollObject : MonoBehaviour
{
    [Tooltip("How fast this background slides left. Every copy has to use the same number.")]
    [SerializeField] private float scrollSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private float width;
    private float startX;
    private int copyCount = 1;

    // Which position this copy has in the queue (0, 1, 2...) and where the queue starts.
    //
    // We keep these so Update() can work out this copy's position from scratch every frame,
    // instead of shoving it along a little each time. Shoving it along is what used to let the
    // copies slowly slide apart and open up a flickering line between them.
    private float anchorX;
    private int index;

    // When the whole row started moving. We copy this from the FIRST copy rather than each copy
    // noting its own time, because copies can start up a frame later than each other, and a
    // one-frame difference would get baked in as a permanent gap. That was the bug.
    private float startTime;

    // Where the left edge of the screen is. Read once, because the camera never moves in this game.
    private float leftEdge;

    // The copies are placed a tiny bit closer together than their actual width, so they overlap
    // slightly.
    //
    // Sharing one clock stopped the copies DRIFTING apart, but two pictures placed edge to edge
    // perfectly still leave a hairline crack wherever the join lands between two pixels. Neither
    // picture quite fills that column, so the camera's background colour — a light blue-grey,
    // much brighter than the cave — flashes through the crack. Overlapping them hides it.
    // ObstacleSpawner already does the same thing with rocks for the same reason.
    // ponytail: 0.02 units is about 2px at 1080p. Raise it if a line ever shows on a sharper screen.
    private const float SEAM_OVERLAP = 0.02f;

    // How far apart to place the copies. Everything that walks along the row uses this. Only the
    // recycling check below uses the real width, because that one is asking where the picture
    // actually ends.
    private float Step => width - SEAM_OVERLAP;

    // - Measures how wide this copy is on screen and notes when it started moving.
    // - Start() replaces both with the first copy's values. These are just a backup in case a
    //   copy somehow ends up running on its own.
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Width on screen, including any scaling
        width = spriteRenderer.bounds.size.x;
        startX = transform.position.x;
        startTime = Time.time;
    }

    // - Lines this copy up in the row: finds the other copies, makes any that are missing, and
    //   copies the width, speed and start time off the first one so they can't disagree.
    // - Also shifts the whole row left to the screen edge, so a wider phone shows no bare patch.
    private void Start()
    {
        List<ScrollObject> copies = FindCopies();

        // One picture can't scroll forever on its own, so we make the extra copies here in code
        // rather than hoping somebody remembered to duplicate it in the Editor
        copies = AddMissingCopies(copies);

        copyCount = copies.Count;

        // Everyone uses the first copy's width, so a copy that got scaled differently by accident
        // still lines up properly
        ScrollObject first = copies[0];
        width = first.width;

        // Speed and start time are taken from the first copy too, not trusted per copy. One copy
        // set to a different speed in the Inspector would rip the row apart, and a tooltip can
        // only ask people nicely.
        scrollSpeed = first.scrollSpeed;
        startTime = first.startTime;

        index = copies.IndexOf(this);

        // Pull the row left to the screen edge, so a wider phone doesn't show empty space. Saved
        // in a field rather than used and thrown away, because Update() needs it for recycling.
        Camera cam = Camera.main;
        leftEdge = cam != null
            ? cam.transform.position.x - cam.orthographicSize * cam.aspect
            : -width;

        anchorX = Mathf.Min(first.startX, leftEdge + width / 2f);
        float x = anchorX + index * Step;
        transform.position = new Vector3(x, first.transform.position.y, transform.position.z);
        transform.localScale = first.transform.localScale;
    }

    // - Works out where this copy should be from the clock, every frame, from scratch.
    // - Moves it round to the back of the queue once its right edge is off the screen.
    private void Update()
    {
        float span = Step * copyCount;

        // Worked out fresh each frame, never added up over time. Every copy reads the same clock,
        // so neighbours stay exactly the same distance apart forever.
        //
        // The old way shoved each copy along by its own small amount each frame. Computers store
        // decimals with tiny rounding errors, and two copies at different positions don't round
        // the same way — so the gap between them crept, and every trip round the queue added
        // another error on top. The result was that hairline crack flashing in and out as it
        // crossed the pixel grid.
        //
        // Time.time is game time, so this still stops dead when GameManager freezes the game.
        float travelled = Mathf.Repeat((Time.time - startTime) * scrollSpeed, span);
        float x = anchorX + index * Step - travelled;

        // We recycle once this copy's RIGHT edge is past the screen edge, not at a fixed distance.
        //
        // The old fixed distance assumed the screen was never wider than one picture. On a 16:9
        // screen it sent a copy to the back of the queue with 1.9 units of it still visible — 3.8
        // on a tall phone — which left a bare strip down the left edge for about a second every
        // time it wrapped around.
        if (x + width / 2f <= leftEdge) x += span;

        Vector3 p = transform.position;
        transform.position = new Vector3(x, p.y, p.z);
    }

    // - Makes copies until the row covers the screen, plus one spare waiting off the right side.
    // - Done in code rather than in the Editor, so a wider phone can't open a gap in the
    //   background.
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

        // The new copies each bring their own ScrollObject with them, so go and find them all again
        return FindCopies();
    }

    // - Collects the other copies sitting next to this one that use the same picture.
    // - Checking the picture matches is what keeps two different backgrounds in the same parent
    //   from getting muddled up with each other.
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
