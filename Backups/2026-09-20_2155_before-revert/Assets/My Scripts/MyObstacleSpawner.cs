using UnityEngine;

public class MyObstacleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float spawnInterval = 2f;

    // The GameManager decides when spawning starts, so nothing happens on scene load
    public void StartSpawning()
    {
        // InvokeRepeating (methodName, delayBeforeStart, repeatRate)
        InvokeRepeating(nameof(SpawnObstacle), spawnInterval, spawnInterval);
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(SpawnObstacle));
    }

    private void SpawnObstacle()
    {
        // The last argument makes the obstacle a child of the spawner, so ClearObstacles can find it
        Instantiate(obstaclePrefab, transform.position, transform.rotation, transform);
    }
}