using UnityEngine;
using UnityEngine.Serialization;

// Ends the run. Two ways to die: bump into something, or sink off the bottom of the screen.
public class PlayerHit : MonoBehaviour
{
    [Tooltip("How far below the screen the fish has to sink before the run ends.")]
    [FormerlySerializedAs("_belowScreenMargin")]
    [SerializeField] private float belowScreenMargin = 1f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // There is no floor in this game, so without this check a fish that fell off the bottom would
    // keep falling forever and the run would never end.
    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (mainCamera == null) return;

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

    // Both ways of dying come through here, so a run can only ever end one way
    private void EndRun()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.GameOver();
    }
}
