using UnityEngine;

// Makes a piece of rubbish bob up and down as it slides past, so it
// looks like it is floating instead of riding an invisible rail.
// It only moves up and down; sideways is ObstacleMovement's job.
public class ObstacleDrift : MonoBehaviour
{
    // How long a piece keeps going one way before choosing again.
    private const float MIN_SECONDS_UNTIL_TURN = 1.5f;
    private const float MAX_SECONDS_UNTIL_TURN = 3.5f;

    private float allowedDrift;
    private float driftOffset;
    private float driftVelocity;
    private float minDriftSpeed;
    private float maxDriftSpeed;
    private float secondsUntilTurn;

    // Filled in by ConfigureCurrent(). With no spawner handed over, the
    // piece ignores the current and just bobs.
    private CaveSpawner rockSpawner;
    private float currentPush;
    private float pieceRadius;
    private float wallGap;

    // Sets how far and how fast this piece may wander, then starts it.
    // The spawner calls this straight after making the piece.
    public void Configure(float allowed, float minSpeed, float maxSpeed)
    {
        allowedDrift = allowed;
        minDriftSpeed = minSpeed;
        maxDriftSpeed = maxSpeed;
        PickVelocity();
    }

    // Signs this piece up to be carried by the current.
    // Without this it stays put against the walls and just bobs.
    public void ConfigureCurrent(CaveSpawner rocks, float push, float radius, float wallGap = 0f)
    {
        rockSpawner = rocks;
        currentPush = push;
        pieceRadius = radius;
        this.wallGap = wallGap;
    }

    // Picks a new up or down speed, and how long to stick with it.
    // Every piece rolls its own, which is what stops the rubbish moving
    // together like one big sheet.
    private void PickVelocity()
    {
        driftVelocity = Random.Range(minDriftSpeed, maxDriftSpeed) * (Random.value < 0.5f ? -1f : 1f);
        secondsUntilTurn = Random.Range(MIN_SECONDS_UNTIL_TURN, MAX_SECONDS_UNTIL_TURN);
    }

    // Moves the piece up or down, turning it round at its box edge.
    private void Update()
    {
        secondsUntilTurn -= Time.deltaTime;
        if (secondsUntilTurn <= 0f)
        {
            PickVelocity();
        }

        float next = driftOffset + driftVelocity * Time.deltaTime;

        // Turn at the edge, or every piece piles up against its own
        // limit and the wall of rubbish ends up looking flat.
        if (Mathf.Abs(next) > allowedDrift)
        {
            driftVelocity = -driftVelocity;
            next = Mathf.Clamp(next, -allowedDrift, allowedDrift);
        }

        // Adds the CHANGE in height, not the height, so this and
        // ObstacleMovement do not fight over the position.
        transform.position += new Vector3(0f, next - driftOffset, 0f);
        driftOffset = next;

        CarryForward();
    }

    // Pushes the piece along faster than the cave, harder during a surge.
    // Ahead of the rocks it would land somewhere nobody checked was
    // safe, so the gap is measured every frame and it is kept inside.
    private void CarryForward()
    {
        if (rockSpawner == null || currentPush <= 0f) return;

        transform.position += Vector3.left * (currentPush * ObstacleMovement.Surge) * Time.deltaTime;

        if (!rockSpawner.TryGetGapAt(transform.position.x, out float low, out float high)) return;

        // Too narrow to fit: give up keeping it clear of the walls
        // rather than give up on the piece. Otherwise the maths flips
        // and shoves it straight through the far wall.
        float margin = pieceRadius + wallGap;
        if (high - low < margin * 2f) margin = pieceRadius;
        if (high - low < margin * 2f) return;

        Vector3 p = transform.position;
        float held = Mathf.Clamp(p.y, low + margin, high - margin);
        if (held == p.y) return;

        transform.position = new Vector3(p.x, held, p.z);

        // Treat this as the middle of its box, so it wanders from here
        // instead of heading straight back into the wall.
        driftOffset = 0f;

        // Flip direction too, or next frame it drives back into the wall
        // and buzzes against it like a fly on a window.
        bool wallBelow = held > p.y;
        bool headingIntoWall = wallBelow ? driftVelocity < 0f : driftVelocity > 0f;
        if (headingIntoWall)
        {
            driftVelocity = -driftVelocity;
        }
    }
}
