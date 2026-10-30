# Code notes — what to say when the code is on screen

Not documentation. This is what you **say out loud** if a slide shows code, or
someone points at a script and asks.

Every number here was read from `SampleScene.unity`, not from the script
defaults. The saved scene wins, so the script defaults are the wrong numbers to
quote. Learn four scripts properly; skim the rest.

---

## 1. GameManager — one number runs the whole feeling

**Say:** "There is one number for how deep you are. Zero at the mouth of the
cave, one at the darkest point. The light dims on that number, and the gap
between the rocks narrows on the same number. So the player watches one thing
getting worse, not two separate systems."

**Design idea:** one signal the player can read, instead of two that have to be
balanced against each other.

**Numbers:** darkest at **300m**. Light goes **0.03 → 0.02**.

> **Likely question: how does the game get harder?**
> Three things at once, all off that one depth number: the light dims, the gap
> narrows from 3.6 to 2.6, and the cave speeds up.

> **Likely question: why not just speed it up?**
> Speed alone gets unfair fast. Darkness takes away information instead of
> reaction time, so it feels tense rather than cheap.

---

## 2. CaveSpawner — walls that can never close up

**Say:** "It builds two rows of rocks, one from the ceiling and one from the
floor. Before placing a rock it measures the opposite wall at that exact spot,
so the two can never meet. There is also a hard floor of 2.2 that nothing is
allowed to go below."

**Design idea:** the gap is a rule, not something you hope the random numbers
respect.

**Numbers:** gap **3.6 → 2.6**. Hard limit **2.2**. The fish is **1.33** tall,
and the part that can actually hit something is **1.11**.

> **Likely question: so is the cave random?**
> The shapes are. The safe gap is not. Each rock may only change depth by a set
> amount from the one before it, so the wall flows instead of jumping.

> **Likely question: why 2.2?**
> Twice the fish. Below that, getting through stops being skill and becomes
> luck — and on a phone your own thumb is covering the fish.

---

## 3. ObstacleMovement — one speed, written in one place

**Say:** "Rocks, rubbish and coins do not have their own speeds. They all read
one shared number. Only one script is allowed to change it, and everything else
just reads it."

**Design idea:** if each thing owned its own speed they would slowly drift out
of step with each other, and nothing on screen would line up.

**Numbers:** base speed **5**, multiplied from **0.9 up to 1.6** over **150
seconds**.

> **Likely question: why lock it down like that?**
> It leaked between runs once. Stopping Play mid-surge left the next run
> starting mid-surge, because the value survived. Now one method writes it and
> it is reset before the game loads.

---

## 4. PlayerController — why this is not Flappy Bird

**Say:** "This was the hardest part. We tuned the tap three times and it still
felt like a bird game. The problem was never the tap — it was the fall. A bird
drops like a stone. A fish should sink."

**Design idea:** the feel lives in how you *fall*, not how you rise.

**Numbers:** swim **4.6**, gravity **1**, water drag **1.5**, top sinking speed
capped at **3.2**.

> **Likely question: what actually makes it feel underwater?**
> Drag and the sink cap. Drag makes it glide and slow down instead of snapping
> about, and the cap stops one missed tap costing you half a gap.

> **Likely question: what are the currents for?**
> They shove you sideways. The fish always swims back to its home spot, so a
> current can only push you so far and never builds up. Push **9**, return
> **2.5**.

---

## The other scripts, one line each

| Script | What it does |
|---|---|
| `PlayerHit` | Ends the run. Two ways to die: bump something, or sink off screen. |
| `TrashSpawner` | Drops rubbish, and owns the whole difficulty curve. |
| `CoinSpawner` / `CoinPickup` | Drops coins in the gap, and collects them. |
| `ObstacleDrift` / `TrashSpin` | Makes debris float and turn so it looks alive. |
| `ScrollObject` | The endless background. |
| `SpotLight2D` | The lamp on the fish's head. |
| `PauseBlur` | The frozen blurred backdrop behind the pause screen. |
| `FishInventory` | The fish shop. |
| `LegalNotice` | The Terms and Privacy screens. |
| `CameraEdges` | Where the screen edges are. |
| `ButtonPressSound` | Clicks on finger-down, not finger-up. |
| `FishShopDebug` | A Tools menu for resetting the save while testing. |

---

## Answers worth having ready

**"How much of this did AI write?"**
A large part, including code. It is disclosed in the README and on the screen
the player has to accept before playing. Say it plainly and move on — it is
already written down, so there is nothing to defend.

**"What does this line do?"** — on a line you do not know
"I would have to check that one." That is a better answer than a guess. A wrong
answer confidently given is the thing that costs you.

**"What is not finished?"**
The sprites are not packed into an atlas, so the game draws more than it needs
to. It has been tested on one phone. That is the honest list.

**"What would you do next?"**
Wider device testing, and the sprite atlas.

---

## Before you present

- Say each of the four out loud once. If a sentence does not come out, it is
  not yours yet — shorten it until it is.
- Do not read comments off the screen. The comments are for someone reading the
  code later; you are talking to a room.
- Pick four scripts and stop. Eighteen is a tour nobody remembers.
