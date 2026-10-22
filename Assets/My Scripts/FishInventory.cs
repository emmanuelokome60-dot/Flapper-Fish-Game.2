using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The fish shop behind the Characters button. Five blocks on a horizontally scrolling shelf:
// locked ones can be bought with coins, owned ones can be equipped, and exactly one is equipped
// at a time.
public class FishInventory : MonoBehaviour
{
    [Header("Shelf")]
    [Tooltip("The ScrollRect's Content. Its children are the fish blocks, in shelf order.")]
    [SerializeField] private Transform content;

    [Header("Card art")]
    [Tooltip("The grey card shown for a fish that has not been bought yet.")]
    [SerializeField] private Sprite lockedArt;

    [Tooltip("One card per fish, in the same order as the blocks. Index 0 is owned from the " +
             "start -- it is the fish the game ships with.")]
    [SerializeField] private Sprite[] fishArt;

    [Header("Button art")]
    [Tooltip("Shown on a locked block. Clicking it spends the price.")]
    [SerializeField] private Sprite buyArt;

    [Tooltip("Shown on an owned block that is not the equipped one.")]
    [SerializeField] private Sprite selectArt;

    [Tooltip("Shown on the one equipped block. Clicking it does nothing.")]
    [SerializeField] private Sprite equippedArt;

    [Header("Price")]
    [Tooltip("Coins to unlock one fish. The same for every block.")]
    [Min(0)]
    [SerializeField] private int price = 300;

    [Header("Wallet")]
    // The run counters are hidden on the menu, so without this a player cannot see their balance
    // on the one screen that charges them. Optional so the shelf still runs before it is wired.
    [Tooltip("Shows the coin wallet while the shop is open. Leave empty to show no balance.")]
    [SerializeField] private TMP_Text walletText;

    // Which fish the player is wearing, for this panel's own drawing.
    //
    // Do NOT read this to skin the in-game fish: the panel is inactive at scene load, so Awake has
    // not run and this reads 0 whatever the save says. Read PlayerPrefs "FishEquipped" instead.
    public int EquippedIndex { get; private set; }

    // Five booleans in one int. A bitmask rather than five keys because PlayerPrefs has no array
    // and five parallel keys is five chances to write one and not the others.
    private const string OWNED_KEY = "FishOwned";

    // Public because PlayerController reads it to skin the in-game fish. One const rather than
    // the same string literal typed into two files, which is how the two end up disagreeing.
    public const string EQUIPPED_KEY = "FishEquipped";

    private Image[] cardArt;
    private Button[] buttons;
    private TMP_Text[] priceText;
    private int ownedMask;

    // - Reads the shelf's blocks out of the hierarchy and wires each badge to its own index.
    // - Blocks are found rather than listed in the Inspector, so adding a fish needs no edit here.
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

            // Captured per block, or every button would act on the last value of i
            int index = i;
            button.onClick.AddListener(() => OnBlockClicked(index));
        }

        // Same coin art AND the same size as the run counter -- see BuildWalletCounter. Sizing
        // this box's icon from its own height instead gave the shop a 73.6px coin against the
        // run's 80px, which reads as the balance having changed meaning between screens.
        if (walletText != null && GameManager.Instance != null)
        {
            GameManager.Instance.BuildWalletCounter(walletText);

            // The box is pivoted top-right, so fitting it to its content is what puts the digits
            // ON the frame edge. A fixed 300-wide box with Left-aligned text -- and Left is
            // forced, because BuildCoinIcon pins the coin to the box's left edge -- left a gap
            // that grew as the balance got shorter.
            ContentSizeFitter fitter = walletText.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = walletText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    // Re-read on open rather than only in Awake. Coins earned during a run change what the player
    // can afford, and the panel is not rebuilt between visits -- and reading the save again here
    // means anything that changed it outside the panel is picked up by closing and reopening.
    private void OnEnable()
    {
        if (cardArt != null) Load();
    }

    // - Loads which fish are owned and which is worn, then redraws the shelf.
    // - Every block but the last is free by construction, so adding a block makes it the new
    //   one to buy without touching this.
    private void Load()
    {
        ownedMask = PlayerPrefs.GetInt(OWNED_KEY, 0);
        EquippedIndex = PlayerPrefs.GetInt(EQUIPPED_KEY, 0);

        // Every fish but the last ships with the game: the player can swap skins from the first
        // launch and saves coins for one aspirational fish. Derived from the block count rather
        // than a constant, so adding a block makes it the new one to buy without touching this.
        if (cardArt.Length > 1) ownedMask |= (1 << (cardArt.Length - 1)) - 1;

        if (!Owns(EquippedIndex)) EquippedIndex = 0;

        Refresh();
    }

    // - True when the player owns that fish, read out of the owned bitmask.
    // - Guards a negative index: C# masks a shift count to 5 bits, so 1 << -1 would become
    //   1 << 31 and report nonsense.
    private bool Owns(int index)
    {
        // Guards a negative saved index -- earlier builds wrote -1 for "nothing equipped", and C#
        // masks a shift count to 5 bits, so 1 << -1 would quietly become 1 << 31.
        return index >= 0 && (ownedMask & (1 << index)) != 0;
    }

    // - Buys the fish if it is locked and affordable, then equips it either way.
    // - Buying equips too, because nobody spends 300 coins and then wants a second button.
    private void OnBlockClicked(int index)
    {
        if (!Owns(index))
        {
            // Fails silently and leaves the wallet alone when the player is short. TrySpendCoins
            // checks and subtracts in one step, so there is no window where the fish is granted
            // and the coins are not taken.
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.TrySpendCoins(price)) return;

            ownedMask |= 1 << index;
            PlayerPrefs.SetInt(OWNED_KEY, ownedMask);

            // A bought fish that does not survive the app being killed is a stolen 300 coins
            PlayerPrefs.Save();
        }

        // Buying equips it too. Nobody spends 300 coins and then wants to press a second button.
        EquippedIndex = index;
        PlayerPrefs.SetInt(EQUIPPED_KEY, EquippedIndex);
        PlayerPrefs.Save();

        Refresh();
    }

    // The single place that decides what a block looks like. Every block is redrawn from
    // EquippedIndex each time, which is what makes "only one is equipped" true by construction
    // rather than by remembering to clear the last one.
    private void Refresh()
    {
        for (int i = 0; i < cardArt.Length; i++)
        {
            bool owned = Owns(i);

            if (cardArt[i] != null)
            {
                // fishArt may be unassigned: every other array here is reached through a
                // null check, and this one threw instead of drawing the locked card.
                bool hasArt = fishArt != null && i < fishArt.Length;
                Sprite art = owned && hasArt ? fishArt[i] : lockedArt;
                if (art != null) cardArt[i].sprite = art;
            }

            // Hidden on an owned card: that strip is where the fish's description sits.
            if (priceText[i] != null)
            {
                priceText[i].gameObject.SetActive(!owned);
                if (!owned) priceText[i].text = price.ToString();
            }

            if (buttons[i] == null) continue;

            Sprite badge = !owned ? buyArt : (i == EquippedIndex ? equippedArt : selectArt);
            if (badge != null && buttons[i].image != null) buttons[i].image.sprite = badge;

            // Unity's own ColorTint disabled state, so a button the player cannot afford greys
            // itself AND stops swallowing clicks. Without it, being broke looks like a dead button.
            buttons[i].interactable = owned || (GameManager.Instance != null
                                                && GameManager.Instance.TotalCoins >= price);
        }

        if (walletText != null && GameManager.Instance != null)
        {
            walletText.text = GameManager.Instance.TotalCoins.ToString();
        }
    }
}
