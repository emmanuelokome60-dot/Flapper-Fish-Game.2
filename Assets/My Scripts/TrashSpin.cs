using UnityEngine;
using UnityEngine.Serialization;

// Turns debris slowly on the spot. Time.deltaTime, so it freezes with everything else.
public class TrashSpin : MonoBehaviour
{
    [Tooltip("Degrees per second. Negative spins the other way.")]
    [FormerlySerializedAs("_degreesPerSecond")]
    [SerializeField] private float degreesPerSecond = 20f;

    public void SetSpin(float degreesPerSecond)
    {
        this.degreesPerSecond = degreesPerSecond;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
