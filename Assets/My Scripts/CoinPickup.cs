using UnityEngine;

// Added to every coin by CoinSpawner. Collection is OnTriggerEnter2D, never
// OnCollisionEnter2D: PlayerHit ends the run on any real collision.
public class CoinPickup : MonoBehaviour
{
    // Only the first touch counts
    private bool collected;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected) return;

        // Only the fish collects
        if (collision.GetComponent<PlayerControllerScript>() == null) return;

        collected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin();
        }

        Destroy(gameObject);
    }
}
