using UnityEngine;
using UnityEngine.UI;

// The frozen, blurry, darkened picture behind the pause and game over
// screens. Two layers: a blurry photo of the last frame, and a dark
// sheet on top that also soaks up taps meant for the frozen game.
public class PauseBlur
{
    private readonly Transform canvasRoot;
    private readonly float dimDarkness;
    private readonly int blurSteps;

    private GameObject dim;
    private GameObject blur;
    private RenderTexture blurTexture;

    // Remembers where to build, and how dark and blurry to make it.
    // Builds nothing yet, so a run where you never pause never pays.
    public PauseBlur(Transform canvasRoot, float dimDarkness, int blurSteps)
    {
        this.canvasRoot = canvasRoot;
        this.dimDarkness = dimDarkness;
        this.blurSteps = blurSteps;
    }

    // Darkens the frozen game and soaks up taps meant for it.
    // You hand in whatever must stay readable on top, so this never
    // needs to know what a "game over panel" is.
    public void Show(bool visible, params GameObject[] raiseAbove)
    {
        if (dim == null)
        {
            if (!visible) return;

            dim = new GameObject("PauseDim", typeof(RectTransform), typeof(Image));

            // On the Canvas itself, NOT on whatever holds the Pause
            // button. Once the buttons were grouped into panels, a dark
            // sheet inside one group could not cover the others.
            dim.transform.SetParent(canvasRoot, false);
            Stretch(dim);

            // UI soaks up taps by default, which is what blocks the game.
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, dimDarkness);
        }

        if (blur == null)
        {
            blur = new GameObject("PauseBlur", typeof(RectTransform), typeof(RawImage));
            blur.transform.SetParent(canvasRoot, false);
            Stretch(blur);

            // The dark sheet above already soaks up the taps.
            blur.GetComponent<RawImage>().raycastTarget = false;
        }

        dim.SetActive(visible);
        blur.SetActive(visible && blurSteps > 0);

        if (!visible) return;

        Capture();

        // The UI draws in list order, so later is on top: blur, then
        // dark sheet, then the buttons. Anything not lifted here stays
        // buried, which is how the Pause button becomes untappable.
        blur.transform.SetAsLastSibling();
        dim.transform.SetAsLastSibling();

        foreach (GameObject target in raiseAbove)
        {
            Raise(target);
        }
    }

    // Hands the blur photo's memory back for good.
    // This kind of texture is not cleaned up on its own, so GameManager
    // has to say when the game is shutting down.
    public void Release()
    {
        if (blurTexture != null) blurTexture.Release();
    }

    // Pins something to all four corners of its parent, so it fills the
    // screen whatever shape the phone is.
    private static void Stretch(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Lifts something in front of the dark sheet.
    // Walks UP the tree first and lifts the whole group: lifting the
    // object alone only moves it to the front of its OWN group.
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

    // Makes the blurry photo of the game behind the dark sheet.
    // Takes ONE photo and blurs that. The game is frozen anyway, so it
    // looks identical and costs nothing per frame: no shaders, no assets.
    private void Capture()
    {
        Camera cam = Camera.main;
        if (cam == null || blurSteps <= 0) return;

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);

        // The 24 asks for depth bits. Nothing here uses depth, but
        // Unity 6 refuses to draw a camera into a photo without them
        // and warns every single time you pause.
        RenderTexture frame = RenderTexture.GetTemporary(width, height, 24);

        // Borrowed photos come back carrying the last user's smoothing
        // setting, and when we shrink below, the setting of the thing
        // being READ decides what happens. Smooth averages each 2x2
        // block; unset just grabs one pixel of four, which is chunky,
        // not blurry, and those hard edges survive every later shrink.
        frame.filterMode = FilterMode.Bilinear;

        // The UI is drawn after the camera, so none of it lands in this
        // photo. That is why the sheet and buttons cannot blur themselves.
        RenderTexture wasTargeting = cam.targetTexture;
        Color wasBackground = cam.backgroundColor;

        cam.backgroundColor = new Color(wasBackground.r, wasBackground.g, wasBackground.b, 1f);
        cam.targetTexture = frame;
        cam.Render();
        cam.targetTexture = wasTargeting;
        cam.backgroundColor = wasBackground;

        // Blurs by halving the photo over and over. Each halving
        // averages pixels together, so a few rounds look soft and smeary.
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

        // Borrowed photos are taken back the moment anything else asks,
        // so copy the finished blur into one we own before showing it.
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
