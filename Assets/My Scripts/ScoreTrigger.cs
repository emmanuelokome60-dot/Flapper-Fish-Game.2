using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    // Each gap is worth exactly one point, even if the fish clips the zone twice
    private bool scored;

    // Runs when something passes through this object's "Is Trigger" collider (the gap between the pipes)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (scored) return;

        // Only the fish scores. Anything else passing through is ignored.
        if (collision.GetComponent<PlayerControllerScript>() == null) return;

        scored = true;
        GameManager.Instance.AddScore();
    }
}
