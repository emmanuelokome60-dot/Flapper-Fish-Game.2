# Abyssal Fish — the code, explained

All 18 scripts. Every setting and every method named, in plain English.

Read it with the code open beside you. Each method shows its file and line,
like `PlayerController.cs:146` — type the name into Unity's search box.

---

## Read this first: the numbers come from the scene, not the scripts

A Unity script holds a **starting** number. The scene file holds the **real** one.
When the game runs, **the scene wins**.

So `PlayerController.cs` says the fish swims with a force of `4`. The game
actually uses **4.6**. Both are true. Only one is what the player feels.

**Every number in this guide was read from the scene**, so these are the numbers
the game really uses. Where the script differs it says so: *Swim Force — 4.6
(script says 4)*.

If your teacher opens a script and reads out a different number:

> *"The script holds the starting value. The scene holds the real one, and the
> scene wins. We tuned it in the Unity window, so the number lives in the scene."*

**30 settings disagree.** All listed in Part 3.

---

# Part 0 — The big picture

Four ideas carry the whole game.

**1. One depth number runs the feeling.** `GameManager` works out `CaveDepth`:
`0` at the cave mouth, `1` at the deepest. Three things read it — the cave light
dims, the rock gap narrows, the fish's lamp brightens. So the player watches
**one** thing getting worse, not three systems. Best decision in the project, and
it leaves only one dial to balance.

**2. One shared speed.** Rocks, rubbish and coins all read
`ObstacleMovement.SpeedMultiplier`. Only `SetPace()` may write it. If each owned
its own speed they would drift out of step and nothing on screen would line up.

**3. The gap is a rule, not luck.** Before placing a rock, `CaveSpawner` measures
the opposite wall **at that exact spot**, so the two rows can never meet. A hard
floor of `2.2` is locked in code. The shapes are random; the safe gap is not.

**4. Everything except coins kills you.** Rocks and rubbish use a **collider**
(bump). Coins use a **trigger** (touch). That one difference is why collecting a
coin does not kill you — and it reads so instantly the game needed no tutorial.

**A run, start to finish:** `Start()` calls `BackToMenu()`, freezing everything →
`LegalNotice` shows the terms if unaccepted → Play calls `StartGame()` → the three
spawners start → your tap hits `PlayerController.Update()` → `GameManager.Update()`
counts metres and dims the light → you bump something → `PlayerHit.EndRun()` calls
`GameOver()` → everything freezes, the score saves.

---

# Part 1 — The four scripts you will be asked about

---

## 1. GameManager.cs — the boss of the game

**760 lines, the biggest script.** Runs the menu, starts and ends runs, keeps the
score and coins, plays sounds, darkens the cave. Anything that happens to the
*whole game* happens here. Only one ever exists; any script reaches it with
`GameManager.Instance`. That pattern is called a **singleton**.

> **"Why is one script 760 lines?"** Honest answer: some could be split. But it
> all shares state — score, coins, whether the game is frozen. Splitting it means
> two scripts holding copies of one number, and every bug in this project where
> two things disagreed came from exactly that.

### The settings that matter

| Setting | Value | What it does in the game |
|---|---|---|
| **Start Light** | **0.03** (script 0.35) | Brightness at the cave mouth. |
| **Deep Light** | **0.02** (script 0.08) | Brightness at the very deepest. |
| **Darkest At Distance** | **300** (script 400) | Metres before the cave is as dark as it gets. |
| **Start Fraction From Left** | **0.4** (script 0.5) | Where the fish starts, as a share of screen width. |
| **Pause Dim Darkness** | **0.7** (script 0.78) | How dark it goes behind the pause buttons. |
| Pause Blur Steps | 4 | How blurry the frozen game goes. Each step halves sharpness. |
| Units Per Second | 5 | How fast the metre counter climbs. Matches the rocks' speed. |
| **Movement Pop Volume** | **0.05** (script 0.35) | The swim pop. Quiet because it fires on *every* tap. |
| **Bg Music Volume** | **0.5** (script 0.3) | Music under the effects. |
| Coin Icon Size / Inset / Gap | 80 / 0 / 8 | The coin picture beside the counter. |

> **"Why so dark — 0.03 to 0.02 is almost nothing?"** Contrast sells darkness,
> not dimness. A uniformly dim screen reads as muddy, the eye adapts in seconds,
> and on a phone outdoors it disappears. The cave goes very dark **and** the lamp
> goes bright. The **gap between them** is the effect.

`Start Fraction From Left` is a **fraction**, not a distance — four-tenths across
is four-tenths across on every phone. A fixed distance sits off-centre on a tall one.

**Wired slots** (things dragged in, no numbers to quote): Game Over Text, Game Over
Menu, Game Over Score Text, Game Over Best Text, Game Over Coin Text, Main Menu,
Characters Menu, Restart Button, Home Button, Pause Button, Resume Button, Menu
Sound Button, Pause Sound Button, Sound On Sprite, Sound Off Sprite, Coin Text,
Score Text Counter, Player, Obstacle Spawner, Cave Light, Button Click, Coin Clip,
Movement Pop, Game Over Clip, Bg Music.

Two sound buttons, **one** saved setting behind them — `ApplySound()` swaps the
picture on **both**, even the hidden one, so the menus can never disagree.

### What it works out for itself

| Name | Holds |
|---|---|
| `Instance` | The one GameManager. How every script finds it. |
| `CaveDepth` | Depth, `0`–`1`. The light, rock gap and lamp all read it. |
| `Distance` / `distanceTravelled` | Metres this run, whole / with decimals. |
| `Coins` / `TotalCoins` | **This run only** / your saved total. The shop spends the total. |
| `HighScore` | Furthest ever. Saved on the phone. |
| `Muted` / `OnMenu` / `isPaused` / `isGameOver` | The four state flags. |
| `sfx` / `music` | **Two** sound players — one cannot fire a short sound *and* loop a track. |
| `COINS_KEY` `BEST_KEY` `MUTE_KEY` | What your save is filed under. |

### The methods that matter

**`Awake()` — :197** Loads your save, builds the sound, draws the coin icon. Also
does this, which is a real bug fix:

```csharp
foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
    if (counter != null) counter.raycastTarget = false;
```

**In the game: this is why tapping the top of the screen makes the fish swim.**
Unity UI soaks up taps by default, and the score counter is **1018×141** pixels
across the top — it silently ate every tap landing on it. A counter is not a
button, so it is told to stop catching taps.

**`Update()` — :303** Counts metres, dims the cave.

```csharp
distanceTravelled += unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime;
int whole = (int)distanceTravelled;
if (whole == Distance) return;
Distance = whole; ShowDistance(); ApplyCaveDepth();
```

Multiplying by `SpeedMultiplier` makes the counter speed up with the cave, so the
number matches what you see going past. The `return` means the text is only
rewritten when the whole number changes, not 60 times a second. And there is **no
pause check** — `Time.deltaTime` stops counting while frozen, so the menu, pause
and game over screens all stop the counter for free.

**`ApplyCaveDepth()` — :666** Five lines, and the darkness lives here.

```csharp
CaveDepth = Mathf.Clamp01(Distance / darkestAtDistance);
if (caveLight == null) return;
caveLight.intensity = Mathf.Lerp(startLight, deepLight, CaveDepth);
```

`Distance / darkestAtDistance` is how far through you are (150m of 300 = `0.5`).
`Clamp01` stops past-300m getting darker. `Lerp` mixes the two brightnesses.
**`CaveDepth` is set *before* the null check** — so with no cave light wired, the
fish's lamp still tracks depth. Check-first would have broken the lamp too.

**`GameOver()` — :318** Freezes everything, saves a new best, shows the panel.
Opens with `if (isGameOver) return;` — a **guard**, because the fish can hit two
things in one frame. The death sound sits *after* the guard on purpose, so it
plays once. `PlayerPrefs.Save()` writes immediately: a phone can be closed without
shutting down properly.

**`TrySpendCoins(int amount)` — :607** How you buy a fish. Checking and
subtracting are **one step** — as two methods, a caller that subtracted but forgot
to check would hand out a fish free. Returns `true`/`false` so the shop knows.

**`BuildSound()` — :482** Why every button clicks without wiring 20 buttons:

```csharp
foreach (Button b in CanvasRoot().GetComponentsInChildren<Button>(true))
    if (b.GetComponent<ButtonPressSound>() == null) b.gameObject.AddComponent<ButtonPressSound>();
```

The `true` means "include switched-off ones" — the shop shelf is off at load, so
without it the shop buttons would be silent.

**`StartGame()` — :355** Unfreezes, resets the fish and counters, starts spawners.
Plays music **only if not already playing** — `Play()` on running music snaps it to
the start, so a retry would never hear past the intro.

### Every other method

| Method | Line | In the game |
|---|---|---|
| `Start()` | 238 | Opens on the menu, frozen, terms on top. |
| `BuildLegalButton()` | 257 | The Terms & Privacy button, bottom right. |
| `TogglePause()` | 388 | `Time.timeScale = 0f` *is* the whole pause. |
| `BackToMenu()` | 401 | Back to menu, frozen. **The only place music stops.** |
| `PlayerStart()` | 436 | The fish's starting spot, as a share of screen width. |
| `ShowRunButtons()` | 444 | Shows/hides the pause screen's buttons. |
| `ShowPauseDim()` | 467 | The dark blurry backdrop on or off. |
| `PreloadClips()` | 519 | Loads sounds up front, so the first play has no stutter. |
| `PlayClick()` / `PlayMovementPop()` | 529 / 535 | The click; the swim pop. |
| `ToggleSound()` / `ApplySound()` | 542 / 554 | Mute, save it, swap **both** buttons' pictures. |
| `CanvasRoot()` | 568 | Finds the Canvas by walking **up** the tree. |
| `OnDestroy()` | 575 | Hands the blur's memory back. Not freed on its own. |
| `ClearObstacles()` | 581 | Deletes every rock, coin and bit of rubbish left. |
| `AddCoin()` | 590 | +1 this run, +1 total, save, ping. Sound is **here**, not in `CoinPickup`, so a coin touched as you die stays silent. |
| `ShowCharacters()` | 623 | Opens/closes the shop. Calls `ApplySkin()`, because closing the shop misses both `StartGame` and `BackToMenu`. |
| `ResetDistance()` / `ResetCoins()` | 645 / 688 | Back to zero. Coins = this run only. |
| `ShowDistance()` / `ShowCoins()` | 657 / 696 | **The one and only place** each is written. |
| `ShowGameOver()` | 678 | Fills in the three end-screen numbers. |
| `BuildCoinIcon()` | 708 | Coin picture left of a number. Also `raycastTarget = false`. |
| `BuildWalletCounter()` | 742 | Shop's coin total, same size and picture as in-game. |
| `ShowRunCounters()` | 752 | Shows/hides both counters **together**. |

> **Why "the one and only place" keeps appearing:** two methods writing one piece
> of text is what made the distance counter flicker, and one counter saved ON
> while the other was saved OFF is what made them disagree.

---

## 2. CaveSpawner.cs — walls that can never close up

**323 lines.** Two endless rows of rocks, ceiling and floor, always leaving an
opening the fish fits through.

| Setting | Value | What it does in the game |
|---|---|---|
| **Min Gap** | **3.6** (script 3) | The opening at the cave mouth. |
| Deep Gap | 2.6 | The opening at the darkest. **Must** be smaller than Min Gap. |
| **Min Depth** | **2.5** (script 2) | Every rock sticks out this far, so none is a useless sliver. |
| **Max Depth Change** | **1** (script 1.2) | How different a rock may be from the one before. Lower = smoother walls. |
| **Floor Y** | **-5** (script -4) | Top of the Ground. Floor rocks stand here. |
| **Min Scale** | **0.55** (script 0.35) | Smallest stretch allowed. |
| Max Scale | 1.2 | Largest stretch allowed. |
| **Max Width** | **10** (script 8) | Widest one rock may be. Screen is ~17.8 wide. |
| Overlap | 0.2 | How far each rock tucks under its neighbour, hiding the join. |

Scale: the fish is **1.33** tall; the part that can hit things is **1.11**. So 3.6
is about three fish, 2.6 about two.

**Locked in code** (`const`, so Unity cannot change them): `ABSOLUTE_MIN_GAP` 2.2,
`ROW_START_MARGIN` 0.5, `FORGET_MARGIN` 5.

> **"Why lock 2.2 away?"** It is a safety rule, not a taste setting — twice the
> fish's 1.11. Below it, getting through is luck, not skill, and on a phone your
> own thumb covers the fish. In code, no amount of fiddling can make it unfair.
> If Deep Gap is set too low the game **warns and clamps**: a silent rescue is how
> you ship a cave nobody fits through that looks deliberate.

**Two helper classes.** `Rock` remembers one placed rock (`rockTransform`,
`halfWidth`, `depth`, and `Left`/`Right` worked out from them). `Row` is one row
(`isTop`, `rocks`, `last`, `lastIndex`, `startX`, `RightEdge`). A row only needs
its **last** rock to place the next, which is why this is cheap every frame.

### The methods

**`CurrentGap()` — :168** Three lines, and one of the best decisions here.

```csharp
float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
return Mathf.Max(Mathf.Lerp(minGap, deepGap, depth), ABSOLUTE_MIN_GAP);
```

It reads the **same** `CaveDepth` that dims the light — so walls narrowing and
screen darkening happen off one number. One signal for the player, not two. The
`Max` is the safety net: however it is tuned, the gap can never come out under 2.2.

**`SpawnRock(row, otherRow)` — :178** The cleverest method in the project. The
**order** is what makes it safe:

```csharp
float gap = CurrentGap();
float budget = CameraEdges.Top - floorY - gap;
float taken = DeepestBetween(otherRow, startX, startX + spriteSize.x * maxScale);
float allowed = Mathf.Min(budget - taken, budget - minDepth);
```

As a sum: **budget** = screen height minus the opening the fish needs, i.e. all
the space the two rows may share. **taken** = how much the *opposite* row already
used right here. **allowed** = what is genuinely left. The opposite wall is
measured **first**, before anything is placed.

```csharp
float depthChange = maxDepthChange * (gap / minGap);
```

Subtle and worth knowing: **the allowed jump shrinks as the gap shrinks.** A
1-unit step is a third of a 3.6 gap but nearly half of a 2.6 one, and the fish
climbs no faster. Without it the deep cave zig-zags too fast to be fair.

Rocks flip with a **negative size**, not Unity's Flip Y tickbox — the tickbox
flips only the picture, leaving invisible walls in the wrong place.

**`TryGetGapAt(x, out low, out high)` — :260** Says where the clear opening is at
a spot, so rubbish and coins land in the cave and not inside a rock. Returns
`false` until both rows are built that far, and the spawners just skip that turn —
a small empty stretch beats a coin buried in a wall. **Public**, because
TrashSpawner, CoinSpawner and ObstacleDrift all ask it.

**`PickRock(lastIndex)` — :288** Picks a random rock, never the one used last:

```csharp
int index = Random.Range(0, rockPrefabsArray.Length - 1);
if (index >= lastIndex) index++;
```

Picks from a list **one shorter**, then skips past the last. Every other rock stays
equally likely, with no re-rolling — the obvious "pick again if it matches" can in
theory loop forever. This cannot.

| Method | Line | In the game |
|---|---|---|
| `StartSpawning()` | 94 | Starts building, both rows parked off screen. |
| `StopSpawning()` | 119 | Stops building. GameManager clears what is out there. |
| `Update()` | 127 | Keeps both rows built past the right edge. Hands each row **the other** — that is what stops the walls meeting. |
| `Forget(row)` | 142 | Drops notes on long-gone rocks. Counts **backwards** so removing one does not shuffle the rest; looped by hand because `RemoveAll` allocates, twice a frame, enough to stutter. |
| `FillRow()` | 156 | Adds rocks until the row reaches far enough. Caps at 10 — a zero-width rock would loop forever. |
| `DeepestBetween()` | 241 | The measuring tape. Makes "walls can never meet" true. |
| `RowReaches()` | 274 | True once a row covers that spot. |
| `HasValidRocks()` | 303 | Checks rocks **and their pictures** before the run. `SpawnRock` measures the picture, so an empty slot would crash every frame. |

---

## 3. PlayerController.cs — why this is not Flappy Bird

**224 lines.** Reads your taps, swims, sinks, tilts, wears your chosen skin.

The hardest part of the project. The tap was tuned three times and it still felt
like a bird game. **The problem was never the tap — it was the fall.** A bird drops
like a stone. A fish should sink.

**Nine of eleven settings differ from the script.** Be most careful here.

| Setting | Value | What it does in the game |
|---|---|---|
| **Swim Force** | **4.6** (script 4) | How far up the fish goes per tap. |
| **Water Gravity** | **1** (script 1.2) | Pull downwards. Lower is floatier. |
| **Water Drag** | **1.5** (script 1) | How thick the water feels. **This is what stops it feeling like a bird.** |
| **Max Sink Speed** | **3.2** (script 4.5) | The fastest it may sink. |
| **Current Push** | **9** (script 3) | How hard a surge shoves it back. 0 turns currents off. |
| **Current Return** | **2.5** (script 4) | How hard it swims home. Higher = shoved less. |
| **Rotation Speed** | **10** (script 5) | How much it tips for a given speed. |
| **Max Tilt Up / Down** | **20 / -40** (script 25 / -45) | Tilt limits, in degrees. |
| Tilt Smoothing | 5 | How fast it swings to the new angle. Lower is smoother. |
| Skins | 5 fish | One per shop slot. Slot 0 is the starting fish. |

> **Current Push is the big one.** The script's `3` gave only **0.23 units** of
> shove. The fish is **1.09** wide — a fifth of a body. The mechanic the team was
> proudest of was almost switched off and nobody noticed, because nobody measured
> it; it looked fine in the Inspector. Raised to **9**, a full body width.

> **"What makes it feel underwater?"** Drag and the sink cap. Drag makes it glide
> instead of snapping about; the cap stops one missed tap costing half a gap.
> Dropping the sink from 4.6 to 3.2 made it **25% less punishing** and it still dives.

**Worked out for itself:** `rb` (the physics body), `IsAlive`, `SwamThisFrame`,
`homeX`/`homeY` (the spot it returns to / bobs around), `fishRenderer`.
**Locked in code:** `IDLE_BOB_HEIGHT` 0.12, `IDLE_BOB_SPEED` 1.4, `IDLE_TILT` 3.

> **Why are those three `const` and not Unity settings?** A real Unity trap: a
> **new** setting added to a script the scene **already saved** loads as **0** —
> and 0 here means a completely still fish, the exact bug this code fixes.

### The methods

**`SwimPressed()` — :146** The whole control scheme.

```csharp
if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
```

Space, click, or finger — so it works on a computer and a phone with no extra
code. **The first line is the clever bit:** if your finger landed on a button it
says no, which is what stops tapping Pause also making the fish swim.
`wasPressedThisFrame` means the moment it went down, so holding does not fly.

**`FixedUpdate()` — :158** The physics, and a tug of war.

```csharp
float push = (ObstacleMovement.Surge - 1f) * currentPush;
float home = (homeX - transform.position.x) * currentReturn;
rb.linearVelocityX = home - push;
```

`push` is the current shoving left — in calm water `Surge` is 1, so `(1-1)*9` is
**zero**. `home` is the fish swimming back, and **the further out, the harder it
pulls.** Because the pull grows with distance, the two settle where they cancel —
so a current can shove you only so far and **never builds up**, however long it
lasts. No clamp needed; the maths does it.

```csharp
if (rb.linearVelocity.y < -maxSinkSpeed) rb.linearVelocityY = -maxSinkSpeed;
```

The sink cap. Four lines, and the single biggest thing making the fish a fish.

Then tilt: angle from vertical speed, `Mathf.Clamp`ed so it never flips over, and
`LerpAngle` to swing round gradually instead of snapping.

> **Why `FixedUpdate` not `Update`?** Physics belongs on a steady clock. Physics
> in `Update` behaves differently on a fast phone and a slow one.

**`Update()` — :102** Three guards, each stopping a real bug: dead fish do not
swim; a frozen game means a tap is a **button press** (one check covers menu, pause
*and* game over); `SwimPressed()` throws out taps on buttons. Then
`rb.linearVelocityY = swimForce` — it **sets** the upward speed rather than adding,
which is why fast tapping does not launch the fish off the top.

| Method | Line | In the game |
|---|---|---|
| `Awake()` | 82 | Finds the body and picture; applies drag and gravity **in code**, so these beat whatever the scene saved on the Rigidbody. Tune the fish on this script, never its Rigidbody 2D. `freezeRotation` stops physics spinning it on a bump. |
| `MenuIdle()` | 127 | Bobs the fish on the menu, on the **real** clock since physics is frozen. Height worked out **fresh from `homeY`** every frame, never nudged — a menu can sit for minutes, and accumulated error is what once walked the background out of line. |
| `OnCollisionEnter2D()` | 187 | `IsAlive = false`. `PlayerHit` actually ends the run. |
| `ResetPlayer()` | 195 | Fish back to the start: place, angle, still, alive, skin on. |
| `ApplySkin()` | 212 | Wears your shop choice. A saved choice that no longer exists falls back to fish 0 rather than leaving the last picture up. Reads the key from `FishInventory.EQUIPPED_KEY`, so two files cannot disagree on the name. |

---

## 4. ObstacleMovement.cs — one speed, written in one place

**63 lines, and it holds the game together.** Slides one thing left and deletes it
once gone. Every rock, coin and piece of rubbish has one.

| Name | Value | What it does in the game |
|---|---|---|
| Speed | 5 | How fast things slide left. Set on the rock **prefabs**, not the scene. |
| `SpeedMultiplier` | starts 1 | How much faster the cave is than normal. **Shared by everything.** |
| `Surge` | starts 1 | Just the wave part. The **fish** reads this too. |
| `DESPAWN_MARGIN` | 1 | Far enough off screen that nothing pops out of existence. |

Both shared numbers are `static` — one copy for the whole game, not one each — and
read-only from outside.

**`SetPace(speedMultiplier, surge)` — :28** Two lines, the most important design
decision in the project:

```csharp
SpeedMultiplier = speedMultiplier;
Surge = surge;
```

Rocks, rubbish and coins cannot drift out of step because there is nothing to
drift out of step *with*. They all read one number.

> **"Why lock it to one writer?"** It leaked between runs once. The project keeps
> shared values between Plays to start faster, so stopping Play mid-surge left the
> **next** run starting mid-surge. A second writer would be near impossible to spot.

**`ResetStatics()` — :38** The fix for that leak. The `[RuntimeInitializeOnLoadMethod]`
**attribute** tells Unity to run it automatically before anything loads.

**`Update()` — :53** `speed * SpeedMultiplier` is the entire difficulty curve in
one multiplication: 5 × (0.9 → 1.6) = 4.5 → 8. It waits for the object's **right**
edge, not its middle — a rock is up to 10 wide, so checking the middle would make
half of it vanish on screen. **`Awake()` — :45** remembers the renderer that check needs.

---

# Part 2 — The supporting cast

Fourteen scripts, compact. Skim once so you know where to look.

---

## 5. TrashSpawner.cs — the difficulty curve lives here

**274 lines.** Drops rubbish — **and runs the whole game's difficulty.** If asked
"where does the game get harder?", the answer is this file.

| Setting | Value | Setting | Value |
|---|---|---|---|
| **Min Size** | **0.6** (script 0.5) | Surge Strength | 1.3 |
| Max Size | 0.9 | Surge Seconds | 2.5 |
| Min / Max Spin | 10 / 45 | Surge Interval | 9 |
| Drift Y | 0.4 | Debris Current Push | 1.2 |
| **Drift Min / Max Speed** | **0.1 / 0.32** (script 0.15 / 0.5) | Start / Min Interval | 3 / 0.9 |
| **Ramp Seconds** | **150** (script 90) | Max On Screen | 8 |
| **Start Speed Multiplier** | **0.9** (script 1.15) | Max Speed Multiplier | 1.6 |

So speed runs **0.9 × 5 = 4.5** up to **1.6 × 5 = 8** over **150 seconds**.
Fixed in code: `CALM_PACE` 1, `CALM_SURGE` 1, `SURGE_AT_START` 0.5, `SPAWN_MARGIN` 1.
Wired slot: **Trash Prefabs** — the bits of rubbish to pick from.

> **"Why is Surge Interval never random?"** So you can learn the rhythm and plan.
> A random shove is bad luck; one every 9 seconds is something to get good at.

**`Update()` — :136** The difficulty curve itself:

```csharp
float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
float ramped = Mathf.Lerp(startSpeedMultiplier, maxSpeedMultiplier, progress);
float surge = Surge(progress);
ObstacleMovement.SetPace(ramped * surge, surge);
```

`surge` is published **twice** — once multiplied into the speed, once alone. That
second one is how the **fish** feels the current, since `PlayerController` reads
`Surge` directly. Rubbish has two separate limits: a timer that speeds up **and** a
hard on-screen cap.

**`Surge(progress)` — :164** `elapsed % (surgeInterval + surgeSeconds)` makes the
clock repeat every 11.5s — that is the rhythm. Worked out **purely from the
clock**, so nothing is remembered between frames and it cannot drift or stick on.
`Mathf.Sin(t * Mathf.PI)` draws a smooth arch, so the surge swells and fades
instead of snapping like a switch. Strength grows on the same counter as speed:
shallow water shoves a bit, the deep cave twice as hard.

**`SpawnPiece()` — :186** Uses `magnitude` for the **corner-to-corner** length, not
width, because a piece **spins** while it floats. `clearance = radius + driftY` —
the piece *plus how far it may wander* — so it can never drift into a rock later.
Gives up quietly if the rocks are not built that far or the gap is too narrow. Then
one piece becomes four small scripts stacked: `ObstacleMovement` (slide),
`TrashSpin` (spin), `ObstacleDrift` (bob + current).

| Method | Line | In the game |
|---|---|---|
| `Awake()` | 94 | Finds the rock spawner, which owns the gap. |
| `StartSpawning()` | 100 | Starts dropping, difficulty back at the start. |
| `StopSpawning()` | 112 | Stops, **and puts the cave back to calm** — the speed is shared, so without that the difficulty survives into the next run. |
| `ResetRun()` | 120 | Wipes progress. Resets the **surge** too: `Update` would overwrite it, but a stale value in something called "current surge" makes both numbers untrustworthy while debugging. |
| `PickTrash()` | 238 | Random piece, never the same twice running. |
| `HasValidTrash()` | 253 | Checks pieces and pictures before the run. |

---

## 6. CoinSpawner.cs — the only thing you may touch

**182 lines.** Drops coins into the gap. Sits on the **same object** as
CaveSpawner, so clearing rocks clears coins. It does **not** speed coins up — they
carry an `ObstacleMovement` reading the shared speed. Only how **often** changes here.

| Setting | Value | Setting | Value |
|---|---|---|---|
| Coin Size | 0.7 | **Ramp Seconds** | **150** (script 90) |
| Spin Degrees | 25 | Start / Min Interval | 2.5 / 1 |
| Drift Y | 0.4 | Max On Screen | 5 |
| Wall Gap | 0.35 | Coin Current Push | 1.2 |
| **Drift Min / Max Speed** | **0.1 / 0.32** (script 0.15 / 0.5) | `SPAWN_MARGIN` | 1 |

Wired slot: **Coin Prefab** — the coin to copy.

> **"Its collider MUST stay Is Trigger"** — the most important line in the file. A
> trigger means *touch*. A normal collider means *bump*, and `PlayerHit` ends the
> run on any bump — so you would **die collecting a coin**.

`CoinSprite` (:52) is a **property**, not a method: it hands back the coin's
picture so the counter uses the same image with no extra slot to get wrong.

**`SpawnCoin()` — :125** Like TrashSpawner's, plus one term:
`clearance = radius + driftY + wallGap`. Coins get that extra gap and rubbish does
not — **a coin hugging a rock is a trap**, because reaching for it kills you.
Rubbish is already something you avoid. It reuses `TrashSpin` for spin; a spin is a
spin.

**`Update()` — :102** `liveCoins.RemoveAll(coin => coin == null)` — collected coins
leave **holes** in the list. Without clearing them the on-screen cap counts coins
you already collected, and eventually no new coins appear at all.
**`Awake()` :75**, **`StartSpawning()` :83** (a badly set-up coin just does not
start — a run with no coins beats an error every frame), **`StopSpawning()` :95**,
**`HasValidCoin()` :164**.

---

## 7. SpotLight2D.cs — the lamp on the fish's head

**99 lines. Every setting matches the scene** — nothing to catch you out.

| Setting | Value | What it does in the game |
|---|---|---|
| Deep Multiplier | 1.6 | How much brighter at the cave's darkest. 1 = depth changes nothing. |
| Pulse Boost | 0.3 | Extra brightness for one swim. 0 turns the flash off. |
| Recover Speed | 7 | How fast the flash fades. |
| Flicker Depth | 0.16 | How much it wobbles. 0 makes it a steady lamp. |
| Flicker Speed | 6.5 | How fast the wobble runs. |

> **Why wobble at all?** A real anglerfish bulb is part of the animal. Light that
> holds perfectly still looks like a torch taped to a fish.

**`Update()` — :74** is the whole script, and four things in it matter:

```csharp
float resting = Mathf.Lerp(1f, deepMultiplier, GameManager.Instance.CaveDepth);
float peak = resting + pulseBoost;
if (swimmer != null && swimmer.SwamThisFrame) level = Mathf.Min(level + pulseBoost, peak);
level = Mathf.Lerp(level, resting, Time.deltaTime * recoverSpeed);
float flicker = 1f + (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0f) - 0.5f) * 2f * flickerDepth;
lure.intensity = authoredIntensity * level * flicker;
```

**1.** Reads the same `CaveDepth` as the cave light, so they cannot drift apart.
**2.** Watches `SwamThisFrame`, **not taps** — so it inherits every one of the
fish's rules free: no flash while dead, paused, on the menu, or when the tap hit a
button. Nothing written twice. **3.** The `Mathf.Min` is a clamp **and a bug fix**:

> **The best bug in the project.** Making the flare depth-aware turned a bounded
> line into `level += pulseBoost`. Tapping is the one input a player can spam
> without limit, and mashing reached **9 times** the intended peak — enough to
> remove the darkness from the game. Proved by simulation: **1.25** against a 1.30
> ceiling clamped, **11.7** unclamped.

**4.** `PerlinNoise`, not `Random` — Random jumps and looks like a loose wire;
Perlin wanders smoothly and looks alive. Note `unscaledTime` for the flicker but
`deltaTime` for the fade: the flash **holds** behind the pause screen instead of
quietly fading out.

**`authoredIntensity`** is the Intensity you typed, read once in **`Awake()` :47**
as this bulb's 100%, and only ever **nudged**, never replaced.

> The old version wrote brightness *from its own field*, so the Light 2D slider did
> nothing once the game started. **Two sessions were lost to that.** Because it is
> read in `Awake`, set it with the game **stopped**.

`Awake` also finds the fish with `GetComponentInParent` — the lamp is attached
under the fish, so there is no Inspector slot to come unplugged.

---

## 8. ObstacleDrift.cs — making debris look alive

**119 lines, no Unity settings** — the spawners hand it everything. Bobs a piece up
and down so it looks like it floats instead of riding an invisible rail. Up and
down only; sideways is `ObstacleMovement`'s job.

Fixed: `MIN_/MAX_SECONDS_UNTIL_TURN` 1.5 / 3.5. Holds: `allowedDrift`,
`driftOffset`, `driftVelocity`, `minDriftSpeed`, `maxDriftSpeed`,
`secondsUntilTurn`, `rockSpawner`, `currentPush`, `pieceRadius`, `wallGap`.

| Method | Line | In the game |
|---|---|---|
| `Configure()` | 28 | How far and fast this piece may wander. |
| `ConfigureCurrent()` | 38 | Signs it up to be carried. Two separate setup methods, so a piece can bob **without** being carried — the current stays optional. |
| `PickVelocity()` | 49 | New direction and how long to keep it. **Every piece rolls its own**, which stops the rubbish moving like one big sheet. |
| `Update()` | 56 | Adds the **change** in height, not the height — setting position directly would make this and `ObstacleMovement` fight every frame and judder. Flips at the edge, or every piece piles against its limit and the wall looks flat. |
| `CarryForward()` | 85 | Sweeps it along, harder in a surge. |

`CarryForward` is the careful one. The piece moves **ahead** of the rocks, so it
arrives somewhere nobody checked was safe — the gap is re-measured **every frame**
and the piece held inside it. If it will not fit, it drops the wall gap and retries
with just the piece; if even that fails it gives up on *holding it clear* rather
than on the piece, because otherwise the maths flips and shoves it through the far
wall. And if it had to be pushed off a wall it also **turns round**, or next frame
it drives back in and buzzes like a fly on a window.

---

## 9. PlayerHit.cs — the two ways to die

**48 lines.** One setting: Below Screen Margin **1** — how far below the screen the
fish must sink before the run ends. A margin exists so the fish fully disappears
first; ending it on the screen edge looks like a glitch.

| Method | Line | In the game |
|---|---|---|
| `Awake()` | 14 | Finds the camera. |
| `Update()` | 22 | Watches for sinking off the bottom. **There is no floor in this game** — without this the fish falls forever and the run never ends. |
| `OnCollisionEnter2D()` | 36 | Bumped a rock or rubbish. Unity calls this itself. |
| `EndRun()` | 42 | Two ways in, **one** way out — so a change to how a run ends is made once, not twice. |

---

## 10. CoinPickup.cs — collecting, not crashing

**27 lines, one method.** `OnTriggerEnter2D()` — :11

```csharp
if (collected) return;
if (collision.GetComponent<PlayerController>() == null) return;
collected = true;
GameManager.Instance.AddCoin();
Destroy(gameObject);
```

**`OnTriggerEnter2D`, not `OnCollisionEnter2D`** — that one word is the whole
reason collecting a coin does not kill you. Two guards: `collected` stops one coin
paying twice if the fish overlaps across two frames; the `PlayerController` check
stops drifting rubbish collecting it for you.

---

## 11. TrashSpin.cs — the smallest script in the game

**24 lines.** Degrees Per Second (20 in code, **set per piece by the spawners**
through `SetSpin()` :13 — correctly *not* in the scene). `Update()` :19 is
`transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime)` — **game** time, so
spin freezes with everything else on pause. The real clock would leave rubbish
spinning on a frozen screen.

---

## 12. ScrollObject.cs — the endless background

**156 lines.** Scroll Speed **2**. Several copies in a row; one sliding off the
left goes to the back of the queue, so a few pictures look endless.

Holds `spriteRenderer`, `width`, `startX`, `copyCount`, `anchorX`, `index`,
`startTime`, `leftEdge`, `SEAM_OVERLAP` **0.02**, and `Step` (width − overlap).

> **Why overlap 0.02?** Even a perfect join leaves a hairline crack on the pixel
> grid, and the pale camera background flashes through it. ~2px at 1080p.
> CaveSpawner overlaps rocks for the same reason.

**`Update()` — :90** The position is **worked out fresh from the clock** every
frame, never added up:

```csharp
float travelled = Mathf.Repeat((Time.time - startTime) * scrollSpeed, span);
float x = anchorX + index * Step - travelled;
if (x + width / 2f <= leftEdge) x += span;
```

> **The bug this fixed:** nudging each copy along let rounding errors creep in, and
> the gap grew a little on every trip round the queue until you could see daylight.
> Clock-based cannot drift — there is nothing to accumulate. `MenuIdle` was
> rewritten the same way for the same reason.

Recycling uses the copy's **right** edge passing the screen edge; a fixed distance
once wrapped a copy with 1.9 units still showing, leaving a bare strip.

| Method | Line | In the game |
|---|---|---|
| `Awake()` | 44 | Measures width, notes the start time. |
| `Start()` | 55 | Takes width, speed **and start time** from copy 0, so copies cannot disagree — a one-frame difference would bake in a permanent gap. |
| `AddMissingCopies()` | 113 | Makes copies until the row covers the screen, plus a spare. In code, so a wider phone cannot open a gap. |
| `FindCopies()` | 135 | Matches on the **picture**, which keeps a far and a near background apart. |

---

## 13. PauseBlur.cs — the frozen, blurry backdrop

**177 lines.** Not a MonoBehaviour — a plain class GameManager makes when first
needed, so a run where you never pause never pays. Holds `canvasRoot`,
`dimDarkness` (0.7), `blurSteps` (4), `dim`, `blur`, `blurTexture`.

**`Capture()` — :115** The most technical method in the project, and the idea is
simple: **take one photo, halve it four times.** Each halving averages pixels, so
it goes soft; then stretch the small photo back over the screen. **No shader, no
post-processing stack, no extra asset** — the graphics card's own smoothing does
it. And the game is frozen, so one photo looks identical to blurring every frame
and costs nothing per frame.

Three hard-won details:

- **The `24`** asks for depth bits. Nothing uses depth, but Unity 6's Render Graph
  **refuses** to draw a camera into a target without them, and warns on every pause.
- **`FilterMode.Bilinear`, set twice.** Borrowed textures arrive with the last
  user's setting. Smooth averages each 2×2 block; unset grabs one pixel of four —
  **chunky, not blurry** — and those hard edges survive every later shrink.
- **Background alpha forced to 1.** The camera clears to see-through, halving
  smears that outwards, and `RawImage` multiplies by it, so the HUD ghosted as dark
  smudges. Forcing it solid fixed **every** overlay element, not just the one reported.

| Method | Line | In the game |
|---|---|---|
| `Show()` | 29 | Darkens the frozen game and soaks up its taps. The sheet goes on **the Canvas itself** — inside one button group it could not cover the others. Draw order is blur, sheet, buttons; anything **not** lifted stays buried, which is how the Pause button becomes untappable on purpose. The caller hands in what must stay readable, so this never needs to know what a "game over panel" is. |
| `Release()` | 79 | Hands the memory back. This texture is **not** freed on its own. |
| `Stretch()` | 86 | Pins something to all four corners, so it fills any phone shape. |
| `Raise()` | 98 | Walks **up** the tree and lifts the whole group — lifting the object alone only moves it to the front of its own group, still buried. |

---

## 14. FishInventory.cs — the fish shop

**229 lines.** A shelf you swipe. Unbought fish are grey and cost coins; owned fish
can be worn, one at a time.

| Setting | Value | What it does in the game |
|---|---|---|
| Price | 300 | Coins per fish. Same for all of them. |
| Wired slots | Content, Locked Art, Fish Art, Buy Art, Select Art, Equipped Art, Wallet Text | The shelf and its card pictures. |

> **Why its own coin total?** The run counters are hidden on the menu, so without
> the wallet you could not see your coins on the one screen that spends them.

### `ownedMask` — the cleverest variable in the project

**Picture a row of light switches, one per fish. On means you own it.** All packed
into one whole number.

```csharp
return index >= 0 && (ownedMask & (1 << index)) != 0;
```

`1 << index` makes a number with only that switch on; `&` compares switch by
switch. So the line asks: *is switch `index` on?*

**Why?** Unity's save cannot store a list, and separate values per fish are
separate chances to update one and forget the others. One number, one save, one
place to be wrong.

> **Why "FishOwned2"?** The `2` retires every save from when the shelf had six
> cards. Slot 4 came free then and slot 4 is **now the paid fish** — trusting an
> old save would hand it over for nothing. Coins are a separate key, so nobody
> loses money.

> **Why check `index >= 0`?** Old saves used `-1` for "nothing worn", and the
> shifting maths turns `-1` into `31`. A real bug, caught and guarded.

| Method | Line | In the game |
|---|---|---|
| `Awake()` | 74 | Finds the slots **in code**, so a sixth fish needs no change here. Contains `int index = i;` — looks pointless, is essential: without it all five buttons share one counter and act on the **last** slot. |
| `OnEnable()` | 129 | Re-reads your save **every time the shop opens**, because coins earned in a run change what you can afford. |
| `Load()` | 135 | Turns on every switch **except the last** — so all but one fish come free, and you save up for one. Worked out from the slot count, so adding a slot makes *that* the new paid fish automatically. |
| `Owns()` | 154 | Reads one switch out of the packed number. |
| `OnBlockClicked()` | 160 | Buys if you can afford it, then wears it. `TrySpendCoins` checks **and** subtracts in one go, so there is no gap to get a fish unpaid. Saves twice, immediately — **a fish that vanishes when the app closes is 300 stolen coins.** Buying also wears it; nobody spends 300 then wants a second button. |
| `Refresh()` | 189 | Redraws **all** cards **every** time, which is what makes "only one fish is worn" true with nobody un-wearing the old one. `interactable = false` greys the button **and** stops it eating taps — otherwise being broke just looks like a broken button. |

> **Written as a warning in the code:** do **not** read `EquippedIndex` to pick the
> in-game fish. The shop panel is off at load, so its setup has not run and it
> reads 0. `PlayerController` reads the **save** instead, which is always right.

---

## 15. LegalNotice.cs — the terms and privacy screens

**313 lines**, another plain class. Two screens: a one-off gate you must accept,
and a scrollable reader with both full documents. **Everything is built in code** —
deliberately, so these could be added without touching the scene file at all.

Holds `ACCEPTED_KEY`, `TERMS_PATH`, `PRIVACY_PATH`, `GATE_SUMMARY`, `PlateColour`
(0.204, 0.627, 0.604 — taken off the shop artwork so it matches the game rather
than looking like a system pop-up), `canvasRoot`, `gameFont`, `accepted`, `gate`,
`reader`.

The gate says plainly: an amateur school project by students, given away free; not
finished, not optimised; **a large part made with AI assistance including the
code**; no warranty, no liability; collects nothing — no internet, accounts, ads or
tracking. Full documents together are about **1,399 words**.

> **Why keep the gate short?** A wall of text on first launch gets closed unread,
> and the full text is one tap away on the same screen.

| Method | Line | In the game |
|---|---|---|
| constructor | 48 | Just remembers where to build. Builds nothing yet. |
| `Accepted` | 54 | True once accepted on this device. |
| `ClearAcceptance()` | 58 | Forgets acceptance, so the gate shows again. For testing. |
| `ShowGateIfNeeded()` | 67 | Puts the gate up unless already accepted. `SetAsLastSibling` puts it on top; the panel is solid so the menu cannot be tapped through. |
| `Quit()` | 86 | `#if UNITY_EDITOR` is a **compiler switch**: stop Play in the Editor, `Application.Quit()` in the real build — which is why Exit looked broken while testing. Exit really quits, because the alternative is playing a game whose terms you never agreed to. |
| `OpenReader()` | 97 | Opens both full documents, scrollable. |
| `BuildGate()` | 109 | Summary, full-terms link, Accept and Exit. **The harmless middle button separates Accept and Exit**, so neither is the one you hit by accident. |
| `BuildReader()` | 135 | Both documents, one after the other. |
| `Load()` | 151 | Returns a readable message if a file is missing, so a broken build cannot leave the panel **blank**. |
| `BuildPanel()` | 163 | The full-screen background. Its image is what **stops taps reaching the menu** — not decoration. |
| `BuildTitle()` | 180 | A heading across the top. |
| `BuildScrollingText()` | 206 | **Scrolls** rather than shrinking the font, because a 1,399-word document shrunk to a phone screen is not really shown to anyone. |
| `ApplyGameFont()` | 254 | **Borrows** the font off a label already in the scene instead of using an Inspector slot — that is what let these screens skip the scene file entirely. |
| `BuildButton()` | 274 | One button, routed through `GameManager.PlayClick` so it sounds like every other button. |

---

## 16. CameraEdges.cs — where the screen edges are

**29 lines**, a `static` class — no object, call it by name from anywhere.

```csharp
public static float HalfWidth => Cam.orthographicSize * Cam.aspect;
public static float Left  => Cam.transform.position.x - HalfWidth;
public static float Right => Cam.transform.position.x + HalfWidth;
public static float Top   => Cam.transform.position.y + Cam.orthographicSize;
```

**This is why the game fits every phone.** Height is **fixed**, width **follows the
phone** — so a wide phone shows **more cave**, not a stretched one.

`cached` holds the camera; `Cam` re-finds it if it went away, because Unity reports
a destroyed camera as null and handing one back broken would crash callers.

> **Why does this exist?** `Camera.main` is slow to look up, and the edges used to
> be recomputed in several scripts — once per object, per frame. The presentation
> line: *"screen edges computed once, not per object per frame."*

---

## 17. ButtonPressSound.cs — clicks that feel fast

**26 lines**, added to every button automatically by `BuildSound()`. Clicks on
**finger-down**, not finger-up, which feels faster.

**`OnPointerDown()` — :18** opens with `if (!button.interactable) return;` — **a
real bug fix.** Unity only checks a button is enabled at finger-**up**, so without
it the greyed-out BUY button clicks when you are broke, making it sound like the
purchase worked. **`Awake()` :12** finds the button;
`[RequireComponent(typeof(Button))]` makes Unity refuse to separate them.

---

## 18. FishShopDebug.cs — the Tools menu for testing

**90 lines, in `Assets/Editor` so it is not in the built game.** Found at
**Tools → Fish Shop**. Keys come **from FishInventory**, not retyped — they were
typed twice once, and *a reset that clears a key nothing reads looks exactly like a
reset that worked.*

| Menu item | Method | Line |
|---|---|---|
| Reset Legal Acceptance | `ResetLegal()` | 24 |
| Reset Skins | `ResetSkins()` | 36 |
| Set Coins to 0 / 100 / 854 | `Coins0()` `Coins100()` `Coins854()` | 46 / 49 / 52 |
| Show Save State | `Show()` | 55 |
| shared by the three above | `SetCoins(int)` | 60 |

All six are wired by the `[MenuItem(...)]` attribute; you never call them from
code. The three coin items share `SetCoins`, so one place writes your coins.

> **Why this script exists — it caught the team twice.** Two things keep their own
> copy of the save: the **running game** holds coins and owned fish in memory and
> saves over your change when it stops; the **Editor** holds its own copy, so
> editing the save from outside Unity does nothing. So: **always stop Play first,
> and always change things through this menu.**

**`PlayStopped()` :69** enforces that with a pop-up rather than failing silently.
**`ResetSkins()`** writes plain `0`, **not** a list of the free fish — which fish
come free is `FishInventory`'s decision, and copying that rule here is how the two
would disagree. **`Report()` :84** prints the save to the Console: the only honest
way to see what the game was *actually* handed.

---

# Part 3 — The safety net

---

## If they point at a line you do not know

Say: **"I would have to check that one."** That is better than a guess — **a wrong
answer confidently given is what costs you.** Nobody holds 3,163 lines in their head.

You can nearly always say something true without knowing the line:

| If the line has... | You can honestly say |
|---|---|
| `if (x == null) return;` | "A guard. It stops a crash if that was never wired up." |
| `Time.deltaTime` | "Makes movement the same speed on a fast phone and a slow one." |
| `Clamp` / `Min` / `Max` | "A limit, so the number can never go somewhere unfair." |
| `Mathf.Lerp` | "Mixes two values. How things fade smoothly instead of jumping." |
| `PlayerPrefs` | "The save. Writes to the phone, so coins and scores survive closing the app." |
| `static` | "Shared by the whole game, one copy rather than one each." |
| `const` | "Locked in code on purpose, so the Unity window cannot change it." |
| `GameManager.Instance` | "How any script reaches the one GameManager." |
| `CaveDepth` | "The one depth number. The light, rock gap and lamp all read it." |
| `Instantiate` / `Destroy` | "Makes a new copy while the game runs / removes one." |
| `GetComponent` | "Finds another script or part on the same object." |

## Questions to expect

**"How does it get harder?"** Three things at once off one depth number: light
dims 0.03 to 0.02, gap narrows 3.6 to 2.6, cave speeds 4.5 to 8 over 150 seconds.

**"Why not just speed it up?"** Speed alone gets unfair fast. Darkness takes away
**information** instead of reaction time, so it feels tense rather than cheap.

**"Is the cave random?"** The shapes are. The gap is not — the opposite wall is
measured before every rock, and 2.2 is locked in code.

**"What makes it not Flappy Bird?"** Water drag and the sink cap. A bird falls like
a stone; our fish sinks at no more than 3.2. That change made it 25% less punishing
and it still dives.

**"Why a global light if the fish has a lamp?"** The strongest answer you have,
because it is **measured, not felt**:

| Measurement | Value |
|---|---|
| Rock speed | 3.5 start, 8 by 90s, 10.4 mid-surge |
| Fish to right edge | 11.85 units |
| Lure reach | 3.63 units |
| **Warning at 8 u/s** | **0.45 s** |
| **Warning in a surge** | **0.35 s** |

Human reaction alone is about 0.25s and the fish still has to travel. So at full
speed the lure **cannot** be the only light — you have to read rock silhouettes
against the background. **The global light is a fairness mechanism, not atmosphere.**

**"How much did AI write?"** A large part, including code. Disclosed in the README
and on the screen the player must accept. Say it plainly and move on — it is
already written down, so there is nothing to defend.

**"What is not finished?"** Sprites are not packed into an atlas, so the game draws
more than it needs. Tested on one phone. That is the honest list.

**"What next?"** Wider device testing, the sprite atlas, a sixth fish.

**"Hardest bug?"** Pick one: the **score counter ate the taps** (a 1018x141 text box
absorbed every tap; one line fixed it); **tap-spam broke the darkness** (reached 9
times the intended peak — 1.25 clamped vs 11.7 unclamped); **the current was almost
switched off** (0.23 units of shove against a fish 1.09 wide, and it looked fine in
the Inspector because nobody measured it).

## The 30 settings where script and scene disagree

**The right column is what the game uses.**

| Script | Setting | Script says | **Game uses** |
|---|---|---|---|
| PlayerController | Swim Force | 4 | **4.6** |
| PlayerController | Water Gravity | 1.2 | **1** |
| PlayerController | Water Drag | 1 | **1.5** |
| PlayerController | Max Sink Speed | 4.5 | **3.2** |
| PlayerController | Current Push | 3 | **9** |
| PlayerController | Current Return | 4 | **2.5** |
| PlayerController | Rotation Speed | 5 | **10** |
| PlayerController | Max Tilt Up | 25 | **20** |
| PlayerController | Max Tilt Down | -45 | **-40** |
| GameManager | Start Light | 0.35 | **0.03** |
| GameManager | Deep Light | 0.08 | **0.02** |
| GameManager | Darkest At Distance | 400 | **300** |
| GameManager | Pause Dim Darkness | 0.78 | **0.7** |
| GameManager | Start Fraction From Left | 0.5 | **0.4** |
| GameManager | Movement Pop Volume | 0.35 | **0.05** |
| GameManager | Bg Music Volume | 0.3 | **0.5** |
| CaveSpawner | Min Gap | 3 | **3.6** |
| CaveSpawner | Min Depth | 2 | **2.5** |
| CaveSpawner | Max Depth Change | 1.2 | **1** |
| CaveSpawner | Floor Y | -4 | **-5** |
| CaveSpawner | Min Scale | 0.35 | **0.55** |
| CaveSpawner | Max Width | 8 | **10** |
| TrashSpawner | Min Size | 0.5 | **0.6** |
| TrashSpawner | Drift Min Speed | 0.15 | **0.1** |
| TrashSpawner | Drift Max Speed | 0.5 | **0.32** |
| TrashSpawner | Ramp Seconds | 90 | **150** |
| TrashSpawner | Start Speed Multiplier | 1.15 | **0.9** |
| CoinSpawner | Drift Min Speed | 0.15 | **0.1** |
| CoinSpawner | Drift Max Speed | 0.5 | **0.32** |
| CoinSpawner | Ramp Seconds | 90 | **150** |

Everything in **SpotLight2D**, **PlayerHit**, **ScrollObject**, **TrashSpin**,
**FishInventory** and the rest of the spawners matches. Those you can read straight
off the screen.

## Numbers worth memorising

Fish **1.33** tall (**1.11** can hit things), **1.09** wide. Gap **3.6** to **2.6**,
hard floor **2.2**. Light **0.03** to **0.02**, darkest at **300m**. Speed **4.5**
to **8** over **150s**. Surge every **9s** lasting **2.5**. Swim **4.6**, max sink
**3.2**. A fish costs **300** coins. Screen about **17.8** units wide.

## Word list

| Word | Means |
|---|---|
| `Awake()` / `Start()` | Run once at the beginning. Start is slightly later, when other scripts exist. |
| `Update()` | Every frame, about 60 times a second. |
| `FixedUpdate()` | Like Update but on a steady clock. Physics goes here. |
| `OnEnable()` / `OnDestroy()` | When the object is switched on / removed. |
| MonoBehaviour | A script that can attach to an object in the scene. |
| `[SerializeField]` | Makes a hidden value into a box you can type in, in Unity. |
| `private` / `public` | Only this script can see it / other scripts can too. |
| `const` | Locked in the code. Unity cannot change it. |
| `static` | One copy shared by the whole game. |
| `float` / `int` / `bool` | Decimal number / whole number / true-or-false. |
| `null` | Empty. Nothing there. |
| guard | An early `if (...) return;` stopping a method that should not run. |
| prefab | A saved object you stamp copies of while the game runs. |
| collider / trigger | The shape things bump into / one that detects touch and lets things pass. **Coins are triggers.** |
| Rigidbody2D | Unity's physics body. What makes the fish fall. |
| singleton | A script with only one copy ever, reachable from anywhere. |
| `PlayerPrefs` | The small save file on the phone. |
| `Time.timeScale` | The speed of game time. `0` freezes everything — the whole pause. |
| `Time.unscaledTime` | The real clock. Keeps running while frozen. |
| `raycastTarget` | Whether a UI thing catches taps. Off is how counters stop eating taps. |
| attribute | The bits in square brackets. Instructions to Unity, not code that runs. |

## Before you present

- Say each Part 1 script out loud once. If a sentence will not come out, it is not
  yours yet — shorten it until it is.
- **Do not read the comments off the screen.** They are for someone reading the
  code later; you are talking to a room.
- Asked about a number? Say where it lives: **the scene, not the script.**
- "I would have to check that one" is a complete answer.

---

## About this file

This Markdown file is the source. The Word version is generated from it, so edit
this one and rebuild:

    python docs/tools/build.py

Every number here was read from `SampleScene.unity`. After tuning anything in
Unity, run `python docs/tools/check-numbers.py` — it fails if this guide quotes a
value the game no longer uses.
