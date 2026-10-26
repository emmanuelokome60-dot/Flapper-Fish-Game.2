using UnityEngine;

// Goes on every coin, added by CoinSpawner.
//
// Coins use "touch" (a trigger), not "bump" (a collision). PlayerHit ends the run on any real
// bump, so if coins were bumps you would die collecting one.
public class CoinPickup : MonoBehaviour
{
    private bool collected;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Stops one coin paying out twice if the fish overlaps it for two frames
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
