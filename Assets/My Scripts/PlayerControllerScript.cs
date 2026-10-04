using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerControllerScript : MonoBehaviour
{
    // Auto-wired in Awake()
    private Rigidbody2D rb;

    [Tooltip("How far up the fish swims on each press. Lower = smaller swim.")]
    [FormerlySerializedAs("_swimForce")]
    [SerializeField] private float swimForce = 4f;

    [Header("Underwater Feel")]
    [Tooltip("How strongly the fish sinks. Lower = floatier. Flappy Bird is around 3-4.")]
    [FormerlySerializedAs("_waterGravity")]
    [SerializeField] private float waterGravity = 1.2f;

    [Tooltip("Water resistance, so movement glides instead of snapping.")]
    [FormerlySerializedAs("_waterDrag")]
    [SerializeField] private float waterDrag = 1f;

    [Tooltip("Fastest the fish can sink, so it drifts down instead of dropping.")]
    [FormerlySerializedAs("_maxSinkSpeed")]
    [SerializeField] private float maxSinkSpeed = 4.5f;

    [Header("Current")]
    [Tooltip("How hard a surge shoves the fish back, in units per second. 0 turns it off.")]
    [FormerlySerializedAs("_currentPush")]
    [SerializeField] private float currentPush = 3f;

    // The fish settles where this balances the push, so it can never be walked off screen
    [Tooltip("How strongly the fish swims back to its start. Higher = less drift.")]
    [FormerlySerializedAs("_currentReturn")]
    [SerializeField] private float currentReturn = 4f;

    [Header("Tilt")]
    [FormerlySerializedAs("_rotationSpeed")]
    [SerializeField] private float rotationSpeed = 5f;
    [FormerlySerializedAs("_maxTiltUp")]
    [SerializeField] private float maxTiltUp = 25f;
    [FormerlySerializedAs("_maxTiltDown")]
    [SerializeField] private float maxTiltDown = -45f;

    [Tooltip("How quickly the fish turns to its new angle. Lower = smoother.")]
    [FormerlySerializedAs("_tiltSmoothing")]
    [SerializeField] private float tiltSmoothing = 5f;

    public bool IsAlive { get; private set; } = true;

    // Where the fish swims back to between surges, set by ResetPlayer()
    private float homeX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Stop physics spinning the fish when it bumps into things
        rb.freezeRotation = true;

        rb.gravityScale = waterGravity;
        rb.linearDamping = waterDrag;
    }

    private void Update()
    {
        if (!IsAlive) return;

        // Frozen on the menu and the game over screen, so a button press is not also a swim
        if (Time.timeScale == 0f) return;

        if (!SwimPressed()) return;

        rb.linearVelocityY = swimForce;
    }

    // Space, a mouse click, or a screen tap all make the fish swim
    private bool SwimPressed()
    {
        // A tap on a UI button must not also swim
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }

    private void FixedUpdate()
    {
        // The current shoves the fish back and it swims home afterwards. The pull home grows
        // with distance, so displacement caps at push / currentReturn and nothing accumulates.
        if (IsAlive)
        {
            float push = (ObstacleMovement.Surge - 1f) * currentPush;
            float home = (homeX - transform.position.x) * currentReturn;
            rb.linearVelocityX = home - push;
        }

        // Sink slowly, like in water
        if (rb.linearVelocity.y < -maxSinkSpeed)
        {
            rb.linearVelocityY = -maxSinkSpeed;
        }

        // Tilt up when swimming up, down when sinking, clamped so it never flips over
        float targetAngle = rb.linearVelocity.y * rotationSpeed;
        targetAngle = Mathf.Clamp(targetAngle, maxTiltDown, maxTiltUp);

        // Turn towards it gradually instead of snapping
        float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, tiltSmoothing * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsAlive = false;
    }

    // Called by the GameManager when a run starts
    public void ResetPlayer(Vector3 startPosition)
    {
        transform.position = startPosition;
        homeX = startPosition.x;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = waterGravity;
        IsAlive = true;
    }
}
