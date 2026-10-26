using UnityEngine;
using UnityEngine.Rendering.Universal;

// The glowing bulb hanging off the anglerfish's head.
//
// It lights up a small pool of cave around the fish. It gets brighter the deeper you go, wobbles
// a little all the time so it looks alive, and flashes briefly every time you swim.
public class SpotLight2D : MonoBehaviour
{
    [Tooltip("How much brighter the bulb sits once the cave is at its darkest. This multiplies " +
             "the Intensity you set on the Light 2D. 1 means depth changes nothing.")]
    [Min(0f)]
    [SerializeField] private float deepMultiplier = 1.6f;

    [Tooltip("Extra brightness for one swim, added on top. 0 turns the flash off.")]
    [Min(0f)]
    [SerializeField] private float pulseBoost = 0.3f;

    [Tooltip("How fast the flash fades away. Higher fades quicker.")]
    [Min(0.01f)]
    [SerializeField] private float recoverSpeed = 7f;

    [Header("Flicker")]
    // A real anglerfish bulb is part of the animal, not a torch. Light that holds perfectly
    // still looks like a flashlight taped to a fish. The wobble is most of what makes it
    // convincing, and it costs almost nothing.
    [Tooltip("How much the bulb wobbles on its own. 0 makes it a steady lamp.")]
    [Range(0f, 1f)]
    [SerializeField] private float flickerDepth = 0.16f;

    [Tooltip("How fast the wobble runs. Higher is twitchier.")]
    [Min(0f)]
    [SerializeField] private float flickerSpeed = 6.5f;

    private Light2D lure;
    private PlayerController swimmer;

    // The Intensity you typed into the Light 2D. Read once and treated as this bulb's "100%".
    //
    // This is the important bit. The old version did it the other way round: it wrote the
    // brightness FROM its own field, which meant the Intensity slider on the Light 2D did
    // nothing once the game started. Two sessions got wasted typing numbers into a slider that
    // was being overwritten on the very first frame. Now the slider is the brightness dial
    // again, and everything in this script only nudges it up and down.
    //
    // The catch: because it's read in Awake, dragging the Intensity slider WHILE the game is
    // running also does nothing. Set it with the game stopped, then press Play.
    private float authoredIntensity;

    // 1 means "exactly the Intensity you typed in". Going deep pushes it up, a swim flashes it.
    private float level = 1f;

    // - Grabs the light and the fish it hangs off, and saves the Intensity you set as 100%.
    // - Read once here on purpose, so tune it with the game stopped, not while playing.
    private void Awake()
    {
        lure = GetComponent<Light2D>();

        if (lure == null)
        {
            Debug.LogError("SpotLight2D: no Light 2D on this object. The lure will not light.", this);
            enabled = false;
            return;
        }

        authoredIntensity = lure.intensity;

        // This light is attached underneath the fish, so we can just look upwards to find it.
        // No Inspector slot to fill in, and nothing that can come unplugged later.
        swimmer = GetComponentInParent<PlayerController>();

        if (swimmer == null)
        {
            Debug.LogError("SpotLight2D: not parented under the player, so it cannot see swims. "
                           + "Make it a child of Player_Fish.", this);
        }
    }

    // - Works out how bright the bulb should be right now: brighter deeper down, a flash on each
    //   swim, and a constant wobble so it looks alive.
    // - Only ever nudges the Intensity you typed in. It never replaces it.
    private void Update()
    {
        // One "how deep are we" number for the whole game, owned by GameManager. We cope with it
        // being missing, so this scene still works if you open it on its own.
        float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
        float resting = Mathf.Lerp(1f, deepMultiplier, depth);
        float peak = resting + pulseBoost;

        // We watch the fish's swim rather than watching for taps ourselves. That way we get all
        // the fish's rules for free: no flash while dead, while paused, on the menu, or when the
        // tap was actually meant for a button.
        if (swimmer != null && swimmer.SwamThisFrame)
        {
            level = Mathf.Min(level + pulseBoost, peak);
        }

        // While paused, deltaTime is 0, so the flash just holds still instead of quietly fading
        // out behind the pause screen.
        level = Mathf.Lerp(level, resting, Time.deltaTime * recoverSpeed);

        // PerlinNoise, not Random. Random would jump to a totally different value every frame,
        // which looks like a loose wire. Perlin wanders smoothly between values, which looks
        // alive. It's centred on 1, so setting flickerDepth to 0 changes nothing at all.
        float flicker = 1f + (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0f) - 0.5f) * 2f * flickerDepth;

        lure.intensity = authoredIntensity * level * flicker;
    }
}
