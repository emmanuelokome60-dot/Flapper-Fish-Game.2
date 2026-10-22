using UnityEngine;

// Slides one obstacle left at the cave's shared pace and deletes it once it is off screen.
// Added at runtime to rocks, debris and coins alike, so every one of them moves off the same
// number and none of them can drift out of step.
public class ObstacleMovement : MonoBehaviour
{
    // How much faster than its own speed the whole cave is moving. TrashSpawner ramps it.
    public static float SpeedMultiplier { get; private set; } = 1f;

    // Just the swell part: 1 in calm water, higher during a surge. The fish reads it too.
    public static float Surge { get; private set; } = 1f;

    [Tooltip("How fast obstacles slide left. Every rock must use the same speed.")]
    [SerializeField] private float speed = 5f;

    // Far enough past the edge that a rock is gone before it is deleted, close enough that
    // nothing lingers. Not tunable: it is structural, and nothing about it rewards tweaking.
    private const float DESPAWN_MARGIN = 1f;

    private Renderer pieceRenderer;

    // The pace of the whole cave, set in one place. These are statics: left as public fields,
    // any script could write them and a second writer would be invisible at the call site --
    // which is exactly how they once survived into the next run. TrashSpawner owns the ramp and
    // calls this; everything else reads the properties above.
    // - Sets the pace of the whole cave in one place: rocks, debris and coins all read it.
    // - TrashSpawner owns the ramp and is the only caller; everything else reads the properties.
    public static void SetPace(float speedMultiplier, float surge)
    {
        SpeedMultiplier = speedMultiplier;
        Surge = surge;
    }

    // Enter Play Mode has domain reload disabled (EditorSettings m_EnterPlayModeOptions: 3), so
    // these statics would otherwise carry the previous session's values into the next one -- stop
    // Play mid-surge and the next run begins mid-swell. Runs before the first scene loads whether
    // or not domain reload is on, which is the only reliable place to undo it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    // - Puts the shared pace back to calm before the first scene loads.
    // - Enter Play Mode has domain reload off, so without this a run would inherit the previous
    //   session's speed -- stop Play mid-surge and the next run began mid-swell.
    private static void ResetStatics()
    {
        SpeedMultiplier = 1f;
        Surge = 1f;
    }

    // - Caches the renderer, which the despawn test below needs every frame.
    private void Awake()
    {
        pieceRenderer = GetComponentInChildren<Renderer>();
    }

    // - Slides this obstacle left at the cave's shared speed, then deletes it once it is gone.
    // - The test waits for the renderer's right edge to clear the screen, so a wide rock does
    //   not vanish while part of it is still visible.
    private void Update()
    {
        transform.position += Vector3.left * (speed * SpeedMultiplier) * Time.deltaTime;

        // Wait for the right edge to clear the screen, so wide rocks don't vanish early
        float rightEdge = pieceRenderer != null ? pieceRenderer.bounds.max.x : transform.position.x;
        if (rightEdge < CameraEdges.Left - DESPAWN_MARGIN)
        {
            Destroy(gameObject);
        }
    }
}
