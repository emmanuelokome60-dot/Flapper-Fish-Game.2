using UnityEngine;

// Slides one thing leftwards and deletes it once it has left the screen.
//
// Rocks, rubbish and coins all get one of these, and they all read the same speed number. That's
// the point: if each one had its own speed they would slowly drift out of step with each other.
public class ObstacleMovement : MonoBehaviour
{
    // How much faster the whole cave is moving than its normal speed. TrashSpawner winds this up
    // as the run goes on deeper into the cave, which is how the game gets harder.
    public static float SpeedMultiplier { get; private set; } = 1f;

    // Just the wave part: 1 in calm water, bigger while a surge is passing. The fish reads this
    // too, so the current can shove it around.
    public static float Surge { get; private set; } = 1f;

    [Tooltip("How fast things slide left. Every rock has to use the same number.")]
    [SerializeField] private float speed = 5f;

    // How far past the edge a rock gets before we delete it. Far enough that you never see it
    // pop out of existence, close enough that we aren't keeping dead rocks around. There is no
    // reason to ever change this, so it isn't in the Inspector.
    private const float DESPAWN_MARGIN = 1f;

    private Renderer pieceRenderer;

    // The speed of the whole cave, changed in one place only.
    //
    // The two values above are shared by every rock in the game at once. They're locked so only
    // this method can change them: if any script could write to them, a second script quietly
    // writing one would be almost impossible to spot. That is exactly how they once leaked from
    // one run into the next. TrashSpawner is in charge of the speed and calls this; everyone
    // else just reads.
    public static void SetPace(float speedMultiplier, float surge)
    {
        SpeedMultiplier = speedMultiplier;
        Surge = surge;
    }

    // - Puts the cave's speed back to calm before the game loads.
    // - Needed because this project has a Unity setting turned on that makes pressing Play
    //   faster by NOT wiping shared values like the two above. Handy, except it means stopping
    //   Play in the middle of a surge left the next run starting mid-surge. This wipes them by
    //   hand, and it runs before the first scene loads, which is the only place that always works.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SpeedMultiplier = 1f;
        Surge = 1f;
    }

    // - Finds the thing that draws this object and remembers it, because the check below needs
    //   it every frame.
    private void Awake()
    {
        pieceRenderer = GetComponentInChildren<Renderer>();
    }

    // - Slides this object left at the cave's speed, then deletes it once it's gone.
    // - We wait for its RIGHT edge to pass the screen edge, so a really wide rock doesn't
    //   disappear while you can still see half of it.
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
