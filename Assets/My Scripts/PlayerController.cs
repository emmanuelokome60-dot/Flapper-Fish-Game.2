using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// The fish you control. Reads your taps, swims up, sinks down, tilts,
// and wears whichever skin you picked in the shop.
public class PlayerController : MonoBehaviour
{
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

    // The fish settles where these two cancel out, so a current can
    // never drag it off the screen however long it lasts.
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

    // The spot the fish swims back to after a current shoves it.
    private float homeX;

    // The height the fish bobs around on the menu.
    private float homeY;

    // How the fish drifts on the menu while the game is frozen.
    // Constants, NOT Inspector fields, on purpose: a new field on a
    // script the scene already saved loads as 0, and 0 here means a
    // totally still fish, which is the bug this code exists to fix.
    private const float IDLE_BOB_HEIGHT = 0.12f;   // how far it floats up and down
    private const float IDLE_BOB_SPEED = 1.4f;     // how fast it bobs
    private const float IDLE_TILT = 3f;            // how far it tips, in degrees

    // The picture lives on a child object. Found rather than wired, so
    // renaming or rebuilding that child breaks nothing.
    private SpriteRenderer fishRenderer;

    // Finds the physics body and the picture, then applies the water
    // settings above.
    // Set HERE so these numbers beat whatever the scene saved. Tune the
    // fish on this script, never on its Rigidbody 2D.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fishRenderer = GetComponentInChildren<SpriteRenderer>();

        // Stops physics spinning the fish when it bumps something.
        rb.freezeRotation = true;

        rb.gravityScale = waterGravity;
        rb.linearDamping = waterDrag;
    }

    // True only on the frame the fish actually swims.
    // The head lamp watches this instead of watching taps, so it gets
    // every rule below for free.
    public bool SwamThisFrame { get; private set; }

    // Checks whether you asked to swim, and kicks the fish upwards.
    // Guarded, so a tap cannot swim while dead, while frozen, or when
    // it was meant for a button.
    private void Update()
    {
        SwamThisFrame = false;

        if (!IsAlive) return;

        // Frozen on the menu and game over, so a button is not a swim.
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

    // Floats the fish up and down on the menu, so it looks like it is
    // hanging in water rather than switched off.
    // The menu freezes physics, so we move it ourselves on the real
    // clock instead of the game one.
    private void MenuIdle()
    {
        if (GameManager.Instance == null || !GameManager.Instance.OnMenu) return;

        float t = Time.unscaledTime * IDLE_BOB_SPEED;

        // Worked out fresh from homeY each frame, never nudged along. A
        // menu can sit for minutes, and tiny errors adding up is what
        // once walked the scrolling background out of line.
        Vector3 position = transform.position;
        position.y = homeY + Mathf.Sin(t) * IDLE_BOB_HEIGHT;
        transform.position = position;

        // A little behind the bobbing, so the nose trails the body.
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t - Mathf.PI * 0.5f) * IDLE_TILT);
    }

    // True on the frame you asked to swim: space, click, or a tap.
    // Taps that landed on a button are thrown out here.
    private bool SwimPressed()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }

    // Does the physics: the current shoves the fish, it swims home,
    // sinking is capped, and the body tilts the way it is heading.
    private void FixedUpdate()
    {
        if (IsAlive)
        {
            // The further from home, the harder it swims back. That is
            // why a current can only shove it so far, and never builds up.
            float push = (ObstacleMovement.Surge - 1f) * currentPush;
            float home = (homeX - transform.position.x) * currentReturn;
            rb.linearVelocityX = home - push;
        }

        // Sink slowly, like something underwater should.
        if (rb.linearVelocity.y < -maxSinkSpeed)
        {
            rb.linearVelocityY = -maxSinkSpeed;
        }

        // Nose up when swimming, nose down when sinking, with limits so
        // it never flips over.
        float targetAngle = rb.linearVelocity.y * rotationSpeed;
        targetAngle = Mathf.Clamp(targetAngle, maxTiltDown, maxTiltUp);

        // Swing round gradually instead of snapping.
        float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, tiltSmoothing * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Marks the fish dead the moment it bumps anything.
    // PlayerHit is what actually ends the run.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        IsAlive = false;
    }

    // Puts the fish back at the start: right place, facing straight,
    // not moving, ready to go.
    // StartGame and BackToMenu both call this, so the skin goes on here.
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

    // Puts on whichever fish you picked in the shop.
    // ResetPlayer covers starting and the menu. ShowCharacters calls it
    // too, because closing the shop goes through neither.
    public void ApplySkin()
    {
        if (fishRenderer == null || skins == null || skins.Length == 0) return;

        int skin = PlayerPrefs.GetInt(FishInventory.EQUIPPED_KEY, 0);

        // A saved choice that does not exist falls back to the starting
        // fish, rather than leaving the last run's picture on screen.
        if (skin < 0 || skin >= skins.Length || skins[skin] == null) skin = 0;

        if (skins[skin] != null) fishRenderer.sprite = skins[skin];
    }
}
