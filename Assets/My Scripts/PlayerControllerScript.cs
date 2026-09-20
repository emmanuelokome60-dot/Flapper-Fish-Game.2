using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerControllerScript : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [Tooltip("How far up the fish swims on each press. Lower = smaller swim.")]
    [SerializeField] private float _swimForce = 4f;

    [Header("Underwater Feel")]
    [Tooltip("How strongly the fish sinks. Lower = floatier. Normal Flappy Bird is around 3-4.")]
    [SerializeField] private float _waterGravity = 1.2f;
    [Tooltip("Water resistance. Slows every movement down so it glides instead of snapping.")]
    [SerializeField] private float _waterDrag = 1f;
    [Tooltip("The fastest the fish can sink, so it drifts down instead of dropping like a stone.")]
    [SerializeField] private float _maxSinkSpeed = 4.5f;

    [Header("Tilt")]
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _maxTiltUp = 25f;
    [SerializeField] private float _maxTiltDown = -45f;
    [Tooltip("How quickly the fish turns to its new angle. Lower = slower, smoother turning.")]
    [SerializeField] private float _tiltSmoothing = 5f;

    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Stop physics from spinning the fish when it bumps into things
        rb.freezeRotation = true;

        // Water physics
        rb.gravityScale = _waterGravity;
        rb.linearDamping = _waterDrag;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsAlive) return;

        // Frozen on the menu or the game over screen, so the click that presses
        // a button is not also read as a swim
        if (Time.timeScale == 0f) return;

        if (SwimPressed())
        {
            rb.linearVelocityY = _swimForce;
        }
    }

    // Space, a mouse click, or a screen tap all make the fish swim
    private bool SwimPressed()
    {
        // A tap on a UI button (Pause, Restart, Home) must not also make the fish swim
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }

    private void FixedUpdate()
    {
        // Sink slowly, like in water
        if (rb.linearVelocity.y < -_maxSinkSpeed)
        {
            rb.linearVelocityY = -_maxSinkSpeed;
        }

        // Tilt the fish up when swimming up and down when sinking
        float targetAngle = rb.linearVelocity.y * _rotationSpeed;

        // Keep the tilt between the limits so the fish never flips over
        targetAngle = Mathf.Clamp(targetAngle, _maxTiltDown, _maxTiltUp);

        // Turn towards the target angle gradually instead of snapping to it
        float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, _tiltSmoothing * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsAlive = false;
    }

    // Called by the GameManager when the Play button restarts the game
    public void ResetPlayer(Vector3 startPosition)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        IsAlive = true;
    }
}
