using UnityEngine;

// Where the screen edges are. Anything spawning just off screen, or deleting what has left,
// asks here. Four scripts used to each work this out themselves, and the rock mover asked Unity
// for the main camera once per rock per frame.
public static class CameraEdges
{
    private static Camera cached;

    private static Camera Cam
    {
        get
        {
            // Unity reports a destroyed camera as null too, so a camera that goes away is simply
            // looked up again rather than handed back broken.
            if (cached == null) cached = Camera.main;
            return cached;
        }
    }

    // Height is fixed, width follows the phone, so a wide phone shows MORE cave rather than a
    // stretched one. Line something up with a screen edge any other way and it lands differently
    // on every device.
    public static float HalfWidth => Cam.orthographicSize * Cam.aspect;

    public static float Left => Cam.transform.position.x - HalfWidth;

    public static float Right => Cam.transform.position.x + HalfWidth;

    public static float Top => Cam.transform.position.y + Cam.orthographicSize;
}
