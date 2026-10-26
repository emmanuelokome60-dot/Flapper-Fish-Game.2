using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// The fish you control. Reads your taps, swims up, sinks down, tilts, and wears whichever skin
// you picked in the shop.
public class PlayerController : MonoBehaviour
{
    // Found automatically in Awake()
    private Rigidbody2D rb;

    [Tooltip("How far up the fish swims on each tap. Lower = smaller hops, finer control.")]
    [FormerlySerializedAs("_swimForce")]
    [SerializeField] private float swimForce = 4f;

    [Header("Underwater Feel")]
    [Tooltip("How strongly the fish is pulled down. Lower = floatier.")]
    [FormerlySerializedAs("_waterGravity")]
    [SerializeField] private float waterGravity = 1.2f;

    [Tooltip("How thick the water feels. Higher = the fish glides and slows down instead of " +
             "snapping about. This is what stops it feeling like a bird.")]
    [FormerlySerializedAs("_waterDrag")]
    [SerializeField] private float waterDrag = 1f;

    [Tooltip("The fastest the fish is allowed to sink. Keep this low or it drops like a stone " +
             "and one missed tap costs you half a gap.")]
    [FormerlySerializedAs("_maxSinkSpeed")]
    [SerializeField] private float maxSinkSpeed = 4.5f;

    [Header("Current")]
    [Tooltip("How hard a surge shoves the fish backwards. 0 turns currents off.")]
    [FormerlySerializedAs("_currentPush")]
    [SerializeField] private float currentPush = 3f;

    // The fish ends up sitting wherever these two cancel out, so a current can never drag it
    // off the side of the screen no matter how long it lasts
    [Tooltip("How hard the fish swims back to its starting spot. Higher = it gets shoved less.")]
    [FormerlySerializedAs("_currentReturn")]
    [SerializeField] private float currentReturn = 4f;

    [Header("Tilt")]
    [FormerlySerializedAs("_rotationSpeed")]
    [SerializeField] private float rotationSpeed = 5f;
    [FormerlySerializedAs("_maxTiltUp")]
    [SerializeField] private float maxTiltUp = 25f;
    [FormerlySerializedAs("_maxTiltDown")]
    [SerializeField] private float maxTiltDown = -45f;

    [Tooltip("How quickly the fish swings round to its new angle. Lower = smoother.")]
    [FormerlySerializedAs("_tiltSmoothing")]
    [SerializeField] private float tiltSmoothing = 5f;

    [Header("Skins")]
    [Tooltip("One plain fish per shop shelf slot, in shelf order. Slot 0 is the starting fish.")]
    [SerializeField] private Sprite[] skins;

    public bool IsAlive { get; private set; } = true;

    // The spot the fish swims back to after a current shoves it. Set by ResetPlayer()
    private float homeX;

    // The height the fish bobs around on the menu. Set by ResetPlayer() next to homeX
    private float homeY;

    // How the fish drifts on the menu, while the game is frozen.
    //
    // These are plain constants, NOT Inspector fields, and that's deliberate. Adding a new
    // Inspector field to a script that's already saved in the scene makes it load as 0 — and 0
    // here would mean a totally still fish, which is the exact bug this code exists to fix.
    private const float IDLE_BOB_HEIGHT = 0.12f;   // how far it floats up and down
    private const float IDLE_BOB_SPEED = 1.4f;     // how fast it bobs
    private const float IDLE_TILT = 3f;            // how far it tips, in degrees

    // The picture of the fish lives on a child object, not this one. We go and find it rather
    // than filling in an Inspector slot, so nothing breaks if that child gets renamed or rebuilt.
    private SpriteRenderer fishRenderer;

    // - Finds the physics body and the fish picture, then applies the water settings above.
    // - Setting gravity and drag HERE means the numbers above always win over whatever the scene
    //   happened to have saved. So tune the fish on this script, never on its Rigidbody 2D.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fishRenderer = GetComponentInChildren<SpriteRenderer>();

        // Stops the physics from spinning the fish round when it bumps something
        rb.freezeRotation = true;

        rb.gravityScale = waterGravity;
        rb.linearDamping = waterDrag;
    }

    // True only on the single frame the fish actually swims.
    //
    // The head lamp watches this instead of watching for taps itself, which means it gets all the
    // rules below for free: no flash while dead, while paused, on the menu, or when your tap was
    // really aimed at a button.
    public bool SwamThisFrame { get; private set; }

    // - Checks whether you asked to swim, and if so gives the fish its kick upwards.
    // - Guarded so a tap can't swim while you're dead, while frozen, or when it was meant for
    //   a button.
    private void Update()
    {
        SwamThisFrame = false;

        if (!IsAlive) return;

        // Frozen on the menu and the game over screen, so pressing a button isn't also a swim
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

    // Floats the fish slowly up and down on the menu, so it looks like it's hanging in water
    // rather than switched off.
    //
    // The menu freezes the game, which stops the physics completely. Update still runs though, so
    // we move the fish ourselves and use the real-world clock instead of the game clock.
    private void MenuIdle()
    {
        if (GameManager.Instance == null || !GameManager.Instance.OnMenu) return;

        float t = Time.unscaledTime * IDLE_BOB_SPEED;

        // We work out the height fresh from homeY every frame instead of nudging it along a bit
        // at a time. A menu can sit there for several minutes, and tiny errors adding up is
        // exactly what once walked the scrolling background out of line.
        Vector3 position = transform.position;
        position.y = homeY + Mathf.Sin(t) * IDLE_BOB_HEIGHT;
        transform.position = position;

        // Deliberately a bit behind the up-and-down movement, so the nose trails after the body
        // instead of swinging around it
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t - Mathf.PI * 0.5f) * IDLE_TILT);
    }

    // - True on the frame you asked to swim: spacebar, a mouse click, or a tap on the screen.
    // - Taps that landed on a button are thrown out here, so pressing a button never also swims.
    private bool SwimPressed()
    {
        // A tap on a button must not also make the fish swim
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }

    // - Does the physics: the current shoves the fish back, the fish swims home again, sinking is
    //   capped, and the body tilts towards wherever it's heading.
    private void FixedUpdate()
    {
        if (IsAlive)
        {
            // The further the fish gets from home, the harder it swims back. That's why a current
            // can only ever shove it so far and no further, and why nothing builds up over time.
            float push = (ObstacleMovement.Surge - 1f) * currentPush;
            float home = (homeX - transform.position.x) * currentReturn;
            rb.linearVelocityX = home - push;
        }

        // Sink slowly, like something underwater should
        if (rb.linearVelocity.y < -maxSinkSpeed)
        {
            rb.linearVelocityY = -maxSinkSpeed;
        }

        // Nose up when swimming up, nose down when sinking, with limits so it never flips over
        float targetAngle = rb.linearVelocity.y * rotationSpeed;
        targetAngle = Mathf.Clamp(targetAngle, maxTiltDown, maxTiltUp);

        // Swing round to that angle gradually instead of snapping to it
        float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, tiltSmoothing * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // - Marks the fish as dead the moment it properly bumps into anything. PlayerHit is what
    //   actually ends the run.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsAlive = false;
    }

    // - Puts the fish back at the start: right place, facing straight, not moving, ready to go.
    // - Both StartGame and BackToMenu call this, which is why the skin gets applied from in here.
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

    // - Puts on whichever fish you picked in the shop.
    // - ResetPlayer calls this, which covers starting a game and going back to the menu.
    //   GameManager.ShowCharacters calls it too, because closing the shop goes through neither of
    //   those, and without it the menu fish would keep the old skin until your next run.
    public void ApplySkin()
    {
        if (fishRenderer == null || skins == null || skins.Length == 0) return;

        int skin = PlayerPrefs.GetInt(FishInventory.EQUIPPED_KEY, 0);

        // If the saved choice doesn't exist, fall back to the starting fish rather than leaving
        // the last run's picture on screen. Shelf slot 6 has no fish yet, so a bad number here
        // is a real thing that happens, not us being paranoid.
        if (skin < 0 || skin >= skins.Length || skins[skin] == null) skin = 0;

        if (skins[skin] != null) fishRenderer.sprite = skins[skin];
    }
}
