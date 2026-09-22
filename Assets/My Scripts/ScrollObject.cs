using System.Collections.Generic;
using UnityEngine;

public class ScrollObject : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private float width;
    private float startX;
    private int copyCount = 1;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Real on-screen width of the sprite, including the object's scale.
        width = spriteRenderer.bounds.size.x;
        startX = transform.position.x;
    }

    private void Start()
    {
        // Line up every copy of this background, starting from the first one in the Hierarchy.
        List<ScrollObject> copies = FindCopies();
        copyCount = copies.Count;

        int index = copies.IndexOf(this);
        float x = copies[0].startX + index * width;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    private void Update()
    {
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        // Once this copy is fully off the left edge, move it to the back of the line.
        // Adding the width (instead of snapping to a fixed X) keeps the copies touching.
        if (transform.position.x <= -width)
        {
            transform.position += Vector3.right * width * copyCount;
        }
    }

    // Copies are the active objects under the same parent that use ScrollObject and the same sprite.
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
