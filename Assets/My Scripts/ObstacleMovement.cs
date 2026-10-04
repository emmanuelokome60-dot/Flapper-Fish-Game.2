using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    // Shared by every obstacle, so the whole cave speeds up together. TrashSpawner ramps it.
    public static float SpeedMultiplier = 1f;

    // Just the swell part: 1 in calm water, higher during a surge. The fish reads it too.
    public static float Surge = 1f;

    [Tooltip("How fast obstacles slide left. Every rock must use the same speed.")]
    [SerializeField] private float speed = 5f;

    private Renderer pieceRenderer;

    private void Awake()
    {
        pieceRenderer = GetComponentInChildren<Renderer>();
    }

    private void Update()
    {
        transform.position += Vector3.left * (speed * SpeedMultiplier) * Time.deltaTime;

        // Wait for the right edge to clear the screen, so wide rocks don't vanish early
        float rightEdge = pieceRenderer != null ? pieceRenderer.bounds.max.x : transform.position.x;
        if (rightEdge < CameraLeftEdge() - 1f)
        {
            Destroy(gameObject);
        }
    }

    private float CameraLeftEdge()
    {
        Camera cam = Camera.main;
        return cam.transform.position.x - cam.orthographicSize * cam.aspect;
    }
}
