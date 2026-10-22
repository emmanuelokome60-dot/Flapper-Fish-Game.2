using UnityEngine;
using UnityEngine.Serialization;

public class PlayerHit : MonoBehaviour
{
    [Tooltip("How far below the screen the fish sinks before the run ends.")]
    [FormerlySerializedAs("_belowScreenMargin")]
    [SerializeField] private float belowScreenMargin = 1f;

    private Camera mainCamera;

    // - Caches the camera once, since the fall check below needs its position every frame.
    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // - Ends the run once the fish has sunk far enough below the camera to be gone for good.
    // - There is no floor collider, so this is the only thing that catches a fish that fell out.
    private void Update()
    {
        // Frozen on the menu, on pause and after a game over
        if (Time.timeScale == 0f) return;

        if (mainCamera == null) return;

        // No Ground object, so nothing to hit until the bottom row arrives
        float screenBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;

        if (transform.position.y < screenBottom - belowScreenMargin)
        {
            EndRun();
        }
    }

    // - Ends the run on contact with anything solid: rocks and debris both arrive here.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        EndRun();
    }

    // - The single exit both the fall and the collision route through, so a run can only end
    //   one way. GameManager.GameOver guards against being called twice in a frame.
    private void EndRun()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.GameOver();
    }
}
