using UnityEngine;
using UnityEngine.Rendering.Universal;

// The glowing bulb hanging off the anglerfish's head.
// Lights a small pool of cave, gets brighter the deeper you go, wobbles
// so it looks alive, and flashes each time you swim.
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
    // A real anglerfish bulb is part of the animal. Light that holds
    // perfectly still looks like a torch taped to a fish.
    [Tooltip("How much the bulb wobbles on its own. 0 makes it a steady lamp.")]
    [Range(0f, 1f)]
    [SerializeField] private float flickerDepth = 0.16f;

    [Tooltip("How fast the wobble runs. Higher is twitchier.")]
    [Min(0f)]
    [SerializeField] private float flickerSpeed = 6.5f;

    private Light2D lure;
    private PlayerController swimmer;

    // The Intensity you typed on the Light 2D, read once as this bulb's
    // 100%. The old version wrote brightness FROM its own field, so the
    // slider did nothing once the game started -- two sessions were lost
    // to that. Read in Awake, so set it with the game stopped.
    private float authoredIntensity;

    // 1 means exactly the Intensity you typed. Depth pushes it up, a
    // swim flashes it.
    private float level = 1f;

    // Grabs the light and the fish, and saves your Intensity as 100%.
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

        // Attached under the fish, so we just look upwards for it. No
        // Inspector slot to fill in or come unplugged.
        swimmer = GetComponentInParent<PlayerController>();

        if (swimmer == null)
        {
            Debug.LogError("SpotLight2D: not parented under the player, so it cannot see swims. "
                           + "Make it a child of Player_Fish.", this);
        }
    }

    // Works out how bright the bulb should be: brighter deeper down, a
    // flash on each swim, and a constant wobble.
    // Only ever nudges your Intensity. It never replaces it.
    private void Update()
    {
        // One depth number for the whole game, owned by GameManager.
        float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
        float resting = Mathf.Lerp(1f, deepMultiplier, depth);
        float peak = resting + pulseBoost;

        // Watches the fish's swim, not taps, so it gets the fish's rules
        // for free: no flash while dead, paused, or on the menu.
        if (swimmer != null && swimmer.SwamThisFrame)
        {
            level = Mathf.Min(level + pulseBoost, peak);
        }

        // deltaTime is 0 while paused, so the flash holds instead of
        // quietly fading out behind the pause screen.
        level = Mathf.Lerp(level, resting, Time.deltaTime * recoverSpeed);

        // PerlinNoise, not Random. Random jumps about and looks like a
        // loose wire; Perlin wanders smoothly and looks alive. Centred
        // on 1, so flickerDepth 0 changes nothing.
        float flicker = 1f + (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0f) - 0.5f) * 2f * flickerDepth;

        lure.intensity = authoredIntensity * level * flicker;
    }
}
