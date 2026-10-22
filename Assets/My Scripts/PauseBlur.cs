using UnityEngine;
using UnityEngine.UI;

// The frozen, blurred, darkened backdrop behind the pause and game over screens: a dim that
// swallows taps aimed at the frozen game, and a blurred still of the last frame under it.
public class PauseBlur
{
    private readonly Transform canvasRoot;
    private readonly float dimDarkness;
    private readonly int blurSteps;

    private GameObject dim;
    private GameObject blur;
    private RenderTexture blurTexture;

    // - Remembers where to build the dim and how dark and how blurred it should be.
    // - Builds nothing yet; the objects are made on the first Show so a run that never pauses
    //   never pays for them.
    public PauseBlur(Transform canvasRoot, float dimDarkness, int blurSteps)
    {
        this.canvasRoot = canvasRoot;
        this.dimDarkness = dimDarkness;
        this.blurSteps = blurSteps;
    }

    // Darkens the frozen game behind the buttons and swallows taps aimed at it. Whatever has to
    // stay readable on top is passed in: this class decides the stacking, the caller decides
    // what is in the stack, so it never has to know what a game over panel is.
    public void Show(bool visible, params GameObject[] raiseAbove)
    {
        if (dim == null)
        {
            if (!visible) return;

            dim = new GameObject("PauseDim", typeof(RectTransform), typeof(Image));

            // Parented to the Canvas itself rather than to the Pause button's parent. Those
            // were the same thing while every element sat directly under the Canvas, but the
            // moment the UI is grouped into panels the button's parent is a group, and a dim
            // built inside one group cannot cover the others.
            dim.transform.SetParent(canvasRoot, false);
            Stretch(dim);

            // A Graphic is a raycast target by default, which is what blocks the taps
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, dimDarkness);
        }

        if (blur == null)
        {
            blur = new GameObject("PauseBlur", typeof(RectTransform), typeof(RawImage));
            blur.transform.SetParent(canvasRoot, false);
            Stretch(blur);

            // The dim sits on top of this and already swallows the taps
            blur.GetComponent<RawImage>().raycastTarget = false;
        }

        dim.SetActive(visible);
        blur.SetActive(visible && blurSteps > 0);

        if (!visible) return;

        Capture();

        // UI draws in sibling order: the dim above the run, these above the dim. Anything not
        // raised here stays under it, which is how Pause and the score end up untappable.
        blur.transform.SetAsLastSibling();
        dim.transform.SetAsLastSibling();

        foreach (GameObject target in raiseAbove)
        {
            Raise(target);
        }
    }

    // A RenderTexture is native memory and is not collected with the object that held it, so
    // GameManager.OnDestroy() has to say when this is finished with.
    public void Release()
    {
        if (blurTexture != null) blurTexture.Release();
    }

    // Fill the Canvas whatever shape the device is
    // - Pins a RectTransform to all four corners of its parent, so it covers any screen shape.
    private static void Stretch(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Raises whichever top-level object under the Canvas contains this one. Calling
    // SetAsLastSibling on the object itself only reorders it among its own siblings, so once
    // the UI is grouped into panels it would move within its group and stay under the dim.
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

    // Blurs the frozen game behind the dim, by grabbing one frame rather than blurring live.
    // timeScale is 0 the whole time the dim is up, so a still is indistinguishable from the
    // real thing, and this costs nothing per frame. No shader, no renderer feature, no asset.
    private void Capture()
    {
        Camera cam = Camera.main;
        if (cam == null || blurSteps <= 0) return;

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);

        // 24 depth bits rather than 0: Unity 6's Render Graph refuses to render a camera into
        // a target whose Depth Stencil Format is None, and warns on every pause until it has
        // one. Nothing here reads the depth; it just has to exist.
        RenderTexture frame = RenderTexture.GetTemporary(width, height, 24);

        // GetTemporary hands back a pooled texture carrying whatever filterMode its last user
        // left on it, and the filter a Blit uses is the SOURCE's, not the destination's. Left
        // unset, the first halving below reads one texel in four instead of averaging a 2x2
        // block: it decimates rather than blurs, and those hard edges survive every later step
        // however many times they are halved. The copies in the loop set it for the same reason.
        frame.filterMode = FilterMode.Bilinear;

        // The Canvas is Screen Space - Overlay, so UI is composited after the camera and never
        // lands in this capture. The dim and the buttons cannot blur themselves.
        RenderTexture wasTargeting = cam.targetTexture;
        Color wasBackground = cam.backgroundColor;

        cam.backgroundColor = new Color(wasBackground.r, wasBackground.g, wasBackground.b, 1f);
        cam.targetTexture = frame;
        cam.Render();
        cam.targetTexture = wasTargeting;
        cam.backgroundColor = wasBackground;

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

        // Temporaries go back to the pool as soon as anything else asks for one, so the result
        // is copied into a texture this class owns before the RawImage is pointed at it.
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
