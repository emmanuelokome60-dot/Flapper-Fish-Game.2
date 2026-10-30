using UnityEditor;
using UnityEngine;

// A Tools menu for resetting the shop's saved data while testing.
//
// Two things keep their own copy of that save, and both have cost a
// session of confusion:
//   1. The running game holds the coins and owned fish in memory, and
//      saves over your change when it stops.
//   2. The Editor holds its own copy, so editing the save from outside
//      Unity does nothing: the Editor hands the game its old copy.
// So: always stop Play first, and always change things through here.
public static class FishShopDebug
{
    // Both fish keys come from FishInventory rather than being typed
    // again here. They were typed twice once already, and a reset that
    // clears a key nothing reads looks exactly like a reset that worked.
    private const string OWNED_KEY = FishInventory.OWNED_KEY;
    private const string EQUIPPED_KEY = FishInventory.EQUIPPED_KEY;
    private const string COINS_KEY = "Coins";

    // Forgets that you agreed to the terms, so the gate shows again.
    [MenuItem("Tools/Fish Shop/Reset Legal Acceptance (shows the terms gate again)")]
    private static void ResetLegal()
    {
        if (PlayStopped() == false) return;
        LegalNotice.ClearAcceptance();
        Report("legal acceptance cleared -- the terms gate shows on the next Play");
    }

    // Forgets every fish you own and which you wore, like a new install.
    // Writes plain 0, not a list of the free fish. Which fish come free
    // is FishInventory's decision, and copying that rule in here is how
    // the two would end up disagreeing.
    [MenuItem("Tools/Fish Shop/Reset Skins (locks the paid fish)")]
    private static void ResetSkins()
    {
        if (PlayStopped() == false) return;
        PlayerPrefs.SetInt(OWNED_KEY, 0);
        PlayerPrefs.SetInt(EQUIPPED_KEY, 0);
        PlayerPrefs.Save();
        Report("reset -- the paid fish is locked again");
    }

    [MenuItem("Tools/Fish Shop/Set Coins to 0")]
    private static void Coins0() { SetCoins(0); }

    [MenuItem("Tools/Fish Shop/Set Coins to 100 (test the broke path)")]
    private static void Coins100() { SetCoins(100); }

    [MenuItem("Tools/Fish Shop/Set Coins to 854")]
    private static void Coins854() { SetCoins(854); }

    [MenuItem("Tools/Fish Shop/Show Save State")]
    private static void Show() { Report("current state"); }

    // Sets your coins to an exact number, for testing the shop.
    // Goes through Unity's save system, not the Windows registry, so the
    // Editor's copy and the real value cannot disagree.
    private static void SetCoins(int amount)
    {
        if (PlayStopped() == false) return;
        PlayerPrefs.SetInt(COINS_KEY, amount);
        PlayerPrefs.Save();
        Report("coins set to " + amount);
    }

    // Refuses to run any of these while the game is playing, and says why.
    private static bool PlayStopped()
    {
        if (!EditorApplication.isPlaying) return true;
        EditorUtility.DisplayDialog(
            "Stop Play first",
            "The game holds the wallet and the owned skins in memory while it runs, so a change "
            + "made now would be ignored and then overwritten.\n\nStop Play, pick this again, "
            + "then press Play.",
            "OK");
        return false;
    }

    // Prints the saved data to Unity's Console.
    // The only honest way to see what the game was ACTUALLY handed,
    // rather than what sits in the registry.
    private static void Report(string what)
    {
        Debug.Log("[FishShopDebug] " + what + " -- owned=" + PlayerPrefs.GetInt(OWNED_KEY, 0)
                  + " equipped=" + PlayerPrefs.GetInt(EQUIPPED_KEY, 0)
                  + " coins=" + PlayerPrefs.GetInt(COINS_KEY, 0));
    }
}
