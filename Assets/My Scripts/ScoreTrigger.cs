using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    // One point per gap, even if the fish clips the zone twice
    private bool scored;

    // Fires when something passes through the gap's trigger collider
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (scored) return;

        // Only the fish scores
        if (collision.GetComponent<PlayerControllerScript>() == null) return;

        scored = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore();
        }
    }
}
