using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
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

    [Header("Skins")]
    [Tooltip("One bare fish per shelf block, in shelf order. Index 0 is the fish the game ships with.")]
    [SerializeField] private Sprite[] skins;

    public bool IsAlive { get; private set; } = true;

    // Where the fish swims back to between surges, set by ResetPlayer()
    private float homeX;

    // The height the menu idle bobs around, set by ResetPlayer() beside homeX
    private float homeY;

    // How the fish drifts while the menu holds the game at timeScale 0.
    // ponytail: consts, not serialized fields. A new [SerializeField] on a component already
    // saved in the scene comes back as 0, and 0 here is the frozen fish this exists to fix.
    private const float IDLE_BOB_HEIGHT = 0.12f;   // world units either side of rest
    private const float IDLE_BOB_SPEED = 1.4f;     // radians per second
    private const float IDLE_TILT = 3f;            // degrees

    // Lives on the Animator child, not here. Found rather than wired, so there is no Inspector
    // reference to go stale if that child is ever renamed or rebuilt.
    private SpriteRenderer fishRenderer;

    // - Wires up the rigidbody and the sprite renderer on the child, and applies the water
    //   feel from the Inspector.
    // - Writing gravity and damping here means these fields win over whatever the scene saved.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fishRenderer = GetComponentInChildren<SpriteRenderer>();

        // Stop physics spinning the fish when it bumps into things
        rb.freezeRotation = true;

        rb.gravityScale = waterGravity;
        rb.linearDamping = waterDrag;
    }

    // True only on the frame the fish actually swims. The lure light reads this rather than
    // polling the pointer itself, so it inherits every guard below for free: no flicker while
    // dead, while paused, on the menu, or when the tap was aimed at a UI button.
    // ponytail: a frame-flag, not an event. A light reading it one frame late is invisible;
    // make it an event if anything ever needs the exact frame.
    public bool SwamThisFrame { get; private set; }

    // - Reads the swim input and gives the fish its upward kick.
    // - Guarded so a tap cannot swim while dead, while frozen, or when it was meant for a button.
    private void Update()
    {
        SwamThisFrame = false;

        if (!IsAlive) return;

        // Frozen on the menu and the game over screen, so a button press is not also a swim
        if (Time.timeScale == 0f)
        {
            MenuIdle();
            return;
        }

        if (!SwimPressed()) return;

        rb.linearVelocityY = swimForce;
        SwamThisFrame = true;

        if (GameManager.Instance != null) GameManager.Instance.PlayMovementPop();
    }

    // A slow rise and sink so the fish reads as suspended in water rather than switched off.
    // The menu holds timeScale at 0, which stops FixedUpdate and the rigidbody with it; Update
    // still runs, so this moves the transform itself off the unscaled clock.
    //
    // Scoped to the menu, not to "frozen": pause and game over are frozen too, and a fish still
    // drifting there reads as the pause not having taken, or as a dead fish still swimming.
    private void MenuIdle()
    {
        if (GameManager.Instance == null || !GameManager.Instance.OnMenu) return;

        float t = Time.unscaledTime * IDLE_BOB_SPEED;

        // Rebuilt from homeY every frame rather than nudged along. A menu can sit for minutes,
        // and an accumulated offset is what once walked the scrolling background out of step.
        Vector3 position = transform.position;
        position.y = homeY + Mathf.Sin(t) * IDLE_BOB_HEIGHT;
        transform.position = position;

        // A quarter-phase behind the rise, so the nose lags it instead of pivoting around it
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t - Mathf.PI * 0.5f) * IDLE_TILT);
    }

    // Space, a mouse click, or a screen tap all make the fish swim
    // - True on the frame a swim was asked for: space, a click, or a screen tap.
    // - Taps landing on UI are rejected here, so a button press is never also a swim.
    private bool SwimPressed()
    {
        // A tap on a UI button must not also swim
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }

    // - Runs the physics side: the current shoves the fish back and it swims home afterwards,
    //   the sink speed is capped, and the body tilts toward where it is heading.
    // - The pull home grows with distance, so displacement caps and nothing accumulates.
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

    // - Marks the fish dead on any real collision. PlayerHit ends the run separately.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsAlive = false;
    }

    // Called by the GameManager when a run starts
    // - Puts the fish back to its starting position, upright and still, ready for a run.
    // - Called by both StartGame and BackToMenu, which is why the skin is applied from here.
    public void ResetPlayer(Vector3 startPosition)
    {
        transform.position = startPosition;
        homeX = startPosition.x;
        homeY = startPosition.y;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = waterGravity;
        IsAlive = true;

        ApplySkin();
    }

    // The fish the player picked in the shop. ResetPlayer calls it, which covers StartGame and
    // BackToMenu; GameManager.ShowCharacters calls it too, because closing the shop goes through
    // neither of those and the menu fish would otherwise keep the old skin until the next run.
    //
    // Read from PlayerPrefs, NOT FishInventory.EquippedIndex: the shop panel is inactive at scene
    // load, so its Awake has not run and the property reads 0 whatever the save says.
    public void ApplySkin()
    {
        if (fishRenderer == null || skins == null || skins.Length == 0) return;

        int skin = PlayerPrefs.GetInt(FishInventory.EQUIPPED_KEY, 0);

        // Falls back to the shipped fish rather than leaving the last run's sprite on screen.
        // Block 6 has no skin yet, so an index past the end is a real case, not paranoia.
        if (skin < 0 || skin >= skins.Length || skins[skin] == null) skin = 0;

        if (skins[skin] != null) fishRenderer.sprite = skins[skin];
    }
}
