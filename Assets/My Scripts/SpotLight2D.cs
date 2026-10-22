using UnityEngine;
using UnityEngine.Rendering.Universal;

// The anglerfish's lure: one Multiply Light 2D that opens a pool of visible cave around the fish,
// rising as the run goes deeper, wavering the way a living organ does, and blipping on each swim.
//
// Multiply on purpose, and that is the whole design decision. URP 2D blend styles are fixed in
// Renderer2D.asset: 0 is Multiply, 1 is Additive. A Multiply light accumulates into a factor the
// sprite is multiplied by, so it only touches pixels something already drew. An Additive light is
// painted over everything inside its radius, empty water included, which is why the additive
// version of this read as a haze blob at every radius and intensity that was tried. Hazing is not
// a misconfiguration, it is what additive does.
//
// "Only pixels something drew" is NOT the same as "only the rocks": the scrolling background is
// a sprite too, so the lure shows as a bright disc on the water for its whole radius, sliding
// along with the fish. The radius is therefore a look decision, not a reach decision. At outer
// radius 7 the disc was 14 units across on a 10-unit-tall screen and read as a transparent aura
// stuck to the player; 3.5 reads as a pool around the fish. Keep it well under the screen height.
//
// Shadows stay OFF on this light. There is no ShadowCaster2D anywhere in the scene, so the pass
// URP runs for a shadow-enabled light composites a result that cannot change a pixel -- it was
// left on with Shadow Intensity 0, which is all cost and no image.
//
// The cost, accepted deliberately: a Multiply light cannot make the lure itself appear to emit
// anything. The fish stays a silhouette and there is no visible glowing bulb. If that is ever
// wanted, the cheap answer is a small additive sprite on the lure tip, not a second Light 2D.
//
// Class and file name must stay in step -- Unity returns the component as a missing script on
// the next reimport otherwise, the same trap MyGameManager.cs hit early on. This file briefly
// held a class called LightTapPulse while the file was SpotLight2D.cs; both are SpotLight2D now.
public class SpotLight2D : MonoBehaviour
{
    [Tooltip("How much brighter the lure rests once the cave is at its darkest, as a multiple of " +
             "the Intensity set on the Light 2D. 1 means it never changes with depth.")]
    [Min(0f)]
    [SerializeField] private float deepMultiplier = 1.6f;

    [Tooltip("Extra brightness for one swim, as a fraction added on top. 0 turns the blip off.")]
    [Min(0f)]
    [SerializeField] private float pulseBoost = 0.3f;

    [Tooltip("How quickly the blip dies away. Higher is snappier.")]
    [Min(0.01f)]
    [SerializeField] private float recoverSpeed = 7f;

    [Header("Flicker")]
    // A lure is a living organ, not a lamp. A steady pool of light reads as a flashlight taped to
    // a fish; the waver is most of what sells it, and it costs one noise lookup a frame.
    [Tooltip("How far the lure wavers on its own, as a fraction of its level. 0 is a steady lamp.")]
    [Range(0f, 1f)]
    [SerializeField] private float flickerDepth = 0.16f;

    [Tooltip("How fast the waver runs. Higher is twitchier.")]
    [Min(0f)]
    [SerializeField] private float flickerSpeed = 6.5f;

    private Light2D lure;
    private PlayerController swimmer;

    // The Intensity authored on the Light 2D, captured once and treated as this light's 100%.
    //
    // This is the inversion that matters. The old version wrote lure.intensity from its own
    // baseIntensity field, so the Intensity slider on the Light 2D did nothing at runtime -- two
    // sessions were spent typing values into a slider that was overwritten on the first frame of
    // play. The slider is the brightness knob again; everything here only modulates it.
    //
    // The cost of that: this is read in Awake, so dragging Intensity DURING play does nothing
    // either. Tune in edit mode, then press Play.
    private float authoredIntensity;

    // Resting level is 1, meaning the authored intensity. Depth raises it, a swim blips it.
    private float level = 1f;

    // - Grabs the Light 2D and the fish above it, and captures the authored Intensity as 100%.
    // - Read once here, so dragging Intensity during play does nothing: tune in edit mode.
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

        // The light is a child of the fish, so this finds it without an Inspector slot to wire
        // or to come loose.
        swimmer = GetComponentInParent<PlayerController>();

        if (swimmer == null)
        {
            Debug.LogError("SpotLight2D: not parented under the player, so it cannot see swims. "
                           + "Make it a child of Player_Fish.", this);
        }
    }

    // - Drives the lure's brightness: higher the deeper the run, a blip on each swim, and a
    //   constant waver so it reads as a living organ rather than a lamp.
    // - Only modulates the authored Intensity, never replaces it.
    private void Update()
    {
        // One depth number for the whole game, owned by GameManager. Tolerates a missing one so
        // this still behaves in a scene opened on its own.
        float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
        float resting = Mathf.Lerp(1f, deepMultiplier, depth);
        float peak = resting + pulseBoost;

        // Driven off the swim rather than off the pointer: that way the lure inherits every guard
        // the swim already has, and cannot blip while dead, while paused, on the menu, or when
        // the tap was meant for a UI button.
        //
        // Clamped to one blip rather than stacked. Tapping is the one input a player can spam
        // without limit, and an unclamped `level += pulseBoost` let them mash the cave bright and
        // take the darkness back out of the game entirely.
        if (swimmer != null && swimmer.SwamThisFrame)
        {
            level = Mathf.Min(level + pulseBoost, peak);
        }

        // deltaTime is 0 while paused, so the blip holds rather than draining behind the dim
        level = Mathf.Lerp(level, resting, Time.deltaTime * recoverSpeed);

        // Perlin rather than Random: it wanders between values instead of snapping frame to frame,
        // which reads as something alive rather than a loose connection. Centred on 1, so
        // flickerDepth 0 leaves the level exactly as it was.
        //
        // unscaledTime so the lure still breathes on the menu, where timeScale is 0 -- the pause
        // screen is a captured still, so nothing live shows behind it either way.
        float flicker = 1f + (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0f) - 0.5f) * 2f * flickerDepth;

        lure.intensity = authoredIntensity * level * flicker;
    }
}
