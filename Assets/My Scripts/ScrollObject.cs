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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // On-screen width, including scale
        width = spriteRenderer.bounds.size.x;
        startX = transform.position.x;
    }

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

        int index = copies.IndexOf(this);

        // Pull the row left to the screen edge, so a wider device shows no bare space
        float anchor = first.startX;
        Camera cam = Camera.main;
        if (cam != null)
        {
            float leftEdge = cam.transform.position.x - cam.orthographicSize * cam.aspect;
            anchor = Mathf.Min(anchor, leftEdge + width / 2f);
        }

        float x = anchor + index * width;
        transform.position = new Vector3(x, first.transform.position.y, transform.position.z);
        transform.localScale = first.transform.localScale;
    }

    private void Update()
    {
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        // Off the left edge: move to the back of the line. Adding width keeps copies touching.
        if (transform.position.x <= -width)
        {
            transform.position += Vector3.right * width * copyCount;
        }
    }

    // Enough copies to cover the screen, plus one waiting off the right edge
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
