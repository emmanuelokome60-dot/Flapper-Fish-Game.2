# Abyssal Fish — the code, explained

This guide covers all 18 scripts in the game. Every setting and every method is
named, and explained in plain English.

Read it with the code open next to you. Each method shows the file it lives in
and the line it starts on, like `PlayerController.cs:146`. Type that file name
into Unity's search box and you will find it.

**You do not need to know how to program to read this.** Every word that belongs
to programming is explained the first time it turns up, and there is a word list
at the very end.

---

## Read this first: the numbers come from the scene, not the scripts

This is the one thing to understand before anything else, because it is the
easiest thing to get caught out on.

A Unity project has two places a number can live.

One is the **script**. That is the text file a programmer types. A number written
there is only a **starting suggestion**.

The other is the **scene**. That is the file holding the actual game: where
everything sits, and what every setting was turned to. When you drag a slider in
the Unity window, the new number is saved into the scene.

**When the game runs, the scene wins.** Always.

Here is what that looks like. `PlayerController.cs` says the fish swims with a
force of `4`. The game actually uses **4.6**. Both numbers are really there. Only
one of them is what the player feels under their thumb.

**Every number in this guide was read out of the scene**, so these are the
numbers the game really runs on. Where the script says something different, this
guide tells you both, like this: *Swim Force — 4.6 (script says 4)*.

So if your teacher opens a script and reads out a number that does not match
what you said, you have a true answer ready:

> *"The script holds the starting value. The scene holds the real one, and the
> scene is what the game uses. We tuned that in the Unity window, so the number
> got saved into the scene."*

**30 settings disagree between the two.** They are all listed in Part 3, in one
table, so you can check any of them quickly.

---

# Part 0 — The big picture

Before the detail, here are the four ideas the whole game is built on. If you
understand these four, you understand the game.

**1. One depth number runs the whole feeling.**

`GameManager` works out a number it calls `CaveDepth`. It is `0` at the mouth of
the cave and `1` at the deepest point, and it slides between the two as you swim.

Three separate things read that one number. The cave light dims on it. The gap
between the rocks narrows on it. The fish's own lamp brightens on it.

That means the player watches **one** thing getting worse, not three different
systems fighting each other. It is the best decision in the project, and it also
leaves us with only one dial to balance instead of three.

**2. One shared speed.**

The rocks, the rubbish and the coins all read the same speed, kept in
`ObstacleMovement.SpeedMultiplier`. Only one method is allowed to change it,
called `SetPace()`.

If every object owned its own speed they would slowly drift apart, and nothing on
screen would line up with anything else.

**3. The gap in the rocks is a rule, not luck.**

Before placing a rock, `CaveSpawner` measures the opposite wall **at that exact
spot** to see how much room is already used. That is why the two rows of rocks
can never meet and seal the cave shut.

There is also a hard floor of `2.2` written into the code itself, which nothing
can override. So the **shapes** of the cave are random, but the **safe gap** is
not.

**4. Everything except coins kills you.**

Rocks and rubbish use a **collider**. That is a shape in Unity that things bump
into, like a solid wall.

Coins use a **trigger** instead. A trigger is a shape that notices when something
touches it but lets it pass straight through.

That single difference is the whole reason collecting a coin does not end your
run. It also reads so instantly to a player that the game needed no tutorial at
all.

**A run, from start to finish.** Here is the order things happen in:

`Start()` calls `BackToMenu()`, which freezes everything, so the game opens on
the menu. `LegalNotice` puts the terms on top if you have not accepted them yet.
Pressing Play calls `StartGame()`, which unfreezes the game and starts the three
spawners. Your tap arrives at `PlayerController.Update()` and the fish swims.
Meanwhile `GameManager.Update()` counts the metres and dims the light. Then you
bump something, `PlayerHit.EndRun()` calls `GameOver()`, everything freezes again
and your score is saved.

---

# Part 1 — The four scripts you will be asked about

These four own the feel of the game, and nearly every interesting decision in the
project is in one of them. Read these four properly.

---

## 1. GameManager.cs — the boss of the game

**760 lines, the biggest script in the project.**

It runs the menu. It starts and ends your runs. It keeps the score and your
coins. It plays the sounds and it darkens the cave. Basically, if something
happens to the *whole game* rather than to one object, it happens in here.

Only one GameManager ever exists, and any script can reach it by writing
`GameManager.Instance`. A script set up that way is called a **singleton**, which
just means "there is only ever one of these, and everyone shares it".

> **If you are asked "why is one script 760 lines?"** The honest answer is that
> some of it could be split out. We left it together because it all shares the
> same information: the score, the coins, and whether the game is frozen right
> now. Splitting it up would mean two scripts each keeping their own copy of one
> number, and every bug in this project where two things disagreed came from
> exactly that.

### The settings that matter

| Setting | Value | What it does in the game |
|---|---|---|
| **Start Light** | **0.03** (script 0.35) | How bright the cave is at the mouth. |
| **Deep Light** | **0.02** (script 0.08) | How bright it is at the very deepest. |
| **Darkest At Distance** | **300** (script 400) | How many metres until the cave is as dark as it gets. |
| **Start Fraction From Left** | **0.4** (script 0.5) | Where the fish starts, as a share of the screen's width. |
| **Pause Dim Darkness** | **0.7** (script 0.78) | How dark the screen goes behind the pause buttons. |
| Pause Blur Steps | 4 | How blurry the frozen game goes. Each step halves the sharpness. |
| Units Per Second | 5 | How fast the metre counter climbs. It matches the speed of the rocks. |
| **Movement Pop Volume** | **0.05** (script 0.35) | The little pop when you swim. It is quiet because it fires on *every single tap*. |
| **Bg Music Volume** | **0.5** (script 0.3) | The music underneath the sound effects. |
| Coin Icon Size / Inset / Gap | 80 / 0 / 8 | The size and spacing of the coin picture next to the counter. |

> **If you are asked "why so dark? 0.03 down to 0.02 is almost nothing."** This
> is a good question and there is a good answer. What sells darkness is
> **contrast**, not dimness. A screen that is evenly dim just looks muddy, your
> eye adjusts to it within seconds, and outdoors on a phone it vanishes
> completely. So we do two things at once: the cave goes very dark **and** the
> fish's lamp goes bright. The **difference between them** is the effect you
> actually see.

One setting is worth explaining on its own. `Start Fraction From Left` is a
**fraction**, not a distance. Four tenths of the way across the screen is four
tenths of the way across on every phone, whatever shape it is. If we had used a
fixed distance instead, the fish would sit off-centre on a taller phone.

**Wired slots.** These are the things dragged into the script by hand in the
Unity window, so they have no numbers to quote. They are: Game Over Text, Game
Over Menu, Game Over Score Text, Game Over Best Text, Game Over Coin Text, Main
Menu, Characters Menu, Restart Button, Home Button, Pause Button, Resume Button,
Menu Sound Button, Pause Sound Button, Sound On Sprite, Sound Off Sprite, Coin
Text, Score Text Counter, Player, Obstacle Spawner, Cave Light, Button Click,
Coin Clip, Movement Pop, Game Over Clip and Bg Music.

There are two sound buttons in the game, one on the menu and one on the pause
screen, but only **one** saved setting behind them both. `ApplySound()` swaps the
picture on **both** buttons, including the one currently hidden. That is why the
two menus can never end up showing different things.

### What it works out for itself

These are values the script keeps for itself while the game runs, rather than
ones you type in.

| Name | What it holds |
|---|---|
| `Instance` | The one and only GameManager. This is how every other script finds it. |
| `CaveDepth` | How deep you are, from `0` to `1`. The light, the rock gap and the lamp all read this. |
| `Distance` / `distanceTravelled` | The metres this run, as a whole number and with decimals. |
| `Coins` / `TotalCoins` | What you picked up **this run only**, and your saved total. The shop spends the total. |
| `HighScore` | The furthest you have ever got. Saved on the phone. |
| `Muted` / `OnMenu` / `isPaused` / `isGameOver` | Four true-or-false flags saying what state the game is in. |
| `sfx` / `music` | **Two** separate sound players. One player cannot fire a short sound *and* loop a long track at the same time. |
| `COINS_KEY` `BEST_KEY` `MUTE_KEY` | The names your save is filed under on the phone. |

### The methods that matter

A **method** is a named block of instructions. Something calls it by name, and
the instructions inside it run.

**`Awake()` — line 200.** This one runs once, right at the very beginning. It
loads your save, sets up the sound, and draws the coin picture. It also does this,
which is a real bug fix:

```csharp
foreach (TMP_Text counter in new[] { scoreTextCounter, coinText })
    if (counter != null) counter.raycastTarget = false;
```

**In the game, this is why tapping near the top of the screen makes the fish
swim.** In Unity, anything drawn on screen catches your taps unless you tell it
not to. Our score counter is a wide box, **1018 by 141** pixels, stretched right
across the top. It was quietly eating every tap that landed on it, and the fish
did not move. A counter is not a button, so these two lines tell it to stop
catching taps.

**`Update()` — line 306.** `Update()` is a special name in Unity: whatever is
inside it runs every single frame, about 60 times a second. This one counts the
metres and dims the cave.

```csharp
distanceTravelled += unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime;
int whole = (int)distanceTravelled;
if (whole == Distance) return;
Distance = whole; ShowDistance(); ApplyCaveDepth();
```

Three things are going on there, and all three are deliberate.

Multiplying by `SpeedMultiplier` makes the counter speed up as the cave speeds
up, so the number on screen matches how fast the rocks are really going past.

The `return` on the third line means the text is only rewritten when the whole
number actually changes, rather than 60 times a second for no reason.

And notice there is **no check for whether the game is paused**. It does not need
one. `Time.deltaTime` stops counting while the game is frozen, so the menu, the
pause screen and the game over screen all stop the counter for free.

**`ApplyCaveDepth()` — line 669.** Five lines long, and the darkness of the whole
game lives in here.

```csharp
CaveDepth = Mathf.Clamp01(Distance / darkestAtDistance);
if (caveLight == null) return;
caveLight.intensity = Mathf.Lerp(startLight, deepLight, CaveDepth);
```

`Distance / darkestAtDistance` works out how far through the cave you are. At
150 metres out of 300, that comes to `0.5`, so you are halfway.

`Clamp01` keeps the answer between 0 and 1, which stops the cave getting any
darker once you pass 300 metres.

`Lerp` is short for "mix". It blends the two brightness numbers together
according to how deep you are.

The careful bit is the order. **`CaveDepth` is set *before* the check for a
missing cave light.** So even if the cave light was never wired up, the fish's
lamp still knows how deep you are. Checking first would have quietly broken the
lamp as well.

**`GameOver()` — line 321.** Freezes everything, saves a new best score if you
beat it, and shows the end panel.

It opens with `if (isGameOver) return;`. That is called a **guard**: a quick check
at the top of a method that stops the rest of it running. It is needed because
the fish can hit two things in the same frame, and the run should only end once.

The death sound sits *after* the guard on purpose, so you hear it exactly once.

It also calls `PlayerPrefs.Save()`, which writes your save to the phone right
away rather than waiting. A phone can be closed without shutting down properly,
and we did not want to lose a new best score to that.

**`TrySpendCoins(int amount)` — line 610.** This is how you buy a fish. The
important thing is that checking you can afford it and taking the coins happen in
**one step**. If they were two separate methods, somebody could one day write
code that took the coins but forgot to check, or checked but forgot to take them,
and hand out a free fish. It hands back `true` or `false` so the shop knows
whether the sale went through.

**`BuildSound()` — line 485.** This is why every button in the game clicks,
without anyone wiring up 20 buttons by hand:

```csharp
foreach (Button b in CanvasRoot().GetComponentsInChildren<Button>(true))
    if (b.GetComponent<ButtonPressSound>() == null) b.gameObject.AddComponent<ButtonPressSound>();
```

It finds every button and gives each one a click sound. The `true` in there means
"include the switched-off ones too". The shop shelf is switched off when the game
loads, so without that one word the shop's buttons would all be silent.

**`StartGame()` — line 358.** Unfreezes the game, puts the fish and the counters
back to the start, and starts the spawners.

It only plays the music **if it is not already playing**. This matters: telling
music that is already running to play again snaps it back to the beginning. So
without that check, a player who kept retrying would never hear past the intro.

### Every other method

| Method | Line | What it does in the game |
|---|---|---|
| `Start()` | 241 | Opens the game on the menu, frozen, with the terms on top. |
| `BuildLegalButton()` | 260 | Makes the Terms and Privacy button in the bottom right. |
| `TogglePause()` | 391 | Setting `Time.timeScale = 0f` *is* the entire pause. |
| `BackToMenu()` | 404 | Back to the menu, frozen. **The only place in the game music ever stops.** |
| `PlayerStart()` | 439 | Works out the fish's starting spot as a share of the screen width. |
| `ShowRunButtons()` | 447 | Shows and hides the buttons on the pause screen. |
| `ShowPauseDim()` | 470 | Turns the dark blurry backdrop on or off. |
| `PreloadClips()` | 522 | Loads the sounds up front, so the first one played does not stutter. |
| `PlayClick()` / `PlayMovementPop()` | 532 / 538 | The button click, and the swim pop. |
| `ToggleSound()` / `ApplySound()` | 545 / 557 | Mutes the game, saves that, and swaps **both** buttons' pictures. |
| `CanvasRoot()` | 571 | Finds the Canvas by walking **up** the tree of objects. |
| `OnDestroy()` | 578 | Hands the blur's memory back. It is not freed automatically. |
| `ClearObstacles()` | 584 | Deletes every rock, coin and piece of rubbish still lying around. |
| `AddCoin()` | 593 | Adds one to this run, one to your total, saves, and plays the ping. The sound lives **here** rather than in `CoinPickup`, so a coin you touch as you die stays silent. |
| `ShowCharacters()` | 626 | Opens and closes the shop. It calls `ApplySkin()`, because closing the shop goes through neither `StartGame` nor `BackToMenu`. |
| `ResetDistance()` / `ResetCoins()` | 648 / 691 | Back to zero. The coins here are this run's only. |
| `ShowDistance()` / `ShowCoins()` | 660 / 699 | **The one and only place** each of those numbers is written to the screen. |
| `ShowGameOver()` | 681 | Fills in the three numbers on the end screen. |
| `BuildCoinIcon()` | 711 | Puts a coin picture to the left of a number. It also sets `raycastTarget = false`. |
| `BuildWalletCounter()` | 745 | The shop's coin total, built at the same size and with the same picture as the in-game one. |
| `ShowRunCounters()` | 755 | Shows and hides both counters **together**. |

> **Why "the one and only place" keeps coming up.** Two different methods writing
> to the same piece of text is what made the distance counter flicker. And one
> counter being saved as ON while the other was saved as OFF is what made the two
> of them disagree. So now each piece of text has exactly one method allowed to
> write it.

---

## 2. CaveSpawner.cs — walls that can never close up

**323 lines.** Its job is to build two endless rows of rocks, one along the
ceiling and one along the floor, always leaving an opening the fish can fit
through.

| Setting | Value | What it does in the game |
|---|---|---|
| **Min Gap** | **3.6** (script 3) | The size of the opening at the cave mouth. |
| Deep Gap | 2.6 | The opening at the darkest point. It **must** be smaller than Min Gap. |
| **Min Depth** | **2.5** (script 2) | Every rock sticks out at least this far, so none of them is a useless little sliver. |
| **Max Depth Change** | **1** (script 1.2) | How different one rock may be from the one before it. A lower number makes smoother walls. |
| **Floor Y** | **-5** (script -4) | The top of the ground. Floor rocks stand on this line. |
| **Min Scale** | **0.55** (script 0.35) | The smallest a rock may be stretched. |
| Max Scale | 1.2 | The largest a rock may be stretched. |
| **Max Width** | **10** (script 8) | The widest a single rock may be. The screen is about 17.8 wide. |
| Overlap | 0.2 | How far each rock tucks under its neighbour, which hides the join. |

To picture those gap numbers, you need the size of the fish. It is **1.33** tall,
and the part of it that can actually hit things is **1.11**. So a gap of 3.6 is
roughly three fish, and 2.6 is roughly two.

**Three numbers are locked in the code.** They are marked `const`, which means
the Unity window cannot change them at all. They are `ABSOLUTE_MIN_GAP` 2.2,
`ROW_START_MARGIN` 0.5 and `FORGET_MARGIN` 5.

> **If you are asked "why lock 2.2 away?"** Because it is a safety rule, not a
> matter of taste. It is roughly twice the 1.11 of fish that can hit something.
> Below that, getting through stops being a skill and becomes luck, and on a phone
> your own thumb is covering the fish anyway. Writing it into the code means no
> amount of fiddling with sliders can ever make the game unfair. And if someone
> sets Deep Gap too low, the game **prints a warning and corrects it** rather than
> fixing it silently, because a silent rescue is how you end up shipping a cave
> nobody fits through that looks completely deliberate.

**Two helper classes.** A **class** here is just a small bundle of related
information with a name.

`Rock` remembers one rock that has been placed. It holds `rockTransform`,
`halfWidth` and `depth`, plus `Left` and `Right`, which it works out from those.

`Row` is one whole row of rocks. It holds `isTop`, `rocks`, `last`, `lastIndex`,
`startX` and `RightEdge`.

A row only ever needs its **last** rock to decide where the next one goes. That
is why all of this stays cheap to run every single frame.

### The methods

**`CurrentGap()` — line 169.** Three lines long, and one of the best decisions in
the project.

```csharp
float depth = GameManager.Instance != null ? GameManager.Instance.CaveDepth : 0f;
return Mathf.Max(Mathf.Lerp(minGap, deepGap, depth), ABSOLUTE_MIN_GAP);
```

It reads the **same** `CaveDepth` that dims the light. So the walls narrowing and
the screen darkening both come off one number, which means the player is reading
one signal rather than two.

The `Max` on the end is the safety net. However the game is tuned, the gap can
never come out below 2.2.

**`SpawnRock(row, otherRow)` — line 179.** The cleverest method in the project,
and what makes it safe is the **order** it does things in.

```csharp
float gap = CurrentGap();
float budget = CameraEdges.Top - floorY - gap;
float taken = DeepestBetween(otherRow, startX, startX + spriteSize.x * maxScale);
float allowed = Mathf.Min(budget - taken, budget - minDepth);
```

Read as a sum, that is:

**budget** is the height of the screen minus the opening the fish needs. In other
words, all the space the two rows are allowed to share between them.

**taken** is how much of that the *opposite* row has already used, right at this
spot.

**allowed** is what is genuinely left over for this rock.

The key point is that the opposite wall is measured **first**, before anything is
placed. That is what makes "the walls can never meet" actually true rather than
just likely.

There is one more line worth knowing about:

```csharp
float depthChange = maxDepthChange * (gap / minGap);
```

**The allowed jump between rocks shrinks as the gap shrinks.** A step of 1 unit
is only a third of a 3.6 gap, but it is nearly half of a 2.6 one, and the fish
cannot climb any faster deep down than it could at the start. Without this line,
the deep cave zig-zags faster than anyone can fairly follow.

One last detail: rocks are flipped by giving them a **negative size**, not by
using Unity's Flip Y tickbox. The tickbox flips only the picture, which would
leave invisible walls sitting in the wrong place.

**`TryGetGapAt(x, out low, out high)` — line 261.** This says where the clear
opening is at a given spot, so that rubbish and coins land inside the cave rather
than inside a rock.

It returns `false` until both rows have actually been built that far ahead, and
the spawners simply skip their turn when that happens. A small empty stretch of
cave is much better than a coin buried in a wall.

It is **public**, meaning other scripts are allowed to call it, because
TrashSpawner, CoinSpawner and ObstacleDrift all need to ask it the same question.

**`PickRock(lastIndex)` — line 289.** Picks a random rock, but never the same one
twice in a row:

```csharp
int index = Random.Range(0, rockPrefabsArray.Length - 1);
if (index >= lastIndex) index++;
```

It picks from a list **one shorter** than the real one, then steps past the rock
used last time. Every other rock stays equally likely, and nothing has to be
rolled twice. The obvious way of doing this — pick again if it matches — can in
theory keep picking the same one forever. This version cannot.

| Method | Line | What it does in the game |
|---|---|---|
| `StartSpawning()` | 95 | Starts building, with both rows parked just off the right of the screen. |
| `StopSpawning()` | 120 | Stops building. GameManager then clears away whatever is left out there. |
| `Update()` | 128 | Keeps both rows built out past the right edge of the screen. It hands each row **the other one**, and that is what stops the walls meeting. |
| `Forget(row)` | 143 | Throws away the notes on rocks that are long gone. It counts **backwards** so that removing one does not shuffle the rest along. It is looped by hand because Unity's `RemoveAll` claims memory every time, twice a frame, which was enough to make the game stutter. |
| `FillRow()` | 157 | Adds rocks until the row reaches far enough ahead. It stops at 10, because a rock with no width would otherwise loop forever. |
| `DeepestBetween()` | 242 | The measuring tape. This is the method that makes "the walls can never meet" true. |
| `RowReaches()` | 275 | Says whether a row has been built as far as a given spot yet. |
| `HasValidRocks()` | 304 | Checks the rocks **and their pictures** before the run starts. `SpawnRock` measures the picture, so an empty slot would crash the game every frame. |
---

## 3. PlayerController.cs — why this is not Flappy Bird

**224 lines.** It reads your taps, swims, sinks, tilts the fish and dresses it in
whichever skin you chose.

This was the hardest part of the whole project. The tap was tuned three separate
times and it still felt like a bird game. **The problem turned out not to be the
tap at all. It was the fall.** A bird drops like a stone. A fish should sink.

**Nine of the eleven settings here differ from the script**, so this is the table
to be most careful with.

| Setting | Value | What it does in the game |
|---|---|---|
| **Swim Force** | **4.6** (script 4) | How far up the fish goes on each tap. |
| **Water Gravity** | **1** (script 1.2) | How strongly it is pulled down. Lower feels floatier. |
| **Water Drag** | **1.5** (script 1) | How thick the water feels. **This is the setting that stops it feeling like a bird.** |
| **Max Sink Speed** | **3.2** (script 4.5) | The fastest it is ever allowed to sink. |
| **Current Push** | **9** (script 3) | How hard a surge of water shoves it backwards. Set it to 0 and currents are off. |
| **Current Return** | **2.5** (script 4) | How hard it swims back home. Higher means it gets shoved less. |
| **Rotation Speed** | **10** (script 5) | How much it tips over for a given speed. |
| **Max Tilt Up / Down** | **20 / -40** (script 25 / -45) | The limits on tilting, measured in degrees. |
| Tilt Smoothing | 5 | How quickly it swings round to the new angle. Lower is smoother. |
| Skins | 5 fish | One for each slot in the shop. Slot 0 is the fish you start with. |

> **Current Push is the big one, and it is a good story.** The script's value of
> `3` worked out to only **0.23 units** of shove. The fish is **1.09** wide, so
> that is about a fifth of its own body. The mechanic the team was proudest of was
> very nearly switched off completely, and nobody had noticed, because nobody had
> measured it. It looked perfectly reasonable sitting in the Unity window. We
> raised it to **9**, which is a full body width of shove.
>
> If you are pushed on that: a full body width is the shove **deep in a run**.
> Surges start at half strength and build, so the first ones move you about
> **half** a body. The sum is `(Surge - 1) × Current Push ÷ Current Return`, which
> at the deepest is `0.3 × 9 ÷ 2.5 = 1.08` against a fish 1.09 wide.

> **If you are asked "what makes it feel underwater?"** Two settings: the drag and
> the sink cap. The drag makes the fish glide instead of snapping about. The cap
> means one missed tap no longer costs you half the gap. Capping the sink at 3.2
> instead of 4.4 cut the drop from a missed tap from **1.52 units to 1.00**,
> against a gap of 2.6, and the fish still dives properly.

**Values it works out for itself:** `rb` (the physics body), `IsAlive`,
`SwamThisFrame`, `homeX` and `homeY` (the spot it returns to, and the spot it bobs
around on the menu), and `fishRenderer`.

**Values locked in the code:** `IDLE_BOB_HEIGHT` 0.12, `IDLE_BOB_SPEED` 1.4 and
`IDLE_TILT` 3.

> **Why are those three locked rather than being Unity settings?** This is a real
> Unity trap and worth knowing. If you add a **new** setting to a script, and the
> scene was saved before that setting existed, the scene loads it as **0**. And 0
> for all three of these means a completely still fish on the menu, which is the
> exact bug this code was written to fix. Locking them in the code means the scene
> cannot get it wrong.

### The methods

**`SwimPressed()` — line 146.** This is the entire control scheme of the game.

```csharp
if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
```

Space bar, mouse click, or a finger on glass. All three count as a swim, which is
how the game works on a computer and on a phone without any extra code.

**The first line is the clever one.** If your finger landed on a button, it
answers no. That is what stops tapping the Pause button from also making the fish
swim.

`wasPressedThisFrame` means the exact moment the button went down, which is why
holding your finger on the screen does not make the fish fly.

**`FixedUpdate()` — line 158.** This is the physics, and it is a tug of war.

```csharp
float push = (ObstacleMovement.Surge - 1f) * currentPush;
float home = (homeX - transform.position.x) * currentReturn;
rb.linearVelocityX = home - push;
```

`push` is the current shoving the fish left. In calm water `Surge` sits at 1, so
the sum is `(1-1) * 9`, which is **zero**. No current, no shove.

`home` is the fish swimming back to where it belongs. The important part is that
**the further out it gets, the harder it pulls**.

Because the pull grows with distance, the two forces naturally settle at the
point where they cancel out. So a current can only ever shove you so far, and it
**never builds up**, no matter how long it lasts. Nothing had to be clamped or
limited. The maths does it on its own.

```csharp
if (rb.linearVelocity.y < -maxSinkSpeed) rb.linearVelocityY = -maxSinkSpeed;
```

That is the sink cap. Four lines of code, and the single biggest thing that makes
the fish feel like a fish.

After that comes the tilt. The angle is worked out from how fast the fish is
moving up or down, then `Mathf.Clamp`ed so it can never flip right over, and run
through `LerpAngle` so it swings round gradually instead of snapping.

> **Why `FixedUpdate` instead of `Update`?** Physics needs a steady clock.
> `Update` runs as fast as the phone can manage, so physics written in there
> behaves differently on a fast phone and a slow one. `FixedUpdate` always runs at
> the same rate, so the fish feels the same everywhere.

**`Update()` — line 102.** Three guards at the top, and each one stops a real bug.
A dead fish does not swim. A frozen game means your tap is a **button press**
instead, and that single check covers the menu, the pause screen *and* the game
over screen. Then `SwimPressed()` throws out any tap that landed on a button.

After the guards comes `rb.linearVelocityY = swimForce`. Notice it **sets** the
upward speed rather than adding to it. That is why tapping very fast does not
launch the fish off the top of the screen.

| Method | Line | What it does in the game |
|---|---|---|
| `Awake()` | 82 | Finds the physics body and the picture, then applies the drag and gravity **in code**, so those beat whatever the scene saved on the Rigidbody. Tune the fish on this script, never on its Rigidbody 2D. `freezeRotation` stops the physics spinning the fish round when it bumps something. |
| `MenuIdle()` | 127 | Bobs the fish gently on the menu. It uses the **real** clock, because game time is frozen on the menu. The height is worked out **fresh from `homeY`** every frame rather than nudged along, because a menu can sit there for minutes, and small errors piling up is what once walked the background out of line. |
| `OnCollisionEnter2D()` | 187 | Sets `IsAlive = false`. `PlayerHit` is what actually ends the run. |
| `ResetPlayer()` | 195 | Puts the fish back to the start: position, angle, stopped, alive, and wearing the right skin. |
| `ApplySkin()` | 212 | Puts on whichever fish you chose in the shop. If your saved choice no longer exists, it falls back to fish 0 rather than leaving the last picture on screen. It reads the name of the save key from `FishInventory.EQUIPPED_KEY`, so the two files can never disagree about what it is called. |

---

## 4. ObstacleMovement.cs — one speed, written in one place

**63 lines, and it holds the game together.** Its job is to slide one object to
the left and delete it once it is gone. Every rock, every coin and every piece of
rubbish has one of these on it.

| Name | Value | What it does in the game |
|---|---|---|
| Speed | 5 | How fast things slide left. This is set on the rock **prefabs**, not in the scene. |
| `SpeedMultiplier` | starts at 1 | How much faster than normal the cave is going. **Shared by everything.** |
| `Surge` | starts at 1 | Just the wave part of that. The **fish** reads this one too. |
| `DESPAWN_MARGIN` | 1 | How far off screen something must be before it is deleted, so nothing vanishes in view. |

Both of those shared numbers are marked `static`, which means there is one copy
for the entire game rather than one per object. They can be read from anywhere
but only written here.

**`SetPace(speedMultiplier, surge)` — line 28.** Two lines long, and the most
important design decision in the project:

```csharp
SpeedMultiplier = speedMultiplier;
Surge = surge;
```

Rocks, rubbish and coins cannot drift out of step with each other, because there
is nothing for them to drift out of step *with*. They all read the same number.

> **If you are asked "why only let one thing write it?"** Because it leaked
> between runs once. Unity keeps shared values like this between presses of Play
> so the game starts faster, which meant stopping Play in the middle of a surge
> left the **next** run starting mid-surge. If a second script had also been
> allowed to write it, that bug would have been nearly impossible to find.

**`ResetStatics()` — line 38.** This is the fix for that leak. It is marked
`[RuntimeInitializeOnLoadMethod]`, which is an **attribute**: an instruction to
Unity rather than code that runs. This one tells Unity to call the method
automatically before anything else loads.

**`Update()` — line 53.** The line `speed * SpeedMultiplier` is the entire
difficulty curve in a single multiplication: 5 × (0.9 up to 1.6) gives 4.5 up to
8.

It waits for the object's **right** edge to pass the screen, not its middle. A
rock can be up to 10 units wide, so checking the middle would make half of it
disappear while you were still looking at it. **`Awake()` — line 45** is what
remembers the renderer that check needs.

---

# Part 2 — The supporting cast

Fourteen more scripts, in shorter form. Skim these once so you know where to look
if you get asked.

---

## 5. TrashSpawner.cs — the difficulty curve lives here

**274 lines.** It drops the rubbish, **and it runs the difficulty of the whole
game**. If anyone asks "where does the game get harder?", this file is the answer.

| Setting | Value | Setting | Value |
|---|---|---|---|
| **Min Size** | **0.6** (script 0.5) | Surge Strength | 1.3 |
| Max Size | 0.9 | Surge Seconds | 2.5 |
| Min / Max Spin | 10 / 45 | Surge Interval | 9 |
| Drift Y | 0.4 | Debris Current Push | 1.2 |
| **Drift Min / Max Speed** | **0.1 / 0.32** (script 0.15 / 0.5) | Start / Min Interval | 3 / 0.9 |
| **Ramp Seconds** | **150** (script 90) | Max On Screen | 8 |
| **Start Speed Multiplier** | **0.9** (script 1.15) | Max Speed Multiplier | 1.6 |

Put together, the speed runs from **0.9 × 5 = 4.5** up to **1.6 × 5 = 8**, over
**150 seconds**.

Four values are fixed in the code: `CALM_PACE` 1, `CALM_SURGE` 1,
`SURGE_AT_START` 0.5 and `SPAWN_MARGIN` 1. One wired slot: **Trash Prefabs**,
which is the list of rubbish pieces to pick from.

> **If you are asked "why is the surge interval never random?"** So that you can
> learn the rhythm and plan around it. A random shove is just bad luck. One on a
> fixed 11.5-second beat is something a player can get good at.

**`Update()` — line 138.** This is the difficulty curve itself:

```csharp
float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
float ramped = Mathf.Lerp(startSpeedMultiplier, maxSpeedMultiplier, progress);
float surge = Surge(progress);
ObstacleMovement.SetPace(ramped * surge, surge);
```

Notice `surge` is published **twice**: once multiplied into the overall speed, and
once on its own. That second one is how the **fish** feels the current, because
`PlayerController` reads `Surge` directly.

Rubbish is kept in check two ways at once: a timer that gradually speeds up, and
a hard cap on how many pieces may be on screen.

**`Surge(progress)` — line 166.** The line
`elapsed % (surgeInterval + surgeSeconds)` makes the clock repeat every 11.5
seconds, and that repeat is the rhythm the player learns.

It is worked out **purely from the clock**, so nothing is remembered between
frames. That means it cannot drift out of time and it cannot get stuck on.

`Mathf.Sin(t * Mathf.PI)` draws a smooth arch, so the surge swells and fades away
rather than snapping on like a switch.

The strength grows on the same counter as the speed, so shallow water shoves you
a little and the deep cave shoves you about twice as hard.

**`SpawnPiece()` — line 188.** It uses `magnitude`, which is the
corner-to-corner length of a piece rather than its width, because a piece
**spins** while it floats and its corners sweep further out than its sides.

Then `clearance = radius + driftY` — the piece **plus how far it is allowed to
wander** — so it can never drift into a rock later on.

If the rocks are not built that far ahead yet, or the gap is too narrow for the
piece, it gives up quietly rather than forcing it.

Finally, one piece of rubbish is really four small scripts stacked on one object:
`ObstacleMovement` to slide it, `TrashSpin` to spin it, and `ObstacleDrift` to bob
it and carry it on the current.

| Method | Line | What it does in the game |
|---|---|---|
| `Awake()` | 96 | Finds the rock spawner, because that is what owns the gap. |
| `StartSpawning()` | 102 | Starts dropping rubbish, with the difficulty back at the beginning. |
| `StopSpawning()` | 114 | Stops, **and puts the cave back to calm**. The speed is shared, so without that the difficulty would survive into the next run. |
| `ResetRun()` | 122 | Wipes the progress. It resets the **surge** as well. `Update` would overwrite it anyway, but a stale value sitting in something called "current surge" makes both numbers untrustworthy when you are trying to debug. |
| `PickTrash()` | 240 | Picks a random piece, never the same one twice in a row. |
| `HasValidTrash()` | 255 | Checks the pieces and their pictures before the run starts. |

---

## 6. CoinSpawner.cs — the only thing you may touch

**182 lines.** It drops the coins into the gap. It sits on the **same object** as
CaveSpawner, so clearing the rocks clears the coins too.

It does **not** make coins speed up. Each coin carries an `ObstacleMovement` that
reads the shared speed. The only thing this script changes over time is how
**often** a coin appears.

| Setting | Value | Setting | Value |
|---|---|---|---|
| Coin Size | 0.7 | **Ramp Seconds** | **150** (script 90) |
| Spin Degrees | 25 | Start / Min Interval | 2.5 / 1 |
| Drift Y | 0.4 | Max On Screen | 5 |
| Wall Gap | 0.35 | Coin Current Push | 1.2 |
| **Drift Min / Max Speed** | **0.1 / 0.32** (script 0.15 / 0.5) | `SPAWN_MARGIN` | 1 |

One wired slot: **Coin Prefab**, the coin to make copies of.

> **"Its collider MUST stay Is Trigger"** is written in the file, and it is the
> most important line in it. A trigger means *touch*. A normal collider means
> *bump*, and `PlayerHit` ends the run on any bump at all. So if that tickbox ever
> came off, you would **die collecting a coin**.

`CoinSprite` on line 53 is a **property** rather than a method. A property is
something you read like a value but which is really worked out on the spot. This
one hands back the coin's picture, so the counter uses the same image with no
extra slot for anyone to wire up wrongly.

**`SpawnCoin()` — line 126.** Much like TrashSpawner's version, with one extra
term: `clearance = radius + driftY + wallGap`.

Coins get that extra bit of clearance and rubbish does not, and the reason
matters. **A coin hugging a rock is a trap**, because reaching for it kills you.
Rubbish is already something you are trying to avoid, so it needs no such
protection.

It reuses `TrashSpin` to spin the coins, because a spin is a spin.

**`Update()` — line 103.** The line `liveCoins.RemoveAll(coin => coin == null)`
matters more than it looks. Collected coins leave **holes** in the list. Without
clearing them out, the on-screen cap keeps counting coins you already picked up,
and eventually no new coins appear at all.

The rest: **`Awake()`** line 76, **`StartSpawning()`** line 84 (a badly set-up
coin simply does not start, because a run with no coins is better than an error
every frame), **`StopSpawning()`** line 96 and **`HasValidCoin()`** line 165.

---

## 7. SpotLight2D.cs — the lamp on the fish's head

**99 lines, and every setting matches the scene**, so there is nothing here to
catch you out.

| Setting | Value | What it does in the game |
|---|---|---|
| Deep Multiplier | 1.6 | How much brighter the lamp gets at the darkest point. Set it to 1 and depth changes nothing. |
| Pulse Boost | 0.3 | The extra flash of brightness for one swim. Set it to 0 and the flash is off. |
| Recover Speed | 7 | How quickly that flash fades away. |
| Flicker Depth | 0.16 | How much the lamp wobbles. Set it to 0 and it is a steady lamp. |
| Flicker Speed | 6.5 | How fast the wobble runs. |

> **Why make it wobble at all?** A real anglerfish's bulb is part of a living
> animal. A light that holds perfectly still looks like a torch taped to a fish.

**`Update()` — line 74** is basically the whole script, and four things in it are
worth knowing:

```csharp
float resting = Mathf.Lerp(1f, deepMultiplier, GameManager.Instance.CaveDepth);
float peak = resting + pulseBoost;
if (swimmer != null && swimmer.SwamThisFrame) level = Mathf.Min(level + pulseBoost, peak);
level = Mathf.Lerp(level, resting, Time.deltaTime * recoverSpeed);
float flicker = 1f + (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0f) - 0.5f) * 2f * flickerDepth;
lure.intensity = authoredIntensity * level * flicker;
```

**One.** It reads the same `CaveDepth` as the cave light, so the two can never
drift apart.

**Two.** It watches `SwamThisFrame` rather than watching for taps. That means it
inherits every one of the fish's rules for free: no flash while dead, no flash
while paused, none on the menu, and none when your tap landed on a button. None
of those rules had to be written twice.

**Three.** That `Mathf.Min` is a limit, and it is also a bug fix:

> **The best bug in the project.** Making the flare depth-aware turned a line that
> had a ceiling into plain `level += pulseBoost`, with nothing stopping it.
> Tapping is the one input a player can spam as fast as they like, and mashing the
> screen reached **9 times** the brightness it was meant to, which was enough to
> remove the darkness from the game entirely. We proved it by simulating it:
> **1.25** against a ceiling of 1.30 with the limit in place, and **11.7** with it
> taken out.

**Four.** It uses `PerlinNoise` rather than `Random`. Random numbers jump about
and look like a loose wire. Perlin noise wanders smoothly and looks alive.

Also notice it uses `unscaledTime` for the flicker but `deltaTime` for the fade.
That is deliberate: the flash **holds** behind the pause screen instead of quietly
fading out while the game is frozen.

**`authoredIntensity`** is the Intensity value you typed into Unity. It is read
once in **`Awake()` — line 47** and treated as this bulb's 100%. After that it is
only ever **nudged up and down**, never replaced.

> The old version of this script wrote the brightness *from its own field*, which
> meant the Light 2D slider in Unity did nothing at all once the game started.
> **Two sessions were lost to that.** Because the value is read in `Awake`, you
> have to set it with the game **stopped**.

`Awake` also finds the fish using `GetComponentInParent`. The lamp is attached
underneath the fish in the scene, so there is no Inspector slot for anyone to
accidentally unplug.

---

## 8. ObstacleDrift.cs — making debris look alive

**119 lines, and no Unity settings at all** — the spawners hand it everything it
needs. Its job is to bob a piece of rubbish up and down so it looks like it is
floating rather than riding an invisible rail. Up and down only; sliding sideways
is `ObstacleMovement`'s job.

Fixed in code: `MIN_SECONDS_UNTIL_TURN` 1.5 and `MAX_SECONDS_UNTIL_TURN` 3.5.

It holds: `allowedDrift`, `driftOffset`, `driftVelocity`, `minDriftSpeed`,
`maxDriftSpeed`, `secondsUntilTurn`, `rockSpawner`, `currentPush`, `pieceRadius`
and `wallGap`.

| Method | Line | What it does in the game |
|---|---|---|
| `Configure()` | 28 | Sets how far and how fast this piece may wander. |
| `ConfigureCurrent()` | 38 | Signs the piece up to be carried by the current. There are two separate setup methods so that a piece can bob **without** being carried, which keeps the current optional. |
| `PickVelocity()` | 49 | Chooses a new direction and how long to keep it. **Every piece rolls its own**, which is what stops the rubbish moving like one big sheet. |
| `Update()` | 56 | Adds the **change** in height rather than setting the height. Setting the position directly would make this and `ObstacleMovement` fight each other every frame, and the piece would judder. It also flips direction at the edge, because otherwise every piece ends up piled against its limit and the wall of rubbish looks flat. |
| `CarryForward()` | 85 | Sweeps the piece along, harder during a surge. |

`CarryForward` is the careful one. The piece moves **ahead** of the rocks, which
means it arrives somewhere nobody ever checked was safe. So the gap is measured
**again every frame** and the piece is held inside it.

If it will not fit, the method drops the wall gap and tries again with just the
piece. If even that fails it gives up on *holding the piece clear* rather than
giving up on the piece, because otherwise the maths flips over and shoves it
straight through the far wall.

And if a piece had to be pushed off a wall, it also **turns round**. Without
that, it drives straight back into the wall next frame and buzzes there like a
fly on a window.

---

## 9. PlayerHit.cs — the two ways to die

**48 lines.** One setting: Below Screen Margin **1**, which is how far below the
screen the fish has to sink before the run ends. The margin is there so the fish
disappears completely first. Ending the run right on the screen edge looks like a
glitch.

| Method | Line | What it does in the game |
|---|---|---|
| `Awake()` | 14 | Finds the camera. |
| `Update()` | 22 | Watches for the fish sinking off the bottom. **There is no floor in this game**, so without this the fish would fall forever and the run would never end. |
| `OnCollisionEnter2D()` | 36 | The fish bumped a rock or a piece of rubbish. Unity calls this method itself when that happens. |
| `EndRun()` | 42 | Two ways in, **one** way out. That means a change to how a run ends gets made once rather than twice. |

---

## 10. CoinPickup.cs — collecting, not crashing

**27 lines and one method.** `OnTriggerEnter2D()` — line 11:

```csharp
if (collected) return;
if (collision.GetComponent<PlayerController>() == null) return;
collected = true;
GameManager.Instance.AddCoin();
Destroy(gameObject);
```

It is **`OnTriggerEnter2D`, not `OnCollisionEnter2D`**, and that one word is the
whole reason collecting a coin does not kill you. Trigger means touch. Collision
means bump.

There are two guards. `collected` stops one coin paying out twice if the fish
overlaps it across two frames. The `PlayerController` check stops a drifting piece
of rubbish collecting your coins for you.

---

## 11. TrashSpin.cs — the smallest script in the game

**24 lines.** It has one value, Degrees Per Second, set to 20 in the code but
**given to each piece by the spawners** through `SetSpin()` on line 13. It is
deliberately *not* in the scene, because every piece wants a different spin.

`Update()` on line 19 is a single line:
`transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime)`.

It uses **game** time, so the spin freezes along with everything else when you
pause. The real clock would leave the rubbish spinning away on a frozen screen.

---

## 12. ScrollObject.cs — the endless background

**156 lines.** Scroll Speed is **2**. The trick is that there are several copies
of the background in a row, and whichever one slides off the left goes to the back
of the queue. A handful of pictures then look endless.

It holds `spriteRenderer`, `width`, `startX`, `copyCount`, `anchorX`, `index`,
`startTime`, `leftEdge`, `SEAM_OVERLAP` at **0.02**, and `Step`, which is the
width minus that overlap.

> **Why overlap by 0.02?** Because even a perfect join leaves a hairline crack on
> the pixel grid, and the pale camera background flashes through it. That works
> out to about 2 pixels at 1080p. CaveSpawner overlaps its rocks for exactly the
> same reason.

**`Update()` — line 90.** The position is **worked out fresh from the clock** every
frame, and never added up:

```csharp
float travelled = Mathf.Repeat((Time.time - startTime) * scrollSpeed, span);
float x = anchorX + index * Step - travelled;
if (x + width / 2f <= leftEdge) x += span;
```

> **The bug this fixed.** Nudging each copy along a bit at a time let tiny
> rounding errors creep in, and the gap between copies grew a little on every trip
> round the queue, until eventually you could see daylight through it. Working it
> out from the clock cannot drift, because there is nothing being added up to go
> wrong. `MenuIdle` in PlayerController was rewritten the same way for the same
> reason.

Recycling a copy waits for its **right** edge to pass the screen edge. An earlier
version used a fixed distance, which once wrapped a copy round while 1.9 units of
it were still on screen, leaving a bare strip.

| Method | Line | What it does in the game |
|---|---|---|
| `Awake()` | 44 | Measures the width and notes the start time. |
| `Start()` | 55 | Takes the width, the speed **and the start time** from copy 0, so the copies cannot disagree. A difference of even one frame would bake in a permanent gap. |
| `AddMissingCopies()` | 113 | Makes copies until the row covers the screen, plus one spare. It is done in code so that a wider phone cannot open up a gap. |
| `FindCopies()` | 135 | Matches copies on the **picture** they use, which is what keeps a far background and a near background from being muddled together. |

---

## 13. PauseBlur.cs — the frozen, blurry backdrop

**177 lines.** This one is not a MonoBehaviour. It is a plain class that
GameManager creates the first time it is needed, so a run where you never pause
never pays for it.

It holds `canvasRoot`, `dimDarkness` (0.7), `blurSteps` (4), `dim`, `blur` and
`blurTexture`.

**`Capture()` — line 115** is the most technical method in the project, but the
idea behind it is simple: **take one photo of the screen, then halve its size four
times.** Each halving averages neighbouring pixels together, so the picture goes
soft. Then stretch that small soft photo back over the whole screen.

**No shader, no post-processing stack, no extra asset.** The graphics card's own
smoothing does all the work. And because the game is frozen anyway, one photo
looks exactly the same as blurring every frame would, and costs nothing per frame.

Three details here were hard won:

- **The `24`** asks for depth bits. Nothing in the blur uses depth, but Unity 6's
  Render Graph **refuses** to draw a camera into a target without them, and
  complains on every single pause.
- **`FilterMode.Bilinear`, set twice.** Borrowed textures turn up still carrying
  the last user's setting. Bilinear averages each 2×2 block of pixels; without it,
  Unity grabs one pixel out of every four, which comes out **chunky rather than
  blurry** — and those hard edges survive every later shrink.
- **Background alpha forced to 1.** The camera clears to see-through, halving the
  image smears that transparency outwards, and `RawImage` multiplies by it. The
  result was the game's own counters ghosting as dark smudges. Forcing it solid
  fixed **every** overlay element at once, not just the one we noticed.

| Method | Line | What it does in the game |
|---|---|---|
| `Show()` | 29 | Darkens the frozen game and soaks up taps on it. The dark sheet goes on **the Canvas itself**, because inside one group of buttons it could not cover the others. The draw order is blur, then sheet, then buttons, so anything **not** deliberately lifted stays buried. That is how the Pause button becomes untappable on purpose. The caller passes in whatever must stay readable, so this method never needs to know what a "game over panel" is. |
| `Release()` | 79 | Hands the memory back. This kind of texture is **not** freed automatically. |
| `Stretch()` | 86 | Pins something to all four corners, so it fills any shape of phone. |
| `Raise()` | 98 | Walks **up** the tree of objects and lifts the whole group. Lifting the object on its own only moves it to the front of its own group, where it is still buried. |

---

## 14. FishInventory.cs — the fish shop

**229 lines.** A shelf of fish you can swipe through. The ones you have not bought
are grey and cost coins. The ones you own can be worn, one at a time.

| Setting | Value | What it does in the game |
|---|---|---|
| Price | 300 | The coins one fish costs. It is the same for all of them. |
| Wired slots | Content, Locked Art, Fish Art, Buy Art, Select Art, Equipped Art, Wallet Text | The shelf itself and the pictures on its cards. |

> **Why does the shop have its own coin total?** Because the run counters are
> hidden while you are on the menu. Without the wallet, you could not see your
> coins on the one screen where you spend them.

### `ownedMask` — the cleverest variable in the project

**Picture a row of light switches, one for each fish. A switch being on means you
own that fish.** All of those switches are packed into one single whole number.

```csharp
return index >= 0 && (ownedMask & (1 << index)) != 0;
```

`1 << index` builds a number with only that one switch turned on. The `&` then
compares the two numbers switch by switch. So the whole line is asking one
question: *is switch number `index` on?*

**Why do it that way?** Two reasons. Unity's save cannot store a list at all. And
keeping a separate value for each fish would mean a separate chance to update one
and forget the rest. One number means one save, and one place to be wrong.

> **Why is the save called "FishOwned2"?** That `2` on the end retires every save
> from back when the shelf had six cards. Slot 4 used to be free, and slot 4 is
> **now the paid fish** — so trusting an old save would hand it over for nothing.
> The coins are kept under a separate key, so nobody loses their money in the
> process.

> **Why check `index >= 0`?** Because old saves used `-1` to mean "nothing worn",
> and the shifting maths turns `-1` into `31`. That was a real bug. It got caught,
> and now it is guarded against.

| Method | Line | What it does in the game |
|---|---|---|
| `Awake()` | 74 | Finds the slots **in code**, so adding a sixth fish needs no change here. It contains the line `int index = i;`, which looks completely pointless but is essential: without it, all five buttons share one counter and every one of them acts on the **last** slot. |
| `OnEnable()` | 129 | Re-reads your save **every time the shop opens**, because the coins you just earned change what you can afford. |
| `Load()` | 135 | Turns on every switch **except the last one**, so all but one fish come free and you save up for that one. It works it out from how many slots there are, so adding a slot automatically makes *that* one the new paid fish. |
| `Owns()` | 154 | Reads one switch out of the packed number. |
| `OnBlockClicked()` | 160 | Buys the fish if you can afford it, then puts it on. `TrySpendCoins` checks **and** subtracts in one go, so there is no gap for anyone to get a fish unpaid. It saves twice, immediately, because **a fish that vanishes when you close the app is 300 stolen coins**. Buying also wears it, since nobody spends 300 coins and then wants to press a second button. |
| `Refresh()` | 189 | Redraws **all** the cards **every** time. That is what makes "only one fish is worn" true, with nobody ever having to un-wear the old one. It also sets `interactable = false` to grey a button out, which stops it eating taps as well as greying it — otherwise being broke just looks like a broken button. |

> **Written as a warning in the code:** do **not** read `EquippedIndex` to decide
> which fish to use in the game. The shop panel is switched off when the game
> loads, so its setup has not run yet and it reads 0. `PlayerController` reads the
> **save** instead, which is always right.

---

## 15. LegalNotice.cs — the terms and privacy screens

**313 lines**, and another plain class rather than a MonoBehaviour. There are two
screens: a one-off gate you have to accept, and a scrollable reader holding both
full documents.

**Everything here is built in code**, deliberately, so that these screens could be
added without touching the scene file at all.

It holds `ACCEPTED_KEY`, `TERMS_PATH`, `PRIVACY_PATH`, `GATE_SUMMARY`,
`PlateColour` (0.204, 0.627, 0.604 — taken straight off the shop artwork so it
looks like part of the game rather than a system pop-up), `canvasRoot`,
`gameFont`, `accepted`, `gate` and `reader`.

The gate says, plainly: this is an amateur school project made by students and
given away free; it is not finished and not optimised; **a large part of it was
made with AI assistance, including the code**; there is no warranty and no
liability; and it collects nothing at all — no internet, no accounts, no ads, no
tracking. The two full documents together come to about **1,399 words**.

> **Why keep the gate itself short?** A wall of text on first launch gets closed
> unread. The full version is one tap away on the same screen.

| Method | Line | What it does in the game |
|---|---|---|
| constructor | 48 | Just remembers where to build things. It builds nothing yet. |
| `Accepted` | 54 | True once you have accepted on this device. |
| `ClearAcceptance()` | 58 | Forgets that, so the gate shows again. For testing. |
| `ShowGateIfNeeded()` | 67 | Puts the gate up unless you have already accepted. `SetAsLastSibling` puts it on top, and the panel behind it is solid, so the menu cannot be tapped through it. |
| `Quit()` | 86 | `#if UNITY_EDITOR` is a **compiler switch**: the code inside only exists in one kind of build. In the Editor it stops Play; in the real build it calls `Application.Quit()`. That is why Exit looked broken while we were testing. Exit really does quit, because the alternative is playing a game whose terms you never agreed to. |
| `OpenReader()` | 97 | Opens both full documents, scrollable. |
| `BuildGate()` | 109 | Builds the summary, the link to the full terms, and the Accept and Exit buttons. **The harmless middle button sits between Accept and Exit on purpose**, so neither of them is the one you hit by accident. |
| `BuildReader()` | 135 | Both documents, one after the other. |
| `Load()` | 151 | Returns a readable message if a file is missing, so a broken build cannot leave the panel completely **blank**. |
| `BuildPanel()` | 163 | The full-screen background. Its image is what **stops taps reaching the menu**, so it is not decoration. |
| `BuildTitle()` | 180 | A heading across the top. |
| `BuildScrollingText()` | 206 | Makes the text **scroll** rather than shrinking the font, because a 1,399-word document shrunk to fit a phone screen is not really being shown to anybody. |
| `ApplyGameFont()` | 254 | **Borrows** the font from a label already in the scene instead of using an Inspector slot. That is what let these screens skip the scene file entirely. |
| `BuildButton()` | 274 | Makes one button, routed through `GameManager.PlayClick` so it sounds like every other button in the game. |

---

## 16. CameraEdges.cs — where the screen edges are

**29 lines**, and a `static` class, meaning there is no object to create. Any
script just calls it by name.

```csharp
public static float HalfWidth => Cam.orthographicSize * Cam.aspect;
public static float Left  => Cam.transform.position.x - HalfWidth;
public static float Right => Cam.transform.position.x + HalfWidth;
public static float Top   => Cam.transform.position.y + Cam.orthographicSize;
```

**This is why the game fits every phone.** The height is **fixed** and the width
**follows the phone**. So a wider phone shows **more cave**, rather than showing a
stretched one.

`cached` holds onto the camera, and `Cam` re-finds it if it has gone away. Unity
reports a destroyed camera as null, and handing a broken one back would crash
whatever asked for it.

> **If you are asked why this exists at all.** `Camera.main` is slow to look up,
> and the screen edges used to be recalculated in several different scripts, once
> per object, every frame. The line for the presentation is: *"screen edges
> computed once, not per object per frame."*

---

## 17. ButtonPressSound.cs — clicks that feel fast

**26 lines**, and it is added to every button automatically by `BuildSound()`. It
clicks on **finger-down** rather than finger-up, which feels noticeably faster.

**`OnPointerDown()` — line 18** opens with `if (!button.interactable) return;`,
and that is **a real bug fix**. Unity only checks that a button is enabled at
finger-**up**. Without this line, the greyed-out BUY button still clicked when you
were broke, which made it sound like the purchase had worked.

**`Awake()` — line 12** finds the button. The
`[RequireComponent(typeof(Button))]` attribute makes Unity refuse to let the two
be separated.

---

## 18. FishShopDebug.cs — the Tools menu for testing

**90 lines, and it lives in `Assets/Editor`, so it is not in the built game at
all.** You find it in Unity under **Tools → Fish Shop**.

The save keys come **from FishInventory** rather than being retyped here. They
were typed out twice once, and *a reset that clears a key nothing reads looks
exactly like a reset that worked.*

| Menu item | Method | Line |
|---|---|---|
| Reset Legal Acceptance | `ResetLegal()` | 24 |
| Reset Skins | `ResetSkins()` | 36 |
| Set Coins to 0 / 100 / 854 | `Coins0()` `Coins100()` `Coins854()` | 46 / 49 / 52 |
| Show Save State | `Show()` | 55 |
| shared by the three coin items | `SetCoins(int)` | 60 |

All six are wired up by the `[MenuItem(...)]` attribute, so you never call them
from code. The three coin items all share `SetCoins`, which keeps one place
writing your coins.

> **Why this script exists, and it caught the team twice.** Two different things
> keep their own copy of your save. The **running game** holds your coins and owned
> fish in memory, and saves over your change when it stops. The **Editor** holds
> its own copy as well, so editing the save from outside Unity does nothing at
> all. So the rule is: **always stop Play first, and always change things through
> this menu.**

**`PlayStopped()` — line 69** enforces that with a pop-up, rather than failing
quietly and leaving you confused.

**`ResetSkins()`** writes a plain `0`, **not** a list of the free fish. Which fish
come free is `FishInventory`'s decision, and copying that rule into a second file
is exactly how the two would end up disagreeing.

**`Report()` — line 84** prints your save to the Console. It is the only honest
way to see what the game was *actually* handed.
---

# Part 3 — The safety net

This part is for the presentation itself rather than for understanding the code.

---

## If they point at a line you do not know

Say: **"I would have to check that one."**

That is a better answer than a guess, because **a wrong answer given confidently
is what actually costs you marks.** Nobody holds 3,163 lines of code in their
head.

That said, you can nearly always say something true about a line without knowing
what that particular line does. Here is how:

| If the line has... | You can honestly say |
|---|---|
| `if (x == null) return;` | "That is a guard. It stops the game crashing if that was never wired up." |
| `Time.deltaTime` | "That makes movement run at the same speed on a fast phone and a slow one." |
| `Clamp` / `Min` / `Max` | "That is a limit, so the number can never go somewhere unfair." |
| `Mathf.Lerp` | "That mixes two values together. It is how things fade smoothly instead of jumping." |
| `PlayerPrefs` | "That is the save. It writes to the phone, so coins and scores survive closing the app." |
| `static` | "That means it is shared by the whole game — one copy, rather than one each." |
| `const` | "That is locked in the code on purpose, so the Unity window cannot change it." |
| `GameManager.Instance` | "That is how any script reaches the one GameManager." |
| `CaveDepth` | "That is the one depth number. The light, the rock gap and the lamp all read it." |
| `Instantiate` / `Destroy` | "That makes a new copy while the game is running, or removes one." |
| `GetComponent` | "That finds another script or part sitting on the same object." |

## Questions to expect

**"How does the game get harder?"**

Three things at once, all off one depth number. The light dims from 0.03 to 0.02.
The gap narrows from 3.6 to 2.6. The cave speeds up from 4.5 to 8 over 150
seconds.

**"Why not just make it faster?"**

Because speed on its own gets unfair very quickly. Darkness takes away your
**information** rather than your reaction time, so it feels tense instead of
cheap.

**"Is the cave random?"**

The shapes are. The gap is not. The opposite wall gets measured before every
single rock is placed, and a hard floor of 2.2 is locked into the code.

**"What makes it not Flappy Bird?"**

The water drag and the sink cap. A bird falls like a stone; our fish sinks at no
more than 3.2. That one change cut the drop from a missed tap from 1.52 units to
1.00, against a gap of 2.6, and it still dives properly.

**"Why have a global light at all if the fish has a lamp?"**

This is the strongest answer you have, because it is **measured rather than felt**:

| Measurement | Value |
|---|---|
| Rock speed | 3.5 at the start, 8 by 90 seconds, 10.4 mid-surge |
| Distance from fish to right edge | 11.85 units |
| How far the lure reaches | 3.63 units |
| **Warning time at 8 units/second** | **0.45 seconds** |
| **Warning time during a surge** | **0.35 seconds** |

Human reaction time on its own is about 0.25 seconds, and after reacting the fish
still has to physically travel. So at full speed the lure **cannot** be the only
light in the game — you have to be reading the rock silhouettes against the
background. **The global light is a fairness mechanism, not atmosphere.**

**"How much of it did AI write?"**

A large part, including code. It is disclosed in the README and on the screen the
player has to accept before playing. Say it plainly and move on. It is already
written down, so there is nothing to defend.

**"What is not finished?"**

The sprites are not packed into an atlas, so the game draws more than it needs to.
And it has only been tested on one phone. That is the honest list.

**"What would you do next?"**

Testing on more devices, the sprite atlas, and a sixth fish.

**"What was the hardest bug?"**

Pick whichever of these three you tell best:

The **score counter was eating the taps**. A text box 1018 by 141 pixels absorbed
every tap that landed on it, and one line fixed it.

**Tap-spam broke the darkness.** Mashing the screen reached 9 times the intended
brightness: 1.25 with the limit in place against 11.7 without it.

**The current was almost switched off.** It was giving 0.23 units of shove to a
fish 1.09 wide, and it looked perfectly fine in the Unity window because nobody
had measured it.

## The 30 settings where the script and the scene disagree

**The right-hand column is what the game actually uses.**

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
**FishInventory** and the rest of the spawners matches between the two. Those you
can safely read straight off the screen.

## Numbers worth memorising

The fish is **1.33** tall, of which **1.11** can hit things, and **1.09** wide.

The gap runs from **3.6** down to **2.6**, with a hard floor of **2.2**.

The light runs from **0.03** down to **0.02**, reaching its darkest at **300
metres**.

The speed runs from **4.5** up to **8** over **150 seconds**.

A surge comes every **11.5 seconds**: **9** seconds of calm water, then a push
lasting **2.5**.

A swim is worth **4.6**, and the fastest sink is **3.2**.

A fish in the shop costs **300** coins, and the screen is about **17.8** units
wide.

## Word list

| Word | What it means |
|---|---|
| `Awake()` / `Start()` | Runs once at the beginning. `Start` is slightly later, once the other scripts exist. |
| `Update()` | Runs every frame, about 60 times a second. |
| `FixedUpdate()` | Like `Update`, but on a steady clock. Physics goes in here. |
| `OnEnable()` / `OnDestroy()` | Runs when the object is switched on, or when it is removed. |
| MonoBehaviour | A script that can be attached to an object in the scene. |
| `[SerializeField]` | Turns a hidden value into a box you can type into, in the Unity window. |
| `private` / `public` | Only this script can see it / other scripts can see it too. |
| `const` | Locked in the code. Unity cannot change it. |
| `static` | One copy, shared by the whole game. |
| `float` / `int` / `bool` | A decimal number / a whole number / a true-or-false. |
| `null` | Empty. Nothing there. |
| guard | An early `if (...) return;` that stops a method which should not be running. |
| prefab | A saved object you stamp copies of while the game runs. |
| collider / trigger | A shape things bump into / a shape that notices a touch but lets things pass. **Coins are triggers.** |
| Rigidbody2D | Unity's physics body. It is what makes the fish fall. |
| singleton | A script with only one copy ever, which anything can reach. |
| `PlayerPrefs` | The small save file on the phone. |
| `Time.timeScale` | The speed of game time. Setting it to `0` freezes everything, and that is the whole pause. |
| `Time.unscaledTime` | The real clock. It keeps running while the game is frozen. |
| `raycastTarget` | Whether something on screen catches taps. Switching it off is how counters stop eating them. |
| attribute | The bits in square brackets. They are instructions to Unity, not code that runs. |

## Before you present

- Say each Part 1 script out loud once. If a sentence will not come out properly,
  it is not yours yet. Shorten it until it is.
- **Do not read the comments off the screen.** They were written for somebody
  reading the code later. You are talking to a room.
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
