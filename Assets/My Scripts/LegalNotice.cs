using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The Terms of Use and Privacy Policy, shown inside the game.
//
public class LegalNotice
{
    // Set once the player accepts. Checked before the menu is handed over.
    private const string ACCEPTED_KEY = "LegalAccepted";

    private const string TERMS_PATH = "Legal/Terms";
    private const string PRIVACY_PATH = "Legal/Privacy";

    // What the gate says. Deliberately short: a wall of text on first launch gets dismissed
    // unread, and the full documents are one tap away on the same screen.
    private const string GATE_SUMMARY =
        "Please read this before you play.\n\n" +
        "<b>Abyssal Fish is an amateur school project.</b> It was made by students, not by " +
        "professionals, and it is given away free.\n\n" +
        "<b>It is not finished and not optimised.</b> It may crash, run badly, heat your device " +
        "or lose your saved progress.\n\n" +
        "<b>A large part of it was made with AI assistance</b>, including the code.\n\n" +
        "<b>It comes with no warranty, and we accept no liability</b> for any damage to your " +
        "device, your data, your money or any account of yours. You play entirely at your own " +
        "risk.\n\n" +
        "<b>It collects nothing.</b> No internet, no accounts, no ads, no tracking. Your " +
        "progress is five numbers saved on your own phone.\n\n" +
        "Tap Read the full terms below for the complete Terms of Use and Privacy Policy.\n\n" +
        "By tapping Accept you confirm you have read and understood this.";

    private readonly Transform canvasRoot;

    private GameObject gate;
    private GameObject reader;

    // - Remembers where to build, and nothing else.
    // - Nothing is created here: a player who has already accepted and never opens the documents
    //   pays for none of this.
    public LegalNotice(Transform canvasRoot)
    {
        this.canvasRoot = canvasRoot;
    }

    // - True once the player has accepted the terms on this device.
    public static bool Accepted => PlayerPrefs.GetInt(ACCEPTED_KEY, 0) == 1;

    // - Clears the acceptance so the gate shows again. Used by the Editor menu for testing.
    public static void ClearAcceptance()
    {
        PlayerPrefs.DeleteKey(ACCEPTED_KEY);
        PlayerPrefs.Save();
    }

    // - Puts the first-launch gate up, if it has not been accepted on this device yet.
    // - Opaque and on top of everything, so the menu underneath cannot be tapped through it.
    public void ShowGateIfNeeded()
    {
        if (Accepted) return;

        if (gate == null) gate = BuildGate();

        gate.SetActive(true);
        gate.transform.SetAsLastSibling();
    }

    // - Opens the full Terms of Use and Privacy Policy in a scrollable panel.
    // - Reached from the menu button, and from the gate before accepting.
    public void OpenReader()
    {
        if (reader == null) reader = BuildReader();

        reader.SetActive(true);
        reader.transform.SetAsLastSibling();
    }

    // - Builds the first-launch gate: the summary, a link to the full text, Accept and Exit.
    // - Exit quits rather than letting the player past, because the alternative is a player who
    //   never agreed to the terms playing anyway.
    private GameObject BuildGate()
    {
        GameObject panel = BuildPanel("LegalGate", 0.97f);

        BuildTitle(panel.transform, "BEFORE YOU PLAY");
        BuildScrollingText(panel.transform, GATE_SUMMARY, -120f, -200f);

        BuildButton(panel.transform, "Read the full terms", new Vector2(0f, 150f), OpenReader);

        BuildButton(panel.transform, "Accept", new Vector2(-190f, 60f), () =>
        {
            PlayerPrefs.SetInt(ACCEPTED_KEY, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
        });

        BuildButton(panel.transform, "Exit", new Vector2(190f, 60f), Application.Quit);

        return panel;
    }

    // - Builds the scrollable reader holding both documents end to end.
    // - Missing text files are reported in the panel rather than thrown, so a broken build still
    //   shows the player something instead of an empty black screen.
    private GameObject BuildReader()
    {
        GameObject panel = BuildPanel("LegalReader", 0.97f);

        BuildTitle(panel.transform, "TERMS AND PRIVACY");
        BuildScrollingText(panel.transform, Load(TERMS_PATH) + "\n\n\n" + Load(PRIVACY_PATH),
                           -120f, -140f);

        BuildButton(panel.transform, "Close", new Vector2(0f, 60f), () => panel.SetActive(false));

        return panel;
    }

    // - Reads one document out of Resources.
    // - Returns a readable message rather than null, so a missing file cannot blank the panel.
    private static string Load(string path)
    {
        TextAsset asset = Resources.Load<TextAsset>(path);

        if (asset != null) return asset.text;

        return "[" + path + " is missing from Assets/Resources. "
               + "Contact the authors for a copy of this document.]";
    }

    // - Makes the full-screen backing panel that everything else sits on.
    // - Its Image is what blocks taps reaching the menu underneath; the alpha keeps it opaque.
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

    // - Puts a heading across the top of a panel.
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
        label.fontSize = 52f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.93f, 0.90f, 0.78f);
    }

    // - Builds the scrolling body text, sized so it never runs under the buttons.
    // - A ScrollRect rather than a shrinking font: these documents have to stay readable on a
    //   phone, and a vertical drag is what people already expect from text this long.
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
        body.fontSize = 30f;
        body.lineSpacing = 6f;
        body.color = new Color(0.88f, 0.92f, 0.92f);
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

    // - Builds one button anchored to the bottom of a panel and wires it to an action.
    // - Routes through GameManager.PlayClick so these sound like every other button in the game.
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
        rect.sizeDelta = new Vector2(340f, 78f);
        rect.anchoredPosition = position;

        button.GetComponent<Image>().color = new Color(0.10f, 0.42f, 0.44f);

        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(button.transform, false);

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI caption = label.AddComponent<TextMeshProUGUI>();
        caption.text = text;
        caption.fontSize = 30f;
        caption.alignment = TextAlignmentOptions.Center;
        caption.color = Color.white;

        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayClick();
            onClick();
        });
    }
}
