using UnityEngine;
using UnityEngine.UI;

// The frozen, blurry, darkened picture you see behind the pause and game over screens.
//
// Two layers: a blurry photo of the last frame of the game, and a dark sheet on top of it. The
// dark sheet also soaks up any taps aimed at the frozen game underneath.
public class PauseBlur
{
    private readonly Transform canvasRoot;
    private readonly float dimDarkness;
    private readonly int blurSteps;

    private GameObject dim;
    private GameObject blur;
    private RenderTexture blurTexture;

    // - Remembers where to build things, and how dark and how blurry to make them.
    // - Builds nothing right now. The layers get made the first time you actually pause, so a run
    //   where you never pause never pays for them.
    public PauseBlur(Transform canvasRoot, float dimDarkness, int blurSteps)
    {
        this.canvasRoot = canvasRoot;
        this.dimDarkness = dimDarkness;
        this.blurSteps = blurSteps;
    }

    // - Darkens the frozen game behind the buttons and soaks up taps meant for it.
    // - You hand in whatever needs to stay readable on top. This class works out the layering;
    //   the caller decides what goes in the pile. That way this never needs to know what a
    //   "game over panel" even is.
    public void Show(bool visible, params GameObject[] raiseAbove)
    {
        if (dim == null)
        {
            if (!visible) return;

            dim = new GameObject("PauseDim", typeof(RectTransform), typeof(Image));

            // Attached to the Canvas itself, NOT to whatever holds the Pause button.
            //
            // Those used to be the same thing, back when every button sat directly on the Canvas.
            // But once the buttons got grouped into panels, the Pause button's parent became a
            // group — and a dark sheet built inside one group can't cover the other groups.
            dim.transform.SetParent(canvasRoot, false);
            Stretch(dim);

            // Anything drawn in the UI soaks up taps by default, which is what blocks the game
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, dimDarkness);
        }

        if (blur == null)
        {
            blur = new GameObject("PauseBlur", typeof(RectTransform), typeof(RawImage));
            blur.transform.SetParent(canvasRoot, false);
            Stretch(blur);

            // The dark sheet sits on top of this one and already soaks up the taps
            blur.GetComponent<RawImage>().raycastTarget = false;
        }

        dim.SetActive(visible);
        blur.SetActive(visible && blurSteps > 0);

        if (!visible) return;

        Capture();

        // The UI draws things in the order they're listed, so later = on top. Blur photo above the
        // game, dark sheet above that, then the buttons above everything. Anything we don't lift
        // up here stays buried under the dark sheet — which is exactly how the Pause button and
        // the score end up untappable while you're paused.
        blur.transform.SetAsLastSibling();
        dim.transform.SetAsLastSibling();

        foreach (GameObject target in raiseAbove)
        {
            Raise(target);
        }
    }

    // - Hands the blur photo's memory back when we're finished with it for good.
    // - This kind of texture isn't cleaned up automatically like normal objects are, so
    //   GameManager has to tell us when the game is shutting down.
    public void Release()
    {
        if (blurTexture != null) blurTexture.Release();
    }

    // - Pins something to all four corners of its parent, so it fills the screen whatever shape
    //   the phone is.
    private static void Stretch(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // - Lifts something to the front, on top of the dark sheet.
    // - It walks UP the family tree first, and lifts the whole group this thing belongs to.
    //   Lifting the object by itself would only move it to the front of its OWN group, and the
    //   group as a whole would stay buried under the dark sheet.
    private void Raise(GameObject target)
    {
        if (target == null) return;

        Transform t = target.transform;

        while (t.parent != null && t.parent != canvasRoot)
        {
            t = t.parent;
        }

        t.SetAsLastSibling();
    }

    // - Makes the blurry photo of the game behind the dark sheet.
    // - It takes ONE photo and blurs that, instead of blurring the game live. The game is frozen
    //   the whole time the pause screen is up, so a photo looks identical to the real thing — and
    //   it costs nothing per frame. No shaders, no special assets, nothing to maintain.
    private void Capture()
    {
        Camera cam = Camera.main;
        if (cam == null || blurSteps <= 0) return;

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);

        // The 24 at the end asks for depth bits. We don't use depth for anything here, but
        // Unity 6 flatly refuses to draw a camera into a photo that has none, and prints a
        // warning every single time you pause until you give it some. So it just has to be there.
        RenderTexture frame = RenderTexture.GetTemporary(width, height, 24);

        // Borrowed photos come back from Unity's pool still carrying whatever smoothing setting
        // the last person left on them, so we always set it ourselves.
        //
        // This matters more than it looks. When we shrink the photo below, the smoothing setting
        // of the thing being READ decides what happens. Set to smooth, shrinking averages each
        // 2x2 block of pixels together — a real blur. Left unset, it just grabs one pixel out of
        // every four and throws the rest away. That's not blurry, it's just chunky, and those
        // hard edges survive every shrink after it no matter how many times you halve it.
        frame.filterMode = FilterMode.Bilinear;

        // The UI is drawn on top AFTER the camera is done, so none of it lands in this photo.
        // That's why the dark sheet and the buttons can't accidentally blur themselves.
        RenderTexture wasTargeting = cam.targetTexture;
        Color wasBackground = cam.backgroundColor;

        cam.backgroundColor = new Color(wasBackground.r, wasBackground.g, wasBackground.b, 1f);
        cam.targetTexture = frame;
        cam.Render();
        cam.targetTexture = wasTargeting;
        cam.backgroundColor = wasBackground;

        // Blur by halving the photo over and over. Each halving averages pixels together, so a
        // few rounds of it leaves a soft, smeary picture. Cheap and it looks right.
        for (int step = 0; step < blurSteps; step++)
        {
            width = Mathf.Max(1, width / 2);
            height = Mathf.Max(1, height / 2);

            RenderTexture smaller = RenderTexture.GetTemporary(width, height, 0);
            smaller.filterMode = FilterMode.Bilinear;
            Graphics.Blit(frame, smaller);

            RenderTexture.ReleaseTemporary(frame);
            frame = smaller;
        }

        // Borrowed photos get taken back the moment anything else asks for one, so we copy the
        // finished blur into a photo we own before showing it on screen.
        if (blurTexture == null || blurTexture.width != width || blurTexture.height != height)
        {
            if (blurTexture != null) blurTexture.Release();
            blurTexture = new RenderTexture(width, height, 0, RenderTextureFormat.Default)
            {
                filterMode = FilterMode.Bilinear
            };
        }

        Graphics.Blit(frame, blurTexture);
        RenderTexture.ReleaseTemporary(frame);

        blur.GetComponent<RawImage>().texture = blurTexture;
    }
}
