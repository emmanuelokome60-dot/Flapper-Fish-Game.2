using UnityEngine;
using UnityEngine.Serialization;

// Spins a piece of rubbish on the spot, so it looks like it is drifting
// in the water.
public class TrashSpin : MonoBehaviour
{
    [Tooltip("How fast it spins, in degrees each second. A minus number spins it the other way.")]
    [FormerlySerializedAs("_degreesPerSecond")]
    [SerializeField] private float degreesPerSecond = 20f;

    // TrashSpawner gives each piece its own spin as it drops it in.
    public void SetSpin(float degreesPerSecond)
    {
        this.degreesPerSecond = degreesPerSecond;
    }

    // Turns the piece a little every frame.
    private void Update()
    {
        // Game time, so it freezes with everything else when paused.
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
