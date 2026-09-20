using UnityEngine;

public class MyObstacleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float spawnInterval = 2f;
    
    private void Start()
    {
        // InvokeRepeating (methodName, delayBeforeStart, repeatRate)
        InvokeRepeating(nameof(SpawnObstacle), 0f, spawnInterval);
    }

    private void SpawnObstacle()
    {
       
        // The last argument makes the obstacle a child of the spawner, so ClearObstacles can find it
        Instantiate(obstaclePrefab, transform.position, transform.rotation, transform);
    }
}
