using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The Terms of Use and Privacy Policy screens, shown inside the game.
// Two of them: a one-off screen you must accept the first time, and a
// scrollable reader with both full documents, reached from the menu.
public class LegalNotice
{
    // Saved once you accept. Checked before you get the menu.
    private const string ACCEPTED_KEY = "LegalAccepted";

    private const string TERMS_PATH = "Legal/Terms";
    private const string PRIVACY_PATH = "Legal/Privacy";

    // What the first screen says. Kept short on purpose: a wall of text
    // on first launch gets closed unread, and the full documents are one
    // tap away on the same screen.
    private const string GATE_SUMMARY =
        "Please read this before you play.\n\n" +
        "Abyssal Fish is an amateur school project. It was made by students, not by " +
        "professionals, and it is given away free.\n\n" +
        "It is not finished and not optimised. It may crash, run badly, heat your device " +
        "or lose your saved progress.\n\n" +
        "A large part of it was made with AI assistance, including the code.\n\n" +
        "It comes with no warranty, and we accept no liability for any damage to your " +
        "device, your data, your money or any account of yours. You play entirely at your own " +
        "risk.\n\n" +
        "It collects nothing. No internet, no accounts, no ads, no tracking. Your " +
        "progress is five numbers saved on your own phone.\n\n" +
        "Tap Read the full terms below for the complete Terms of Use and Privacy Policy.\n\n" +
        "By tapping Accept you confirm you have read and understood this.";

    // Taken off the shop's button artwork, so these screens match the
    // game instead of looking like a system pop-up.
    private static readonly Color PlateColour = new Color(0.204f, 0.627f, 0.604f);

    private readonly Transform canvasRoot;
    private TMP_FontAsset gameFont;

    private System.Action accepted;
    private GameObject gate;
    private GameObject reader;

    // Just remembers where to build the screens.
    // Nothing is built yet, so someone who already accepted and never
    // opens the documents never pays for any of it.
    public LegalNotice(Transform canvasRoot)
    {
        this.canvasRoot = canvasRoot;
    }

    // True once you have accepted the terms on this device.
    public static bool Accepted => PlayerPrefs.GetInt(ACCEPTED_KEY, 0) == 1;

    // Forgets that you accepted, so the first screen shows again.
    // Used by the Tools menu for testing.
    public static void ClearAcceptance()
    {
        PlayerPrefs.DeleteKey(ACCEPTED_KEY);
        PlayerPrefs.Save();
    }

    // Puts the first-launch screen up, unless you already accepted.
    // Solid and on top of everything, so the menu cannot be tapped
    // through it.
    public void ShowGateIfNeeded(System.Action onAccepted = null)
    {
        accepted = onAccepted;

        if (Accepted)
        {
            accepted?.Invoke();
            return;
        }

        if (gate == null) gate = BuildGate();

        gate.SetActive(true);
        gate.transform.SetAsLastSibling();
    }

    // Closes the game when someone does not agree to the terms.
    // Application.Quit does nothing in the Editor, which is why Exit
    // looked broken while testing, so there we stop Play instead.
    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Opens both full documents in a panel you can scroll.
    // Reached from the menu button, and from the first screen.
    public void OpenReader()
    {
        if (reader == null) reader = BuildReader();

        reader.SetActive(true);
        reader.transform.SetAsLastSibling();
    }

    // Builds the first-launch screen: summary, a link to the full text,
    // Accept and Exit.
    // Exit really quits, because the alternative is somebody playing a
    // game whose terms they never agreed to.
    private GameObject BuildGate()
    {
        GameObject panel = BuildPanel("LegalGate", 0.97f);

        BuildTitle(panel.transform, "BEFORE YOU PLAY");
        BuildScrollingText(panel.transform, GATE_SUMMARY, -120f, -200f);

        // All three in a row. The harmless middle button separates
        // Accept and Exit, so neither is the one you hit by accident.
        BuildButton(panel.transform, "Accept", new Vector2(-350f, 70f), () =>
        {
            PlayerPrefs.SetInt(ACCEPTED_KEY, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
            accepted?.Invoke();
        });

        BuildButton(panel.transform, "Read the full terms", new Vector2(0f, 70f), OpenReader);

        BuildButton(panel.transform, "Exit", new Vector2(350f, 70f), Quit);

        return panel;
    }

    // Builds the scrollable reader with both documents, one after the
    // other.
    private GameObject BuildReader()
    {
        GameObject panel = BuildPanel("LegalReader", 0.97f);

        BuildTitle(panel.transform, "TERMS AND PRIVACY");
        BuildScrollingText(panel.transform, Load(TERMS_PATH) + "\n\n\n" + Load(PRIVACY_PATH),
                           -120f, -200f);

        BuildButton(panel.transform, "Close", new Vector2(0f, 70f), () => panel.SetActive(false));

        return panel;
    }

    // Loads one document's text out of the project.
    // Gives back a readable message if the file is missing, so a broken
    // build cannot leave the panel blank.
    private static string Load(string path)
    {
        TextAsset asset = Resources.Load<TextAsset>(path);

        if (asset != null) return asset.text;

        return "[" + path + " is missing from Assets/Resources. "
               + "Contact the authors for a copy of this document.]";
    }

    // Makes the full-screen background panel everything else sits on.
    // Its image is what stops taps reaching the menu underneath.
    private GameObject BuildPanel(string name, float alpha)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasRoot, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.02f, 0.06f, 0.07f, alpha);

        return panel;
    }

    // Puts a heading across the top of a panel.
    private void BuildTitle(Transform parent, string text)
    {
        GameObject title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(parent, false);

        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(60f, 0f);
        rect.offsetMax = new Vector2(-60f, -40f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 70f);

        TextMeshProUGUI label = title.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 56f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.93f, 0.90f, 0.78f);
        ApplyGameFont(label);
    }

    // Builds the scrolling body text, sized so it never runs under the
    // buttons.
    // It scrolls rather than shrinking the font, because these documents
    // have to stay readable on a phone.
    private void BuildScrollingText(Transform parent, string text, float topInset, float bottomInset)
    {
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                             typeof(Mask), typeof(ScrollRect));
        viewport.transform.SetParent(parent, false);

        RectTransform viewRect = viewport.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(60f, -bottomInset);
        viewRect.offsetMax = new Vector2(-60f, topInset);

        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(24f, 0f);
        contentRect.offsetMax = new Vector2(-24f, 0f);

        TextMeshProUGUI body = content.AddComponent<TextMeshProUGUI>();
        body.text = text;
        body.fontSize = 32f;
        body.lineSpacing = 8f;
        body.color = new Color(0.96f, 0.97f, 0.97f);
        body.alignment = TextAlignmentOptions.Top;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 40f;
    }

    // Puts the game's own font on a piece of text.
    // BORROWS it off a label already in the scene instead of using an
    // Inspector slot, which is what let these screens be added without
    // touching the scene file at all.
    public void ApplyGameFont(TMP_Text label)
    {
        if (gameFont == null)
        {
            foreach (TMP_Text existing in canvasRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (existing.font == null || existing == label) continue;
                if (!existing.font.name.Contains("Hegarty")) continue;

                gameFont = existing.font;
                break;
            }
        }

        if (gameFont != null) label.font = gameFont;
    }

    // Builds one button at the bottom of a panel.
    // Goes through GameManager.PlayClick, so it sounds like every other
    // button in the game.
    private void BuildButton(Transform parent, string text, Vector2 position,
                             System.Action onClick)
    {
        GameObject button = new GameObject(text, typeof(RectTransform), typeof(Image),
                                           typeof(Button));
        button.transform.SetParent(parent, false);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(320f, 84f);
        rect.anchoredPosition = position;

        button.GetComponent<Image>().color = PlateColour;

        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(button.transform, false);

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI caption = label.AddComponent<TextMeshProUGUI>();
        caption.text = text;
        caption.fontSize = 28f;
        caption.fontStyle = FontStyles.Bold;
        caption.alignment = TextAlignmentOptions.Center;
        caption.color = Color.white;
        ApplyGameFont(caption);

        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayClick();
            onClick();
        });
    }
}
