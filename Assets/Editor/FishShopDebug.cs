using UnityEditor;
using UnityEngine;

// Reset the shop's save state from inside Unity.
//
// Two caches make this fiddly, and both have already cost a session:
//   1. The Editor reads PlayerPrefs into memory once per session, so editing the registry from
//      outside is invisible to the running game and is overwritten when Unity quits. Going through
//      PlayerPrefs here changes the very copy the game reads.
//   2. GameManager reads the wallet into a C# field in Awake. Writing PlayerPrefs while Play is
//      running does not touch that field, and the next coin or purchase writes the stale value
//      straight back over it.
//
// So: these refuse to run during Play. Stop Play, use them, press Play.
//
// Editor-only, so none of this ships in a build.
public static class FishShopDebug
{
    private const string OWNED_KEY = "FishOwned";
    private const string EQUIPPED_KEY = "FishEquipped";
    private const string COINS_KEY = "Coins";

    // 0, not a mask: which fish ship free is FishInventory.Load's decision (every block but the
    // last), and duplicating that rule here is how the two would drift apart. 0 means "owns
    // nothing beyond what ships free", which is exactly what a fresh save looks like.
    [MenuItem("Tools/Fish Shop/Reset Skins (locks the paid fish)")]
    // - Clears every owned fish and the equipped choice, putting the shop back to a fresh install.
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

    // - Writes the wallet to an exact figure, for testing the shop at a known balance.
    // - Goes through PlayerPrefs rather than the registry, so the Editor's cached copy and the
    //   saved value cannot disagree.
    private static void SetCoins(int amount)
    {
        if (PlayStopped() == false) return;
        PlayerPrefs.SetInt(COINS_KEY, amount);
        PlayerPrefs.Save();
        Report("coins set to " + amount);
    }

    // Changing the save under a running game is silently undone, so say so rather than pretend.
    // - Refuses to run any of these while the game is playing.
    // - GameManager caches the wallet in a field at Awake, so a mid-play write is ignored and
    //   then overwritten when Play stops.
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

    // - Prints the save state to the Console, which is the only way to read what the running
    //   game was actually handed rather than what the registry holds.
    private static void Report(string what)
    {
        Debug.Log("[FishShopDebug] " + what + " -- owned=" + PlayerPrefs.GetInt(OWNED_KEY, 0)
                  + " equipped=" + PlayerPrefs.GetInt(EQUIPPED_KEY, 0)
                  + " coins=" + PlayerPrefs.GetInt(COINS_KEY, 0));
    }
}
