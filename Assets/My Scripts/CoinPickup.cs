using UnityEngine;

// Goes on every coin, added by CoinSpawner.
// Coins use touch, not bump. PlayerHit ends the run on any real bump,
// so as bumps you would die collecting one.
public class CoinPickup : MonoBehaviour
{
    private bool collected;

    // The fish touched this coin. Pays it in and removes it.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Stops one coin paying twice if the fish overlaps two frames.
        if (collected) return;

        if (collision.GetComponent<PlayerController>() == null) return;

        collected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin();
        }

        Destroy(gameObject);
    }
}
