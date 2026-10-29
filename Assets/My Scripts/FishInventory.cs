using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The fish shop you get to from the Characters button.
//
// It's a shelf you swipe sideways. Fish you haven't got yet are grey and can be bought with coins.
// Fish you own can be worn. Exactly one fish is worn at any time.
public class FishInventory : MonoBehaviour
{
    [Header("Shelf")]
    [Tooltip("The scrolling area's Content object. The things inside it are the fish slots, " +
             "in the order they appear on the shelf.")]
    [SerializeField] private Transform content;

    [Header("Card art")]
    [Tooltip("The grey card shown for a fish you haven't bought yet.")]
    [SerializeField] private Sprite lockedArt;

    [Tooltip("One card per fish, in the same order as the shelf slots. The first one is yours " +
             "from the start -- it's the fish the game comes with.")]
    [SerializeField] private Sprite[] fishArt;

    [Header("Button art")]
    [Tooltip("Shown on a fish you don't own yet. Tapping it spends your coins.")]
    [SerializeField] private Sprite buyArt;

    [Tooltip("Shown on a fish you own but aren't currently wearing.")]
    [SerializeField] private Sprite selectArt;

    [Tooltip("Shown on the one fish you're wearing. Tapping it does nothing.")]
    [SerializeField] private Sprite equippedArt;

    [Header("Price")]
    [Tooltip("How many coins one fish costs. Same price for all of them.")]
    [Min(0)]
    [SerializeField] private int price = 300;

    [Header("Wallet")]
    // The score counters are hidden on the menu, so without this you couldn't see how many coins
    // you have on the one screen that actually takes them off you. Optional, so the shelf still
    // works before this slot is filled in.
    [Tooltip("Shows your coin total while the shop is open. Leave empty to show no total.")]
    [SerializeField] private TMP_Text walletText;

    // Which fish you're wearing. Used by this panel for its own drawing.
    //
    // Do NOT read this to decide what the in-game fish looks like. This panel is switched off when
    // the scene loads, so its setup hasn't run yet and this reads 0 no matter what you saved.
    // Read the saved "FishEquipped" value instead.
    public int EquippedIndex { get; private set; }

    // Which fish you own, packed into a single whole number.
    //
    // Think of the number in binary: one digit per fish, 1 for owned and 0 for not. We do it this
    // way because Unity's save system can't store a list, and five separate saved values would be
    // five chances to accidentally update one and forget the others.
    //
    // Public for the same reason as EQUIPPED_KEY below. The Tools menu resets this key, and a
    // second copy of the name is how a reset ends up clearing something nothing reads.
    //
    // The "2" retires every save written while the shelf had six cards. Slot 4 came free back
    // then, so those saves already have its digit set -- and slot 4 is now the fish you pay for.
    // Trusting them would hand it over for nothing. Coins are a separate key and are untouched.
    public const string OWNED_KEY = "FishOwned2";

    // Public because PlayerController reads this to pick the in-game fish. It's written down once,
    // here, rather than typed as text into two different files — which is how two files end up
    // quietly disagreeing about the name.
    public const string EQUIPPED_KEY = "FishEquipped";

    private Image[] cardArt;
    private Button[] buttons;
    private TMP_Text[] priceText;
    private int ownedMask;

    // - Finds all the fish slots on the shelf and connects each button to its own slot number.
    // - The slots are found in code rather than listed in the Inspector, so adding a new fish
    //   needs no changes in here at all.
    private void Awake()
    {
        if (content == null)
        {
            Debug.LogError("FishInventory: no Content wired, so there is no shelf to fill.", this);
            enabled = false;
            return;
        }

        int count = content.childCount;
        cardArt = new Image[count];
        buttons = new Button[count];
        priceText = new TMP_Text[count];

        for (int i = 0; i < count; i++)
        {
            Transform block = content.GetChild(i);
            cardArt[i] = block.GetComponent<Image>();
            priceText[i] = block.GetComponentInChildren<TMP_Text>(true);

            Button button = block.GetComponentInChildren<Button>(true);

            if (button == null)
            {
                Debug.LogError("FishInventory: block '" + block.name + "' has no Button child, so "
                               + "it can never be bought or equipped.", block);
                continue;
            }

            buttons[i] = button;

            // We copy i into its own variable here. Without that, all the buttons would end up
            // sharing one counter and every single one of them would act on the LAST slot.
            int index = i;
            button.onClick.AddListener(() => OnBlockClicked(index));
        }

        // The shop's coin total uses the same coin picture AND the same size as the in-game one.
        // Letting the shop work out its own size gave it a 73.6px coin against the game's 80px,
        // which makes it look like the number means something different on each screen.
        if (walletText != null && GameManager.Instance != null)
        {
            GameManager.Instance.BuildWalletCounter(walletText);

            // The text box is centred on the frame and shrinks to fit its own text, so the digits
            // stay centred however long the number gets. A fixed 300-wide box would leave the
            // text sitting wherever it landed inside that box, drifting off centre as your coin
            // total got shorter.
            ContentSizeFitter fitter = walletText.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = walletText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    // - Re-reads your save every time the shop opens, not just once at the start.
    // - Coins you earned during a run change what you can afford, and this panel isn't rebuilt
    //   between visits. Reading the save again here also means anything that changed it from
    //   elsewhere gets picked up just by closing and reopening the shop.
    private void OnEnable()
    {
        if (cardArt != null) Load();
    }

    // - Loads which fish you own and which you're wearing, then redraws the shelf.
    private void Load()
    {
        ownedMask = PlayerPrefs.GetInt(OWNED_KEY, 0);
        EquippedIndex = PlayerPrefs.GetInt(EQUIPPED_KEY, 0);

        // Every fish except the LAST one comes free with the game. That way you can swap fish from
        // the very first launch, and you save your coins up for one special fish to aim for.
        //
        // Worked out from how many slots there are, not written down as a number, so adding a new
        // slot automatically makes it the new one to save up for without touching this line.
        if (cardArt.Length > 1) ownedMask |= (1 << (cardArt.Length - 1)) - 1;

        if (!Owns(EquippedIndex)) EquippedIndex = 0;

        Refresh();
    }

    // - True if you own that fish. Reads one digit out of the packed number described above.
    // - It also guards against a negative slot number. Old versions of the game saved -1 to mean
    //   "nothing worn", and the maths here would turn -1 into 31 and give a nonsense answer.
    private bool Owns(int index)
    {
        return index >= 0 && (ownedMask & (1 << index)) != 0;
    }

    // - Buys the fish if you don't own it and can afford it, then wears it either way.
    private void OnBlockClicked(int index)
    {
        if (!Owns(index))
        {
            // Does nothing and leaves your coins alone if you can't afford it. TrySpendCoins both
            // checks and subtracts in one go, so there's no gap in between where you could get
            // the fish without paying for it.
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.TrySpendCoins(price)) return;

            ownedMask |= 1 << index;
            PlayerPrefs.SetInt(OWNED_KEY, ownedMask);

            // Saved straight away. A fish you bought that vanishes when the app closes is
            // 300 stolen coins.
            PlayerPrefs.Save();
        }

        // Buying also wears it. Nobody spends 300 coins and then wants to press a second button.
        EquippedIndex = index;
        PlayerPrefs.SetInt(EQUIPPED_KEY, EquippedIndex);
        PlayerPrefs.Save();

        Refresh();
    }

    // - The one and only place that decides what a shelf slot looks like.
    // - Every slot is redrawn from scratch each time. That's what makes "only one fish is worn"
    //   automatically true, instead of relying on somebody remembering to un-wear the old one.
    private void Refresh()
    {
        for (int i = 0; i < cardArt.Length; i++)
        {
            bool owned = Owns(i);

            if (cardArt[i] != null)
            {
                // The fish pictures might not be filled in yet. Everything else in here is checked
                // for being empty, and this one wasn't — it crashed instead of just drawing the
                // grey locked card.
                bool hasArt = fishArt != null && i < fishArt.Length;
                Sprite art = owned && hasArt ? fishArt[i] : lockedArt;
                if (art != null) cardArt[i].sprite = art;
            }

            // The price is hidden once you own the fish, because that strip of the card is where
            // the fish's description goes.
            if (priceText[i] != null)
            {
                priceText[i].gameObject.SetActive(!owned);
                if (!owned) priceText[i].text = price.ToString();
            }

            if (buttons[i] == null) continue;

            Sprite badge = !owned ? buyArt : (i == EquippedIndex ? equippedArt : selectArt);
            if (badge != null && buttons[i].image != null) buttons[i].image.sprite = badge;

            // Switching the button off makes Unity grey it out AND stop it eating taps. Without
            // this, being short of coins just looks like a broken button.
            buttons[i].interactable = owned || (GameManager.Instance != null
                                                && GameManager.Instance.TotalCoins >= price);
        }

        if (walletText != null && GameManager.Instance != null)
        {
            walletText.text = GameManager.Instance.TotalCoins.ToString();
        }
    }
}
