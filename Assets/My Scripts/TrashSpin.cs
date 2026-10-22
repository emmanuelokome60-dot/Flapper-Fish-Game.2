using UnityEngine;
using UnityEngine.Serialization;

// Turns debris slowly on the spot. Time.deltaTime, so it freezes with everything else.
public class TrashSpin : MonoBehaviour
{
    [Tooltip("Degrees per second. Negative spins the other way.")]
    [FormerlySerializedAs("_degreesPerSecond")]
    [SerializeField] private float degreesPerSecond = 20f;

    // - Sets this piece's turn rate, called by the spawner right after it is created.
    // - Negative values spin it the other way, which is how the spawner varies the debris.
    public void SetSpin(float degreesPerSecond)
    {
        this.degreesPerSecond = degreesPerSecond;
    }

    // - Turns the piece a little further every frame.
    // - Scaled time, so it stops with the rest of the game on the menu and the pause screen.
    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
