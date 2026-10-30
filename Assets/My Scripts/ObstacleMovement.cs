using UnityEngine;

// Slides one thing leftwards and deletes it once it has left the screen.
// Rocks, rubbish and coins all get one, and all read the same speed, so
// none of them can drift out of step with the others.
public class ObstacleMovement : MonoBehaviour
{
    // How much faster the cave is moving than normal. TrashSpawner winds
    // this up as the run goes on, which is how the game gets harder.
    public static float SpeedMultiplier { get; private set; } = 1f;

    // Just the wave part: 1 in calm water, bigger during a surge. The
    // fish reads this too, so the current can shove it around.
    public static float Surge { get; private set; } = 1f;

    [Tooltip("How fast things slide left. Every rock has to use the same number.")]
    [SerializeField] private float speed = 5f;

    // Far enough that you never see a rock pop out of existence.
    private const float DESPAWN_MARGIN = 1f;

    private Renderer pieceRenderer;

    // The speed of the whole cave, changed in one place only.
    // Both values are shared by every rock at once, and locked so only
    // this method writes them. A second writer would be near impossible
    // to spot, and that is how they once leaked between runs.
    public static void SetPace(float speedMultiplier, float surge)
    {
        SpeedMultiplier = speedMultiplier;
        Surge = surge;
    }

    // Puts the cave's speed back to calm before the game loads.
    // This project keeps shared values between Plays to start faster, so
    // stopping mid-surge used to leave the next run starting mid-surge.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SpeedMultiplier = 1f;
        Surge = 1f;
    }

    // Remembers what draws this object; the check below needs it.
    private void Awake()
    {
        pieceRenderer = GetComponentInChildren<Renderer>();
    }

    // Slides this object left, then deletes it once it is gone.
    // Waits for its RIGHT edge, so a wide rock does not vanish while
    // you can still see half of it.
    private void Update()
    {
        transform.position += Vector3.left * (speed * SpeedMultiplier) * Time.deltaTime;

        float rightEdge = pieceRenderer != null ? pieceRenderer.bounds.max.x : transform.position.x;
        if (rightEdge < CameraEdges.Left - DESPAWN_MARGIN)
        {
            Destroy(gameObject);
        }
    }
}
