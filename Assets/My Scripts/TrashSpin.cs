using UnityEngine;
using UnityEngine.Serialization;

// Spins a piece of rubbish on the spot so it looks like it's drifting in water.
public class TrashSpin : MonoBehaviour
{
    [Tooltip("How fast it spins, in degrees each second. A minus number spins it the other way.")]
    [FormerlySerializedAs("_degreesPerSecond")]
    [SerializeField] private float degreesPerSecond = 20f;

    public void SetSpin(float degreesPerSecond)
    {
        this.degreesPerSecond = degreesPerSecond;
    }

    private void Update()
    {
        // Game time, not real time, so it freezes with everything else on the menu and pause
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
