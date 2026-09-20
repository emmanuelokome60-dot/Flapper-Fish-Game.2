using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    // Runs when something passes through this object's "Is Trigger" collider (the gap between the pipes)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameManager.Instance.AddScore();
    }
}
