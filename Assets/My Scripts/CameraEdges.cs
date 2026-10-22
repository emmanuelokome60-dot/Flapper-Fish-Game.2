using UnityEngine;

// Where the screen ends, in world units. Four scripts had each grown their own private copy of
// these one-line methods -- three copies too many -- and ObstacleMovement was calling
// Camera.main once per obstacle per frame to get one of them.
public static class CameraEdges
{
    private static Camera cached;

    // - Hands back the main camera, fetching it the first time and reusing it after.
    // - Unity's == null reports true for a destroyed camera as well as a missing one, so a
    //   camera that goes away is simply looked up again rather than returning a dead reference.
    private static Camera Cam
    {
        get
        {
            if (cached == null) cached = Camera.main;
            return cached;
        }
    }

    // Height is locked at the orthographic size and the width follows the aspect, so a wider
    // phone sees more cave rather than a stretched one. Everything that places something
    // relative to the screen edge has to go through this or it lands differently per device.
    public static float HalfWidth => Cam.orthographicSize * Cam.aspect;

    public static float Left => Cam.transform.position.x - HalfWidth;

    public static float Right => Cam.transform.position.x + HalfWidth;

    public static float Top => Cam.transform.position.y + Cam.orthographicSize;
}
