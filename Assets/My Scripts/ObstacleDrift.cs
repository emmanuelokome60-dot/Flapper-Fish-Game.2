using UnityEngine;

// Lets debris rise and sink while it slides past, so it floats instead of running on rails.
// It re-picks a direction every couple of seconds and turns back at the edge of a band
// around where it was dropped. Vertical only: sideways belongs to ObstacleMovement, and
// one axis keeps all of the speed visible. Spawners keep this distance clear of the walls.
public class ObstacleDrift : MonoBehaviour
{
    // How long a piece holds one direction before picking another
    private const float MIN_SECONDS_UNTIL_TURN = 1.5f;
    private const float MAX_SECONDS_UNTIL_TURN = 3.5f;

    private float allowedDrift;
    private float driftOffset;
    private float driftVelocity;
    private float minDriftSpeed;
    private float maxDriftSpeed;
    private float secondsUntilTurn;

    // Set by ConfigureCurrent(). A null spawner means this piece stays locked to the cave.
    private MyObstacleSpawner rockSpawner;
    private float currentPush;
    private float pieceRadius;
    private float wallGap;

    public void Configure(float allowed, float minSpeed, float maxSpeed)
    {
        allowedDrift = allowed;
        minDriftSpeed = minSpeed;
        maxDriftSpeed = maxSpeed;
        PickVelocity();
    }

    public void ConfigureCurrent(MyObstacleSpawner rocks, float push, float radius, float wallGap = 0f)
    {
        rockSpawner = rocks;
        currentPush = push;
        pieceRadius = radius;
        this.wallGap = wallGap;
    }

    private void PickVelocity()
    {
        driftVelocity = Random.Range(minDriftSpeed, maxDriftSpeed) * (Random.value < 0.5f ? -1f : 1f);
        secondsUntilTurn = Random.Range(MIN_SECONDS_UNTIL_TURN, MAX_SECONDS_UNTIL_TURN);
    }

    private void Update()
    {
        secondsUntilTurn -= Time.deltaTime;
        if (secondsUntilTurn <= 0f)
        {
            PickVelocity();
        }

        float next = driftOffset + driftVelocity * Time.deltaTime;

        // Turn back at the edge, or everything parks at its limit and the wall looks flat
        if (Mathf.Abs(next) > allowedDrift)
        {
            driftVelocity = -driftVelocity;
            next = Mathf.Clamp(next, -allowedDrift, allowedDrift);
        }

        // A delta, so it composes with ObstacleMovement sliding the piece left
        transform.position += new Vector3(0f, next - driftOffset, 0f);
        driftOffset = next;

        CarryForward();
    }

    private void CarryForward()
    {
        if (rockSpawner == null || currentPush <= 0f) return;

        // Stronger while a swell passes
        transform.position += Vector3.left * (currentPush * ObstacleMovement.Surge) * Time.deltaTime;

        // Overtaking the walls puts the piece somewhere it was never placed for, so the
        // corridor is re-checked every frame and the piece held inside it
        if (!rockSpawner.TryGetGapAt(transform.position.x, out float low, out float high)) return;

        // A corridor too tight gives up the gap, not the piece: otherwise the clamp range
        // inverts and shoves it through the far wall
        float margin = pieceRadius + wallGap;
        if (high - low < margin * 2f) margin = pieceRadius;
        if (high - low < margin * 2f) return;

        Vector3 p = transform.position;
        float held = Mathf.Clamp(p.y, low + margin, high - margin);
        if (held == p.y) return;

        transform.position = new Vector3(p.x, held, p.z);

        // Re-centre the band here, so it wanders from here instead of back into the wall
        driftOffset = 0f;

        // Turn the velocity away too, or it drives back in next frame and the piece buzzes
        // against the wall. held > p.y means the wall is below.
        bool wallBelow = held > p.y;
        bool headingIntoWall = wallBelow ? driftVelocity < 0f : driftVelocity > 0f;
        if (headingIntoWall)
        {
            driftVelocity = -driftVelocity;
        }
    }
}
