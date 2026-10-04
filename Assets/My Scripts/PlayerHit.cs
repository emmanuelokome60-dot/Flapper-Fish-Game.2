using UnityEngine;
using UnityEngine.Serialization;

public class PlayerHit : MonoBehaviour
{
    [Tooltip("How far below the screen the fish sinks before the run ends.")]
    [FormerlySerializedAs("_belowScreenMargin")]
    [SerializeField] private float belowScreenMargin = 1f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EndRun();
    }

    private void EndRun()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.GameOver();
    }
}
