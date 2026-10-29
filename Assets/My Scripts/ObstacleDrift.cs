using UnityEngine;

// Makes a piece of rubbish bob up and down as it slides past, so it looks like it's floating
// instead of riding on an invisible rail.
//
// Every couple of seconds it picks a new up-or-down direction, and it turns back when it reaches
// the edge of an invisible box around where it was dropped.
//
// It only moves up and down. Sideways is ObstacleMovement's job. Sticking to one direction each
// keeps things simple and means you can actually see how fast it's going. The spawners make sure
// the box never overlaps the cave walls.
public class ObstacleDrift : MonoBehaviour
{
    // How long a piece keeps going one way before choosing again
    private const float MIN_SECONDS_UNTIL_TURN = 1.5f;
    private const float MAX_SECONDS_UNTIL_TURN = 3.5f;

    private float allowedDrift;
    private float driftOffset;
    private float driftVelocity;
    private float minDriftSpeed;
    private float maxDriftSpeed;
    private float secondsUntilTurn;

    // Filled in by ConfigureCurrent(). If no spawner was handed over, this piece ignores the
    // current and just bobs about.
    private CaveSpawner rockSpawner;
    private float currentPush;
    private float pieceRadius;
    private float wallGap;

    // - Sets how far and how fast this piece is allowed to wander, then starts it off.
    // - The spawner calls this straight after making the piece.
    public void Configure(float allowed, float minSpeed, float maxSpeed)
    {
        allowedDrift = allowed;
        minDriftSpeed = minSpeed;
        maxDriftSpeed = maxSpeed;
        PickVelocity();
    }

    // - Signs this piece up to be carried along by the current, faster than the cave moves.
    // - If nobody calls this, the piece just stays put relative to the walls and bobs.
    public void ConfigureCurrent(CaveSpawner rocks, float push, float radius, float wallGap = 0f)
    {
        rockSpawner = rocks;
        currentPush = push;
        pieceRadius = radius;
        this.wallGap = wallGap;
    }

    // - Picks a random new up/down speed and direction, and how long to stick with it.
    // - Every piece rolls its own numbers. That's what stops all the rubbish moving together
    //   like one big sheet.
    private void PickVelocity()
    {
        driftVelocity = Random.Range(minDriftSpeed, maxDriftSpeed) * (Random.value < 0.5f ? -1f : 1f);
        secondsUntilTurn = Random.Range(MIN_SECONDS_UNTIL_TURN, MAX_SECONDS_UNTIL_TURN);
    }

    // - Moves the piece up or down a bit, turning it around when it hits the edge of its box.
    private void Update()
    {
        secondsUntilTurn -= Time.deltaTime;
        if (secondsUntilTurn <= 0f)
        {
            PickVelocity();
        }

        float next = driftOffset + driftVelocity * Time.deltaTime;

        // Turn around at the edge. Without this every piece would pile up against its own limit
        // and the whole wall of rubbish would end up looking flat.
        if (Mathf.Abs(next) > allowedDrift)
        {
            driftVelocity = -driftVelocity;
            next = Mathf.Clamp(next, -allowedDrift, allowedDrift);
        }

        // We add the CHANGE in height rather than setting the height outright. That way this and
        // ObstacleMovement (which is sliding the piece left) don't fight each other.
        transform.position += new Vector3(0f, next - driftOffset, 0f);
        driftOffset = next;

        CarryForward();
    }

    // - Pushes the piece along faster than the cave, and harder while a surge is passing.
    // - If it got ahead of the rocks it would end up somewhere nobody checked was safe, so we
    //   measure the gap between the walls every frame and keep the piece inside it.
    private void CarryForward()
    {
        if (rockSpawner == null || currentPush <= 0f) return;

        // Pushed harder while a surge passes
        transform.position += Vector3.left * (currentPush * ObstacleMovement.Surge) * Time.deltaTime;

        // Ask the rock spawner how wide the gap is at this exact spot
        if (!rockSpawner.TryGetGapAt(transform.position.x, out float low, out float high)) return;

        // If the gap is too narrow to fit, we give up on keeping the piece clear of the walls
        // rather than give up on the piece. Otherwise the "keep it between low and high" maths
        // flips inside out and ends up shoving it straight through the far wall.
        float margin = pieceRadius + wallGap;
        if (high - low < margin * 2f) margin = pieceRadius;
        if (high - low < margin * 2f) return;

        Vector3 p = transform.position;
        float held = Mathf.Clamp(p.y, low + margin, high - margin);
        if (held == p.y) return;

        transform.position = new Vector3(p.x, held, p.z);

        // Treat this new spot as the middle of its box, so it wanders from here rather than
        // immediately heading back into the wall.
        driftOffset = 0f;

        // Flip its direction too, or next frame it drives straight back into the wall and you
        // get a piece buzzing against it like a fly on a window.
        // If we had to move it UP to fix it, the wall must be below it.
        bool wallBelow = held > p.y;
        bool headingIntoWall = wallBelow ? driftVelocity < 0f : driftVelocity > 0f;
        if (headingIntoWall)
        {
            driftVelocity = -driftVelocity;
        }
    }
}
