# Abyssal Fish — the code, explained

A plain-English guide to all 18 scripts in the game.

Every variable. Every method. What each one actually does in the game.

Written for reading, not for skimming. Read it with the code open beside you.

---

## How to use this guide

- **Part 1** is the four scripts you will be asked about. Read these properly.
- **Part 2** is the other fourteen. Skim once, so you know where to look.
- **Part 3** is the safety net: what to say about a line you do not know, plus a word list.

Every script section has the same three parts:

1. **What it is** — two or three lines.
2. **The settings** — the boxes you can type in, inside Unity.
3. **The methods** — the jobs the script can do.

Each method shows the file and the line number, like `PlayerController.cs:146`.
Type the name into Unity's search box and it will take you there.

---

## Important: the numbers in this guide come from the scene, not the scripts

This is the thing most likely to trip you up, so read it twice.

A Unity script holds a **starting** number. The scene file holds the **real** one.
When the game runs, the scene wins. Always.

So the script `PlayerController.cs` says the fish swims with a force of `4`.
The game actually uses **4.6**. Both are true. Only one is what the player feels.

**Every number in this guide was read from the scene.** That means they are the
numbers the game really uses. Where the script says something different, this
guide says so, like this:

> **Swim Force — 4.6** *(the script file says 4)*

If your teacher opens a script and reads a different number out, that is the answer:
*"The script holds the starting value. The scene holds the real one, and the scene
wins. We tuned it in the Unity window, so the number lives in the scene."*

That is a good answer. It shows you know how Unity saves things.

There are **30** settings where the two disagree. All of them are listed in Part 3.

---

# Part 0 — The big picture

Before the scripts, here is how the game fits together. Four ideas carry everything.

## Idea 1: one depth number runs the whole feeling

`GameManager` works out how deep you are. It is a single number called `CaveDepth`.

- `0` means you are at the mouth of the cave.
- `1` means you are as deep as the game goes.

Three things read that one number:

- The cave light gets dimmer.
- The gap between the rocks gets narrower.
- The lamp on the fish gets brighter.

So the player watches **one** thing getting worse, not three separate systems.
That was the best decision in the whole project. It also means there is only
one dial to balance.

## Idea 2: one shared speed

Rocks, rubbish and coins do not each have their own speed. They all read one
number, `ObstacleMovement.SpeedMultiplier`.

Only one method is allowed to write it: `ObstacleMovement.SetPace()`.
Everything else just reads it.

If each thing owned its own speed, they would slowly drift out of step, and
nothing on screen would line up.

## Idea 3: the gap is a rule, not luck

`CaveSpawner` builds two rows of rocks. Before it places a rock, it **measures
the opposite wall at that exact spot**. So the two rows can never meet.

There is also a hard floor of `2.2`. Nothing is allowed to go below it, whatever
the settings say. The fish is `1.33` tall, so `2.2` is a little under two fish.

The shapes are random. The safe gap is not.

## Idea 4: everything except coins kills you

One rule the player is never told. It reads instantly, so the game needed no tutorial.

Rocks and rubbish use a **collider**, which means bump. Coins use a **trigger**,
which means touch. That one difference is why collecting a coin does not kill you.

## How a run flows, start to finish

1. The game opens. `GameManager.Start()` calls `BackToMenu()`, which freezes everything.
2. `LegalNotice` shows the terms screen, if you have not accepted yet.
3. You press Play. `GameManager.StartGame()` runs.
4. `CaveSpawner`, `TrashSpawner` and `CoinSpawner` all start making things.
5. You tap. `PlayerController.Update()` sees the tap and kicks the fish upwards.
6. `GameManager.Update()` counts the metres and dims the light.
7. You bump something. `PlayerHit.EndRun()` calls `GameManager.GameOver()`.
8. Everything freezes. The score is saved. The game over screen appears.

---

# Part 1 — The four scripts you will be asked about

---

## 1. GameManager.cs — the boss of the game

**760 lines. The biggest script in the project.**

It runs the menu, starts and ends runs, keeps the score and the coins, plays the
sounds, and darkens the cave. If something happens to the whole game rather than
to one object, it happens in here.

There is only ever **one** GameManager. Any script can reach it by writing
`GameManager.Instance`. That pattern has a name: a **singleton**.

### Why one script does so much

Fair question, and the honest answer is that some of this could be split up.
But the things in here all share state: the score, the coins, whether the game is
frozen. Splitting them would mean two scripts holding copies of one number, and
every bug in this project where two things disagreed came from exactly that.

### The settings — Game over screen

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Game Over Text | wired | The big GAME OVER words. Switched on by `GameOver()`. |
| Game Over Menu | wired to GameOverMenu | The panel that appears when you die. |
| Game Over Score Text | wired | Shows how far you got, like SCORE: 23m. |
| Game Over Best Text | wired | Shows your best ever, like BEST SCORE: 98m. |
| Game Over Coin Text | wired | Coins collected in the run that just ended. |

### The settings — Screens and buttons

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Main Menu | wired | The home screen. Hidden once you press Play. |
| Characters Menu | wired | The fish shop. |
| Restart Button | wired | Restarts the run. This is the **pause** screen's one. |
| Home Button | wired | Back to the menu. Also the pause screen's one. |
| Pause Button | wired | Pauses the run. Only visible while playing. |
| Resume Button | wired | Carries on a paused run. |
| **Pause Dim Darkness** | **0.7** — script says 0.78 | How dark the screen goes behind the pause buttons. 0 would be no dimming. |
| Pause Blur Steps | 4 | How blurry the frozen game goes. Each step halves the sharpness. |

The game over screen has its **own** Restart and Home buttons, separate from these.
That was a deliberate fix: they used to be the same two objects, so moving the
game over pair also moved the paused pair. The fix was duplication, not cleverness.

### The settings — Cave depth (the important ones)

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Cave Light | wired to the Global Light 2D | Leave it empty and the cave never gets dark. |
| **Start Light** | **0.03** — script says 0.35 | How bright it is at the mouth of the cave. |
| **Deep Light** | **0.02** — script says 0.08 | How bright it is at the very deepest. |
| **Darkest At Distance** | **300** — script says 400 | How many metres before the cave is as dark as it gets. |

Those three numbers are the whole mood of the game. 0.03 down to 0.02 sounds
like almost nothing, and it is — the cave is already very dark at the start.

> **If asked why so dark:** contrast is what sells darkness, not dimness. A
> uniformly dim screen reads as muddy, your eye adapts to it in seconds, and on a
> phone outdoors it just disappears. So the cave goes very dark and the lamp on
> the fish goes bright. The **gap between them** is the effect.

### The settings — Sound Button

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Menu Sound Button | wired | The **main menu's** sound on/off button. |
| Pause Sound Button | wired | The **pause screen's** one. Hidden with the other pause buttons while you play. |
| Sound On Sprite | wired | The picture shown while sound is on. |
| Sound Off Sprite | wired | The picture shown while sound is muted. |

Two buttons, **one** saved setting behind them. `ApplySound()` swaps the picture
on **both**, even the hidden one, so the two menus can never disagree about
whether sound is on.

### The settings — Sound

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Button Click | wired | Played by every button in the game. |
| Coin Clip | wired | Played when the fish collects a coin. |
| Movement Pop | wired | Played every time the fish swims up. |
| **Movement Pop Volume** | **0.05** — script says 0.35 | How loud the swim pop is. Very quiet on purpose. |
| Game Over Clip | wired | Played once when the fish dies. |
| Bg Music | wired | Loops under the whole game, menu included. |
| **Bg Music Volume** | **0.5** — script says 0.3 | How loud the music sits under the effects. |

Why is the swim pop only 0.05? Because it fires on **every single tap**. At the
same volume as the other sounds it was exhausting. The global volume moves
everything together and cannot fix one sound on its own, so this setting exists.

### The settings — Distance, coins and the fish

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Units Per Second | 5 | How fast the metre counter climbs. 5 matches the rocks' speed. |
| Coin Icon Size | 80 | How big the coin picture is. The number itself is 90. |
| Coin Icon Inset | 0 | How far in from the left edge the coin picture sits. |
| Coin Icon Text Gap | 8 | Space between the coin picture and the number. |
| **Start Fraction From Left** | **0.4** — script says 0.5 | Where the fish starts, as a share of the screen. 0.5 would be the middle. |

Start Fraction From Left is a **fraction**, not a distance. That matters: a fixed
distance sits off-centre on a tall phone, but four-tenths of the way across is
four-tenths of the way across on every phone.

### The numbers the game works out for itself

These are not settings. You cannot type in them. The game fills them in as you play.

| Name | What it holds |
|---|---|
| `Instance` | The one and only GameManager. How every other script finds it. |
| `HighScore` | The furthest you have ever got. Saved on your phone. |
| `Coins` | Coins from **this run only**. Cleared every run. |
| `TotalCoins` | Your saved total. This is what the shop spends. |
| `Distance` | How far this run has got, as a whole number. |
| `distanceTravelled` | The same thing with decimals. `Distance` is this, rounded down. |
| `CaveDepth` | How deep you are: 0 at the mouth, 1 at the darkest. |
| `Muted` | Whether the sound is off. One setting behind both sound buttons. |
| `OnMenu` | True only on the menu, where the fish floats gently. |
| `isPaused` | Whether the game is paused right now. |
| `isGameOver` | Whether the fish is dead right now. |
| `sfx` | The sound player for short sounds. |
| `music` | A **second** sound player, just for the music. |
| `COINS_KEY` `BEST_KEY` `MUTE_KEY` | The names your save is filed under on the phone. |

> **Why two sound players?** One player cannot fire a short sound and loop a
> track at the same time. Starting the click would cut the music off.

> **Why is `Coins` separate from `TotalCoins`?** The game over screen shows what
> you earned in that run. The shop spends your lifetime total. Two different
> questions, so two different numbers.

### The methods

#### Awake() — GameManager.cs:197

**Starts the game up.** The very first thing that runs.

```csharp
Instance = this;
TotalCoins = PlayerPrefs.GetInt(COINS_KEY, 0);
HighScore  = PlayerPrefs.GetInt(BEST_KEY, 0);
Muted      = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
```

It finds the spawners, loads your save off the phone, builds the sound, and draws
the coin picture.

It also does one odd-looking thing that fixed a real bug:

```csharp
foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
{
    if (counter != null) counter.raycastTarget = false;
}
```

**In the game:** that line is why tapping the top of the screen makes the fish swim.

Anything drawn in Unity's UI soaks up taps by default. The score counter is
**1018 by 141** pixels across the top of the screen, so it was silently eating
every tap that landed on it. A counter is not a button, so it is told to stop
catching taps. This is a favourite bug to talk about.

#### Start() — GameManager.cs:238

**Opens the game on the menu, frozen, with the terms screen on top.**

```csharp
BackToMenu();
legalNotice = new LegalNotice(CanvasRoot());
BuildLegalButton();
if (legalButton != null) legalButton.SetActive(false);
legalNotice.ShowGateIfNeeded(() => { legalButton.SetActive(true); });
```

The terms are built **after** the menu, so they sit on top of a finished screen.
The Terms button is hidden until you accept, so a missing menu can never become a
way to skip the terms.

#### Update() — GameManager.cs:303

**Counts the metres and darkens the cave as they climb.** Runs every frame.

```csharp
distanceTravelled += unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime;

int whole = (int)distanceTravelled;
if (whole == Distance) return;

Distance = whole;
ShowDistance();
ApplyCaveDepth();
```

**In the game:** this is the metre counter, and the dimmer switch.

Two clever bits:

1. It multiplies by `SpeedMultiplier`, so as the cave speeds up, the metres count
   up faster too. The number matches what you can see going past.
2. `if (whole == Distance) return;` means it only redraws the text when the whole
   number actually changes. Without that it would rewrite the text sixty times a
   second to say the same thing.

There is also something it does **not** do: there is no check for whether the game
is paused. It does not need one. `Time.deltaTime` stops counting while the game is
frozen, so the menu, the pause screen and the game over screen all stop the
counter for free.

#### GameOver() — GameManager.cs:318

**The fish is dead.**

```csharp
if (isGameOver) return;
isGameOver = true;
if (sfx != null && gameOverClip != null) sfx.PlayOneShot(gameOverClip);

Time.timeScale = 0f;
obstacleSpawner.StopSpawning();

if (Distance > HighScore)
{
    HighScore = Distance;
    PlayerPrefs.SetInt(BEST_KEY, HighScore);
}
PlayerPrefs.Save();
```

**In the game:** it freezes everything, saves a new best score, and shows the
game over panel.

The first line is a **guard**. The fish can hit two things in the same frame, and
without that guard the death sound would play twice. Notice the sound comes
*after* the guard, on purpose.

`PlayerPrefs.Save()` writes your score to the disk straight away. A phone can be
closed without ever shutting down properly, so waiting is how you lose a high score.

#### StartGame() — GameManager.cs:355

**Starts a run.** Play and both Restart buttons all call this.

```csharp
Time.timeScale = 1f;
player.GetComponent<PlayerController>().ResetPlayer(PlayerStart());
ClearObstacles();
ResetCoins();
ResetDistance();
obstacleSpawner.StartSpawning();
if (music != null && bgMusic != null && !music.isPlaying) music.Play();
```

Unfreezes time, puts the fish back, deletes anything left over from last time,
zeroes the counters, and starts the spawners.

That last line only plays the music **if it is not already playing**. Calling
Play on running music snaps it back to the start, so without the check a
retry would never hear past the intro.

#### TogglePause() — GameManager.cs:388

**Pauses the run, or carries on.**

```csharp
if (isGameOver) return;
isPaused = !isPaused;
Time.timeScale = isPaused ? 0f : 1f;
ShowRunButtons(isPaused, true);
```

`Time.timeScale = 0f` is the whole pause. One line stops every moving thing in
the game at once.

The exclamation mark means "not", so `isPaused = !isPaused` flips it. One method
does both jobs.

Note the Pause button stays on screen while paused — it just sits **underneath**
the dark backdrop, which soaks up the taps. On a phone it was too easy to hit by
accident.

#### BackToMenu() — GameManager.cs:401

**Goes back to the menu and freezes everything.**

Stops the spawners, stops the music, saves, clears the cave, resets the counters,
hides the game over panel, puts the fish back, shows the menu, freezes time.

This is **the only place the music stops**, which is what lets a retry carry
straight on without restarting the track.

#### ApplyCaveDepth() — GameManager.cs:666

**The deeper you swim, the dimmer the cave gets.** Only five lines, and one of
the most important methods in the game.

```csharp
CaveDepth = Mathf.Clamp01(Distance / darkestAtDistance);

if (caveLight == null) return;

caveLight.intensity = Mathf.Lerp(startLight, deepLight, CaveDepth);
```

**In the game:** this is the darkness.

- `Distance / darkestAtDistance` is how far through the journey you are. At 150m
  out of 300, that is 0.5.
- `Clamp01` keeps it between 0 and 1, so going past 300m does not make it darker still.
- `Lerp` means "mix". Lerp from 0.03 to 0.02 at 0.5 gives the number halfway between.

**The line order is deliberate.** `CaveDepth` is set **before** the null check.
So if nobody wired up the cave light, the fish's lamp still tracks depth correctly.
Putting the check first would have broken the lamp too.

#### AddCoin() — GameManager.cs:590

**The fish swam into a coin.**

```csharp
if (isGameOver) return;

Coins++;
TotalCoins++;
PlayerPrefs.SetInt(COINS_KEY, TotalCoins);
ShowCoins();

if (sfx != null && coinClip != null) sfx.PlayOneShot(coinClip);
```

Adds one to this run and one to your total, saves it, redraws the counter, plays
the ping. The coin sound is played **here**, not in CoinPickup, so a coin touched
on the frame you die stays silent.

#### TrySpendCoins(int amount) — GameManager.cs:607

**Takes coins off you, and says whether it worked.**

```csharp
if (amount <= 0 || TotalCoins < amount) return false;

TotalCoins -= amount;
PlayerPrefs.SetInt(COINS_KEY, TotalCoins);
PlayerPrefs.Save();

ShowCoins();
return true;
```

**In the game:** this is how you buy a fish.

The important thing is that checking and subtracting are **one step**. If they
were two separate methods, a caller that remembered to subtract but forgot to
check would hand out a fish for free. One method, no gap.

It hands back true or false so the shop knows whether the sale went through.

#### ShowCharacters(bool visible) — GameManager.cs:623

**Opens and closes the fish shop.** Reuses the pause screen's dark backdrop, so
the two look identical.

It also calls `ApplySkin()`, because closing the shop goes through neither
StartGame nor BackToMenu. Without that line the menu fish keeps its old look
after you buy a new one.

#### BuildSound() — GameManager.cs:482

**Sets up the two sound players and gives every button a click.**

```csharp
foreach (Button button in CanvasRoot().GetComponentsInChildren<Button>(true))
{
    if (button.GetComponent<ButtonPressSound>() == null)
    {
        button.gameObject.AddComponent<ButtonPressSound>();
    }
}
```

**In the game:** this is why every button in the game clicks, with nobody having
to wire up 20 buttons by hand.

The `true` inside the brackets means "include switched-off ones". That matters:
the shop shelf is switched off when the game loads, so without the `true` the
shop buttons would be silent.

#### BuildCoinIcon(TMP_Text target, float size) — GameManager.cs:708

**Draws the coin picture to the left of a number**, so it reads as coins and not
as a second score. Built in code, so there is no second coin image to keep in sync.

It also sets `raycastTarget = false`, for the same reason as the counters:
never swallow a tap meant for the fish.

#### The small ones

| Method | Line | What it does in the game |
|---|---|---|
| `BuildLegalButton()` | 257 | Adds the Terms and Privacy button to the menu, bottom right. |
| `PlayerStart()` | 436 | Works out the spot the fish starts from, as a share of the screen width. |
| `ShowRunButtons()` | 444 | Shows or hides the pause screen's buttons. |
| `ShowPauseDim()` | 467 | Switches the dark blurry backdrop on or off. |
| `PreloadClips()` | 519 | Loads the sounds up front, so the first play has no stutter. |
| `PlayClick()` | 529 | Plays the button click. |
| `PlayMovementPop()` | 535 | Plays the little pop the fish makes when it swims. |
| `ToggleSound()` | 542 | Turns the sound on or off, and saves the choice. |
| `ApplySound()` | 554 | Applies mute and swaps **both** sound buttons' pictures. |
| `CanvasRoot()` | 568 | Finds the Canvas all the UI sits on, by walking **up** the tree. |
| `OnDestroy()` | 575 | Hands the blur picture's memory back. It is not freed on its own. |
| `ClearObstacles()` | 581 | Deletes every rock, coin and bit of rubbish left in the cave. |
| `ResetDistance()` | 645 | Puts the metre counter back to zero. |
| `ShowDistance()` | 657 | The **one and only** place the distance text is written. |
| `ShowGameOver()` | 678 | Fills in the game over screen's three numbers. |
| `ResetCoins()` | 688 | Clears **this run's** coins. Your saved total keeps climbing. |
| `ShowCoins()` | 696 | The **one and only** place the coin counter is written. |
| `BuildWalletCounter()` | 742 | Makes the shop's coin total match the in-game one, same size and picture. |
| `ShowRunCounters()` | 752 | Shows or hides both run counters **together**. |

> **Why does "the one and only place" keep coming up?** Because two methods
> writing to one piece of text is what made the distance counter flicker, and
> one counter saved ON while the other was saved OFF is what made them disagree.
> Writing each thing in exactly one place is the fix for a whole class of bug.

---

## 2. CaveSpawner.cs — walls that can never close up

**323 lines.**

It builds the cave: two endless rows of rocks, one hanging from the ceiling and
one standing on the floor, always leaving an opening the fish can swim through.

The shapes are random. **The safe gap is not.**

### The settings — Space for the fish

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Rock Prefabs | 5 rocks | The rocks to pick from. Floor rocks are the same ones turned upside down. |
| **Min Gap** | **3.6** — script says 3 | The size of the opening at the mouth of the cave, where every run begins. |
| Deep Gap | 2.6 | The size of the opening once the cave is at its darkest. |
| **Min Depth** | **2.5** — script says 2 | Every rock sticks out at least this far, so none is a useless little sliver. |
| **Max Depth Change** | **1** — script says 1.2 | How much bigger or smaller a rock can be than the one before it. |
| **Floor Y** | **-5** — script says -4 | The height of the top of the Ground. Floor rocks stand on this line. |

Deep Gap **must** be smaller than Min Gap, or the cave never closes in.

For scale: the fish is **1.33** tall, and the part that can actually hit something
is **1.11**. So a 3.6 gap is about three fish, and a 2.6 gap is about two.

### The settings — Size limits

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| **Min Scale** | **0.55** — script says 0.35 | The smallest a rock may be stretched to. |
| Max Scale | 1.2 | The biggest a rock may be stretched to. |
| **Max Width** | **10** — script says 8 | The widest one rock may be. The screen is about 17.8 wide. |
| Overlap | 0.2 | How far each rock tucks under its neighbour, so you never see a join. |

Rocks are **stretched** to reach the depth they were given, so they always touch
their edge. These two limits stop the stretching going silly.

### The numbers that cannot be changed

These are written into the code as `const`, which means fixed forever. You cannot
type in them in Unity.

| Name | Value | What it does in the game |
|---|---|---|
| `ABSOLUTE_MIN_GAP` | 2.2 | The smallest opening allowed, **whatever the settings say**. |
| `ROW_START_MARGIN` | 0.5 | The rows start just past the right edge, so rocks slide into view. |
| `FORGET_MARGIN` | 5 | How far back a rock goes before the script stops tracking it. |

> **Why is 2.2 locked away like that?** Because it is a safety rule, not a taste
> setting. It is twice the fish's 1.11. Below that, getting through stops being
> skill and becomes luck — and on a phone your own thumb is covering the fish.
> Locking it in code means no amount of fiddling in Unity can make the game unfair.

> **Why warn instead of quietly fixing it?** If Deep Gap is set too low, the game
> prints a warning **and** clamps it. A silent rescue is how you ship a cave
> nobody fits through that looks deliberate.

### The two helper classes inside

CaveSpawner has two small classes of its own.

**`Rock`** — one rock that has been placed. It remembers:

| Field | What it holds |
|---|---|
| `rockTransform` | Where the rock is in the world. |
| `halfWidth` | Half its width, so Left and Right are easy to work out. |
| `depth` | How far it sticks out into the cave. |
| `Left` `Right` | Its two edges, worked out from the two above. |

**`Row`** — one row of rocks, ceiling or floor. It remembers:

| Field | What it holds |
|---|---|
| `isTop` | Is this the ceiling row or the floor row? |
| `rocks` | The list of rocks in this row. |
| `last` | The most recent rock. The next one goes right after it. |
| `lastIndex` | Which rock shape was used last, so it is not used twice running. |
| `startX` | Where the row begins. |
| `RightEdge` | How far right the row has been built. |

A row only needs its **last** rock to know where the next one goes. That is why
this is cheap to run every frame.

### The methods

#### StartSpawning() — CaveSpawner.cs:94

**Starts building rocks for a new run**, with both rows parked just off screen.

GameManager decides when. That is why nothing appears while you are on the menu.

#### StopSpawning() — CaveSpawner.cs:119

**Stops building new rocks.** GameManager clears the ones already out there.

#### Update() — CaveSpawner.cs:127

**Keeps both rows built past the right edge, and forgets what is gone.**

```csharp
if (!spawning) return;

Forget(topRow);
Forget(bottomRow);

float fillTo = CameraEdges.Right + maxWidth;
FillRow(topRow, bottomRow, fillTo);
FillRow(bottomRow, topRow, fillTo);
```

Look at the two `FillRow` calls. Each row is handed **the other row** as well as
itself. That is the heart of the whole thing: every rock is measured against the
opposite wall, so the two can never close up.

#### CurrentGap() — CaveSpawner.cs:168

**How big the opening should be right now.** Three lines, and one of the best
decisions in the project.

```csharp
float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
return Mathf.Max(Mathf.Lerp(minGap, deepGap, depth), ABSOLUTE_MIN_GAP);
```

**In the game:** the walls close in as the cave gets darker.

It reads `CaveDepth` — the **same** 0-to-1 number that dims the light. So the
walls narrowing and the screen darkening happen together, off one number.

The player reads **one** signal getting worse, not two systems to balance.

`Mathf.Max(..., ABSOLUTE_MIN_GAP)` is the safety net: however the settings are
tuned, the gap can never come out below 2.2.

#### SpawnRock(Row row, Row otherRow) — CaveSpawner.cs:178

**Places one rock.** The longest method in the script, and the cleverest.

The order of the steps is what makes it safe:

```csharp
float gap = CurrentGap();
float budget = CameraEdges.Top - floorY - gap;

float taken = DeepestBetween(otherRow, startX, startX + spriteSize.x * maxScale);
float allowed = Mathf.Min(budget - taken, budget - minDepth);
```

Read that as a sum:

1. **budget** — the total height of the screen, minus the opening the fish needs.
   That is all the space the two rows are allowed to share.
2. **taken** — how much of it the opposite row has **already used** right here.
3. **allowed** — what is left for this rock.

So the opposite wall is measured **first**, before anything is placed. The rock
can only ever use what is genuinely spare.

Then it picks a depth near the last rock, so the wall flows instead of jumping:

```csharp
float depthChange = maxDepthChange * (gap / minGap);
float low  = Mathf.Max(minDepth, previous - depthChange);
float high = Mathf.Min(allowed,  previous + depthChange);
float wanted = Random.Range(Mathf.Min(low, high), Mathf.Max(low, high));
```

That first line is subtle and worth knowing. **The allowed jump shrinks as the
gap shrinks.** A 1-unit step is a third of a 3.6 gap but nearly half of a 2.6 one,
and the fish climbs no faster. Without that line the deep cave zig-zags too fast
to be fair.

One more detail, at the end:

```csharp
float mirror = Random.value < 0.5f ? -1f : 1f;
float upsideDown = row.isTop ? 1f : -1f;
rock.transform.localScale = new Vector3(scale * mirror, scale * upsideDown, 1f);
```

Rocks are flipped with a **negative size**, not with Unity's Flip Y tickbox.
The tickbox only flips the picture, which would leave invisible walls in the
wrong places. A negative size flips the hit-box too.

#### DeepestBetween(Row otherRow, float left, float right) — CaveSpawner.cs:241

**How far the other row sticks out at its deepest**, anywhere along the stretch a
new rock would cover.

This is the measuring tape. It is what makes "the walls can never meet" true.

#### TryGetGapAt(float x, out float low, out float high) — CaveSpawner.cs:260

**Says where the clear opening is at a given spot.**

```csharp
low  = floorY;
high = CameraEdges.Top;

if (!RowReaches(topRow, x) || !RowReaches(bottomRow, x)) return false;

low  += DeepestBetween(bottomRow, x, x);
high -= DeepestBetween(topRow, x, x);

return high > low;
```

**In the game:** this is how rubbish and coins land in the cave instead of inside
a rock.

It hands back `false` until both rows are actually built that far ahead. The
spawners then just skip that turn, which is why you sometimes get a small stretch
with no rubbish — far better than a coin buried in a wall.

This method is **public**, because TrashSpawner, CoinSpawner and ObstacleDrift all
ask it the same question.

#### PickRock(int lastIndex) — CaveSpawner.cs:288

**Picks a random rock, never the one this row used last.**

```csharp
int index = Random.Range(0, rockPrefabsArray.Length - 1);
if (index >= lastIndex) index++;
return index;
```

A neat trick worth explaining if asked. It picks from a list **one shorter**, then
skips past the last one used. So every other rock is equally likely, and there is
no re-rolling in a loop.

The obvious way — pick one, and pick again if it matches — can in theory loop
forever. This cannot.

#### Forget(Row row) — CaveSpawner.cs:142

**Throws away notes on rocks that are long gone.**

```csharp
for (int i = row.rocks.Count - 1; i >= 0; i--)
```

Two things here are deliberate. It counts **backwards**, so removing one does not
shuffle the rest. And it is looped by hand rather than using `RemoveAll`, because
`RemoveAll` needs a throwaway object every call and this runs twice a frame —
enough rubbish to make the game stutter.

Deleting the rock **object** is `ObstacleMovement`'s job. This only forgets the note.

#### The small ones

| Method | Line | What it does in the game |
|---|---|---|
| `FillRow()` | 156 | Adds rocks to one row until it reaches far enough right. |
| `RowReaches()` | 274 | True once this row has a rock covering a given spot. |
| `HasValidRocks()` | 303 | Checks every rock is usable **before** the run starts. |

`FillRow` has a safety net worth noticing: it loops at most 10 times. A rock with
no width would otherwise loop forever and freeze the game.

`HasValidRocks` checks the **pictures** too, not just the rocks. `SpawnRock`
measures the picture after making the rock, so an empty slot would crash every
single frame. Checking first means one clear error message instead.

---

## 3. PlayerController.cs — why this is not Flappy Bird

**224 lines.**

The fish you control. Reads your taps, swims up, sinks down, tilts, and wears
whichever skin you picked in the shop.

This was the hardest part of the project. The tap was tuned three times and it
still felt like a bird game.

**The problem was never the tap. It was the fall.** A bird drops like a stone.
A fish should sink.

### The settings

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| **Swim Force** | **4.6** — script says 4 | How far up the fish swims on each tap. Lower means smaller hops. |
| **Water Gravity** | **1** — script says 1.2 | How strongly the fish is pulled down. Lower is floatier. |
| **Water Drag** | **1.5** — script says 1 | How thick the water feels. **This is what stops it feeling like a bird.** |
| **Max Sink Speed** | **3.2** — script says 4.5 | The fastest the fish may sink. |
| **Current Push** | **9** — script says 3 | How hard a surge shoves the fish backwards. 0 turns currents off. |
| **Current Return** | **2.5** — script says 4 | How hard the fish swims back to its spot. Higher means it gets shoved less. |
| **Rotation Speed** | **10** — script says 5 | How much the fish tips for a given speed. |
| **Max Tilt Up** | **20** — script says 25 | The furthest it may point upwards, in degrees. |
| **Max Tilt Down** | **-40** — script says -45 | The furthest it may point downwards, in degrees. |
| Tilt Smoothing | 5 | How quickly it swings round to its new angle. Lower is smoother. |
| Skins | 5 fish | One fish per shop shelf slot, in shelf order. Slot 0 is the starting fish. |

**Nine of these eleven differ from the script.** This is the script to be most
careful about if someone reads numbers off the screen.

> **Current Push is the big one.** The script default of 3 gave only **0.23 units**
> of shove. The fish is **1.09** wide, so that was a fifth of a body — the
> mechanic the team was proudest of was almost switched off and nobody noticed,
> because nobody measured it. It was raised to **9**, a full body width. Now you
> feel it.

> **What actually makes it feel underwater?** Drag and the sink cap. Drag makes it
> glide and slow down instead of snapping about. The cap stops one missed tap
> costing you half a gap. Dropping the sink speed from 4.6 to 3.2 made it
> **25% less punishing** and it still dives.

### The numbers the game works out for itself

| Name | Value | What it holds |
|---|---|---|
| `rb` | — | The Rigidbody2D: Unity's physics body for the fish. |
| `IsAlive` | true at start | Whether the fish is still alive. |
| `SwamThisFrame` | — | True **only** on the frame the fish actually swims. |
| `homeX` | — | The spot the fish swims back to after a current shoves it. |
| `homeY` | — | The height the fish bobs around on the menu. |
| `fishRenderer` | — | What draws the fish. Found, not wired. |
| `IDLE_BOB_HEIGHT` | 0.12 | How far it floats up and down on the menu. |
| `IDLE_BOB_SPEED` | 1.4 | How fast it bobs. |
| `IDLE_TILT` | 3 | How far it tips while bobbing, in degrees. |

> **Why are the three bob numbers `const` and not Unity settings?** This is a
> real Unity trap and a good thing to know. A **new** setting added to a script
> the scene has **already saved** loads as **0**. And 0 here means a completely
> still fish, which is the exact bug this code was written to fix. Writing them
> into the code means the scene cannot overwrite them with nothing.

### The methods

#### Awake() — PlayerController.cs:82

**Finds the physics body and the picture, then applies the water settings.**

```csharp
rb = GetComponent<Rigidbody2D>();
fishRenderer = GetComponentInChildren<SpriteRenderer>();

rb.freezeRotation = true;

rb.gravityScale  = waterGravity;
rb.linearDamping = waterDrag;
```

Those last two lines matter more than they look. The water settings are applied
**here, in code**, so these numbers beat whatever the scene saved on the
Rigidbody. The rule for this project: tune the fish on **this script**, never on
its Rigidbody 2D.

`freezeRotation = true` stops physics spinning the fish when it bumps something.
The tilt is handled by hand instead, further down.

#### Update() — PlayerController.cs:102

**Checks whether you asked to swim, and kicks the fish upwards.**

```csharp
SwamThisFrame = false;

if (!IsAlive) return;

if (Time.timeScale == 0f)
{
    MenuIdle();
    return;
}

if (!SwimPressed()) return;

rb.linearVelocityY = swimForce;
SwamThisFrame = true;

if (GameManager.Instance != null) GameManager.Instance.PlayMovementPop();
```

**In the game:** this is the one control in the whole game.

Three guards, in order, each stopping a real bug:

1. Dead fish do not swim.
2. Frozen game means a tap is a **button press**, not a swim. The menu, the pause
   screen and the game over screen are all frozen, so this one check covers all three.
3. `SwimPressed()` throws out taps that landed on a button.

`rb.linearVelocityY = swimForce` **sets** the upward speed rather than adding to
it. That is why tapping fast does not launch the fish off the top of the screen.

#### SwimPressed() — PlayerController.cs:146

**Asks: did the player just tap?**

```csharp
if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
return false;
```

**In the game:** one finger, one job.

It says yes for the space bar, a mouse click, or a finger on glass — so the game
works on a computer and on a phone with no extra code.

**The first line is the clever bit.** If your finger landed on a button, it says
no. That is what stops tapping Pause from also making the fish swim.

`wasPressedThisFrame` means the moment it went down, not "is it held". Holding
your finger down does not make the fish fly.

#### FixedUpdate() — PlayerController.cs:158

**Does the physics.** Runs on the physics clock, not the frame clock.

```csharp
if (IsAlive)
{
    float push = (ObstacleMovement.Surge - 1f) * currentPush;
    float home = (homeX - transform.position.x) * currentReturn;
    rb.linearVelocityX = home - push;
}
```

**In the game:** this is the current shoving you, and the fish swimming back.

Read it as a tug of war:

- `push` is the current shoving the fish left. In calm water `Surge` is 1, so
  `(1 - 1) * 9` is **zero** — no shove at all.
- `home` is the fish swimming back. **The further from home, the harder it pulls.**

Because the pull grows with the distance, the two settle at a point where they
cancel out. That is why a current can only shove you so far and **never builds up**,
however long it lasts. No clamp needed; the maths does it.

```csharp
if (rb.linearVelocity.y < -maxSinkSpeed)
{
    rb.linearVelocityY = -maxSinkSpeed;
}
```

The sink cap. Four lines, and the single biggest thing that makes the fish feel
like a fish instead of a bird.

```csharp
float targetAngle = rb.linearVelocity.y * rotationSpeed;
targetAngle = Mathf.Clamp(targetAngle, maxTiltDown, maxTiltUp);

float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, tiltSmoothing * Time.fixedDeltaTime);
transform.rotation = Quaternion.Euler(0, 0, angle);
```

The tilt. Nose up when swimming, nose down when sinking, with limits so it never
flips over. `LerpAngle` swings it round gradually instead of snapping.

> **Why `FixedUpdate` and not `Update`?** Anything touching physics belongs in
> `FixedUpdate`, which runs on a steady clock. Physics in `Update` behaves
> differently on a fast phone and a slow one.

#### MenuIdle() — PlayerController.cs:127

**Floats the fish up and down on the menu**, so it looks like it is hanging in
water rather than switched off.

```csharp
float t = Time.unscaledTime * IDLE_BOB_SPEED;

Vector3 position = transform.position;
position.y = homeY + Mathf.Sin(t) * IDLE_BOB_HEIGHT;
transform.position = position;

transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t - Mathf.PI * 0.5f) * IDLE_TILT);
```

The menu freezes physics, so the fish is moved by hand here, on the **real** clock
(`unscaledTime`) rather than the game one.

The height is worked out **fresh from `homeY` every frame**, never nudged along.
A menu can sit for minutes, and tiny errors adding up is what once walked the
scrolling background out of line.

The tilt uses the same wave shifted a quarter turn, so the nose trails the body.

#### ResetPlayer(Vector3 startPosition) — PlayerController.cs:195

**Puts the fish back at the start:** right place, facing straight, not moving,
alive, wearing the right skin.

Both `StartGame` and `BackToMenu` call this, which is why the skin is applied here.

#### ApplySkin() — PlayerController.cs:212

**Puts on whichever fish you picked in the shop.**

```csharp
int skin = PlayerPrefs.GetInt(FishInventory.EQUIPPED_KEY, 0);

if (skin < 0 || skin >= skins.Length || skins[skin] == null) skin = 0;

if (skins[skin] != null) fishRenderer.sprite = skins[skin];
```

That middle line is the safety check. A saved choice that does not exist falls
back to the starting fish, rather than leaving the last run's picture on screen.

It reads the key name from `FishInventory.EQUIPPED_KEY` rather than typing the
name again, so the two files cannot disagree about what the save is called.

#### OnCollisionEnter2D(Collision2D collision) — PlayerController.cs:187

**Marks the fish dead the moment it bumps anything.** One line: `IsAlive = false;`

`PlayerHit` is what actually ends the run. This script only stops the fish
responding to taps.

---

## 4. ObstacleMovement.cs — one speed, written in one place

**Only 63 lines, and it holds the whole game together.**

It slides one thing leftwards and deletes it once it has left the screen. Rocks,
rubbish and coins all get one, and all read **the same speed**.

### The settings

| Setting | Value | What it does in the game |
|---|---|---|
| Speed | 5 | How fast things slide left. Set on the rock **prefabs**, not the scene. |

### The two shared numbers

These are the most important two numbers in the game.

| Name | Start | What it holds |
|---|---|---|
| `SpeedMultiplier` | 1 | How much faster the cave is moving than normal. |
| `Surge` | 1 | Just the wave part. 1 in calm water, bigger during a surge. |

Both are **static**, which means there is one copy shared by every rock, coin and
piece of rubbish in the game at once — not one each.

Both are also **read-only from outside**. Only one method can write them.

### The fixed number

| Name | Value | What it does in the game |
|---|---|---|
| `DESPAWN_MARGIN` | 1 | Far enough off screen that you never see a rock pop out of existence. |

### The methods

#### SetPace(float speedMultiplier, float surge) — ObstacleMovement.cs:28

**The speed of the whole cave, changed in one place only.**

```csharp
public static void SetPace(float speedMultiplier, float surge)
{
    SpeedMultiplier = speedMultiplier;
    Surge = surge;
}
```

Two lines. This is the single most important design decision in the project.

**In the game:** rocks, rubbish and coins cannot drift out of step, because there
is nothing for them to drift out of step **with**. They all read one number.

> **Why lock it down like that?** It leaked between runs once. The project keeps
> shared values between Plays to start faster, so stopping Play mid-surge left the
> **next** run starting mid-surge. A second writer would have been near impossible
> to spot. One method writes it, and it is reset before the game loads.

#### ResetStatics() — ObstacleMovement.cs:38

**Puts the cave's speed back to calm before the game loads.**

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
    SpeedMultiplier = 1f;
    Surge = 1f;
}
```

The line in square brackets is an **attribute**: it tells Unity to run this method
automatically, before anything else loads. This is the fix for that leak.

#### Update() — ObstacleMovement.cs:53

**Slides this object left, then deletes it once it is gone.**

```csharp
transform.position += Vector3.left * (speed * SpeedMultiplier) * Time.deltaTime;

float rightEdge = pieceRenderer != null ? pieceRenderer.bounds.max.x : transform.position.x;
if (rightEdge < CameraEdges.Left - DESPAWN_MARGIN)
{
    Destroy(gameObject);
}
```

`speed * SpeedMultiplier` is the whole difficulty curve, in one multiplication.
Base speed 5, multiplied by anything from 0.9 to 1.6, gives 4.5 up to 8.

It waits for the object's **right** edge, not its middle. A wide rock is up to 10
units across, so checking the middle would make half of it vanish while you could
still see it.

#### Awake() — ObstacleMovement.cs:45

**Remembers what draws this object**, because the check above needs it.

---

# Part 2 — The supporting cast

Fourteen more scripts. Shorter sections, same format. Skim these once so you know
where to look.

---

## 5. TrashSpawner.cs — the difficulty curve lives here

**274 lines. Easy to miss, and it runs the whole game's difficulty.**

It drops floating rubbish for the fish to dodge. But it is **also in charge of
making the whole game harder** as the run goes on: the longer you last, the faster
the cave moves and the more rubbish comes.

If someone asks "where does the game get harder?", the answer is this file.

### The settings — Debris

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Trash Prefabs | the rubbish list | The bits of rubbish to pick from. |
| **Min Size** | **0.6** — script says 0.5 | The smallest a piece may be. Keep it under the fish's 1.33. |
| Max Size | 0.9 | The biggest a piece may be. |
| Min Spin | 10 | The slowest a piece spins, in degrees a second. |
| Max Spin | 45 | The fastest. Which way round is random. |

### The settings — Drift

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Drift Y | 0.4 | How far a piece may float up or down from where it was dropped. |
| **Drift Min Speed** | **0.1** — script says 0.15 | The slowest it floats about. |
| **Drift Max Speed** | **0.32** — script says 0.5 | The fastest it floats about. |

### The settings — Difficulty ramp

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| **Ramp Seconds** | **150** — script says 90 | Seconds of play before the game is as hard as it gets. |
| **Start Speed Multiplier** | **0.9** — script says 1.15 | How fast things slide at the **start**, as a multiple of the rocks' speed of 5. |
| Max Speed Multiplier | 1.6 | How fast things slide once it is as hard as it gets. |
| Start Interval | 3 | Seconds between pieces of rubbish at the start. |
| Min Interval | 0.9 | Seconds between pieces at full difficulty. Never quicker. |
| Max On Screen | 8 | Most pieces allowed on screen at once, however long the run lasts. |

So the speed goes from **0.9 × 5 = 4.5** up to **1.6 × 5 = 8** over **150 seconds**.

Start Speed Multiplier also quietly controls something else: how long you swim
through empty water before the first rocks. About 8.8 units of space, divided by
this speed. Turn it up and there is less empty swimming at the start.

### The settings — Currents

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Surge Strength | 1.3 | How strong a surge gets at full difficulty. 1 would mean no currents. |
| Surge Seconds | 2.5 | How long one surge takes to build and fade. |
| Surge Interval | 9 | Calm seconds between surges. **Always the same, never random.** |
| Debris Current Push | 1.2 | How much faster than the cave the rubbish drifts. |

> **Why is Surge Interval never random?** So you can learn the rhythm and plan
> around it. A random shove is just bad luck; a shove every 9 seconds is
> something you can get good at.

> **Careful above 1.4.** At full difficulty the cave already moves at 8 a second,
> and a surge cuts your thinking time as much as it adds.

### The fixed numbers

| Name | Value | What it does in the game |
|---|---|---|
| `CALM_PACE` | 1 | Still water. Multiplying the speed by 1 leaves it alone. |
| `CALM_SURGE` | 1 | Same, for the wave part. |
| `SURGE_AT_START` | 0.5 | Early surges get **half** the full strength, so they build up. |
| `SPAWN_MARGIN` | 1 | Rubbish is made this far past the right edge, so you never see it appear. |

### The methods

#### Update() — TrashSpawner.cs:136

**Runs the whole difficulty curve.** The most important method in the file.

```csharp
elapsed += Time.deltaTime;

float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
float ramped = Mathf.Lerp(startSpeedMultiplier, maxSpeedMultiplier, progress);

float surge = Surge(progress);
ObstacleMovement.SetPace(ramped * surge, surge);
```

**In the game:** this is the game getting harder.

- `progress` goes from 0 to 1 over 150 seconds, then stays at 1.
- `ramped` mixes 0.9 up to 1.6 along that progress.
- `surge` is the wave on top.

Then it publishes **one** shared speed. Notice `surge` is sent **twice**: once
multiplied into the speed, and once on its own. That second one is what lets the
**fish** feel the current, because `PlayerController` reads `Surge` directly.

```csharp
if (elapsed < nextSpawn) return;

nextSpawn = elapsed + Mathf.Lerp(startInterval, minInterval, progress);

if (livePieces.Count >= maxOnScreen) return;

SpawnPiece();
```

Two separate limits on the rubbish: a timer that speeds up, **and** a hard cap on
how many may be on screen. Either one alone is not enough.

#### Surge(float progress) — TrashSpawner.cs:164

**How strong the current is right now.** A wave passing through.

```csharp
float phase = elapsed % (surgeInterval + surgeSeconds);
if (phase < surgeInterval) return 1f;

float strength = Mathf.Lerp(1f + (surgeStrength - 1f) * SURGE_AT_START, surgeStrength, progress);

float t = (phase - surgeInterval) / surgeSeconds;
return 1f + (strength - 1f) * Mathf.Sin(t * Mathf.PI);
```

**In the game:** the shove that arrives every 9 seconds.

- The `%` sign means "remainder". It makes the clock repeat every 11.5 seconds
  (9 calm plus 2.5 of surge). That is the rhythm.
- The whole thing is worked out **purely from the clock**. Nothing is remembered
  between frames, so it cannot drift or get stuck on.
- `Mathf.Sin(t * Mathf.PI)` draws a smooth arch from 0 up to 1 and back to 0.
  That is why the surge swells and fades instead of snapping on like a switch.
- The strength grows on the **same** difficulty counter as the speed. Shallow
  water shoves a bit; the deep cave shoves twice as hard.

#### SpawnPiece() — TrashSpawner.cs:186

**Drops one piece just off the right edge, inside the clear gap.**

```csharp
float radius = new Vector2(spriteSize.x, spriteSize.y).magnitude * scale * 0.5f;
float clearance = radius + driftY;

float x = CameraEdges.Right + SPAWN_MARGIN;
if (!rockSpawner.TryGetGapAt(x, out float low, out float high)) return;
if (high - low < clearance * 2f) return;
```

`magnitude` gives the **corner-to-corner** length, not the width. That matters
because a piece **spins** while it floats, so its widest reach is its diagonal.

`clearance` is the piece **plus how far it may wander**, so it can never drift
into a rock later.

The two `return` lines give up quietly: if the rocks are not built that far yet,
or the gap is too narrow, no piece appears. Better a small empty stretch than a
piece buried in a wall.

Then the piece is assembled from parts:

```csharp
piece.AddComponent<ObstacleMovement>();
piece.AddComponent<TrashSpin>().SetSpin(spin);

ObstacleDrift drift = piece.AddComponent<ObstacleDrift>();
drift.Configure(driftY, driftMinSpeed, driftMaxSpeed);
drift.ConfigureCurrent(rockSpawner, debrisCurrentPush, radius);
```

**In the game:** one piece of rubbish is four small scripts stacked up — slide,
spin, bob, and get carried by the current. Each does one job.

#### StopSpawning() — TrashSpawner.cs:112

**Stops dropping rubbish and puts the cave back to calm.**

```csharp
spawning = false;
ObstacleMovement.SetPace(CALM_PACE, CALM_SURGE);
```

That second line is essential. The speed is shared by the whole game, so without
it the difficulty would survive into your next run.

#### The small ones

| Method | Line | What it does in the game |
|---|---|---|
| `Awake()` | 94 | Finds the rock spawner, which owns the gap rubbish lands in. |
| `StartSpawning()` | 100 | Starts dropping rubbish, difficulty back at the start. |
| `ResetRun()` | 120 | Wipes the run's progress and resets the cave's speed. |
| `PickTrash()` | 238 | Picks a random piece, never the same one twice running. |
| `HasValidTrash()` | 253 | Checks every piece is usable before the run starts. |

`ResetRun` resets the **surge** as well as the speed. `Update` would overwrite it
next frame anyway, but an old value sitting in something called "the current
surge" makes both numbers untrustworthy while you are debugging.

---

## 6. CoinSpawner.cs — the only thing you are allowed to touch

**182 lines.**

It drops coins into the gap between the rocks. It sits on the **same object** as
CaveSpawner, so clearing the rocks clears the coins at the same time.

It does **not** speed coins up. Coins carry an `ObstacleMovement` that reads the
one shared cave speed. Only how **often** they appear changes here.

### The settings

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Coin Prefab | the coin | The coin to copy. Its collider **must** stay on Is Trigger. |
| Coin Size | 0.7 | How big a coin is. Smaller than the fish's 1.33. |
| Spin Degrees | 25 | How fast a coin spins on the spot. 0 for no spin. |
| Drift Y | 0.4 | How far a coin may float up or down. |
| Wall Gap | 0.35 | How much clear space to leave between a coin and the rocks. |
| **Drift Min Speed** | **0.1** — script says 0.15 | The slowest a coin floats about. |
| **Drift Max Speed** | **0.32** — script says 0.5 | The fastest. |
| **Ramp Seconds** | **150** — script says 90 | Seconds before coins come as fast as they ever will. Matches TrashSpawner. |
| Start Interval | 2.5 | Seconds between coins at the start of a run. |
| Min Interval | 1 | Seconds between coins at the fastest. One a second at most. |
| Max On Screen | 5 | Most coins allowed on screen at once. |
| Coin Current Push | 1.2 | How much faster than the cave coins drift. |

> **"Its collider MUST stay set to Is Trigger."** This is the single most
> important sentence in the file. A trigger means **touch**. A normal collider
> means **bump**, and `PlayerHit` ends the run on any real bump — so as a normal
> collider you would **die collecting a coin**.

### `CoinSprite` — CoinSpawner.cs:52

Not a method, a **property**. It hands back the coin's picture.

```csharp
SpriteRenderer renderer = coinPrefab.GetComponent<SpriteRenderer>();
return renderer != null ? renderer.sprite : null;
```

**In the game:** it lets the coin counter at the top of the screen use the same
picture as the real coins, with no extra Inspector slot to fill in or get wrong.

### The methods

#### SpawnCoin() — CoinSpawner.cs:125

**Puts one coin just off the right edge, inside the clear gap.**

Almost the same as TrashSpawner's version, with one extra term:

```csharp
float clearance = radius + driftY + wallGap;
```

The coin, **plus** how far it may wander, **plus** a gap to keep from the rocks.

Coins get that extra `wallGap` and rubbish does not. That is deliberate: a coin
hugging a rock is a trap, because reaching for it kills you. Rubbish is already
something you avoid, so it can sit closer to the wall.

Then it builds the coin from parts:

```csharp
coin.AddComponent<ObstacleMovement>();
coin.AddComponent<TrashSpin>().SetSpin(spinDegrees * (Random.value < 0.5f ? -1f : 1f));

ObstacleDrift drift = coin.AddComponent<ObstacleDrift>();
drift.Configure(driftY, driftMinSpeed, driftMaxSpeed);
drift.ConfigureCurrent(rockSpawner, coinCurrentPush, radius, wallGap);
coin.AddComponent<CoinPickup>();
```

Note it reuses `TrashSpin` for the coin's spin. A spin is a spin; there was no
reason to write a second script for it.

#### Update() — CoinSpawner.cs:102

**Counts down to the next coin and drops one when the timer runs out.**

```csharp
liveCoins.RemoveAll(coin => coin == null);
```

Collected coins leave **holes** in the list, so they are cleared out. Without this
the "most coins on screen" cap would count coins you already collected, and
eventually no new coins would appear at all.

#### The small ones

| Method | Line | What it does in the game |
|---|---|---|
| `Awake()` | 75 | Finds the rock spawner — the only thing that knows where the gap is. |
| `StartSpawning()` | 83 | Starts dropping coins for a new run. |
| `StopSpawning()` | 95 | Stops dropping new coins. GameManager clears the rest. |
| `HasValidCoin()` | 164 | Checks the coin is usable before the run starts. |

`StartSpawning` is written so a badly set-up coin just **does not start**. You get
a run with no coins, instead of an error message every single frame.

---

## 7. SpotLight2D.cs — the lamp on the fish's head

**99 lines.**

The glowing bulb hanging off the anglerfish's head. It lights a small pool of
cave, gets brighter the deeper you go, wobbles so it looks alive, and flashes
each time you swim.

**Every setting in this script matches the scene.** Nothing to catch you out here.

### The settings

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Deep Multiplier | 1.6 | How much brighter the bulb gets at the cave's darkest. 1 means depth changes nothing. |
| Pulse Boost | 0.3 | Extra brightness for one swim. 0 turns the flash off. |
| Recover Speed | 7 | How fast the flash fades. Higher fades quicker. |
| Flicker Depth | 0.16 | How much the bulb wobbles on its own. 0 makes it a steady lamp. |
| Flicker Speed | 6.5 | How fast the wobble runs. Higher is twitchier. |

> **Why does it wobble at all?** A real anglerfish bulb is part of the animal.
> Light that holds perfectly still looks like a torch taped to a fish.

### The hidden numbers

| Name | What it holds |
|---|---|
| `lure` | The Light 2D this script controls. |
| `swimmer` | The fish. Found by looking **up** the tree, not wired in Unity. |
| `authoredIntensity` | The brightness you typed on the Light 2D, read once, treated as 100%. |
| `level` | 1 means exactly that brightness. Depth pushes it up; a swim flashes it. |

> **`authoredIntensity` is there because of a real bug that cost two sessions.**
> The old version wrote brightness **from its own field**, so the Intensity slider
> on the Light 2D did nothing once the game started. Now the slider is read once
> in `Awake` and treated as the bulb's 100%. It is only ever **nudged**, never
> replaced. Because it is read in `Awake`, set it with the game **stopped**.

### The methods

#### Update() — SpotLight2D.cs:74

**Works out how bright the bulb should be.** The whole script, really.

```csharp
float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
float resting = Mathf.Lerp(1f, deepMultiplier, depth);
float peak = resting + pulseBoost;

if (swimmer != null && swimmer.SwamThisFrame)
{
    level = Mathf.Min(level + pulseBoost, peak);
}

level = Mathf.Lerp(level, resting, Time.deltaTime * recoverSpeed);

float flicker = 1f + (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0f) - 0.5f) * 2f * flickerDepth;

lure.intensity = authoredIntensity * level * flicker;
```

Four things happen, and each is worth knowing:

**1. It reads `CaveDepth`** — the same one number as the cave light and the rock
gap. So as the cave darkens, the lamp brightens. They cannot drift out of step.

**2. It watches `SwamThisFrame`, not taps.** This is the neat bit. By watching the
**fish's** swim rather than the player's finger, it inherits every one of the
fish's rules for free: no flash while dead, while paused, on the menu, or when the
tap hit a button. Nothing had to be written twice.

**3. `Mathf.Min(level + pulseBoost, peak)` is a clamp — and it is a bug fix.**

> **This is the best bug in the project to talk about.** Making the flare
> depth-aware changed a bounded line into `level += pulseBoost`. Tapping is the
> one input a player can spam without limit, and mashing reached **9 times** the
> intended peak — enough to take the darkness out of the game completely.
> Clamping fixed it, and the fix was proved by simulation: **1.25** against a
> 1.30 ceiling when clamped, **11.7** when not.

**4. `PerlinNoise`, not `Random`.** Random jumps about and looks like a loose
wire. Perlin noise wanders smoothly and looks alive. It is centred on 1, so
setting Flicker Depth to 0 changes nothing at all.

Also note `Time.unscaledTime` for the flicker but `Time.deltaTime` for the fade.
The flicker carries on while paused; the flash **holds** instead of quietly fading
out behind the pause screen.

#### Awake() — SpotLight2D.cs:47

**Grabs the light and the fish, and saves your Intensity as 100%.**

```csharp
swimmer = GetComponentInParent<PlayerController>();
```

The lamp is attached **under** the fish, so the script just looks upwards for it.
No Inspector slot to fill in, and nothing to come unplugged.

If either the light or the fish is missing, it prints a clear error saying exactly
what to do about it, rather than failing silently.

---

## 8. ObstacleDrift.cs — making debris look alive

**119 lines. No Unity settings at all** — the spawners hand it everything.

It makes a piece of rubbish bob up and down as it slides past, so it looks like it
is floating instead of riding an invisible rail. It only moves **up and down**;
sideways is `ObstacleMovement`'s job.

### The fixed numbers

| Name | Value | What it does in the game |
|---|---|---|
| `MIN_SECONDS_UNTIL_TURN` | 1.5 | The shortest a piece keeps going one way before choosing again. |
| `MAX_SECONDS_UNTIL_TURN` | 3.5 | The longest. |

### The hidden numbers

| Name | What it holds |
|---|---|
| `allowedDrift` | How far this piece may wander from where it was dropped. |
| `driftOffset` | How far it has wandered so far. |
| `driftVelocity` | How fast it is wandering, and which way. |
| `minDriftSpeed` `maxDriftSpeed` | The range it picks a new speed from. |
| `secondsUntilTurn` | Time left before it picks a new direction. |
| `rockSpawner` | Where to ask about the gap. Empty means ignore the current. |
| `currentPush` | How much faster than the cave this piece is carried. |
| `pieceRadius` | How big the piece is, corner to corner. |
| `wallGap` | Extra space to keep from the rocks. Coins use this; rubbish does not. |

### The methods

#### Configure(float allowed, float minSpeed, float maxSpeed) — ObstacleDrift.cs:28

**Sets how far and how fast this piece may wander, then starts it off.**
The spawner calls this straight after making the piece.

#### ConfigureCurrent(CaveSpawner rocks, float push, float radius, float wallGap) — ObstacleDrift.cs:38

**Signs this piece up to be carried by the current.** Without this call, the piece
just bobs and stays put against the rocks.

Two separate setup methods, so a piece can bob **without** being carried. That
keeps the current optional rather than compulsory.

#### PickVelocity() — ObstacleDrift.cs:49

**Picks a new up or down speed, and how long to stick with it.**

```csharp
driftVelocity = Random.Range(minDriftSpeed, maxDriftSpeed) * (Random.value < 0.5f ? -1f : 1f);
secondsUntilTurn = Random.Range(MIN_SECONDS_UNTIL_TURN, MAX_SECONDS_UNTIL_TURN);
```

**Every piece rolls its own.** That is what stops all the rubbish moving together
like one big sheet.

#### Update() — ObstacleDrift.cs:56

**Moves the piece up or down, turning it round at the edge of its box.**

```csharp
transform.position += new Vector3(0f, next - driftOffset, 0f);
driftOffset = next;
```

That line adds the **change** in height, not the height itself. If it set the
position directly, this script and `ObstacleMovement` would fight over the same
position every frame and the piece would judder.

It also flips direction at the edge. Without that, every piece piles up against
its own limit and the wall of rubbish ends up looking flat.

#### CarryForward() — ObstacleDrift.cs:85

**Pushes the piece along faster than the cave, harder during a surge.**

```csharp
transform.position += Vector3.left * (currentPush * ObstacleMovement.Surge) * Time.deltaTime;

if (!rockSpawner.TryGetGapAt(transform.position.x, out float low, out float high)) return;

float margin = pieceRadius + wallGap;
if (high - low < margin * 2f) margin = pieceRadius;
if (high - low < margin * 2f) return;

float held = Mathf.Clamp(p.y, low + margin, high - margin);
```

**In the game:** the rubbish gets swept along by the current, and never ends up
inside a rock.

Because the piece moves **ahead** of the rocks, it arrives somewhere nobody
checked was safe. So the gap is measured **every frame** and the piece is kept
inside it.

Those two `if` lines are a careful climb-down. If the gap is too narrow for the
piece plus its wall gap, it drops the wall gap and tries again with just the
piece. If even that will not fit, it gives up on holding it clear rather than
giving up on the piece. Without that, the maths flips and shoves it straight
through the far wall.

And at the end:

```csharp
bool wallBelow = held > p.y;
bool headingIntoWall = wallBelow ? driftVelocity < 0f : driftVelocity > 0f;
if (headingIntoWall) driftVelocity = -driftVelocity;
```

If the piece had to be pushed off a wall, it also turns round. Without that, next
frame it drives straight back into the wall and buzzes against it like a fly on a
window.

---

## 9. PlayerHit.cs — the two ways to die

**48 lines.**

Ends the run. There are exactly two ways to die: bump into something, or sink off
the bottom of the screen.

### The settings

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Below Screen Margin | 1 | How far below the screen the fish must sink before the run ends. |

Why a margin at all? So the fish fully disappears before you see GAME OVER.
Ending it the instant the fish touches the bottom edge looks like a glitch.

### The methods

#### Update() — PlayerHit.cs:22

**Watches for the fish sinking off the bottom.**

```csharp
if (Time.timeScale == 0f) return;
if (mainCamera == null) return;

float screenBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;

if (transform.position.y < screenBottom - belowScreenMargin)
{
    EndRun();
}
```

**In the game:** this is why sinking kills you.

**There is no floor in this game.** Without this check the fish would fall
forever and the run would never end — the game would just hang with an empty
screen. Worth saying out loud, because it is not obvious.

#### OnCollisionEnter2D(Collision2D collision) — PlayerHit.cs:36

**The fish bumped a rock or a piece of rubbish.** One line: `EndRun();`

Unity calls this method by itself when two colliders touch. Nothing has to ask it to.

#### EndRun() — PlayerHit.cs:42

**Both ways of dying come through here**, so a run ends one way only.

```csharp
if (GameManager.Instance == null) return;
GameManager.Instance.GameOver();
```

Two ways in, **one** way out. If each way called `GameOver()` itself, a change to
how a run ends would have to be made twice — and one of the two would eventually
get missed.

---

## 10. CoinPickup.cs — collecting, not crashing

**27 lines. One method.** Added to every coin by CoinSpawner.

#### OnTriggerEnter2D(Collider2D collision) — CoinPickup.cs:11

**The fish touched this coin.**

```csharp
if (collected) return;

if (collision.GetComponent<PlayerController>() == null) return;

collected = true;

if (GameManager.Instance != null)
{
    GameManager.Instance.AddCoin();
}

Destroy(gameObject);
```

**`OnTriggerEnter2D`, not `OnCollisionEnter2D`.** That single word is the whole
reason collecting a coin does not kill you. A **trigger** means touch and pass
through. A **collision** means bump — and `PlayerHit` ends the run on any bump.

Two guards, both needed:

- `collected` stops one coin paying twice if the fish overlaps it across two frames.
- The `PlayerController` check means a piece of rubbish drifting through a coin
  does not collect it for you.

---

## 11. TrashSpin.cs — the smallest script in the game

**24 lines.** Spins a piece of rubbish on the spot, so it looks like it is
drifting in water. Coins use it too.

### The settings

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Degrees Per Second | set in code | How fast it spins. A minus number spins it the other way. |

This one is **not** in the scene, and that is correct — the spawners set it on
each piece as they drop it in, through `SetSpin`.

#### SetSpin(float degreesPerSecond) — TrashSpin.cs:13

**Gives this piece its own spin.** One line.

#### Update() — TrashSpin.cs:19

**Turns the piece a little every frame.**

```csharp
transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
```

`Time.deltaTime` is **game** time, so the spin freezes with everything else when
you pause. Using the real clock here would leave rubbish spinning on a frozen
screen, which looks broken.

---

## 12. ScrollObject.cs — the endless background

**156 lines.**

It slides a background picture leftwards forever. Several copies sit in a row, and
one that slides off the left goes to the back of the queue. A few pictures then
look endless.

### The settings

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Scroll Speed | 2 | How fast this background slides left. Every copy must use the same number. |

### The hidden numbers

| Name | Value | What it holds |
|---|---|---|
| `spriteRenderer` | — | What draws this copy. |
| `width` | — | How wide the picture is. Copied from the **first** copy. |
| `startX` | — | Where this copy started. |
| `copyCount` | — | How many copies are in the row. |
| `anchorX` | — | Where the queue starts. |
| `index` | — | This copy's place in the queue. |
| `startTime` | — | When the row started moving. Copied from the **first** copy. |
| `leftEdge` | — | Where the left edge of the screen is. The camera never moves. |
| `SEAM_OVERLAP` | 0.02 | How far the copies overlap, so no hairline crack shows. |
| `Step` | width − 0.02 | How far apart the copies sit. |

> **Why do the copies overlap by 0.02?** Even with a perfect join, the pixel grid
> leaves a hairline crack, and the pale camera background flashes through it.
> 0.02 units is about 2 pixels at 1080p. CaveSpawner overlaps rocks for exactly
> the same reason. There is even a note in the code saying to raise it if a line
> ever shows on a sharper screen.

### The methods

#### Update() — ScrollObject.cs:90

**Works out where this copy belongs, from the clock, every frame.**

```csharp
float span = Step * copyCount;

float travelled = Mathf.Repeat((Time.time - startTime) * scrollSpeed, span);
float x = anchorX + index * Step - travelled;

if (x + width / 2f <= leftEdge) x += span;
```

**In the game:** this is the moving background, and it is the fix for a bug that
took a while to find.

The position is **worked out fresh** every frame, never added up. Every copy reads
the same clock, so neighbours stay exactly as far apart forever.

> **The bug this fixed:** nudging each copy along a little every frame let tiny
> rounding errors creep in, and the gap between the copies grew a little more on
> every trip round the queue. Eventually you could see daylight between them.
> Working it out from the clock cannot drift, because there is nothing to add up.
> `PlayerController.MenuIdle` was rewritten the same way for the same reason.

The recycling check uses the copy's **right** edge passing the screen edge, not a
fixed distance. A fixed distance once wrapped a copy with 1.9 units still showing,
leaving a bare strip down the left edge.

#### Start() — ScrollObject.cs:55

**Lines this copy up in the row** and copies the width, speed and start time off
the first one.

```csharp
ScrollObject first = copies[0];
width = first.width;

scrollSpeed = first.scrollSpeed;
startTime = first.startTime;
```

Everything is taken from copy number one, so the copies **cannot disagree**. One
copy set to a different speed would rip the row apart.

`startTime` especially: copies can start a frame apart, and that one frame would
bake in a permanent gap.

#### AddMissingCopies(List<ScrollObject> copies) — ScrollObject.cs:113

**Makes copies until the row covers the screen, plus one spare.**

```csharp
float screenWidth = cam != null ? cam.orthographicSize * cam.aspect * 2f : 20f;
int needed = Mathf.CeilToInt(screenWidth / first.width) + 1;
```

Done **in code**, not in the Unity Editor, so a wider phone cannot open a gap and
nobody has to remember to add more copies by hand.

#### FindCopies() — ScrollObject.cs:135

**Collects the copies next to this one that use the same picture.**

```csharp
if (copy != null && child.gameObject.activeInHierarchy && copy.spriteRenderer.sprite == spriteRenderer.sprite)
```

Matching on the **picture** is what keeps two different backgrounds apart. Without
it, a far background and a near background would be treated as one row.

#### Awake() — ScrollObject.cs:44

**Measures how wide this copy is and notes when it started moving.** `Start()`
then replaces both with the first copy's values.

---

## 13. PauseBlur.cs — the frozen, blurry backdrop

**177 lines.**

The dark blurry picture behind the pause and game over screens. Two layers: a
blurry photo of the last frame, and a dark sheet on top that also soaks up taps
meant for the frozen game.

**Not a MonoBehaviour.** It is a plain C# class, so it is not attached to any
object in the scene. GameManager makes one when it is first needed.

### What it is given

| Name | What it holds |
|---|---|
| `canvasRoot` | Where to build the two layers. |
| `dimDarkness` | How dark the sheet is. GameManager passes 0.7. |
| `blurSteps` | How many times to halve the photo. GameManager passes 4. |
| `dim` | The dark sheet. |
| `blur` | The blurry photo layer. |
| `blurTexture` | The photo itself, in memory. |

Nothing is built in the constructor, so a run where you never pause never pays for
any of this.

### The methods

#### Show(bool visible, params GameObject[] raiseAbove) — PauseBlur.cs:29

**Darkens the frozen game and soaks up taps meant for it.**

```csharp
dim.transform.SetParent(canvasRoot, false);
Stretch(dim);
dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, dimDarkness);
```

The sheet goes on **the Canvas itself**, not on whatever holds the Pause button.
Once the buttons were grouped into panels, a dark sheet inside one group could not
cover the others.

```csharp
blur.transform.SetAsLastSibling();
dim.transform.SetAsLastSibling();

foreach (GameObject target in raiseAbove)
{
    Raise(target);
}
```

**In the game:** this is the layer order. Unity's UI draws in list order, so later
means on top: blur, then dark sheet, then the buttons.

Anything **not** lifted here stays buried — which is exactly how the Pause button
becomes untappable while paused, on purpose.

Note the `raiseAbove` parameter. The caller hands in whatever must stay readable,
so this script never needs to know what a "game over panel" is. It just lifts
what it is given.

#### Capture() — PauseBlur.cs:115

**Makes the blurry photo.** The most technical method in the project.

```csharp
RenderTexture frame = RenderTexture.GetTemporary(width, height, 24);
frame.filterMode = FilterMode.Bilinear;

cam.backgroundColor = new Color(wasBackground.r, wasBackground.g, wasBackground.b, 1f);
cam.targetTexture = frame;
cam.Render();
cam.targetTexture = wasTargeting;

for (int step = 0; step < blurSteps; step++)
{
    width  = Mathf.Max(1, width / 2);
    height = Mathf.Max(1, height / 2);

    RenderTexture smaller = RenderTexture.GetTemporary(width, height, 0);
    smaller.filterMode = FilterMode.Bilinear;
    Graphics.Blit(frame, smaller);

    RenderTexture.ReleaseTemporary(frame);
    frame = smaller;
}
```

**In the game:** the blur you see behind the pause screen.

How it works, in plain terms: it takes **one photo** of the game, then **halves it
four times**. Each halving averages pixels together, so after a few rounds it
looks soft and smeary. Then it stretches the small photo back over the screen.

**No shader. No post-processing stack. No extra asset.** The graphics card's own
smoothing does the blurring for free.

And because the game is frozen anyway, one photo looks identical to blurring every
frame — so it costs nothing per frame.

Three hard-won details are worth knowing if someone asks:

**1. The `24` asks for depth bits.** Nothing here uses depth, but Unity 6's Render
Graph **refuses** to draw a camera into a photo without them, and warns every
single time you pause.

**2. `FilterMode.Bilinear` is set explicitly, twice.** Borrowed photos come back
carrying the last user's smoothing setting. Smooth averages each 2×2 block;
unset just grabs one pixel of four, which is **chunky, not blurry** — and those
hard edges survive every later shrink.

**3. The background alpha is forced to 1.** The camera clears to see-through, and
halving smears that see-through-ness outwards. `RawImage` multiplies by it, so the
HUD ghosted through as dark smudges. Forcing the background fully solid is what
fixed it — and it fixed **every** overlay element, not just the one that was
reported.

The finished blur is copied into a texture the script **owns**, because borrowed
ones are taken back the moment anything else asks for them.

#### Release() — PauseBlur.cs:79

**Hands the blur photo's memory back for good.**

This kind of texture is **not** cleaned up on its own, so GameManager has to say
when the game is shutting down. That is what `GameManager.OnDestroy` is for.

#### Raise(GameObject target) — PauseBlur.cs:98

**Lifts something in front of the dark sheet.**

```csharp
while (t.parent != null && t.parent != canvasRoot)
{
    t = t.parent;
}
t.SetAsLastSibling();
```

It walks **up** the tree first and lifts the whole group. Lifting the object alone
only moves it to the front of its **own** group, which would leave it still buried
under the sheet.

#### Stretch(GameObject target) — PauseBlur.cs:86

**Pins something to all four corners of its parent**, so it fills the screen
whatever shape the phone is.

---

## 14. FishInventory.cs — the fish shop

**229 lines.**

The shop you reach from the Characters button. A shelf you swipe sideways. Fish
you have not bought are grey and cost coins. Fish you own can be worn, one at a time.

### The settings

| Setting in Unity | Value | What it does in the game |
|---|---|---|
| Content | the scroll area | The things inside it are the fish slots, in shelf order. |
| Locked Art | grey card | Shown for a fish you have not bought yet. |
| Fish Art | 5 cards | One card per fish, in the same order as the slots. |
| Buy Art | BUY badge | Shown on a fish you do not own. Tapping it spends your coins. |
| Select Art | SELECT badge | Shown on a fish you own but are not wearing. |
| Equipped Art | WORN badge | Shown on the one fish you are wearing. Tapping does nothing. |
| Price | 300 | How many coins one fish costs. Same price for all of them. |
| Wallet Text | wired | Shows your coin total while the shop is open. |

> **Why does the shop need its own coin total?** The run counters are hidden on
> the menu, so without the wallet you could not see your coins on the one screen
> that spends them.

### The hidden numbers

| Name | Value | What it holds |
|---|---|---|
| `EquippedIndex` | — | Which fish you are wearing. **For this panel's own drawing only.** |
| `OWNED_KEY` | "FishOwned2" | The save name for which fish you own. |
| `EQUIPPED_KEY` | "FishEquipped" | The save name for which fish you wear. |
| `ownedMask` | — | **One number that remembers every fish you own.** |
| `cardArt` `buttons` `priceText` | — | The parts of each shelf slot, found at startup. |

### `ownedMask` — the cleverest variable in the project

Worth understanding properly, because it is the sort of thing a teacher asks about.

**Picture a row of light switches, one switch per fish. On means you own it.**
All those switches are packed into one whole number.

```csharp
private bool Owns(int index)
{
    return index >= 0 && (ownedMask & (1 << index)) != 0;
}
```

- `1 << index` means "make a number with only switch number `index` turned on".
- `&` compares two numbers switch by switch.
- So the whole line asks: **is switch number `index` on in `ownedMask`?**

**Why do it this way?** Unity's save system cannot save a list. And separate
values — one per fish — would be separate chances to update one and forget the
others. One number, one save, one place to be wrong.

> **Why "FishOwned**2**"?** The `2` retires every save written when the shelf had
> six cards. Slot 4 came free back then, and slot 4 is **now the fish you pay
> for** — so trusting an old save would hand it over for nothing. Renaming the key
> is how you throw away saves that are no longer safe to believe. Coins are a
> separate key, so nobody loses their money.

> **Why is `index >= 0` checked?** Old saves used `-1` to mean "nothing worn", and
> the shifting maths would turn `-1` into `31`. A real bug, caught and guarded.

### The methods

#### Awake() — FishInventory.cs:74

**Finds the shelf slots and wires each button to its own slot number.**

```csharp
int count = content.childCount;

for (int i = 0; i < count; i++)
{
    Transform block = content.GetChild(i);
    cardArt[i] = block.GetComponent<Image>();

    int index = i;
    button.onClick.AddListener(() => OnBlockClicked(index));
}
```

The slots are **found in code**, not listed in Unity. So adding a sixth fish needs
no change in this script at all — just a new slot on the shelf.

**The line `int index = i;` looks pointless and is essential.** Without it, every
button would share the one counter `i`, and all five buttons would act on the
**last** slot. Copying it into its own variable gives each button its own number.
This is a classic trap, and a good one to be able to explain.

#### Load() — FishInventory.cs:135

**Loads which fish you own and which you wear, then redraws the shelf.**

```csharp
ownedMask = PlayerPrefs.GetInt(OWNED_KEY, 0);
EquippedIndex = PlayerPrefs.GetInt(EQUIPPED_KEY, 0);

if (cardArt.Length > 1) ownedMask |= (1 << (cardArt.Length - 1)) - 1;

if (!Owns(EquippedIndex)) EquippedIndex = 0;
```

That middle line turns on every switch **except the last**. So every fish except
the final one comes free, and you can swap looks from the first launch while
saving up for one special fish.

It is worked out **from the slot count**, so adding a sixth slot automatically
makes that one the new fish to save for — without touching this line.

#### OnEnable() — FishInventory.cs:129

**Re-reads your save every time the shop opens**, not just once at startup.

Coins earned during a run change what you can afford, so the shelf has to be
re-checked each time you walk in.

#### OnBlockClicked(int index) — FishInventory.cs:160

**You tapped a card.** Buys the fish if you can afford it, then wears it.

```csharp
if (!Owns(index))
{
    if (GameManager.Instance == null) return;
    if (!GameManager.Instance.TrySpendCoins(price)) return;

    ownedMask |= 1 << index;
    PlayerPrefs.SetInt(OWNED_KEY, ownedMask);
    PlayerPrefs.Save();
}

EquippedIndex = index;
PlayerPrefs.SetInt(EQUIPPED_KEY, EquippedIndex);
PlayerPrefs.Save();
```

`TrySpendCoins` checks **and** subtracts in one go, so there is no gap where you
could get the fish without paying.

`ownedMask |= 1 << index` turns on that fish's switch, leaving the others alone.

It saves straight away, twice. **A fish that vanishes when the app closes is 300
stolen coins.**

Buying also **wears** it. Nobody spends 300 coins and then wants a second button
to press.

#### Refresh() — FishInventory.cs:189

**Redraws every card:** picture, price and button badge.

```csharp
Sprite badge = !owned ? buyArt : (i == EquippedIndex ? equippedArt : selectArt);

buttons[i].interactable = owned || (GameManager.Instance != null
                                    && GameManager.Instance.TotalCoins >= price);
```

It redraws **all** of them, **every** time. That is what makes "only one fish is
worn" true without anyone having to remember to un-wear the old one.

Setting `interactable` to false greys the button out **and** stops it eating taps.
Without it, being broke just looks like a broken button.

The price text hides once you own the fish, because that strip of the card is
where the fish's description goes.

> **A warning that is written in the code:** do **not** read `EquippedIndex` to
> pick the in-game fish. The shop panel is switched off when the game loads, so
> its setup has not run and this reads 0. `PlayerController` reads the **save**
> instead, which is always right.

---

## 15. LegalNotice.cs — the terms and privacy screens

**313 lines.** Another plain C# class, not attached to any object.

Two screens: a one-off gate you must accept the first time, and a scrollable
reader with both full documents, reached from the menu.

Everything here is **built in code**. That was deliberate: these screens could be
added without touching the scene file at all, which meant no risk to the game
while the Unity Editor was open.

### What it holds

| Name | Value | What it holds |
|---|---|---|
| `ACCEPTED_KEY` | "LegalAccepted" | The save name. Checked before you get the menu. |
| `TERMS_PATH` | "Legal/Terms" | Where the Terms of Use document lives. |
| `PRIVACY_PATH` | "Legal/Privacy" | Where the Privacy Policy lives. |
| `GATE_SUMMARY` | a short summary | What the first screen says. |
| `PlateColour` | 0.204, 0.627, 0.604 | The button colour, taken off the shop's artwork. |
| `canvasRoot` | — | Where to build the screens. |
| `gameFont` | — | The game's own font, **borrowed** from a label in the scene. |
| `accepted` | — | What to do once you tap Accept. |
| `gate` `reader` | — | The two screens, built only when first needed. |

The gate summary says, in plain words: it is an amateur school project, made by
students, given away free; it is not finished and not optimised; a large part of
it was made with AI assistance **including the code**; there is no warranty and no
liability; and it collects nothing — no internet, no accounts, no ads, no tracking.

> **Why keep the first screen short?** A wall of text on first launch gets closed
> unread. The full documents are one tap away on the same screen. The full Terms
> and Privacy together are about **1,340 words**.

### The methods

#### ShowGateIfNeeded(System.Action onAccepted) — LegalNotice.cs:67

**Puts the first-launch screen up, unless you already accepted.**

```csharp
if (Accepted)
{
    accepted?.Invoke();
    return;
}

if (gate == null) gate = BuildGate();

gate.SetActive(true);
gate.transform.SetAsLastSibling();
```

`SetAsLastSibling` puts it on top of everything. The panel is solid, so the menu
cannot be tapped through it.

#### BuildGate() — LegalNotice.cs:109

**Builds the first-launch screen:** summary, a link to the full text, Accept and Exit.

```csharp
BuildButton(panel.transform, "Accept", new Vector2(-350f, 70f), () => { ... });
BuildButton(panel.transform, "Read the full terms", new Vector2(0f, 70f), OpenReader);
BuildButton(panel.transform, "Exit", new Vector2(350f, 70f), Quit);
```

All three in a row, and the order is deliberate: **the harmless middle button
separates Accept and Exit**, so neither is the one you hit by accident.

#### Quit() — LegalNotice.cs:86

**Closes the game when someone does not agree to the terms.**

```csharp
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
```

Those `#if` lines are a **compiler switch**: the top line is used inside Unity,
the bottom line in the real built game.

`Application.Quit()` does nothing in the Editor, which is why Exit looked broken
while testing. Inside Unity it stops Play instead.

Exit really does quit, because the alternative is somebody playing a game whose
terms they never agreed to.

#### ApplyGameFont(TMP_Text label) — LegalNotice.cs:254

**Puts the game's own font on a piece of text.**

```csharp
foreach (TMP_Text existing in canvasRoot.GetComponentsInChildren<TMP_Text>(true))
{
    if (existing.font == null || existing == label) continue;
    if (!existing.font.name.Contains("Hegarty")) continue;

    gameFont = existing.font;
    break;
}
```

It **borrows** the font off a label already in the scene, rather than using an
Inspector slot. That is what let these screens be added without touching the
scene file at all.

#### BuildScrollingText(...) — LegalNotice.cs:206

**Builds the scrolling body text**, sized so it never runs under the buttons.

It **scrolls** rather than shrinking the font, because these documents have to
stay readable on a phone. A 1,340-word document shrunk to fit a phone screen is
not really being shown to anyone.

#### Load(string path) — LegalNotice.cs:151

**Loads one document's text out of the project.**

```csharp
TextAsset asset = Resources.Load<TextAsset>(path);

if (asset != null) return asset.text;

return "[" + path + " is missing from Assets/Resources. "
       + "Contact the authors for a copy of this document.]";
```

If the file is missing it hands back a readable message, so a broken build cannot
leave the panel **blank**. A blank legal screen is worse than an error.

#### The small ones

| Method | Line | What it does in the game |
|---|---|---|
| constructor | 48 | Just remembers where to build. Builds nothing yet. |
| `Accepted` | 54 | True once you have accepted the terms on this device. |
| `ClearAcceptance()` | 58 | Forgets that you accepted, so the gate shows again. For testing. |
| `OpenReader()` | 97 | Opens both full documents in a panel you can scroll. |
| `BuildReader()` | 135 | Builds that reader, with both documents one after the other. |
| `BuildPanel()` | 163 | Makes the full-screen background everything else sits on. |
| `BuildTitle()` | 180 | Puts a heading across the top of a panel. |
| `BuildButton()` | 274 | Builds one button at the bottom of a panel. |

`BuildPanel`'s image is what **stops taps reaching the menu underneath**. It is
not just decoration.

`BuildButton` goes through `GameManager.PlayClick`, so these buttons sound like
every other button in the game.

---

## 16. CameraEdges.cs — where the screen edges are

**29 lines.** A `static` class, which means no object, no instance — you just call
it by name from anywhere.

Anything spawning just off screen, or deleting what has left, asks here instead of
working it out again.

### What it holds

| Name | What it does in the game |
|---|---|
| `cached` | The camera, remembered so it is not looked up over and over. |
| `Cam` | Hands back the camera, finding it again if it went away. |
| `HalfWidth` | Half the screen width, in world units. |
| `Left` | The left edge of the screen. |
| `Right` | The right edge. |
| `Top` | The top edge. |

```csharp
public static float HalfWidth => Cam.orthographicSize * Cam.aspect;

public static float Left  => Cam.transform.position.x - HalfWidth;
public static float Right => Cam.transform.position.x + HalfWidth;
public static float Top   => Cam.transform.position.y + Cam.orthographicSize;
```

**In the game:** this is why the game fits every phone.

The **height is fixed** and the **width follows the phone's shape**. So a wide
phone shows **more cave** rather than a stretched one. That one decision is most
of what makes the game work on different screens.

```csharp
if (cached == null) cached = Camera.main;
return cached;
```

Small but important: Unity reports a **destroyed** camera as null, so a camera
that goes away is looked up again rather than handed back broken.

> **Why does this class exist at all?** Because `Camera.main` is slow to look up,
> and before this existed the screen edges were worked out separately in several
> scripts — once per object, per frame. Now it is one shared answer. The
> presentation line for it: "screen edges computed once, not per object per frame."

---

## 17. ButtonPressSound.cs — clicks that feel fast

**26 lines.** Added to every button automatically by `GameManager.BuildSound()`.

It clicks on **finger-down** instead of finger-up, which feels faster.

#### OnPointerDown(PointerEventData eventData) — ButtonPressSound.cs:18

**A finger went down on this button. Plays the click.**

```csharp
if (button == null || !button.interactable) return;

if (GameManager.Instance != null) GameManager.Instance.PlayClick();
```

**That guard is a real bug fix.** Unity only checks a button is switched on at
finger-**up**. So without this line, the greyed-out BUY button in the shop would
click when you are broke — making it sound like the purchase worked.

#### Awake() — ButtonPressSound.cs:12

**Finds the button this is attached to.**

The class is marked `[RequireComponent(typeof(Button))]`, which tells Unity this
script cannot exist without a Button. Unity then refuses to let them be separated.

---

## 18. FishShopDebug.cs — the Tools menu for testing

**90 lines. Lives in `Assets/Editor`, so it is not part of the built game.**

A Tools menu for resetting the shop's saved data while testing. In Unity you find
it at **Tools → Fish Shop**.

### The menu items

| Menu item | Method | Line | What it does |
|---|---|---|---|
| Reset Legal Acceptance | `ResetLegal()` | 24 | Forgets that you agreed to the terms, so the gate shows again. |
| Reset Skins | `ResetSkins()` | 36 | Forgets every fish you own and which you wore, like a new install. |
| Set Coins to 0 | `Coins0()` | 46 | For testing with nothing. |
| Set Coins to 100 | `Coins100()` | 49 | For testing the broke path — the greyed-out BUY button. |
| Set Coins to 854 | `Coins854()` | 52 | For testing with plenty. |
| Show Save State | `Show()` | 55 | Prints the saved data to Unity's Console. |

All six are wired up by the `[MenuItem(...)]` attribute above each one. That line
is what puts them in Unity's **Tools** menu — you never call these from code.

The three coin ones all hand off to one shared method, `SetCoins(int amount)` at
line 60, so there is only one place that actually writes your coins.

### Why this script exists at all

**This is worth knowing, because it caught the team twice.**

Two things keep their own copy of the save, and both have cost a session of confusion:

1. **The running game** holds the coins and owned fish in memory, and saves over
   your change when it stops.
2. **The Unity Editor** holds its own copy, so editing the save from outside Unity
   does nothing at all — the Editor hands the game its old copy back.

So the rule is: **always stop Play first, and always change things through this menu.**

#### PlayStopped() — FishShopDebug.cs:69

**Refuses to run any of these while the game is playing, and says why.**

```csharp
if (!EditorApplication.isPlaying) return true;

EditorUtility.DisplayDialog("Stop Play first", "...", "OK");
return false;
```

Rather than failing silently, it pops up a box explaining that the game holds the
wallet in memory while it runs, so a change made now would be ignored and then
overwritten.

#### ResetSkins() — FishShopDebug.cs:36

**Forgets every fish you own.**

```csharp
PlayerPrefs.SetInt(OWNED_KEY, 0);
PlayerPrefs.SetInt(EQUIPPED_KEY, 0);
```

It writes plain `0`, **not** a list of which fish come free. Which fish come free
is `FishInventory`'s decision, and copying that rule in here is how the two would
end up disagreeing.

#### The shared key names

```csharp
private const string OWNED_KEY = FishInventory.OWNED_KEY;
private const string EQUIPPED_KEY = FishInventory.EQUIPPED_KEY;
```

Both names come **from FishInventory** rather than being typed again here. They
were typed twice once already, and **a reset that clears a key nothing reads looks
exactly like a reset that worked.**

#### Report(string what) — FishShopDebug.cs:84

**Prints the saved data to Unity's Console.** The only honest way to see what the
game was **actually** handed, rather than what sits in the registry.

---

# Part 3 — The safety net

---

## If they point at a line you do not know

Say this:

> **"I would have to check that one."**

That is a better answer than a guess. **A wrong answer confidently given is the
thing that costs you.** Nobody expects one person to hold 3,163 lines in their head.

If you want to say more than that, you can almost always say something true about
a line without knowing the line, because the project repeats a few patterns:

| If the line has... | You can honestly say |
|---|---|
| `if (something == null) return;` | "That is a guard. It stops the game crashing if that thing was never wired up." |
| `Time.deltaTime` | "That makes the movement the same speed on a fast phone and a slow one." |
| `Mathf.Clamp` or `Mathf.Min` or `Mathf.Max` | "That is a limit, so the number can never go somewhere unfair." |
| `Mathf.Lerp` | "That mixes between two values. It is how things fade smoothly instead of jumping." |
| `PlayerPrefs` | "That is the save. It writes to the phone, so your score and coins survive closing the app." |
| `static` | "That is shared by the whole game, one copy rather than one each." |
| `const` | "That is locked in the code on purpose, so the Unity window cannot change it." |
| `GameManager.Instance` | "That is how any script reaches the one GameManager." |
| `CaveDepth` | "That is the one depth number. The light, the rock gap and the lamp all read it." |
| `Instantiate` | "That makes a new copy of something while the game is running." |
| `Destroy` | "That removes something, usually once it has gone off screen." |
| `GetComponent` | "That finds another script or part on the same object." |

---

## Questions you should expect

#### "How does the game get harder?"

Three things at once, all off one depth number: the light dims from 0.03 to 0.02,
the gap narrows from 3.6 to 2.6, and the cave speeds up from 4.5 to 8 units a
second over 150 seconds.

#### "Why not just speed it up?"

Speed alone gets unfair fast. Darkness takes away **information** instead of
reaction time, so it feels tense rather than cheap.

#### "Is the cave random?"

The shapes are. The safe gap is not. Before placing a rock the game measures the
opposite wall at that exact spot, and there is a hard floor of 2.2 that nothing
may go below.

#### "What makes it feel underwater and not like Flappy Bird?"

Water drag and the sink cap. A bird falls like a stone; our fish sinks at no more
than 3.2. Dropping it from 4.6 to 3.2 made it 25% less punishing and it still dives.

#### "Why is the global light there at all, if the fish has a lamp?"

**Because the lamp alone is not fair.** This is the strongest answer in the set,
because it is measured, not felt:

| Measurement | Value |
|---|---|
| Rock speed | 3.5 at the start, 8 by 90 seconds, 10.4 mid-surge |
| Fish to the right edge | 11.85 units |
| Lure reach | 3.63 units |
| **Warning time at 8 units/sec** | **0.45 seconds** |
| **Warning time in a surge** | **0.35 seconds** |

Human reaction alone is about 0.25 seconds, and the fish still has to travel.
So at full speed the lure **cannot** be the only light — the player has to read
rock silhouettes against the background. **The global light is a fairness
mechanism, not atmosphere.**

#### "How much of this did AI write?"

A large part, including code. It is disclosed in the README and on the screen the
player has to accept before playing. Say it plainly and move on — it is already
written down, so there is nothing to defend.

#### "What is not finished?"

The sprites are not packed into an atlas, so the game draws more than it needs to.
It has been tested on one phone. That is the honest list.

#### "What would you do next?"

Wider device testing, and the sprite atlas. A sixth fish.

#### "What was the hardest bug?"

Pick one of these three — they are all true and all interesting:

1. **The score counter ate the taps.** A 1018×141 text box across the top of the
   screen silently absorbed every tap that landed on it, because UI soaks up taps
   by default. One line fixed it: `raycastTarget = false`.
2. **Tap-spam broke the darkness.** A changed line turned a capped brightness into
   one that added up, and mashing the screen reached **9 times** the intended
   peak. Proved by simulation: 1.25 clamped, 11.7 unclamped.
3. **The current was almost switched off.** The shove measured only 0.23 units
   against a fish 1.09 wide — a fifth of a body. The mechanic the team was
   proudest of, and it looked fine in the Inspector. Nobody had measured it.

#### "Why is GameManager 760 lines?"

Honest answer: some of it could be split up. But everything in it shares state —
the score, the coins, whether the game is frozen. Splitting it would mean two
scripts holding copies of one number, and every bug where two things disagreed in
this project came from exactly that.

---

## The 30 settings where the script and the scene disagree

**The right-hand column is what the game actually uses.** If a teacher opens a
script and reads out the middle column, the answer is: *"the script holds the
starting value, the scene holds the real one, and the scene wins."*

### PlayerController.cs — 9 of them

| Setting | Script says | **The game uses** |
|---|---|---|
| Swim Force | 4 | **4.6** |
| Water Gravity | 1.2 | **1** |
| Water Drag | 1 | **1.5** |
| Max Sink Speed | 4.5 | **3.2** |
| Current Push | 3 | **9** |
| Current Return | 4 | **2.5** |
| Rotation Speed | 5 | **10** |
| Max Tilt Up | 25 | **20** |
| Max Tilt Down | -45 | **-40** |

### GameManager.cs — 7 of them

| Setting | Script says | **The game uses** |
|---|---|---|
| Start Light | 0.35 | **0.03** |
| Deep Light | 0.08 | **0.02** |
| Darkest At Distance | 400 | **300** |
| Pause Dim Darkness | 0.78 | **0.7** |
| Start Fraction From Left | 0.5 | **0.4** |
| Movement Pop Volume | 0.35 | **0.05** |
| Bg Music Volume | 0.3 | **0.5** |

### CaveSpawner.cs — 6 of them

| Setting | Script says | **The game uses** |
|---|---|---|
| Min Gap | 3 | **3.6** |
| Min Depth | 2 | **2.5** |
| Max Depth Change | 1.2 | **1** |
| Floor Y | -4 | **-5** |
| Min Scale | 0.35 | **0.55** |
| Max Width | 8 | **10** |

### TrashSpawner.cs — 5 of them

| Setting | Script says | **The game uses** |
|---|---|---|
| Min Size | 0.5 | **0.6** |
| Drift Min Speed | 0.15 | **0.1** |
| Drift Max Speed | 0.5 | **0.32** |
| Ramp Seconds | 90 | **150** |
| Start Speed Multiplier | 1.15 | **0.9** |

### CoinSpawner.cs — 3 of them

| Setting | Script says | **The game uses** |
|---|---|---|
| Drift Min Speed | 0.15 | **0.1** |
| Drift Max Speed | 0.5 | **0.32** |
| Ramp Seconds | 90 | **150** |

### The ones that agree

Every setting in **SpotLight2D**, **PlayerHit**, **ScrollObject**, **TrashSpin**,
**FishInventory** and the rest of **CoinSpawner** and **TrashSpawner** matches
between the script and the scene. Those you can read straight off the screen.

---

## The numbers worth memorising

If you learn only ten numbers, learn these.

| What | Number |
|---|---|
| Fish height | 1.33 (the part that can hit something is 1.11) |
| Fish width | 1.09 |
| Opening at the cave mouth | 3.6 |
| Opening at the deepest | 2.6 |
| Hard minimum opening, locked in code | 2.2 |
| Cave light, start to deepest | 0.03 down to 0.02 |
| Darkest at | 300 metres |
| Cave speed, start to hardest | 4.5 up to 8 units a second |
| Time to full difficulty | 150 seconds |
| A surge arrives every | 9 seconds, lasting 2.5 |
| Fish swim force | 4.6 |
| Fastest the fish may sink | 3.2 |
| A new fish costs | 300 coins |
| Screen width | about 17.8 units |

---

## Word list

Unity and C# words that appear in this guide, in plain English.

| Word | What it means |
|---|---|
| **Awake()** | Unity calls this once, before anything else. Used for setting up. |
| **Start()** | Unity calls this once, just after Awake. Used when the setup needs other scripts to exist first. |
| **Update()** | Unity calls this **every frame**, about 60 times a second. |
| **FixedUpdate()** | Like Update, but on a steady clock. Anything touching physics goes here. |
| **OnEnable()** | Unity calls this every time the object is switched on. |
| **OnDestroy()** | Unity calls this when the object is removed. Used for tidying up. |
| **MonoBehaviour** | The kind of script that can be attached to an object in the scene. |
| **SerializeField** | Makes a hidden value show up as a box you can type in, in Unity. |
| **Tooltip** | The little help text that appears when you hover over that box. |
| **Header** | A heading that groups boxes together in Unity. |
| **private** | Only this script can see it. |
| **public** | Other scripts can see it too. |
| **const** | A value locked in the code forever. Unity cannot change it. |
| **static** | One copy shared by the whole game, rather than one per object. |
| **void** | This method hands nothing back. |
| **bool** | True or false. |
| **float** | A number with a decimal point, like 4.6. |
| **int** | A whole number, like 300. |
| **null** | Empty. Nothing there. |
| **guard** | An early `if (...) return;` that stops a method running when it should not. |
| **prefab** | A saved object you can stamp out copies of while the game runs. |
| **Instantiate** | Stamp out one of those copies. |
| **Destroy** | Remove an object. |
| **collider** | The shape things can bump into. Usually invisible. |
| **trigger** | A collider that detects touch but lets things pass through. How coins work. |
| **Rigidbody2D** | Unity's physics body. It is what makes the fish fall. |
| **singleton** | A script with only one copy ever, that anything can reach. GameManager is one. |
| **PlayerPrefs** | Unity's small save file on the phone. Holds your coins, best score and settings. |
| **Time.deltaTime** | How long the last frame took. Multiplying by it makes speed the same on any phone. |
| **Time.timeScale** | The speed of game time. 0 freezes everything. That is the whole pause. |
| **Time.unscaledTime** | The real clock. Keeps running while the game is frozen. |
| **Mathf.Lerp** | Mix between two values. Lerp(a, b, 0.5) is halfway. |
| **Mathf.Clamp** | Keep a number between two limits. |
| **Mathf.Clamp01** | Keep a number between 0 and 1. |
| **Transform** | Where an object is, how big it is, and which way it is facing. |
| **Canvas** | The flat layer all the buttons and text are drawn on. |
| **RectTransform** | Like a Transform, but for things on the Canvas. |
| **raycastTarget** | Whether a UI thing catches taps. Turning it off is how counters stop eating taps. |
| **TMP_Text** | TextMeshPro text. What all the numbers and labels in the game are. |
| **Light2D** | A 2D light. The cave has one big one; the fish has a small one. |
| **RenderTexture** | A picture the game draws into, rather than onto the screen. Used for the pause blur. |
| **attribute** | The bits in square brackets above a line. Instructions to Unity, not code that runs. |

---

## Before you present

- Say each of the four Part 1 scripts out loud once. If a sentence will not come
  out, it is not yours yet — shorten it until it is.
- **Do not read the comments off the screen.** The comments are for someone
  reading the code later. You are talking to a room.
- If you are asked about a number, say where it lives: the scene, not the script.
- "I would have to check that one" is a complete answer. Use it.

