using UnityEngine;

// Where the screen edges are. Anything spawning just off screen, or
// deleting what has left, asks here instead of working it out again.
public static class CameraEdges
{
    private static Camera cached;

    private static Camera Cam
    {
        get
        {
            // Unity reports a destroyed camera as null, so one that goes
            // away is looked up again instead of handed back broken.
            if (cached == null) cached = Camera.main;
            return cached;
        }
    }

    // Height is fixed and width follows the phone, so a wide phone shows
    // MORE cave rather than a stretched one.
    public static float HalfWidth => Cam.orthographicSize * Cam.aspect;

    public static float Left => Cam.transform.position.x - HalfWidth;

    public static float Right => Cam.transform.position.x + HalfWidth;

    public static float Top => Cam.transform.position.y + Cam.orthographicSize;
}
