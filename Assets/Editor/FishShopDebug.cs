using UnityEditor;
using UnityEngine;

// Adds a Tools menu inside Unity for resetting the shop's saved data while testing.
//
// Two things hold copies of that data, and both have already cost a session of confusion:
//   1. The running game keeps the coins and the owned fish in memory. Change the save while it's
//      playing and the game ignores you, then saves over your change when it stops.
//   2. The Unity Editor itself keeps its own copy of the save. Editing the save from outside
//      Unity doesn't work, because the Editor hands the game its old copy instead.
//
// So: always stop Play first, and always change things through this menu.
public static class FishShopDebug
{
    private const string OWNED_KEY = "FishOwned";
    private const string EQUIPPED_KEY = "FishEquipped";
    private const string COINS_KEY = "Coins";

    // - Forgets that you agreed to the terms, so the terms screen shows up again next Play.
    [MenuItem("Tools/Fish Shop/Reset Legal Acceptance (shows the terms gate again)")]
    private static void ResetLegal()
    {
        if (PlayStopped() == false) return;
        LegalNotice.ClearAcceptance();
        Report("legal acceptance cleared -- the terms gate shows on the next Play");
    }

    // - Forgets every fish you own and which one you were wearing, like a brand new install.
    // - We write plain 0, not a list of the free fish. Which fish come free is FishInventory's
    //   decision (every slot except the last), and copying that rule into here is exactly how
    //   the two would end up disagreeing later. 0 means "owns nothing beyond the free ones",
    //   which is what a fresh save looks like anyway.
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

    // - Sets your coins to an exact number, for testing the shop with a known amount.
    // - Goes through Unity's own save system rather than editing Windows' registry directly, so
    //   the Editor's copy and the real saved value can't end up disagreeing.
    private static void SetCoins(int amount)
    {
        if (PlayStopped() == false) return;
        PlayerPrefs.SetInt(COINS_KEY, amount);
        PlayerPrefs.Save();
        Report("coins set to " + amount);
    }

    // - Refuses to run any of these while the game is actually playing, and explains why.
    // - Changing the save under a running game gets silently thrown away, so it's better to say
    //   so than to pretend it worked.
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

    // - Prints the saved data to Unity's Console.
    // - This is the only honest way to see what the game was ACTUALLY handed, rather than what's
    //   sitting in the registry.
    private static void Report(string what)
    {
        Debug.Log("[FishShopDebug] " + what + " -- owned=" + PlayerPrefs.GetInt(OWNED_KEY, 0)
                  + " equipped=" + PlayerPrefs.GetInt(EQUIPPED_KEY, 0)
                  + " coins=" + PlayerPrefs.GetInt(COINS_KEY, 0));
    }
}
