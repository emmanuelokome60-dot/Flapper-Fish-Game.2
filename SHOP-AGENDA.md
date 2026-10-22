# Fish shop — what's left

Living doc for the Characters screen. Written 2026-10-12, updated 2026-10-12. `DEVLOG.md` holds
the history and why; this holds what is still open.

---

## Where it stands

The shelf works and the buying path has now run. Opening Characters shows five blocks over the
pause blur, scrolling horizontally with three visible.

Verified with `python Tools/verify_scene.py "Assets/Scenes/SampleScene.unity"` (Editor closed) and
`dotnet build Assembly-CSharp.csproj`:

| Piece | State |
|---|---|
| `CharactersMenu` panel + `ScrollRect` shelf | built, in the scene |
| `FishInventory` on the panel | wired: Content, 5 fish cards, locked card, 3 button sprites, price 300, wallet text |
| `CharactersButton` / `CloseButton` | working |
| `GameManager.TrySpendCoins(int)` | **exercised** — one purchase deducted exactly 300 |
| Ownership + equipped state in PlayerPrefs | **exercised** — survives a relaunch |
| Short-wallet path (`TrySpendCoins` returns false) | **still never run** — see §8 |

---

## Design decision: five free fish, one to buy

**`PEIXE1`-`PEIXE5` are free and owned from the first launch. Only the sixth block costs coins
(300).** Reversed on 2026-10-13 from the original "one free, four locked".

The reason is pacing. One free fish meant a new player had nothing to play with and had to grind
300 before the shelf did anything at all. Five free skins give them something to enjoy immediately,
and the wallet exists to chase one aspirational fish rather than to unlock the feature.

`Load()` derives this from the block count rather than a constant:

```csharp
if (cardArt.Length > 1) ownedMask |= (1 << (cardArt.Length - 1)) - 1;
```

Add a block and it automatically becomes the new one to buy; nothing to edit. `Tools/add_block.py`
clones the last block for exactly that purpose. The `> 1` guard is not paranoia: at length 0 the
shift count goes negative and C# masks it to 5 bits, so `1 << -1` would become `1 << 31`.

> **Open gap: there is no `PEIXE6.png`.** `Refresh()` falls back to `lockedArt` when `fishArt` has
> no entry for an index, so the sixth card keeps showing the silhouette **even after it is bought**
> -- the purchase registers and the badge changes, but the picture does not. Drop a `PEIXE6.png`
> in, add it as `fishArt[5]`, and the card completes itself with no code change.

---

## Closed on 2026-10-12

**§1 — button art was swapped.** `buyArt` and `equippedArt` pointed at each other's sprites, so a
locked card read EQUIPPED and the owned one read BUY. Fixed in the scene, and the files have since
been renamed `Buy.png` / `Equipped.png` / `Select Button.png`. `verify_scene.py` now pins all three
by GUID, so the swap cannot come back unnoticed.

**§2 — the price lived in two places.** Code owns the number now. The painted coin icon and the
painted `300` were removed from `PEIXE BLOQUEADO.png`; the dark bar, its rounded ends, the padlock
and "Unlock with:" were left intact. A `PriceText` label inside each block is written by `Refresh()`
from the serialized `price`, at the measured centre of the empty bar — pos (-3, -99), font 50.
Hidden on an owned card, because that strip is where the fish's description sits.

**§3 — buying failed silently.** The buttons were already ColorTint with a disabled colour set, so
`buttons[i].interactable` greys an unaffordable BUY badge *and* stops the click that would do
nothing. No new art, no colour maths.

**§4 — the wallet.** `WalletText` sits at the screen's top-left (60, -60) and `Refresh()` writes
`GameManager.TotalCoins` into it. Separately, `ShowCoins()` was writing `Coins` — **this run's
haul, which resets to 0 every run** — instead of `TotalCoins`. That is why the balance never looked
cumulative. The in-run HUD counter now shows the wallet.

---

## 5. Is 300 reachable?

A 42 m run in the 10-11 session paid **3 coins**. If that is typical, the first skin is somewhere
near fifty runs. Nobody has counted properly, and the HUD now shows the wallet rather than the run,
so counting means noting the delta across a run.

| Run | Distance | Coins earned |
|---|---|---|
| | | |

Then decide whether 300 is right, or whether `CoinSpawner`'s rate should come up.

---

## 6. Locked cards all show the same fish -- CLOSED 2026-10-13

No longer a problem. This worried that four identical silhouettes stop a player seeing what they
are buying. Since the shelf flipped to five free fish and one for sale there is exactly **one**
locked card, so the silhouette reads as a mystery box rather than as four copies of nothing.

Per-fish locked art is therefore not needed. If the shelf ever grows to several paid fish at once,
reopen this: the fix is `Sprite[] lockedArt` indexed like `fishArt`, two lines in `Refresh()`.

---

## 7. Equipping does not change the fish in the game

Known and accepted. The cards are 353×410 UI art with frames baked in; there are no per-character
**in-game** fish sprites, so equipping records the choice and moves the badge and nothing else.

**Do not read `FishInventory.EquippedIndex` to do this.** `CharactersMenu` is inactive at scene
load, so `FishInventory.Awake` has not run and the property reads `0` whatever the save says. Read
`PlayerPrefs.GetInt("FishEquipped", 0)` instead — that is the source of truth, and it does not
depend on the shop ever having been opened.

What the swap actually needs, verified 2026-10-13:

| Piece | Where it is |
|---|---|
| In-game fish sprite | `Assets/Graphics/My Assets/Frame 1 (1).png`, 358x268 RGBA, bare fish on transparency — the Abyssal Chomper |
| The renderer to assign | `Player_Fish/Animator` — a child GameObject named "Animator" that holds the `SpriteRenderer` |
| Physics | `Rigidbody2D` + `CircleCollider2D` + `PlayerController` sit on `Player_Fish` itself, not the child |

So the art needed is **four more bare-fish PNGs matching `Frame 1 (1).png`**, not the shop cards —
those are 353x410 with frame, background and description baked in.

Three things to keep in mind:

- Swapping the sprite touches no physics and no script. `PlayerController` drives the Rigidbody2D
  on the parent, so the fish moves identically whatever skin is showing.
- The `CircleCollider2D` does **not** follow the sprite. Keep every skin the same size as
  `Frame 1 (1).png` or the hitbox will stop matching what the player sees.
- Nothing overwrites the sprite each frame only because the `Animator` component on that child
  points at a controller GUID that no longer exists. Wire a real controller and it will stamp over
  the skin every frame. The dead component is worth deleting before anyone does.

---

## 8. The short-wallet path has never run

`TrySpendCoins` returning false has to leave the wallet **exactly** as it was. That branch has
still never executed, because testing has always happened with enough coins to buy.

To run it: set `Coins` to `100`, open the shop, and confirm every BUY badge is greyed and
unclickable and the balance does not move. Then put `Coins` back.

---

## What's left, in order

1. Run the short-wallet case (§8) — the only untested branch in the money path.
2. Fill in the coin-rate table (§5) and settle whether 300 is the right price.
3. Locked card art (§6), when the art exists.
4. Per-character in-game sprites (§7), whenever.

---

## Resetting the shop's save state

**Use the Unity menu: `Tools -> Fish Shop`.** It has *Reset Skins (keeps the free fish and
coins)*, *Set Coins to 0 / 100 / 854* and *Show Save State*. Then close and reopen the Characters panel --
`OnEnable` re-reads the save, so the shelf picks the change up without restarting Play.

**Do not edit the registry from outside Unity. It does not work.** The Editor reads PlayerPrefs
into memory once per session and serves that copy for the whole session, so an external write is
invisible to the running game and is overwritten when Unity quits. On 2026-10-12 this burned most
of a session: the registry read `FishOwned=0, Coins=854` while the game was being handed
`FishOwned=7, Coins=554`, and every "reset" appeared to do nothing. `Tools -> Fish Shop` goes
through `PlayerPrefs` itself, so it changes the very copy the game reads and the two cannot
disagree.

For reference only, the Editor's PlayerPrefs live at
`HKCU:\Software\Unity\UnityEditor\Mobile Game Project\Abyssal Fish`, with every key name suffixed
`_h<hash>`:

| Value | Means |
|---|---|
| `FishOwned_h4005965414` | bitmask, one bit per fish. `0` = nothing owned, `2` = fish index 1 only |
| `FishEquipped_h3826909464` | index of the equipped fish, `-1` for none |
| `Coins_h212384221` | the lifetime wallet |
| `BestDistance_h3051513222` | best run |
| `Muted_h210923752` | sound toggle |

A **built** APK or standalone uses `HKCU\Software\<company>\<product>` (no `Unity\UnityEditor`) and
does not exist for this project yet -- a build has its own wallet, separate from the Editor's.

Do not use *Edit -> Clear All PlayerPrefs*: it takes the wallet and the best distance with it.

---

## How to check the money path

There is no automated test — no test assembly in this project, and a mirrored test would verify a
copy of the arithmetic rather than the code that ships. The structural half is covered by
`Tools/verify_scene.py`; the behaviour is two cases by hand.

- **With 300+** — done, works: balance drops by exactly 300, the card turns into its fish, the
  badge becomes EQUIPPED, and it survives a relaunch.
- **With fewer than 300** — see §8, still outstanding.

Then click SELECT on two different owned blocks in turn: the badge moves, and only ever one block
reads EQUIPPED.
