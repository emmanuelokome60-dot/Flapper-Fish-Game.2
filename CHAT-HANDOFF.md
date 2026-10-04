# Flapper Fish Game.2 — handoff for a new chat

Paste this whole file into a new chat to bring it up to speed. Written 2026-10-04.
`DEVLOG.md` holds the detail behind every decision below; this file is the short version.

---

## Where things stand right now

**The game is well ahead of the last build that anyone tested.** An Android APK went out on
2026-09-24, its bug reports were worked through and closed, and a great deal has been added since.
**Nothing from 2026-09-25 onward has run on a phone.** That is the single biggest open risk, and
the next build is where it gets settled.

Closed since that APK: all seven phone-test bugs; coins end to end; ocean currents; the fish being
shoved by them; a get-ready state; fitted trash colliders; and a clean-code naming pass.

### Do these first

1. **`SpawnObstacles` -> Coin Spawner -> `Wall Gap` is 0, and should be 0.35.** It is the only
   stale Inspector value left. At 0 the corridor clamp lets a coin sit flush against a rock face,
   which reads as stuck in the wall and is awkward to reach. Unity wrote `default(float)` rather
   than the C# initializer when the field was added, which is why it is 0.
2. **Confirm the Console is clean.** The last change fixed two `CS1717` self-assignments, and
   that compile has not been eyeballed yet.
3. **Then build an APK and test on the phone.** Everything listed above is unverified on a device.

### Known good, verified in play

Cave walls are seamless past 40 points. The fish migration through the naming pass kept every
tuned value (`swimForce` 5, `rotationSpeed` 10, `maxTiltUp` 5, `maxTiltDown` -60) and both prefab
arrays, confirmed in the saved scene. Drift speeds are set to 0.1 / 0.32 on both spawners.

### Two traps this project keeps setting

- **A value saved in the scene beats a script default.** Changing a default in code does nothing
  to a field that already exists in `SampleScene.unity`. Renaming or deleting the field orphans
  the saved entry and lets the new default apply, which is the usual way round it while Unity is
  open. `[FormerlySerializedAs]` is the opposite tool: use it when the saved value must survive.
- **Unity is usually open.** Scripts, prefabs and `.meta` files can be edited on disk safely;
  `SampleScene.unity` cannot, because Unity's in-memory copy overwrites it on the next save.

---

## The project

- **Unity 6.3 LTS (6000.3.8f1), URP 2D, New Input System only.** `Active Input Handling` was switched from "Both" to "Input System Package (New)" on 2026-09-24, because Unity refuses to build for Android with "Both". Legacy `Input.GetKey` will now fail to compile; use `Keyboard.current` / `Touchscreen.current`.
- **Path:** `C:\Users\User\Documents\Unity_Projects_2026\Flapper Fish Game.2`
- A Flappy Bird clone with an anglerfish, built by following two tutorials by hand, now well past them. **It is a mobile game** — the target is an Android phone, so anything added has to work with touch and at phone aspect ratios.
- **Git is not the undo history.** Commits are rare and always far behind the working tree. Real restore points are timestamped folders in `Backups/`.
- **`DEVLOG.md` in the project root is the long-form log.** This file is the short version. DEVLOG is the better history for anything up to 2026-09-23; for the 09-24 work (trash debris, aspect locks, the Android export, the background anchor) **this file is the authority** — see DEVLOG's own "Session of 2026-09-24" entry for the summary.
- **All numbers below are Unity world units** unless they say otherwise.
- A project-wide `code-review-graph` MCP is configured — use it for exploring the codebase before reaching for Grep.

## How to work on it

1. **Back up first.** Copy affected files to `Backups/YYYY-MM-DD_HHMM_before-<change>/Assets/...` before changing anything. Backups live next to `Assets`, never inside it — scripts copied inside `Assets` cause "already contains a definition" compile errors.
2. **Scripts, prefabs, `.meta` files and images can be edited on disk.** Unity reloads them when its window gets focus.
3. **Do not edit `Assets/Scenes/SampleScene.unity` while the scene is open in Unity** — Unity's in-memory copy overwrites the file on the next save. Either give the user Editor steps, or have them save and do File → New Scene first.
4. **The user does the Editor work** — dragging references, adding components, ticking checkboxes. Give exact steps.
5. **Changes made during Play mode are discarded.** Always stop first.
6. **Unity only compiles when its window has focus**, and one compile error stops every script.
7. **The Inspector beats the code.** A value already saved in the scene keeps its number even after the script's default changes. Rename the field or change it in the Inspector.
8. **There is no live Editor access.** The Unity MCP relay is configured but the Editor-side package isn't installed, so everything is read from and written to disk. `Logs/` and `%LOCALAPPDATA%\Unity\Editor\Editor.log` are useful for checking imports and errors.
9. **There is no automated test loop.** Verification is the user pressing Play and reporting what happened. Say what to look for and what would count as the fix working.

---

## Scripts (`Assets/My Scripts`)

| Script | What it does |
|---|---|
| `PlayerControllerScript.cs` | Swimming, underwater physics, tilt, `ResetPlayer()` after death |
| `PlayerHit.cs` | Ends the run — on any collision, or when the fish sinks past the bottom of the screen |
| `GameManager.cs` | Singleton. Menu/play/pause/game-over flow, score, clears obstacles |
| `MyObstacleSpawner.cs` | Builds two endless rows of rocks as its own children. `TryGetGapAt()` reports the clear corridor so debris can be dropped safely |
| `ObstacleMovement.cs` | Moves an obstacle left and deletes it once off screen. Carries the **static `SpeedMultiplier`** every obstacle reads, which is how the whole game accelerates in lockstep |
| `ScoreTrigger.cs` | One point per rock, fish only, once each |
| `ScrollObject.cs` | Endless scrolling background, makes its own copies |
| `TrashSpawner.cs` | Floating debris, and the **run timer and difficulty ramp for the whole game**. Sits on `SpawnObstacles` beside the rock spawner |
| `TrashSpin.cs` | Rotates one piece of debris on the spot. Coins reuse it |
| `CoinSpawner.cs` | Drops coins into the cave gap and ramps how often they come. **No speed ramp** — see below |
| `CoinPickup.cs` | One coin's `OnTriggerEnter2D`. Fish only, once each, then destroys itself |
| `ObstacleDrift.cs` | Makes one piece of debris wander inside a fixed box. **Debris only — rocks must stay still** |

### Game flow

Boot goes to the menu with `Time.timeScale = 0`, which freezes fish, background, rocks and debris at once. **PLAY does not start a run — it opens a get-ready state.** The fish bobs on the spot with the background scrolling behind it, nothing spawns, and the first tap calls `GameManager.BeginRun()` and swims on the same press. `isWaitingToStart` guards it, and is cleared by `BeginRun()`, `GameOver()` and `BackToMenu()` so it can never strand. `StartGame()` no longer starts the spawners; `BeginRun()` does. The pause button freezes it again and shows **Resume + Restart + Home**. Dying shows GAME OVER plus **Restart + Home only** — `TogglePause()` refuses to run once `isGameOver` is set, so a Resume button there would be a dead click, and `ShowRunButtons(visible, canResume)` keeps it off that screen.

**Both frozen screens get the same backdrop.** `GameManager.ShowPauseDim()` builds a full-screen black `Image` under the Canvas on first use (`pauseDimAlpha` 0.6), then raises the dim, GAME OVER and the buttons by `SetAsLastSibling()` in that order. Everything not raised — the fish, the cave, the score, **and the Pause button** — stays under it, so it is darkened, and because a Graphic is a raycast target by default the dim also swallows its taps. That is what stops Pause doubling as a Resume: it is still on screen, just dead. (Phone test, 2026-09-25: the two sat close enough together that resuming with the Pause button happened by accident.) Home returns to the menu. Score resets on both `StartGame()` and `BackToMenu()`.

The fish's start position is **camera-relative and measured as a fraction of screen width** (`GameManager.PlayerStart()`, `startFractionFromLeft` 0.3333 — one third across). It was first a hardcoded x = −7, which falls off screen on anything narrower than about 14:10, then a fixed 1.9-unit inset from the left edge, which stayed on screen but landed at a **different fraction on every device** — 10.7% across at 16:9, 8.8% on a 19.5:9 phone. A fraction lands at 33.3% on all of them. `[Range(0f, 1f)]` makes it a slider and stops a typo putting the fish off screen.

### Input

Only `PlayerControllerScript.cs` touches input, and it uses **direct device polling**, not Action Maps:

```csharp
if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
```

- **`.current` plus a null guard** is what makes one code path work on desktop and phone. On a PC `Touchscreen.current` is null; on a phone `Keyboard.current` and `Mouse.current` are. This is why touch needed no extra work for Android.
- **`wasPressedThisFrame`** is edge detection — one flap per tap, not continuous thrust while a finger rests on the screen.
- **Two guards run first.** `Time.timeScale == 0f` returns early so taps on the menu, pause and game-over screens can't swim the fish; `EventSystem.current.IsPointerOverGameObject()` returns false if the tap landed on a UI button. Without the second, pressing Pause would also flap.
- The EventSystem uses **`InputSystemUIInputModule`**, not the legacy `StandaloneInputModule`. Mixing those is a classic cause of dead buttons on mobile.
- There is an `InputSystem_Actions.inputactions` asset registered project-wide, but **no gameplay code reads it** — it is the Unity template's default, sitting unused.

### Rock obstacles — how the spawner works

- **The lead-in is the get-ready state plus the swim from the right edge.** Nothing spawns until
the first tap. Rows then seed at `CameraRightEdge() + 0.5`, and the first rock takes about
**3.5s** in a 16:9 editor and **4.3s** on a 19.5:9 phone to reach the fish at the run-start
speed of 3.5 u/s. `graceSeconds` was a timed pause doing this job badly: built at 3s, cut to
0.6s, removed for good on 2026-10-03 once the get-ready state existed.
- **Hand-written collider YAML: `m_Paths` nests twice.** The first point of a path carries two
dashes (`- - {x: ...}`), because `m_Paths` is a list of paths and each path is a list of points.
One dash parses cleanly and silently yields a collider with **no shape** — nothing warns, and it
simply never collides. Validate baked colliders by polygon area, not by counting points.
- **Two continuous rows**, ceiling and floor. Each rock starts where the previous one in its row ended, minus the `overlap` value (0.2 in the scene, tunable), so a row never has holes. Rows top up when their right end is under 2 units past the camera's right edge.
- **Random**: rock chosen at random, never the same twice in a row within a row, randomly mirrored left/right. Bottom rocks are flipped with a **negative Y scale**, not Sprite Renderer Flip Y, because only the scale flips the collider too.
- **Flush against the edges**: the spawner picks a depth first and **scales the rock to reach it**, so the flat edge always sits against the camera top or `floorY`.
- **The gap is guaranteed**: before placing a rock, the spawner asks the other row how deep it already reaches across that stretch and takes only what's left, so every overlapping pair leaves at least `minGap`. Depth also stays within `maxDepthChange` of the previous rock in that row, so the cave wall flows.
- Camera is orthographic size 5 at y ≈ 0. **Vertical world height is always 10 units; the width follows the aspect ratio** — 17.8 at 16:9, 21.7 on a 19.5:9 phone. Every spawner and despawner already derives its edges from `orthographicSize * aspect`, so they adapt on their own.
- **The fish is 1.333 units tall and 1.79 wide.** Its `CircleCollider2D` is about 1.11 across, and that 1.11 is where the old "fish is about 1.1 units tall" claim came from — it was the collider, not the sprite.

### Floating trash debris

`TrashSpawner` drops debris into the cave's opening for the fish to dodge, and owns the difficulty ramp for the entire game.

- **Difficulty ramps over `rampSeconds` (90), then stops.** Speed goes from `startSpeedMultiplier` to `maxSpeedMultiplier` and the spawn interval from `startInterval` to `minInterval`, both eased by the same `Clamp01(elapsed / rampSeconds)`. `maxOnScreen` (8) caps the count.
- **Currents.** `TrashSpawner.Surge()` multiplies the shared speed by a sine arch, `surgeStrength` 1.3 over `surgeSeconds` 2.5, every `surgeInterval` 9s of calm. It is derived from `elapsed` rather than its own timer, so it is stateless and resets with the run. **It has to go through the shared multiplier**: pushing rocks individually would tear the two rows apart and reopen the holes in the cave wall. A surge cuts reaction time by the same proportion it adds speed — at full difficulty on a phone, 1.80s becomes 1.39s — so treat 1.4 as the ceiling for `surgeStrength`.
- **Speed is shared.** The ramp writes `ObstacleMovement.SpeedMultiplier`, which every rock and every piece of debris reads, so the whole cave accelerates together and debris can never drift out of the gap it spawned in. It is a **static**, so it is reset to 1 in both `StartSpawning()` and `StopSpawning()` — without that it would survive into the next run.
- **Debris is carried forward through the tunnel.** `debrisCurrentPush` (1.2 units/sec,
multiplied by the surge) is added on top of the cave's own scroll by
`ObstacleDrift.ConfigureCurrent()`, so trash closes on the fish instead of scrolling along
with the rock walls. **This breaks the old lockstep guarantee on purpose**, so
`ObstacleDrift.CarryForward()` re-checks `TryGetGapAt()` every frame and clamps the piece
inside whichever corridor it has reached — without that, carrying debris forward would push
it straight through a rock face. Set it to 0 to lock debris to the cave as before.
- **Coins keep a gap from the rock face.** `wallGap` 0.35 on `CoinSpawner` is added to the spawn clearance and passed to `ConfigureCurrent()`, so the corridor clamp holds a coin `radius + wallGap` off each wall instead of letting its edge touch. Debris leaves it at 0 and is still allowed to press against the rock, which is what being shoved by a current should look like. A corridor too tight to hold the gap gives up the gap rather than the piece — without that fallback the clamp range inverts and throws the piece through the far wall.
- **Coins are carried too**, at `coinCurrentPush` 0.8 against the debris 1.2. Lower on
purpose: a coin is small and dense where trash is light and buoyant, so the current moves it
less, and it keeps coins catchable. The run is now dodge the trash, chase the coins, and both
close on the fish faster than the walls do.
- **The clamp turns the drift around, not just the piece.** Repositioning alone left the
drift velocity still pointing into the rock, so it drove back in next frame and the piece
buzzed along the wall. `CarryForward()` now reverses `_velocity` when it is heading into the
wall it was just held off.
- **Debris reuses `ObstacleMovement`** and is parented under `SpawnObstacles`, which means `GameManager.ClearObstacles()` already finds and destroys it on restart with no extra code.
- **All nine trash prefabs now carry a baked `PolygonCollider2D`** (2026-10-03), traced from each PNG's alpha with OpenCV, simplified to 22-49 points and inset 1px so the collider sits just inside the art. Before this only `trash1` had one and the other eight were traced at runtime on every spawn, which was the standing suspect for frame hitches on a mid-range phone. The `TryGetComponent` fallback in `TrashSpawner` is kept but never fires now.
- **This fixed the fit on `trash6` and `trash7`.** Both have heavy transparent padding, so a collider sized to the sprite rect was far larger than the visible junk. Tracing the alpha gives 1.47x1.34 against a 2.58x2.52 sprite on trash6 -- the hitbox now matches what the player can see.
- Pieces are scaled so their largest dimension lands in `minSize`–`maxSize` (0.5–0.9), always smaller than the 1.333-tall fish. Placement reserves the piece's diagonal plus its drift allowance from each wall, so a wandering piece can never end up inside a rock.
- Sorting orders: background 0, rocks 1, **debris 2, fish 3**. The fish is deliberately on top so junk can never hide it. **Only two of these four are authored.** Background and rocks carry their order on the prefab/object; the **nine trash prefabs are all saved at 0** and only become 2 at runtime, in `TrashSpawner.cs:138`. Don't 'fix' a trash prefab that reads 0 in the Inspector — it is supposed to.
- `trash6` and `trash7` have heavy transparent padding, so they look smaller than their neighbours. Nudge those two prefabs' scale if it bothers you.

### Coins (`Assets/Prefabs/Coins/Coin.prefab`)

Built 2026-10-02 from `Assets/Graphics/Coins/Frame 15.png`. Sprite Renderer at sorting order **2** (same band as debris, under the fish's 3) and a **`CircleCollider2D` with `Is Trigger` ticked**, radius 0.36, no offset. The sprite is 0.90 × 0.86 units; the coin pixels inside it are 0.79 × 0.72, which is what the radius covers.

- **The trigger tick is load-bearing.** `PlayerHit.OnCollisionEnter2D` ends the run on *any* collision, so a solid collider on a coin would kill the fish on pickup. Collect it with `OnTriggerEnter2D`; never untick Is Trigger.
- A `CircleCollider2D` rather than a traced `PolygonCollider2D`: a coin is round, and it avoids the runtime tracing that is the project's known source of frame hitches.
- **`Frame 15.png` was switched from Sprite Mode Multiple to Single**, matching every other sprite here. It had been sliced into one sub-sprite, and in Multiple mode a prefab cannot use the dependable `fileID: 21300000` reference. Single mode includes the ~6px transparent border the slice had cropped, which is why the sprite is 0.90 wide and the collider is sized to the coin, not the sprite.
- **`CoinSpawner` on `SpawnObstacles` spawns them**, `CoinPickup` collects them. Coins drift and
spin with the same `ObstacleDrift` and `TrashSpin` the debris uses, and carry `ObstacleMovement`
so they travel at exactly the cave's speed and get swept by `ClearObstacles()` on restart.
- **Drift is vertical only, in a fixed band.** `driftY` 0.4 with `driftMinSpeed`/
`driftMaxSpeed` on both spawners. `ObstacleDrift` moves a piece up and down and nothing else:
left and right belong to `ObstacleMovement`, because the current runs one way and a piece
wandering sideways against the flow reads wrong. `driftX` was removed on 2026-10-02 (its
saved scene entry is orphaned and harmless).
- **All of the drift speed now goes into one axis.** It used to be a random 2D direction, so
only about 64% of the speed was vertical on average. Same numbers now drift roughly 1.5x
faster up and down — `driftMaxSpeed` 0.32 reproduces the old vertical pace.
- An adaptive drift box that scaled to the corridor was built and **reverted the same day**:
correct, but the fast vertical motion read as darting rather than floating.
- **`CoinSpawner` must never write `ObstacleMovement.SpeedMultiplier`.** `TrashSpawner` owns that
static and ramps it 0.7→1.6; coins inherit the speed-up for free by carrying `ObstacleMovement`.
Two scripts writing one static would fight. `CoinSpawner` ramps only its spawn interval, 2.5s
down to `minInterval` 1s over `rampSeconds` 90 — **1s is the cap, at most one coin per second** —
with `maxOnScreen` 5 as a second cap.
- **Two coin counts.** `GameManager.Coins` is what the HUD shows: collected this run, cleared
by `ResetCoins()` next to `ResetScore()` in `StartGame()` and `BackToMenu()`. `TotalCoins` is
the lifetime wallet, loaded from `PlayerPrefs` key `"Coins"` in `Awake()`, written on every
pickup and flushed by `PlayerPrefs.Save()` in `GameOver()`/`BackToMenu()`. **Nothing displays
`TotalCoins` yet** — it is banking in the background for the planned skin shop, so no coin
collected before that shop exists is lost. Clear it with `PlayerPrefs.DeleteKey("Coins")`.
- **The coin icon is drawn in code.** `GameManager.BuildCoinIcon()` parents a UI `Image` to the
counter on `Awake()`, pinned to the middle of its left edge with a right-hand pivot so it sits
outside the box however wide the number grows, and the counter reads `": 42"`. The sprite comes
from `CoinSpawner.CoinSprite` (read off the wired coin prefab), so icon and coin can never
disagree and there is no second Inspector slot. Tune with `coinIconSize` (80) and `coinIconGap`
(6, negative pushes it into the box). The icon adds about 86 UI units to the left of the
counter's box — allow for that when positioning it.

### Rock prefabs (`Assets/Prefabs/Rocks/Rock1–Rock5`)

> The nine **trash** prefabs live in `Assets/Prefabs/Trash&Rocks/` (renamed from `Trash/` on
> 2026-10-02). The rename was done in the Editor, so all nine GUIDs are unchanged and still
> wired into `TrashSpawner` — verified, nothing to re-drag. Rocks are still in `Rocks/`.

Each has: Sprite Renderer at sorting order 1 (above the background), a Polygon Collider 2D traced from the image outline with OpenCV, `ObstacleMovement` (speed 5), and a `ScoreZone` child — a trigger 2 wide by 20 tall in world units, carrying `ScoreTrigger`, reaching from the rock's tip across the gap. Prefab scale is 0.6 but **the spawner overrides it at runtime**.

Full-size (scale 1): Rock1 8.4 × 2.7, Rock2 9.1 × 2.6, Rock3 9.4 × 5.1, Rock4 6.0 × 3.3, Rock5 9.7 × 4.4 units. In play they end up 4 to 8 wide. The five PNGs in `Assets/Graphics/Rock Ostacles/` were cropped to their visible pixels; untrimmed originals are in the `2026-09-22_1950` backup.

### Background

`ScrollObject` counts the copies under the same parent that share its sprite and **creates any it's missing** — enough to cover the screen plus one spare — so one background object is enough. At runtime it also **pulls the row left until it starts at or before the screen's left edge**, so the authored X only has to be roughly right. Without that, the row (whose sprite is `BG_4.jpg`, about 63 units wide as authored) cleared a 16:9 editor by 0.06 units and left bare space on anything wider — which is exactly what showed up on switching to Android. The clone appears in the Hierarchy only during play. Every copy takes the first one's width, Y and scale, so slightly different scaling can't cause drift or a vertical step at the seam. All copies need the same sprite and the same `scrollSpeed`; the first one's X is the anchor.

---

## Current scene state (checked 2026-09-24)

**`SpawnObstacles` carries two components**: `MyObstacleSpawner` (rocks) and `TrashSpawner` (debris + the difficulty ramp), with all nine trash prefabs wired into the latter. `GameManager` finds the trash spawner itself in `Awake()` via `obstacleSpawner.GetComponent<TrashSpawner>()`, so there is no Inspector slot for it.

**Buttons and what they call** (the object names are not all obvious):

| Object | OnClick |
|---|---|
| `Menu Play Button` | `GameManager.StartGame()` |
| `Restart Button` | `GameManager.StartGame()` |
| `PauseButton` | `GameManager.TogglePause()` |
| `ResumeButton` | `GameManager.TogglePause()` |
| `HomeButton` | `GameManager.BackToMenu()` |
| `CharactersButton` | nothing — no OnClick at all |

**UI**: every button and the title carry an `AspectRatioFitter` locking their shape, and the art was swapped to the `Updated UI` set (all aspect 1.7931). `ResumeButton` lives under `Canvas`, starts inactive, and calls `GameManager.TogglePause()`. The fish's sprite is on sorting order **3** so debris on 2 cannot cover it — but note the Sprite Renderer is **not on `Player_Fish`**. `Player_Fish` holds only the Rigidbody 2D, Circle Collider 2D, `PlayerControllerScript` and `PlayerHit`; the sprite and its sorting order live on its single child, named **`Animator`**.

**SpawnObstacles — Inspector values, which differ from the script defaults:**

| Field | In scene | Script default |
|---|---|---|
| Min Gap | 3.5 | 3 |
| Min Depth | 1 | 2 |
| Max Depth Change | 1.5 | 1.2 |
| Floor Y | **-5** | -4 |
| Min/Max Scale | 0.35 / 1.2 | same |
| Max Width | 8 | same |
| Overlap | 0.2 | same |

Lower **Min Gap** for a harder game; raise **Min Depth** for chunkier rocks. All five rock prefabs are wired into Rock Prefabs.

**Three things worth knowing about the scene as it stands:**

1. **The Ground object has been deleted** and Floor Y set to -5, so rocks now run to the bottom edge of the screen. Nothing physical stops the fish falling, and in the first ~3 seconds of a run — before rocks arrive — it used to sink off screen with no collision and no game over. **Fixed in `PlayerHit`**, which now ends the run when the fish drops past the camera's bottom edge plus `Below Screen Margin` (1). Chosen over an invisible Box Collider 2D because it needs no Editor work and can't be knocked loose in the scene.
2. **The `BG_1` object has no ScrollObject component**, so it never moves. It's a static duplicate parked far off to the right, harmless, because `ScrollObject` now makes its own copy. Delete it, or add Scroll Object with Scroll Speed 2 to match `Bg_0`. (Careful: the scene's background **objects** are `Bg_0` and `BG_1` under a `Background` parent, while `BG_1.jpg`/`BG_4.jpg` in `Assets/Graphics/Sea.Bg/` are **sprites**. Same names, different things — the object named `BG_1` does not use the sprite named `BG_1.jpg`.)
3. **`ScoreCounter` is unticked in the scene**, which no longer matters — `GameManager.ShowScore()` turns it on when a run starts and off on the menu.

---

## Known rough edges

- Scoring is one point per rock and **both rows score**, so points come about twice as fast as with pipes. Easy to change to top-row-only.
- `CharactersButton` is on the menu with no OnClick — it does nothing.
- **Debris colliders are traced at runtime**, roughly every 0.9s at full difficulty. The most likely source of frame hitches on a mid-range phone. Pre-baking colliders onto the nine trash prefabs is the fix if it stutters.
- **Possible gap under the cave.** Bottom rocks are placed at `floorY - overlap`, so their flat edge sits at −5.2, **not** at `floorY` −5. The camera sits at y −0.0287, so death triggers at −6.03. That leaves about 0.83 units. The fish's collider is 1.11 across so only a ~0.27-unit band lets it through, and it would be almost entirely below the visible screen. Never seen in play; worth knowing.
- `HighScore` is declared in `GameManager` but never read or written. The tutorial does it with `PlayerPrefs` (1:11:34–1:12:55). `CurrentScore_Label` and `BestScore_Label` sit in the scene fully built but **do nothing at all** — no code touches either one, and nothing is missing from their wiring. They are waiting on the high score work below, not broken.
- `Assets/Graphics/Rock Ostacles/Rock.png` is unused.
- Button positions were typed into the scene file as coordinates, not composed by eye.

## Next up

1. **A phone build.** Nothing since 2026-09-24 has been tested on a device. Tick Development
   Build so a crash gives a stack trace.
2. **A version label on the menu**, so a tester's report can be tied to a build. This was wanted
   last round and still does not exist.
3. **High score with `PlayerPrefs`**, feeding `CurrentScore_Label` and `BestScore_Label`, which
   sit in the scene fully built and wired to nothing. `GameManager.HighScore` is declared and
   never read or written.
4. **The skin shop.** `GameManager.TotalCoins` is already banking to `PlayerPrefs` key `"Coins"`
   for exactly this, and nothing displays it yet.
5. **Sound effects** - the project has no audio at all.
6. **Android app icons** - every slot is empty, so it ships with the default Unity icon.
7. **A "TAP TO START" prompt** on the get-ready screen. The fish bobs, which carries the message,
   but a label would be clearer. Needs one TMP object in the Canvas.

---

## Android

The target platform. **Android Build Support is installed** with a bundled NDK, OpenJDK and SDK (platforms 34/35/36, build-tools 36.0.0) — no external setup needed. `SampleScene` is in the build list, IL2CPP and ARM64 are set.

- **Orientation must stay landscape.** The camera fixes vertical height at 10 units and lets width follow the aspect, so in portrait the view is only about 4.6 units wide. At that width the fish is off screen, rocks despawn before reaching it, and scoring never fires. Portrait is not a settings change — it would need orthographic size ~10.8 and a full retune of gap, rock scales, fish size and speeds.
- **ARM64 only**, so the APK will not install on a standard x86_64 emulator. Use a real phone.
- **Graphics API is Auto**, which resolves to Vulkan first. If the first run is a black screen, force GLES3 — that is the usual culprit.
- **First build takes 10–25 minutes** because IL2CPP compiles ahead of time. Later builds are much quicker.
- `com.unity.visualscripting` is in the runtime package set and unused; it lengthens IL2CPP builds and could be removed.

**Settings already applied** (all confirmed on disk in `ProjectSettings/ProjectSettings.asset`):

| Setting | Value |
|---|---|
| `defaultScreenOrientation` | `3` — Landscape Left |
| `applicationIdentifier → Android` | `com.emmanuelokome.abyssalfish` |
| `productName` | `Abyssal Fish` |
| `companyName` | `Mobile Game Project` |
| `AndroidMinSdkVersion` | 25 (Android 7.1) |
| `AndroidTargetSdkVersion` | 0 — Automatic, resolves to API 36 |

The Standalone identifier is still the template leftover `com.DefaultCompany.2D-URP`, which is an **illegal Android package name** (hyphen, digit-leading segment). Harmless where it is; never copy it to the Android slot.

**Building:** File → Build Profiles → Android → Switch Platform, then **Build** (not Build And Run — that needs a connected device). Output goes outside the project, e.g. `Documents\Unity_Projects_2026\Build\`. Tick **Development Build** for test builds so crashes produce stack traces.

**Sideloading:** the tester must allow "install unknown apps" for whichever app they open the APK from. APKs are debug-signed, which is fine for testing; a real keystore is only needed for the Play Store. Don't send APKs over WhatsApp — it mangles them.

---

## Trip hazards already paid for

- **`OnCollisionEnter2D` must be spelled exactly** — Unity gives no error for a misspelling, the method simply never runs.
- **Create Empty under a Canvas gives a 100×100 RectTransform.** Stretching a child of it does nothing until the parent is stretched. Hold **Alt** when clicking an anchor preset, or it moves the anchors only.
- **UI sprites must be Sprite Mode Single.** In the scene file a Single sprite is always `fileID: 21300000`; the long IDs in a `.meta` file's `internalIDToNameTable` are leftovers from slicing and don't resolve.
- **Renaming a `[SerializeField]` field empties its Inspector slot.** Use `[FormerlySerializedAs("oldName")]`.
- **`Update()` still runs at `timeScale = 0`**, so a tap on a UI button was also read as a swim. Guarded with a timeScale check and `EventSystem.current.IsPointerOverGameObject()`.
- **The tutorial never resets the score** — at 1:15:00 it only hides the panel. That fix is ours.
- **Windows Application Control can silently kill all compilation.** Smart App Control blocked Unity's own unsigned `Bee.Stevedore.Program.dll`, and the Console showed only "Internal build system error. BuildProgram exited with code -532462766" — which looks like a Unity bug and is not. The real cause is only visible in `%LOCALAPPDATA%\Unity\Editor\Editor.log` and in Event Viewer under `Microsoft-Windows-CodeIntegrity/Operational`, event ID **3077**. Turning Smart App Control off needs a **reboot** to take effect, because the policy stays loaded in the kernel until then — the registry flag flips immediately and misleads you. Note it cannot be turned back on without reinstalling Windows.
- **`AspectRatioFitter` greys out a size field in the Inspector.** That is the component driving it, not a bug. Every UI element has one to stop sizes drifting; set the dimension that is still editable and the other follows.
- **A `[SerializeField]` that already has a value saved in the scene ignores the script's new default.** Changing a default in code only affects *new* fields. Existing ones must be changed in the Inspector or the scene file.
- **`Camera.main` is used per frame in several scripts.** Fine in Unity 6 (internally cached) but worth knowing before adding more.
- **A missing TrashSpawner used to take the whole game down, not just the debris.** `GameManager.Awake()` logs an error when `obstacleSpawner.GetComponent<TrashSpawner>()` comes back null, but `StartGame()`, `GameOver()` and `BackToMenu()` then called it anyway — and `Start()` calls `BackToMenu()`, so removing the component from `SpawnObstacles` threw a NullReference on boot and left a dead menu behind a logged error that looked survivable. The three call sites are null-guarded as of 2026-09-25; the game now runs without debris instead of not running at all.
