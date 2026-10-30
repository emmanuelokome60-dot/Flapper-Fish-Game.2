using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The fish shop you reach from the Characters button.
// A shelf you swipe sideways. Fish you have not bought are grey and cost
// coins. Fish you own can be worn, one at a time.
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
    // The run counters are hidden on the menu, so without this you could
    // not see your coins on the one screen that spends them.
    [Tooltip("Shows your coin total while the shop is open. Leave empty to show no total.")]
    [SerializeField] private TMP_Text walletText;

    // Which fish you are wearing, used by this panel for its own drawing.
    // Do NOT read this for the in-game fish: the panel is off at load, so
    // its setup has not run and this reads 0. Read the save instead.
    public int EquippedIndex { get; private set; }

    // One number remembers every fish you own.
    // Picture a row of light switches, one switch per fish. On means you
    // own it. Unity cannot save a list, and separate values would be
    // separate chances to update one and forget the others.
    //
    // Public for the same reason as EQUIPPED_KEY. The Tools menu resets
    // this, and a second copy of the name is how a reset clears nothing.
    //
    // The "2" retires saves written when the shelf had six cards. Slot 4
    // came free then, and slot 4 is now the fish you pay for, so trusting
    // them would hand it over for nothing. Coins are a separate key.
    public const string OWNED_KEY = "FishOwned2";

    // Public because PlayerController reads it to pick the in-game fish.
    // Written once here, so two files cannot disagree about the name.
    public const string EQUIPPED_KEY = "FishEquipped";

    private Image[] cardArt;
    private Button[] buttons;
    private TMP_Text[] priceText;
    private int ownedMask;

    // Finds the shelf slots and wires each button to its own slot number.
    // Found in code, not listed in the Inspector, so adding a fish needs
    // no change in here at all.
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

            // i is copied into its own variable. Without that, every
            // button would share one counter and act on the LAST slot.
            int index = i;
            button.onClick.AddListener(() => OnBlockClicked(index));
        }

        // The shop's coin total uses the same picture AND size as the
        // in-game one, or the number looks like it means something
        // different on each screen.
        if (walletText != null && GameManager.Instance != null)
        {
            GameManager.Instance.BuildWalletCounter(walletText);

            // The text box is centred on the frame and shrinks to fit
            // its own text, so the digits stay centred as the number
            // grows instead of drifting off centre.
            ContentSizeFitter fitter = walletText.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = walletText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    // Re-reads your save every time the shop opens, not just once.
    // Coins earned during a run change what you can afford.
    private void OnEnable()
    {
        if (cardArt != null) Load();
    }

    // Loads which fish you own and which you wear, then redraws the shelf.
    private void Load()
    {
        ownedMask = PlayerPrefs.GetInt(OWNED_KEY, 0);
        EquippedIndex = PlayerPrefs.GetInt(EQUIPPED_KEY, 0);

        // Every fish except the LAST comes free, so you can swap from the
        // first launch and save up for one special fish.
        // Worked out from the slot count, so adding a slot makes it the
        // new one to save for without touching this line.
        if (cardArt.Length > 1) ownedMask |= (1 << (cardArt.Length - 1)) - 1;

        if (!Owns(EquippedIndex)) EquippedIndex = 0;

        Refresh();
    }

    // True if you own that fish. Reads one digit out of the packed number.
    // The negative check matters: old saves used -1 for "nothing worn",
    // and the maths would turn -1 into 31.
    private bool Owns(int index)
    {
        return index >= 0 && (ownedMask & (1 << index)) != 0;
    }

    // You tapped a card. Buys the fish if you can afford it, then wears it.
    private void OnBlockClicked(int index)
    {
        if (!Owns(index))
        {
            // TrySpendCoins checks and subtracts in one go, so there is
            // no gap where you could get the fish without paying.
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.TrySpendCoins(price)) return;

            ownedMask |= 1 << index;
            PlayerPrefs.SetInt(OWNED_KEY, ownedMask);

            // Saved at once. A fish that vanishes when the app closes is
            // 300 stolen coins.
            PlayerPrefs.Save();
        }

        // Buying also wears it. Nobody spends 300 coins then wants a
        // second button to press.
        EquippedIndex = index;
        PlayerPrefs.SetInt(EQUIPPED_KEY, EquippedIndex);
        PlayerPrefs.Save();

        Refresh();
    }

    // Redraws every card: picture, price and button badge.
    // All of them, every time, which is what makes "only one fish is
    // worn" true without anyone remembering to un-wear the old one.
    private void Refresh()
    {
        for (int i = 0; i < cardArt.Length; i++)
        {
            bool owned = Owns(i);

            if (cardArt[i] != null)
            {
                // The pictures might not be filled in yet. Everything
                // else here is checked, and this one crashed instead of
                // drawing the grey locked card.
                bool hasArt = fishArt != null && i < fishArt.Length;
                Sprite art = owned && hasArt ? fishArt[i] : lockedArt;
                if (art != null) cardArt[i].sprite = art;
            }

            // The price hides once you own it, because that strip of the
            // card is where the fish's description goes.
            if (priceText[i] != null)
            {
                priceText[i].gameObject.SetActive(!owned);
                if (!owned) priceText[i].text = price.ToString();
            }

            if (buttons[i] == null) continue;

            Sprite badge = !owned ? buyArt : (i == EquippedIndex ? equippedArt : selectArt);
            if (badge != null && buttons[i].image != null) buttons[i].image.sprite = badge;

            // Switching the button off greys it AND stops it eating taps.
            // Without this, being broke just looks like a broken button.
            buttons[i].interactable = owned || (GameManager.Instance != null
                                                && GameManager.Instance.TotalCoins >= price);
        }

        if (walletText != null && GameManager.Instance != null)
        {
            walletText.text = GameManager.Instance.TotalCoins.ToString();
        }
    }
}
