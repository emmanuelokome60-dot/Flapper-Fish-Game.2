# Flapper Fish Game.2 — Development Log

Unity 6.3 LTS (6000.3.8f1) · URP 2D · New Input System only
Last updated: 2026-10-07

---

## How the project started

Two tutorials were used as reference:

- **How to Make a Flappy Bird Clone in Unity (Beginner Tutorial 2026)** — 1h 30m
- **2D Infinite Runner Unity Tutorial** — 24m

Mechanics taken from them: swim by setting upward speed, moving obstacles from a spawner, scrolling background, a score zone in each gap, and a game manager for score, game over and restart. The runner tutorial added timed spawning at random heights and deleting objects once they leave the screen.

A complete pre-built version of the game plus a one-click scene builder was written first, then dropped in favour of following the tutorial by hand. Those scripts and the builder tool (`Assets/Editor/FlappyFishSetup.cs`) were deleted. Everything in the project now is tutorial code.

---

## Scripts (`Assets/My Scripts`)

| Script | What it does |
|---|---|
| `PlayerControllerScript.cs` | Swimming, underwater physics, tilt, reset after death |
| `PlayerHit.cs` | Ends the run on any collision, **and** when the fish sinks past the bottom of the screen |
| `GameManager.cs` | Singleton, game over, restart, clears obstacles, AddScore |
| `MyObstacleSpawner.cs` | Builds endless top and bottom rows of random rocks as its own children |
| `ObstacleMovement.cs` | Moves obstacles left, deletes them once their right edge is off screen |
| `ScoreTrigger.cs` | Scores when the fish passes a rock |
| `ScrollObject.cs` | Endless scrolling background, makes and aligns its own copies |
| `TrashSpawner.cs` | Floating debris, and the run timer and difficulty ramp for the whole game (added 09-24) |
| `TrashSpin.cs` | Rotates one piece of debris on the spot (added 09-24) |
| `ObstacleDrift.cs` | Makes one piece of debris wander inside a fixed box. Debris only — rocks must stay still (added 09-24) |

### Scene setup (`Assets/Scenes/SampleScene.unity`)

> **This is the scene as of 2026-09-22.** It has moved on since — see *Session of 2026-09-24* below, and `CHAT-HANDOFF.md` for the current state. The two corrections that bite hardest are marked inline.

- **Player_Fish** — Rigidbody 2D (Dynamic), Circle Collider 2D, `PlayerControllerScript`, `PlayerHit`. ~~Starts at X = -7.~~ **Superseded twice:** 09-24 made the start camera-relative via `GameManager.PlayerStart()` with `startMarginFromLeft` 1.9; 09-25 replaced that with `startFractionFromLeft` 0.3333, a fraction of screen width. See *Phone test bugs* below.
- **GameManager** — `GameManager`, with Game Over Text, Play Button, Player and Obstacle Spawner all filled in.
- **SpawnObstacles** — `MyObstacleSpawner`, with the five rock prefabs in Rock Prefabs. Spawned rocks become its children. Its own position no longer matters; spawning works from the camera edges.
- **Background** — background copies with `ScrollObject`.
- ~~**Ground** — Box Collider 2D.~~ **Superseded 09-23:** the Ground object was deleted and `floorY` set to −5 so rocks run to the bottom edge. Falling deaths are handled in `PlayerHit` instead.
- **Canvas** — GameOver Text, Play Button (anglerfish image), EventSystem using the Input System UI module.
- **Prefabs/Rocks/Rock1–Rock5** — one prefab per rock image, see *Rock obstacles* below. `Obstacles.prefab` (the old pipe pair) was deleted on 09-22.

---

## Bugs fixed

### Code mistakes

- `rb` used but never declared in the player script.
- `Input.GetKey` used throughout. It crashes because this project only has the new Input System. Replaced with `Keyboard.current`.
- `transform.position.x += …`, which C# doesn't allow. `transform.position` hands back a copy, so the whole Vector3 has to be replaced.
- `onCollisonEnter2D` misspelled, so nothing stopped the game. Unity only runs `OnCollisionEnter2D` spelled exactly, capitals included. No error is shown for this.
- `OnCollisionEnter(Collision)` (3D) instead of `OnCollisionEnter2D(Collision2D)`.
- `GameManager.Instance` missing, because the singleton part of the tutorial hadn't been written yet.
- The tilt line sat outside any method and used `_rb` and the old `velocity`. Moved into `FixedUpdate()` and renamed to `rb.linearVelocity`.
- Two classes named `ObstacleSpawner`. Yours was renamed `MyObstacleSpawner`.
- `MyGameManager.cs` held a class called `GameManager`. Unity needs the file name and class name to match, so the file was renamed.
- `ScoreTrigger` was still the empty template, so scoring never ran. Trigger methods take `Collider2D`, not `Collision2D`.

### Game behaviour

- **Backgrounds drifted apart.** The old script jumped to a fixed X, which didn't match the image width (12.8 × 1.4 scale = 17.92 units) and lost a little distance on every loop. It now measures the sprite's real width, counts the copies under the same parent, and lines them up when Play starts.
- **The fish spun like a ball** when it hit something. Rotation is locked in `Awake()`.
- **The fish couldn't swim after restart**, because `IsAlive` stayed false. `ResetPlayer()` restores position, rotation, speed and IsAlive.
- **Obstacles stayed on screen after restart.** They spawn as children of the spawner, and `ClearObstacles()` deletes them.

### Visual Studio

- Mixed line endings (from earlier edits made outside the editor) broke auto-indent. All files are Windows line endings (CRLF) now.
- Braces on the wrong line: fixed by ticking every "Place open brace on new line" option under **Tools → Options → Text Editor → C# → Code Style → Formatting → New Lines**.
- **Use adaptive formatting** (Text Editor → Advanced) is off, so Visual Studio follows the Smart indent and 4-space settings instead of guessing.
- Format Document is **Ctrl+E, D** in this setup, not Ctrl+K, Ctrl+D.

---

## Look and feel

- The anglerfish PNG is the Play button image. Its **Sprite Mode** had to be changed from Multiple to Single first, otherwise it can't be dragged into a UI button.
- Underwater movement: gravity **4 → 1.2**, water drag **1**, sink speed capped at **4.5**, smooth tilting limited to **25° up / 45° down**.
- Swimming is gentler: **Swim Force 4**, replacing the old Jump Force of 10.
- Space, mouse click and screen tap all make the fish swim.

### Player settings

| Setting | Value | Notes |
|---|---|---|
| Swim Force | 4 | Try 3 for smaller swims, 5 for bigger |
| Water Gravity | 1.2 | Raise to 1.5–2 if it's too floaty |
| Water Drag | 1 | Lower to 0.5 if it feels sluggish |
| Max Sink Speed | 4.5 | Stops it dropping like a stone |
| Rotation Speed | 5 | |
| Max Tilt Up / Down | 25 / -45 | |
| Tilt Smoothing | 5 | Raise to 8 for quicker turning |

The script sets gravity and drag when the game starts, so the Gravity Scale shown on the Rigidbody 2D is ignored.

---

## Worth remembering

**The Inspector's value always beats the value in the code.** A field already on an object keeps whatever number the Inspector shows, so editing `= 5f` in the script changes nothing. Either change it in the Inspector, or give the field a new name so it starts fresh. Swim Force was renamed for this reason.

**Unity only compiles when its window has focus.** After editing scripts outside Unity, click into the Unity window and wait for it to finish before checking the Console.

**One compile error stops every script in the project**, even scripts that are fine.

---

## Main menu, pause and home

Built after the tutorial's score section. None of this is in the tutorial.

### Flow

Boot goes straight to the menu with everything frozen. `Time.timeScale = 0` stops the fish, the background and the obstacles in one move, because all three run off `Time.deltaTime` or physics. Press PLAY and the game starts. Pause freezes it again and shows Restart and Home; pause again to resume. Dying shows the Game Over text plus that same Restart/Home pair.

### Scene objects

| Object | Parent | Art | Starts | Does |
|---|---|---|---|---|
| `OverLay` | MainMenu | none, black 60% | shown | Dims the game behind the menu |
| `AbyssalFishTitle` | MainMenu | Frame 10 | shown | Title. Raycast Target off so it doesn't swallow clicks |
| `MenuPlayButton` | MainMenu | Frame 12 | shown | `StartGame()` |
| `CharactersButton` | MainMenu | Frame 11 | shown | Nothing yet — placeholder |
| `Play Button` | Canvas | Frame 12 | hidden | `StartGame()` — this one is the restart |
| `HomeButton` | Canvas | Frame 16 | hidden | `BackToMenu()` |
| `PauseButton` | Canvas | Frame 14 | hidden | `TogglePause()`, anchored top-right |

### Script changes

- `GameManager` gained `mainMenu`, `homeButton` and `pauseButton`, plus `Start()`, `TogglePause()`, `BackToMenu()`, `ShowRunButtons()` and `ResetScore()`. Two flags, `isPaused` and `isGameOver`, track state.
- `MyObstacleSpawner` lost its `Start()`. It ran `InvokeRepeating(..., 0f, ...)`, which spawned a pipe on frame one behind the menu. Now `StartSpawning()` / `StopSpawning()`, driven by the GameManager.
- `PlayerControllerScript` got two guards in its input path.
- `ScoreTrigger` now checks that the thing entering is the fish, and scores each gap only once.

---

## Bugs fixed in this pass

**The score never reset.** Play, die, restart, and the counter carried on from where it left off. `ResetScore()` now runs in `StartGame()` and `BackToMenu()`.

Worth being clear about this one: **the tutorial does not fix it.** At 1:14:47 the author notices the restart problem, and at 1:15:00 the fix is `scoreLabel.SetActive(false)` — which only hides the panel. `Score` is never set back to 0 anywhere in the video. Don't wait for the tutorial to solve it.

**`GameOver()` could fire twice.** The fish carries both `PlayerHit` and `PlayerControllerScript`, so clipping a pipe and the ground in the same frame called it once per contact. Guarded with `isGameOver`.

**A point could land after death.** Clearing a gap in the same frame as dying still fired the trigger, ticking the score up on the game over screen. `AddScore()` now returns early once `isGameOver` is set.

**`ScoreTrigger` scored for anything.** There was no check that it was the player. Harmless while only the fish can reach the zone, but it would break silently the moment a second trigger-capable object exists.

**Tapping a UI button also swam the fish.** `Update()` keeps running at `timeScale = 0`, so the click that pressed PLAY was read as a swim too. Two guards now: `if (Time.timeScale == 0f) return;` and `EventSystem.current.IsPointerOverGameObject()`. The second matters most for the pause button, which sits on screen *during* play — without it, pausing could kill you.

---

## More Unity things worth remembering

**Create Empty under a Canvas gives a 100x100 RectTransform.** A child stretched to fill it still only fills 100x100. This cost real time: `OverLay` was stretched correctly but still looked like a small square, because `MainMenu` above it had never been stretched. Stretch the parent first.

**Alt matters on the anchor preset.** Clicking a stretch preset moves the anchors only. Hold Alt and it sets position and size too. Without Alt, the old size stays behind.

**UI sprites must be Sprite Mode Single, not Multiple.** In the scene file a Single sprite is always referenced as `fileID: 21300000`. The long IDs in a `.meta` file's `internalIDToNameTable` are leftovers from when the image was sliced as Multiple, and they do not resolve.

**Renaming a `[SerializeField]` field empties its Inspector slot**, because Unity serialises by field name. `[FormerlySerializedAs("oldName")]` carries the reference across. Use it — re-dragging everything by hand after a rename is exactly how references end up in the wrong slots.

**`Update()` still runs while `Time.timeScale` is 0.** Physics stops, `Time.deltaTime` is 0 and `InvokeRepeating` pauses, but input code keeps reading.

---

## How the scene was edited

Most of the UI in this pass was made by editing `Assets/Scenes/SampleScene.unity` directly as YAML, not through the Editor.

If that is done again: **Unity must have the scene closed first** (File > New Scene), or its in-memory copy overwrites the file on the next save. Back up the scene before each edit, and check afterwards that the document count rose by the expected amount and that no fileIDs are duplicated.

The Unity MCP relay is configured in `~/.claude.json` and `relay_win.exe` runs, but `~/.unity/mcp/connections` stays empty and ports 9001/9002 are closed — the Editor-side package is not installed, so there is no live Editor access. Everything is read from and written to disk.

---

## Scrolling background (2026-09-22)

`ScrollObject` now looks after itself. At startup it counts the copies under the same parent that share its sprite, and **makes any it is missing** — enough to cover the screen plus one waiting off the right edge. A single background object is therefore enough; the extra `Bg_0(Clone)` appears in the Hierarchy only while playing.

Every copy also takes the first one's width, Y and scale, so a copy that was scaled or nudged differently in the Editor still joins cleanly instead of drifting apart or stepping up and down at the seam.

What still has to be right by hand: all copies need the same sprite and the same `scrollSpeed`, and the first one's X is the anchor the rest are laid out from. A copy with no `ScrollObject` is invisible to all of this and just sits there — that was the cause of the empty blue band behind the rocks on 09-22.

---

## Rock obstacles (2026-09-22)

The pipes were replaced by rocks from `Assets/Graphics/Rock Ostacles/rock1–rock5.png`. `Rock.png`, the first single rock, is no longer used.

**Layout.** Two continuous rows, one hanging from the ceiling and one standing on the ground. Each new rock starts where the previous one in its row ended (minus a 0.2 overlap), so a row never has gaps. Rows are topped up whenever their right end is less than 2 units past the camera's right edge.

**Randomness.** Each rock is picked at random, never the same one twice in a row within a row, and randomly mirrored left/right. Bottom rocks are flipped with a negative Y scale, not the Sprite Renderer's Flip Y, because only the scale flips the collider too.

**Rocks sit flush, and the gap is guaranteed.** Each rock's flat edge is pressed against the camera top or `floorY` (-4, the top of the Ground), so rocks never float. Instead of scaling a rock and seeing how deep it lands, the spawner picks a depth first and *scales the rock to reach it* — which is why `sizeVariation` is gone.

Playable height is 8.97 (camera top 4.97 down to floorY). `minGap` (3.0) is reserved for the fish, leaving a depth budget shared by the two rows. Before placing a rock, the spawner asks the other row how deep it already reaches across that stretch and takes only what is left, so every overlapping pair leaves at least `minGap`. Each rock is also kept within `maxDepthChange` (1.2) of the previous one in its row, so the cave wall flows instead of jumping. Simulated over 400 rocks: openings run 3.0 to 4.9, averaging 4.0, with no holes in either row. The fish is about 1.1 units tall.

Scale is clamped by `minScale`/`maxScale` (0.35–1.2) and `maxWidth` (8 units). Prefab scale is ignored at runtime.

**Prefabs.** Each `RockN.prefab` has scale 0.6, a Sprite Renderer at sorting order 1 (above the background), a Polygon Collider 2D traced from the image outline with OpenCV, `ObstacleMovement` (speed 5), and a `ScoreZone` child: a 0.5 × 20 trigger with `ScoreTrigger`, reaching from the rock's tip across the gap. Every rock passed scores a point, top and bottom separately.

**Images were trimmed.** The five PNGs were cropped to their visible pixels (2 px padding). rock3 and rock4 had a lot of empty space, which put their pivot off-centre. The untrimmed originals are in the 1950 backup.

**Full-size rocks (scale 1):** Rock1 8.4 × 2.7, Rock2 9.1 × 2.6, Rock3 9.4 × 5.1, Rock4 6.0 × 3.3, Rock5 9.7 × 4.4 units. In play they end up 4 to 8 wide.

**Tuning** is all on SpawnObstacles: `minGap` for difficulty (lower = harder), `minDepth`, `maxDepthChange`, `maxWidth`, and `floorY` if the Ground moves. Changing a default in the script does *not* change a value already saved in the scene; edit it in the Inspector.

---

## Where the tutorial is up to

Followed to roughly **1:15:00**. The code is ahead of the video in places and deliberately behind in others.

Done: scrolling background, obstacle spawning and movement, collision and game over, restart, score trigger, real scoring with the counter, the score reset, and the menu/pause/home flow above.

**Not done:** `HighScore` is declared in `GameManager` but nothing reads or writes it. The tutorial implements it properly with `PlayerPrefs` between 1:11:34 and 1:12:55 — read once in `Awake()`, then compared and saved in an `UpdateHighScore()` called from `GameOver()`. The two game-over labels (`CurrentScore_Label`, `BestScore_Label`) already exist in the scene but are unused.

Also not done: sound effects, and `CharactersButton` does nothing.

~~**Next:** a Resume button for the pause screen.~~ Built 2026-09-23 — see *Session of 2026-09-24* below.

---

## Score counter (2026-09-23)

The counter never appeared during a run. Nothing was wrong with the object — right font, white, size 90, top-centre, wired to the GameManager — but **no code ever showed it**, so it depended on its checkbox being left ticked in the scene, and it wasn't. `GameManager` now calls `ShowScore(true)` when a run starts and `ShowScore(false)` on the menu, leaving it up on the game over screen. Its saved state no longer matters.

Each rock's ScoreZone trigger was also widened from 0.5 to 2 local units. At the smallest rock scale it was 0.17 world units wide against 0.1 travelled per physics step, so a point could be missed.

---

## Session of 2026-09-24 — debris, UI locks and the Android export

The short version. `CHAT-HANDOFF.md` carries the detail for all of it, including the numbers.

- **Resume button** (late 09-23). `ResumeButton` under `Canvas`, starts inactive, calls `GameManager.TogglePause()`. `ShowRunButtons(visible, canResume)` keeps it off the game-over screen, where `TogglePause()` refuses to run and it would be a dead click.
- **Floating trash debris.** `TrashSpawner` on `SpawnObstacles` beside the rock spawner, with all nine trash prefabs wired in. It also owns the **difficulty ramp for the whole game**: over `rampSeconds` (90) it drives `ObstacleMovement.SpeedMultiplier` from 0.7 to 1.6 and the spawn interval from 3s to 0.9s, then stops. Because the multiplier is one shared static, rocks and debris accelerate in lockstep and debris can never drift out of the gap it spawned in — which is also why it is reset to 1 in both `StartSpawning()` and `StopSpawning()`.
- **Rock drift was tried and removed.** `ObstacleDrift` is debris-only; wandering rocks broke the continuous rows. See the `0053_before-rock-drift` and `0109_before-remove-wobble` backups.
- **Aspect-ratio locks on all UI.** Every button and the title carry an `AspectRatioFitter`; the art was swapped to the `Updated UI` set.
- **Android export.** `Active Input Handling` switched from "Both" to "Input System Package (New)" — Unity refuses to build for Android with "Both", and legacy `Input.GetKey` no longer compiles. IL2CPP, ARM64, landscape-left, `com.emmanuelokome.abyssalfish`. An APK was built and sent to a tester; **reports are still outstanding.**
- **Background anchor fix.** `ScrollObject` now pulls its row left until it starts at or before the screen's left edge, so the authored X only has to be roughly right. Without it the row cleared a 16:9 editor by 0.06 units and left bare space on the wider Android aspect.

---

## Phone test bugs (2026-09-25)

The 09-24 APK was tested on a real phone. Bugs are being fixed one at a time; this section is the running list.

### 1. Pause button doubled as Resume — fixed

`PauseButton` and `ResumeButton` both call the same `GameManager.TogglePause()`, and the Pause button stayed on screen while the game was paused. On a phone the two are close enough together that a second tap on Pause resumed the run by accident, when the player meant to hit Resume.

First fix: `TogglePause()` called `pauseButton.SetActive(!isPaused)`, hiding the button while paused. **Superseded on 09-29 — see entry 6.** The button now stays on screen and is neutralised by the dim instead.

### 2. Fish started too far left — fixed

On the phone the fish sat almost against the left edge. It was placed at `leftEdge + startMarginFromLeft`, a fixed **1.9 world units** in from the camera's left edge. Because screen height is locked at 10 units and width follows the aspect ratio, a fixed inset lands at a different fraction on every device — 10.7% across at 16:9, 8.8% on a 19.5:9 phone. Same class of problem as the hardcoded `x = -7` it replaced.

The field is now `startFractionFromLeft`, a **fraction of screen width**, defaulting to 0.3333, with `[Range(0f, 1f)]` for a slider. That puts the fish at 33.3% across on every aspect: x −2.96 at 16:9, −3.61 on a 19.5:9 phone.

**The rename matters and `[FormerlySerializedAs]` was deliberately left off**, against the usual advice. The old field held 1.9 world units and the new one holds a fraction, so migrating the saved value would have started the fish at `leftEdge + 1.9 × screenWidth` — about x +25 at 16:9, off screen and unplayable. The rename is also what lets the new default apply, since a value already saved in the scene beats a changed script default.

**Difficulty was left alone on purpose.** Moving the fish right shortens the runway rocks travel before reaching it, costing roughly 27% of the reaction time — on a 19.5:9 phone, ~2.5s of warning at full difficulty becomes ~1.8s. Changing speed in the same build would make the next phone test measure two things at once. If it plays too hard, tune `TrashSpawner.maxSpeedMultiplier` (1.6) or `minGap` (3.5), not the fish position.

### 3. No breathing room at the start of a run — fixed

Playtest feedback: debris appeared almost at once and the cave was on screen from the first frame, so a run began mid-action with nothing to read.

`StartSpawning()` seeded both rows at `CameraRightEdge() + 0.5`, so rocks were sliding in from frame one, and `TrashSpawner` had `nextSpawn = startInterval` (3s), putting junk on screen about 3.3s in. The player was never given empty water.

`MyObstacleSpawner` now has **`graceSeconds` (default 3, `[Range(0f, 10f)]`)**. `Update()` tracks `elapsed` and returns before filling the rows until the grace has passed. The rows still seed at the right edge in `StartSpawning()`, so nothing pops — the cave mouth simply arrives later.

**Debris needed no change.** `TrashSpawner.SpawnPiece()` bails when `MyObstacleSpawner.TryGetGapAt()` cannot report an opening, and with no rocks placed there is no opening, so trash spawns are skipped and rescheduled for free. Trash is now tied to the cave rather than to the clock.

On a 19.5:9 phone at the run-start speed of 3.5 u/s, the first rock reaches the fish at **7.3s instead of 4.3s**. Exact time of the first debris varies by up to one spawn interval, because whether the rows have filled in the same frame the trash timer fires depends on script execution order between the two components — harmless, and only affects the first piece.

### 4. Holes in the cave wall after ~40 points — fixed

Rows were topped up only to `CameraRightEdge() + 2f`. Rocks all move in one step per frame, so a frame hitch longer than 2 units of travel (a quarter second at the late-run speed of 8 u/s) let the end of the wall scroll on screen before the next top-up. The following rocks then spawned mid-screen, leaving a hole behind them. It showed up late in a run because both causes ramp together: rocks get faster, and debris — whose colliders are traced at runtime — gets denser.

`fillTo` is now `CameraRightEdge() + maxWidth` (8), so the wall always runs a full rock beyond the screen and spawns stay off-camera. One line, reuses the existing field, no new knob.

Ruled out first: the scale math was simulated over 80,000 rocks with the scene's values and the real sprite sizes. No rock ever came out below `minScale`, and the narrowest was 2.18 units, so rocks were always tiling continuously — the hole was never in the rock sizing.

### 5. Pause screen had no backdrop — fixed

Pausing froze the game but left it at full brightness with three buttons floating over it, so the run still read as the foreground.

`GameManager.ShowPauseDim()` now builds a full-screen black `Image` under the Canvas the first time the game is paused, at `pauseDimAlpha` (0.6, `[Range(0f, 1f)]`), then calls `SetAsLastSibling()` on the dim and on Restart/Home/Resume in turn, so UI sibling order puts the dim above the run and the buttons above the dim. A Graphic is a raycast target by default, so the dim also swallows any tap aimed at what is behind it.

Built in code rather than authored into the scene: no Inspector slot to wire, no scene edit, and nothing to knock loose. It is created lazily on the first pause and then just toggled.

The call sits inside `ShowRunButtons()`, keyed to the same `visible && canResume` that drives the Resume button, so the dim is tied to the paused state at a single point — game over, restart and home all clear it without their own call.

### 6. Pause button should be dimmed, not hidden — done

Follow-up to entries 1 and 5. Hiding the Pause button while paused worked but read as the button vanishing; it should stay put and go dark with the rest of the frozen run, the way the fish and the cave do.

Mostly a deletion. `pauseButton.SetActive(false)` is gone from both `TogglePause()` and `GameOver()`, so the button simply stays on screen; sitting under the dim already makes it dark, and the dim's raycast blocking already makes it dead. The two remaining calls are `StartGame()` (true) and `BackToMenu()` (false — the menu must not show it).

The dim now covers game over too: `ShowRunButtons()` passes `visible` rather than `visible && canResume`, so both frozen screens get the backdrop, and `gameOverText` was added to the list raised above it. Only the Resume button stays keyed to `visible && canResume`.

| State | Pause button | Dim | Above the dim |
|---|---|---|---|
| Playing | visible, live | off | — |
| Paused | visible, dark, dead | on | Resume, Restart, Home |
| Game over | visible, dark, dead | on | GAME OVER, Restart, Home |
| Menu | hidden | off | — |

### 7. Wait before the cave and trash was too long — fixed

Entry 3 added `graceSeconds` (3) to give the player open water at the start of a run. In play that turned out to stack on top of a lead-in that already existed: the rows seed at `CameraRightEdge() + 0.5`, so the first rock already takes ~3.5s (16:9) or ~4.3s (19.5:9 phone) to swim across to the fish. Grace made it ~6.5s and ~7.3s, which read as dead air.

`graceSeconds`, its `elapsed` counter and the `Update()` gate are removed. The travel time is the breathing room. First rock now reaches the fish at ~3.5s in the editor and ~4.3s on the phone; trash makes its first spawn attempt at `startInterval` (3s) and now finds a cave waiting, so it appears around 3s instead of 3–6s.

Removed rather than set to 0 because the Unity editor was open and the scene had `graceSeconds: 3` saved, which beats any script default. Deleting the serialized field orphans that entry, so the change took effect with no scene edit and no Editor work. The orphaned line clears itself next time the scene is saved.

## Coins (2026-10-02)

`Assets/Prefabs/Coins/Coin.prefab`, built on disk from `Assets/Graphics/Coins/Frame 15.png` and modelled on `trash1.prefab`: Transform, Sprite Renderer at sorting order 2, and a `CircleCollider2D` with **`m_IsTrigger: 1`**, radius 0.36, no offset.

**Trigger, not solid, on purpose.** `PlayerHit.OnCollisionEnter2D` ends the run on any collision, so a solid coin collider would kill the fish the moment it was collected. Collection has to go through `OnTriggerEnter2D`.

A circle rather than a traced polygon: the coin is round, and it keeps coins out of the runtime collider tracing that entry 4 pinned as the likely source of frame hitches.

`Frame 15.png` was `spriteMode: 2` (Multiple), sliced into a single sub-sprite `Frame 15_0`. Changed to `spriteMode: 1`. In Multiple mode the only usable reference is the slice's internal ID (`-4629613613065720279`), not `fileID: 21300000`, and the project's own trip-hazard note says those long IDs don't resolve. Single also matches every other sprite in the project. Cost: the slice had cropped ~6px of transparent border, so the sprite is 0.90 × 0.86 units against the coin's actual 0.79 × 0.72 — hence a 0.36 radius sized to the coin rather than the sprite bounds.

Nothing spawns or collects coins yet.

## Coin spawning and collection (2026-10-02)

Two new scripts, plus wiring in `GameManager`.

**`CoinSpawner.cs`** sits on `SpawnObstacles` beside the rock and trash spawners, with
`[RequireComponent(typeof(MyObstacleSpawner))]` so it cannot be put anywhere else. It places
coins with the same `TryGetGapAt()` the debris uses, reserving the coin's diagonal plus its
drift allowance from each wall, and attaches `ObstacleMovement`, `TrashSpin`, `ObstacleDrift`
and `CoinPickup` at runtime. Spawn interval eases from 2.5s to `minInterval` 1s over
`rampSeconds` 90, with `maxOnScreen` 5 — the two caps that answer "a max limit of coins per
second".

**It has no speed ramp, on purpose.** Coins carry `ObstacleMovement`, which reads the shared
`ObstacleMovement.SpeedMultiplier` that `TrashSpawner` already drives from 0.7 to 1.6, so coins
accelerate with the cave for free. Two scripts writing that static would fight over it, and
`TrashSpawner` owns it. This halved the script against its template.

It also skips the `TryGetComponent<Collider2D>` fallback `TrashSpawner` needs: the coin prefab
ships with its trigger collider, and adding a traced `PolygonCollider2D` would both duplicate it
and bring back the runtime tracing that entry 4 pinned as the likely frame-hitch source.

**`CoinPickup.cs`** is `ScoreTrigger` for coins — a `collected` guard, a
`GetComponent<PlayerControllerScript>()` check so only the fish collects, then `AddCoin()` and
`Destroy`. `OnTriggerEnter2D` and never `OnCollisionEnter2D`, because `PlayerHit` ends the run on
any real collision.

**`GameManager`** gained a `coinText` slot, `Coins` backed by `PlayerPrefs` key `"Coins"`,
`AddCoin()` (guarded on `isGameOver` like `AddScore()`), and null-guarded
`coinSpawner.StartSpawning()` / `StopSpawning()` beside every existing `trashSpawner` call.
`PlayerPrefs.Save()` flushes in `GameOver()` and `BackToMenu()`, since `SetInt` alone only writes
to memory and a phone can be killed without a clean quit. The wallet is **not** cleared by
`ResetScore()`.

**The HUD icon** is built in code too. `BuildCoinIcon()` creates a UI `Image` parented to the
counter, anchored to the middle of its left edge with pivot (1, 0.5) so it always sits outside
the box, and `ShowCoins()` formats the number as `": 42"`. The sprite is read from the coin
prefab via a new `CoinSpawner.CoinSprite` property rather than a second serialized slot, so the
icon always matches whatever coin is actually spawning. `raycastTarget` is off so it can never
eat a tap. `coinIconSize` (80) and `coinIconGap` (6) are the tuning knobs.

Note the icon extends roughly 86 UI units left of the counter's box, so the counter needs to sit
at least that far from the screen edge: Pos X 230 at the 1920×1080 reference, not 150.

Still needs Editor work: add the Coin Spawner component, drag the prefab in, make the
`CoinCounter` text and drag it into `GameManager`. Until then the game runs normally and logs a
missing-component error, the same way a missing `TrashSpawner` does.

## Adaptive drift for coins and trash (2026-10-02) — REVERTED SAME DAY

> **Reverted on 2026-10-02.** Playtested and rejected: the vertical motion was too fast and
> read as darting rather than floating. The three scripts were restored from
> `Backups/2026-10-02_2213_before-adaptive-drift/`, which put back the fixed ±0.4 box at
> 0.15–0.5 and the `driftX`/`driftY`/`driftMinSpeed`/`driftMaxSpeed` fields. The scene had
> never been re-saved after the rename, so its original values were still on disk and came
> straight back into play — no scene edit and no re-tuning needed.
>
> Kept for the reasoning, not as a description of current behaviour. The lesson worth
> keeping: the ceiling on a fixed box is real (`driftY` above ~1.25 blocks spawns in tight
> gaps), but players read fast vertical drift as darting, so the answer is not simply a
> bigger box. Lateral float is what reads as water.

The description below is of the reverted change.

Coins drifted in a fixed ±0.4 box at 0.15–0.5 units/sec, copied from the trash, so the player
could sit still and wait for one. Raising those numbers by hand hits a hard ceiling: the spawner
reserved `clearance = radius + driftY` from each side of the gap, so with the coin's 0.48 radius
and the scene's `minGap` 3.5, a `driftY` above about 1.25 stopped coins spawning in tight gaps
entirely. A fixed box has to be sized for the worst corridor in the game, which makes it useless
in the common wide ones.

The box is now derived from the corridor the piece actually landed in. New
`ObstacleDrift.AllowedY(gapLow, gapHigh, radius, margin, cap)` returns what is left of the
half-gap once the piece's own reach and a safety margin are taken off, and the spawner reserves
that same number from each wall — one value feeding both, so they cannot drift apart. Both
spawners call it; `Configure()` and the bounce logic in `ObstacleDrift` are untouched.

Speed is derived from the box rather than set directly, through `minCrossSeconds` and
`maxCrossSeconds`. A bigger wander has to move faster to cross in the same time, or a coin in a
wide corridor crawls. The box is also kept from being too tall and narrow
(`Mathf.Min(maxDriftX, allowY)`), because a thin box makes a piece ricochet sideways and read as
vibrating rather than floating.

Measured result, coin radius 0.48:

| Gap | Coin box | Sweep | Coin speed | Trash box | Sweep |
|---|---|---|---|---|---|
| 3.50 | 1.12 | 64% | 0.89–2.23 | 0.85 | 49% |
| 4.50 | 1.60 | 71% | 1.28–3.20 | 1.20 | 53% |
| 6.47 | 1.60 | 49% | 1.28–3.20 | 1.20 | 37% |

Against the old fixed 0.40 box at 0.15–0.50, that is roughly 3–4x the range and up to 6x the
speed. If coins read as frantic rather than challenging, raise `minCrossSeconds` (1.0) before
touching anything else — it caps the top speed.

All the old values were saved in `SampleScene.unity`, and saved values beat script defaults, so
`driftY` and `driftX` were **renamed** to `maxDriftY`/`maxDriftX` and `driftMinSpeed`/
`driftMaxSpeed` **deleted**. That orphans the saved entries and lets the new defaults apply with
no scene edit, which mattered because Unity was open.

**Trash was included at the user's request and is the risk here.** Livelier debris is a
difficulty increase on hazards, stacked on top of moving the fish to 1/3. `wallMargin` 0.5 and
slower crossing times are what stop a wall-hugging piece plugging a corridor. If the cave turns
unfair, lower trash's `maxDriftY` first, then `maxOnScreen`.

## Currents (2026-10-02)

Requested as "currents pushing the rock to the player fish". Implemented as a swell that accelerates the whole cave in bursts rather than a per-object push, for a structural reason: the two rock rows are only seamless because every obstacle reads one shared `ObstacleMovement.SpeedMultiplier`. Pushing rocks individually would pull the rows out of formation and reopen exactly the holes that entry 4 fixed. Anything that moves rocks must go through that one value, which means everything surges together — rocks, trash and coins stay locked in formation.

`TrashSpawner.Surge()` returns a multiplier applied on top of the existing ramp:

```
phase = elapsed % (surgeInterval + surgeSeconds)
calm   -> 1
swell  -> 1 + (surgeStrength - 1) * sin(t * PI)
```

A sine arch so it eases in and out instead of snapping. Derived from `elapsed` rather than its own timer, so it holds no state and is reset for free by `ResetRun()`. Defaults: `surgeStrength` 1.3, `surgeSeconds` 2.5, `surgeInterval` 9 — an 11.5s cycle. The interval is fixed rather than random on purpose, so the player can learn the rhythm and time a run through a tight gap.

Cost in reaction time on a 19.5:9 phone, fish at 1/3:

| When | Base | Peak | Warning time |
|---|---|---|---|
| Run start | 3.5 | 4.5 | 4.13s -> 3.17s |
| 45s in | 5.8 | 7.5 | 2.51s -> 1.93s |
| Full difficulty | 8.0 | 10.4 | 1.80s -> 1.39s |

A surge cuts reaction time by the same proportion it adds speed, so 1.4 is about the ceiling for `surgeStrength`. Set it to 1 to switch currents off entirely.

It lives in `TrashSpawner` because that script already owns the shared multiplier. That makes the script three things now — debris, difficulty ramp and currents — but splitting it would mean two writers on one static, which is the bug this design exists to avoid.

## Drift made vertical only (2026-10-02)

With currents in, sideways wander stopped making sense: a real current runs one way, so a
piece drifting left and right against the flow reads wrong. Drift is now purely up and down,
and all horizontal movement belongs to `ObstacleMovement` plus the surge.

`ObstacleDrift` lost its x axis entirely — `_allowed`, `_offset` and `_velocity` are floats
rather than `Vector2`, `Configure()` takes a single `allowed`, and the x branch of the bounce
test is gone. `driftX` was removed from both spawners; its saved scene entries are orphaned
and harmless.

**Setting `driftX` to 0 would not have worked.** The velocity was
`Random.insideUnitCircle.normalized * speed`, so a mostly-horizontal pick would have spent
nearly all its speed on an axis clamped to zero and the piece would have looked stalled.
Picking a vertical-only velocity is what keeps the pace steady.

Side effect worth knowing: a random 2D direction put only ~64% of the speed into vertical on
average (the mean absolute sine over a circle is 2/pi). All of it is vertical now, so the same
`driftMinSpeed`/`driftMaxSpeed` give roughly **1.5x** the up-down pace. `driftMaxSpeed` 0.32
reproduces the old feel, and the scene still holds 0.5.

Removing the horizontal axis also closed a latent hole: `clearance` only ever reserved
`driftY` from the walls, so horizontal wander was the one unguarded axis — a piece could in
principle drift along the tunnel into a narrower stretch. It no longer can.

Surges are untouched.

## Per-run coins, and currents that push the fish (2026-10-02)

### Coins split in two

The counter should read per-run, but a wallet is still wanted later for buying fish skins. So
rather than tearing the wallet out and rebuilding it, the one counter became two:
`Coins` (this run, what the HUD shows, cleared by a new `ResetCoins()` called beside
`ResetScore()` in `StartGame()` and `BackToMenu()`) and `TotalCoins` (lifetime, still saved to
`PlayerPrefs` key `"Coins"`). `AddCoin()` increments both. Nothing shows `TotalCoins` yet; it
banks quietly so no coin collected before the shop exists is lost.

Worth recording: the reported "coins go back to 0" could not be traced to a defect. Nothing
in the code reset `Coins`, and the scene wiring was correct (`scoreTextCounter` and `coinText`
point at two different objects). The likeliest explanation is that the per-run behaviour was
what was wanted all along. The split makes the intent explicit either way.

### The current now shoves the fish

Surges already accelerated the cave; the fish now gets pushed back while one passes, so the
player feels the water instead of watching it. This reverses the earlier "obstacles only"
decision.

`ObstacleMovement.Surge` is published next to `SpeedMultiplier` and written by `TrashSpawner`
in the same place, so the fish and the cave are driven by one swell rather than each keeping
its own timer. It is reset to 1 in `StopSpawning()` alongside the multiplier, for the same
reason: it is a static and would survive into the next run.

`PlayerControllerScript.FixedUpdate()`:

```
push = (Surge - 1) * _currentPush       // shoved left while the swell passes
home = (_homeX - x) * _currentReturn    // swims back once it does
rb.linearVelocityX = home - push
```

`_homeX` comes from the start position `ResetPlayer()` is already handed, so nothing extra is
wired. X velocity was previously never touched — only Y — so this does not fight anything.

**The spring is the safety.** The pull home grows with distance, so the fish settles where the
two balance and displacement is capped at `push / _currentReturn`: at the 1.3 surge peak,
`0.3 * 3 / 4 = 0.225` units. Nothing accumulates, so however long a surge lasts the fish
cannot be walked off the left edge — at 19.5:9 it moves from x -3.61 to -3.84 against a left
edge of -10.83. The spring's time constant is 1/4 = 0.25s, so it settles and returns inside
about 0.75s and reads as water rather than input lag.

Tune the feel with `_currentPush` (3); raise `_currentReturn` (4) to snap back harder and
drift less. If a surge plus the push is too punishing at full difficulty, lower
`surgeStrength` first — it affects both the cave and the fish, so it is the single dial.

## Debris carried forward by the current (2026-10-02)

Reported as "the trash doesn't come to the player on the x axis". Not a defect: trash
carried `ObstacleMovement` and so moved at **exactly** the cave's speed, giving it zero
motion relative to the rock walls. It scrolled past as scenery and never visibly closed on
the fish. The code comment actually bragged about this — the lock was deliberate, because it
guaranteed a piece could never travel forward into a rock face.

Breaking that lock is the feature, so the guarantee had to be replaced rather than dropped.
`ObstacleDrift.ConfigureCurrent(rocks, push, radius)` is now called for debris, and
`CarryForward()` runs each frame:

1. moves the piece left by `debrisCurrentPush * ObstacleMovement.Surge`, on top of the
   cave's scroll, so a swell throws debris harder;
2. re-queries `MyObstacleSpawner.TryGetGapAt()` at the piece's **current** x and clamps its y
   inside that corridor.

Step 2 is what makes step 1 safe. The piece is no longer where it was placed, so the corridor
it occupies has to be re-derived continuously rather than trusted from spawn time.

When the clamp fires it also resets `_offset` to 0, re-centring the drift band on where the
piece actually ended up. Without that the band would keep pulling it back into the wall it
was just held off, and the two would fight.

Closing speed against the walls:

| When | Cave | Trash | Closing |
|---|---|---|---|
| Run start, calm | 3.5 | 4.7 | 1.2 |
| Run start, surge | 4.5 | 6.1 | 1.6 |
| Full difficulty, calm | 8.0 | 9.2 | 1.2 |
| Full difficulty, surge | 10.4 | 12.0 | 1.6 |

Over a 21-unit traverse at run-start speed a piece advances about 7 units past the walls, so
it genuinely changes corridor several times on the way across — which is exactly why the
per-frame clamp is not optional.

`debrisCurrentPush` 0 restores the old lockstep behaviour. Coins deliberately do not call
`ConfigureCurrent()` and stay locked to the cave; the same two lines would give them the
same treatment if wanted, at the cost of making them harder to catch.

Cost: one `TryGetGapAt()` per debris piece per frame, up to `maxOnScreen` 8. Each walks the
live rock lists, so on the order of a hundred comparisons a frame — immaterial next to the
runtime collider tracing already happening on spawn.

## Coins carried by the current, and the clamp buzz fixed (2026-10-02)

### Coins get the debris treatment

`CoinSpawner` now calls `ObstacleDrift.ConfigureCurrent()` as well, with its own
`coinCurrentPush` of 0.8 against the debris 1.2. Lower deliberately: a coin is small and
dense where trash is light and buoyant, so the current should carry it less, and it keeps
coins catchable. The run reads as dodge the trash, chase the coins, with both closing on the
fish faster than the rock walls do.

| | Cave | Trash | Coin | Coin reaches fish |
|---|---|---|---|---|
| Run start, calm | 3.5 | 4.7 | 4.3 | 3.4s |
| Run start, surge | 4.5 | 6.1 | 5.6 | 2.6s |
| Full difficulty, calm | 8.0 | 9.2 | 8.8 | 1.6s |
| Full difficulty, surge | 10.4 | 12.0 | 11.4 | 1.3s |

At full difficulty a coin crosses to the fish in about 1.3 seconds, so late-run coins are
close to reflex grabs. If that proves too tight, `coinCurrentPush` is the dial, not
`surgeStrength` — that one drives the cave and the fish push as well.

### The clamp was going to buzz, and now does not

Flagged when carry-forward went in, and fixed before it was seen in play. `CarryForward()`
clamped the piece back inside the corridor but left `ObstacleDrift._velocity` untouched —
still pointing into the rock. Next frame the drift drove back in, the clamp shoved it out,
and the piece would buzz at the wall at frame rate for as long as it was pressed there.

Resetting `_offset` was not enough on its own: that re-centres the wander band but says
nothing about which way the piece is travelling. The clamp now also reverses `_velocity`
when it is heading into the wall it was just held off:

```
wallBelow        = held > p.y
headingIntoWall  = wallBelow ? _velocity < 0 : _velocity > 0
if (headingIntoWall) _velocity = -_velocity
```

Same idea as the band bounce in `Update()`, applied to the real wall instead of the
imaginary one. A piece pressed against a rock now slides along it and drifts away, instead
of vibrating against it.

## A brief grace period, restored (2026-10-03)

Entry 7 deleted `graceSeconds` because 3 seconds of pause on top of the 4.3s the first rock
already takes to cross read as dead air. The want behind it was never wrong though, only the
duration: a run should open on empty water for a moment so the player can read that obstacles
are coming, rather than starting mid-action.

So it is back at **0.6s**, with `[Range(0f, 1f)]` on it. The cap is the point — it stops the
value drifting back to the three seconds that caused the problem, and makes the intended
scale obvious to anyone who opens the Inspector.

Same implementation as before: `elapsed` counted in `Update()`, an early return before the
`Forget`/`FillRow` calls, reset in `StartSpawning()`. Coins and debris are gated for free,
because both bail when `TryGetGapAt()` cannot report an opening and there are no rocks to
report one.

The re-added field took its new default cleanly: the orphaned `graceSeconds: 3` had been
dropped from `SampleScene.unity` when the scene was next saved, so nothing overrode it.

### Drift speed, for the record

The scene had `driftMinSpeed: 0.5` with `driftMaxSpeed: 0.32` on the trash — min above max.
`Random.Range(float, float)` still returns values across that span, so it is not a defect,
but it meant debris was bobbing at 0.32–0.50 rather than the gentler float that was wanted.

Recommended **0.10 / 0.32**. When drift was a random 2D direction only about 64% of the speed
landed on the vertical axis (mean absolute sine over a circle is 2/pi), so the original
0.15–0.5 felt like 0.10–0.32 of vertical motion. Now that all of it is vertical, those are
the numbers that reproduce the float that was approved. Both spawners carry these fields and
both have them saved in the scene, so it is an Inspector change, not a code one.

## Coins kept off the rock face (2026-10-03)

Coins were spawning or ending up pressed against the rocks, reading as stuck in the wall and
awkward to reach.

The spawn placement was not the culprit — that already reserved `radius + driftY`. The clamp
in `ObstacleDrift.CarryForward()` was: it used `Mathf.Clamp(p.y, low + _radius, high -
_radius)`, reserving only the coin's own radius and so permitting its **edge to touch the
rock face exactly**. Since coins are carried forward through corridors they were not placed
for, that clamp fires constantly, and it was the thing actually deciding how close a coin
could get.

`ConfigureCurrent()` gained an optional `wallGap`, defaulting to 0 so debris is unaffected
and still presses against the wall as a current should push it. `CoinSpawner` passes 0.35,
and adds it to the spawn clearance too, so the gap holds from placement through to the clamp:

| | Coin centre held within | Visible gap |
|---|---|---|
| Before | 0.48 of the wall | **0.00** |
| After | 0.83 of the wall | **0.35** |

At spawn the coin's edge now starts `driftY + wallGap` = 0.75 units off the wall.

**The tight-corridor fallback is load-bearing.** `Mathf.Clamp` does not guard against min
above max: it tests `value < min` first, so an inverted range snaps the piece to `min`, which
sits beyond the far wall. A corridor too narrow to hold `radius + wallGap` on both sides
therefore drops back to `radius` alone, and only bails if even that will not fit.

## Trash colliders baked and fitted (2026-10-03)

Only `trash1` had a `PolygonCollider2D`. The other eight relied on `TrashSpawner` adding one
at spawn, which Unity traced from the sprite's fallback physics shape. That worked, but it
was the standing suspect for the frame hitches noted in entry 4, and it meant the outlines
could never be inspected or adjusted.

All nine are now baked, traced from each PNG's alpha channel with OpenCV: threshold at
alpha 16, erode 1px so the collider sits just inside the art, largest external contour,
then `approxPolyDP` at 0.25% of the perimeter. Points are converted to Unity local space as
`x = (px - w/2) / ppu`, `y = (h/2 - py) / ppu` -- all nine sprites are 100 ppu with a centre
pivot.

The pipeline was validated against the one collider that already existed before anything was
written: Unity's own `trash1` outline spans x -0.80..0.82, y -1.48..1.49, and the tracer
produced x -0.78..0.79, y -1.45..1.47 -- the difference being exactly the 1px inset.
`trash1` was then refitted too, for consistency and at 25 points rather than 41.

| | Sprite | Collider | Points |
|---|---|---|---|
| trash1 | 1.68 x 3.04 | 1.57 x 2.92 | 25 |
| trash3 | 1.84 x 3.14 | 1.67 x 2.96 | 36 |
| **trash6** | **2.58 x 2.52** | **1.47 x 1.34** | 45 |
| **trash7** | **2.52 x 2.66** | **1.53 x 1.77** | 32 |
| trash9 | 1.41 x 1.36 | 1.15 x 1.11 | 49 |

trash6 and trash7 are the ones that most needed this: both carry heavy transparent padding,
so anything sized to the sprite rect gave them a hitbox far larger than the visible junk.
The handoff had flagged their padding as a cosmetic oddity; it was a fairness problem too.

Each prefab was checked after writing for exactly one collider, no duplicate fileIDs and no
dangling component references. The `TryGetComponent` fallback in `TrashSpawner` is kept as a
safety net for any future prefab added without a collider -- it would otherwise pass straight
through the fish -- but it no longer fires.

## Get-ready state (2026-10-03)

Added after asking what the genre actually does about start-of-run grace. The answer is that
Flappy Bird has no grace timer at all: it holds a get-ready state and begins on the player's
first tap, so the wait is unbounded and player-controlled. No number to copy, and a better
pattern than any value of `graceSeconds` — a timer cannot know when someone is looking at
the screen, which is the death a phone player actually resents.

`StartGame()` now sets everything up but starts nothing: it flips `isWaitingToStart`, calls
`PlayerControllerScript.SetHovering(true)` and stops there. `SetHovering` zeroes gravity and
velocity so the fish parks. The background keeps scrolling, because `ScrollObject` runs off
its own speed rather than `ObstacleMovement.SpeedMultiplier`, so the world still reads as
alive while nothing is coming at the player.

`PlayerControllerScript.Update()` was already the single input path, so the tap is detected
there rather than duplicating the device-polling idiom in `GameManager`. It calls
`BeginRun()` and then falls through to the normal swim, so **the tap that starts the run is
also a flap** and starting never costs the player one.

`BeginRun()` is where the three spawners actually start. `isWaitingToStart` is cleared there
and in `GameOver()` and `BackToMenu()` as well, so no exit can leave the flag set.

### The bob chases a target rather than integrating a velocity

First version set `linearVelocityY` to a cosine, whose integral is the sine bob wanted. That
is correct on paper and wrong in a game loop: FixedUpdate discretisation accumulates rounding
error, and with gravity off there is nothing to pull the fish back, so it would wander off
its mark during a long wait. It now chases `_homeY + sin(t) * _hoverBob` with a stiffness of
8, which is self-correcting and cannot drift. `_homeY` comes from the start position
`ResetPlayer()` already receives.

`_hoverBob` 0.25 and `_hoverSpeed` 0.8 cycles/sec. The first pass at 0.35 / 1.5 gave a peak
velocity of 3.3 units/sec against a swim force of 4, which read as frantic rather than
waiting.

`graceSeconds` 0.6 is still in place and still useful: it keeps the cave from being on screen
the instant a run begins. The two solve different halves of the same problem.

No Editor work. A "TAP TO START" prompt would need a new TMP object in the Canvas and is the
obvious next polish, but the bob carries the message well enough to test with.

## Trash colliders were silently empty (2026-10-03)

The colliders baked earlier today never collided: the fish swam straight through trash.
Unity logged nothing, and every structural check passed -- right GameObject,
`m_IsTrigger: 0`, enabled, full point data present.

The fault was one dash. `m_Paths` is a list **of paths**, each path itself a list of points,
so Unity writes the first point with both dashes on one line:

```
    m_Paths:
    - - {x: 0.76, y: -1.19}
      - {x: 0.75, y: -1.12}
```

The generator emitted a single dash, which makes that first element a mapping instead of a
list. Valid YAML, parses without complaint, and yields a path with no points -- a collider
with no shape. It broke `trash1` too, which had been working before it was refitted.

Now validated by shoelace area rather than by eye: all nine have exactly one path, 22-49
points, and areas of 0.47 to 4.12 square units. The lesson is that counting points is not a
structural check -- the nesting has to be checked as well, because this failure is invisible
to both the YAML parser and the Unity console.

## Start-of-run grace removed again (2026-10-03)

`graceSeconds` is gone, this time for good. The get-ready state does the same job properly:
the player decides when the run begins, instead of a number guessing when they are looking at
the screen. PLAY hovers the fish with the background scrolling, and the first tap starts the
spawners; the cave then takes its usual ~3.5s to swim across.

Deleted rather than zeroed, because the scene had `graceSeconds: 0.6` saved and saved values
beat script defaults. Deleting orphans that entry, so it took effect with no scene edit while
Unity was open.

## Clean-code naming pass (2026-10-04)

Applied the Code Monkey naming rules across all twelve scripts.

| Rule | Change |
|---|---|
| Fields are camelCase | 28 `_underscore` fields renamed, across 4 scripts |
| Constants are UPPER_SNAKE_CASE | `CoinsKey` to `COINS_KEY`, plus 3 new named constants |
| Collections carry Array or List | `rockPrefabs`, `trashPrefabs`, and three `live` lists |
| Meaningful names | `rend` to `pieceRenderer`, `rocks` to `rockSpawner`, `_changeIn` to `secondsUntilTurn` |
| No magic numbers | the hover spring `8f` became `HOVER_FOLLOW_STRENGTH`, the drift turn interval `1.5f, 3.5f` became `MIN/MAX_SECONDS_UNTIL_TURN` |

**Fourteen of those renames touch serialized fields with values saved in the scene**, and four
of them were tuned away from their script defaults: `_swimForce` 5 against a default of 4,
`_rotationSpeed` 10 against 5, `_maxTiltUp` 5 against 25, `_maxTiltDown` -60 against -45. Plus
the two prefab arrays, which would simply have emptied. Every one carries
`[FormerlySerializedAs]` so Unity migrates the saved value on load.

### Two things went wrong in the pass itself

**The attributes named the wrong field.** `serialized_rename` inserted
`[FormerlySerializedAs("_swimForce")]` and then the rename ran over the whole file -- including
the attribute string it had just written -- leaving `[FormerlySerializedAs("swimForce")]`. That
points at the new name, so Unity would have found nothing to migrate and every tuned value would
have silently reverted to its default. The safety mechanism was decorative until it was checked.
Each attribute is now verified against an entry that actually exists in `SampleScene.unity`.

**Word-boundary renames bled into prose.** Replacing `rocks` with `rockSpawner` also rewrote the
English in comments and tooltips: "Builds two endless rows of rocksList", "turns the rockSpawner'
5 into 8". Fourteen of those across three files, reverted by fixing only the text after `//` and
inside string literals.

### Left alone deliberately

`ObstacleMovement.SpeedMultiplier` and `Surge` are public static fields in PascalCase, which the
rules would make camelCase. PascalCase for public statics is standard .NET, they read as the
shared values they are, and changing them would touch five files for no readability gain.

Brace style is Allman throughout, where Code Monkey prefers braces on the same line. His own note
says the choice matters less than consistency, and the project is already consistent, so
reformatting twelve files would be a large diff with no benefit.

`PlayerControllerScript` keeps its tutorial name, per the guideline about not renaming tutorial
code unnecessarily.

## Two fields shadowed by the naming pass (2026-10-04)

The clean-code renames introduced two parameter/field collisions. Unity reported one as
`CS1717: Assignment made to same variable`; both were real behaviour regressions.

`ObstacleDrift.ConfigureCurrent()` had `_wallGap = wallGap`, which became `wallGap = wallGap`
once the field lost its underscore. The field was never set, so it stayed 0 and coins went back
to pressing against the rock faces -- undoing the fix from the day before.

`TrashSpin.SetSpin()` had the same shape: `_degreesPerSecond = degreesPerSecond` became a
self-assignment, so **every piece of trash and every coin stopped spinning**. Nothing visibly
errored; the pieces just sat still.

Both now use `this.x = x`, which keeps the parameter and the field on the same meaningful name.
Worth noting the general hazard: stripping an underscore prefix is exactly what creates these
collisions, because the prefix existed to separate field from parameter in the first place. A
grep for `^\s+(\w+) = ;` catches them all and found the second one, which had not been
reported.

### The empty Rb slot

`PlayerControllerScript` exposed `[SerializeField] Rigidbody2D rb`, and the scene has always
held `rb: {fileID: 0}` -- never assigned. It worked anyway because `Awake()` does
`rb = GetComponent<Rigidbody2D>()`, so the slot was misleading noise rather than a fault.
`[SerializeField]` dropped; the field is now plain private and the empty slot is gone.

## Get-ready state removed (2026-10-05)

PLAY now starts the run outright. There is no hover-and-wait before the first tap, so the
Flappy Bird get-ready beat is gone from the project.

`StartGame()` calls the three spawners directly, which is all `BeginRun()` ever did once the
hover came out of it.

| Script | Removed |
|---|---|
| `GameManager` | `isWaitingToStart`, `BeginRun()`, the `SetHovering(true)` call |
| `PlayerControllerScript` | `hoverBob`, `hoverSpeed`, `HOVER_FOLLOW_STRENGTH`, `isHovering`, `SetHovering()`, the bob block in `FixedUpdate()`, the `BeginRun()` call in `Update()` |

`homeY` went with the bob. It was only ever the bob's centre line -- `homeX` is what the
current pulls against -- and leaving it behind would have been an assigned-never-read warning.

The lead-in is now `leadInSeconds` plus the swim in from the right edge: roughly 1s on top of
3.5s at 16:9, or 4.3s on a 19.5:9 phone. No grace timer came back, so the fish starts sinking
on the frame PLAY is pressed. That is the one change a player will feel.

`MyObstacleSpawner` carried the state in its prose as well: the lead-in tooltip said "after the
first tap", and the `StartSpawning()` comment argued for the get-ready state over a timed pause.
Both now read for a run that starts at PLAY. The 2.5s cap rationale moved out of that tooltip
into a comment above it, which also brings the tooltip under 90 characters.

Verified by building `Assembly-CSharp.csproj` with `dotnet build` against Unity's reference
assemblies: 0 errors, 0 warnings. Output was redirected to a temp folder, so nothing landed in
the project's `obj/` or `Library/`.

### The scene keeps two orphans

`SampleScene.unity` still holds `hoverBob: 0.25` and `hoverSpeed: 0.8` on the fish. Nothing
reads them and Unity drops them the next time the scene is saved. Left alone deliberately
rather than edited out, because the scene is open in the Editor -- same reasoning as the
`graceSeconds` orphan on 10-03.

### leadInSeconds has never been serialized

`SampleScene.unity` has no `leadInSeconds` entry, so the field is still on its script default
of 1. Worth a look in the Inspector once Unity reloads: a newly added serialized field is the
exact case that can come back as 0.

### All twelve scripts are LF now (2026-10-05)

`MyObstacleSpawner.cs` had been mixed for a while -- 14 CRLF lines among 284 -- which is what makes
Visual Studio raise its "Inconsistent Line Endings" box every time the file is opened. The comment
edits above preserved that mix deliberately, so the diff would show the two hunks and nothing else,
and the normalising was done afterwards as its own step.

Both odd files were converted: `MyObstacleSpawner.cs` and `GameManager.cs`, the latter having been
wholly CRLF since it was written. All twelve scripts now use LF, which is what the other ten already
did and what `.gitattributes` line 20 (`*.cs text diff=csharp`) stores in the repo anyway.

The conversion asserted byte equality with CR characters discounted before writing, and the code
compiles unchanged. The practical payoff is that a whole-file rewrite -- a comment trim, say -- can
no longer silently normalise endings and fake hundreds of diff lines on untouched code.

## Comments shortened across all twelve scripts (2026-10-05)

Comments were to be short and direct rather than paragraphs sitting between the code.

| | Before | After |
|---|---|---|
| Comment lines | 233 | 155 |
| Share of the file | 14% | 10% |
| Tooltips over 90 characters | 26 | 0 |
| Longest tooltip | 278 | 80 |
| Total lines | 1619 | 1547 |

An earlier attempt had moved overflowing tooltip text down into comments, which kept every word
but made the files longer. That was the wrong trade for this project. Long rationale is cut to
its load-bearing clause instead, and the full version lives here.

**Measured numbers were never cut.** The fish at 1.33 tall and its 1.11 collider, the 3.5s and
4.3s crossing times, the 17.8-unit screen width, the 2.5s CoinSpawner cap, the 0.7 and 1.6 speed
multipliers against the rocks' 5 -- all survive, just without the prose around them. Those took
real work to establish and no compiler notices when they go.

### Four bugs, all the same shape

Each is a guard that checked less than the code after it assumed.

| Script | Fault |
|---|---|
| `CoinSpawner` | `Debug.LogError(this)` passed the object as the message. Alone among the project's eight LogError calls it had no text, so a missing coin prefab printed "CoinSpawner (CoinSpawner)" and explained nothing |
| `CoinSpawner`, `TrashSpawner`, `MyObstacleSpawner` | Each validator checked the prefab had a Sprite Renderer but not that a sprite was in it. The spawn code then reads `sprite.bounds`, so an empty renderer passed validation and threw on the first spawn. In `MyObstacleSpawner` it throws after `Instantiate`, leaking a rock every frame |
| `ScoreTrigger` | `GameManager.Instance.AddScore()` had no null check, while `CoinPickup` right next to it guards the identical call |
| `ScrollObject` | `Awake()` reads `spriteRenderer.bounds` with nothing guaranteeing a Sprite Renderer. Fixed with `[RequireComponent(typeof(SpriteRenderer))]` rather than a runtime guard, matching the spawners |

Two `void Update()` declarations, in `ObstacleMovement` and `PlayerControllerScript`, gained an
explicit `private` to match every other method in the project. No behaviour change; C# defaults
to private.

### How it was checked

Rewriting twelve files by hand is exactly where a comment pass quietly eats a line of code, so
each file was compared against the backup with comments, blank lines and tooltip text stripped
out. The remaining code had to be identical except for the fixes above, and every code-level
difference was printed and read. Alongside that: `[FormerlySerializedAs]` strings compared as a
set, no tooltip over 90, no CR reintroduced. Then `dotnet build`: 0 errors, 0 warnings.

Worth keeping as the recipe. The skeleton comparison is what makes a comment pass safe, because
it turns "I was careful" into something checkable.

## Distance counter (2026-10-05)

`Distance Score Counter` had been sitting in the scene unwired: a TextMeshProUGUI at font 90,
saved with `m_IsActive: 0` and its text set to `0`, with no script referencing it. The project
had no notion of distance at all, so that had to be built rather than just hooked up.

It now reads `Distance Count: 0` at the start of a run and counts up from there.

| Added to `GameManager` | |
|---|---|
| `distanceTextCounter` | the Inspector slot for the counter |
| `unitsPerSecond` | 5, matching the rocks' own speed |
| `Distance` | whole world units travelled this run |
| `Update()` | the accumulator; GameManager had no Update before |
| `ResetDistance()`, `ShowDistance()` | mirroring the coin pair |

Distance is `unitsPerSecond * ObstacleMovement.SpeedMultiplier * Time.deltaTime`, accumulated
each frame. Using the shared multiplier means the number tracks what actually slides past, so it
speeds up as the difficulty ramp climbs from 0.7 to 1.6 rather than ticking at a flat rate.

**No paused/playing flag was needed.** `Time.deltaTime` is already 0 whenever `timeScale` is, so
the menu, pause and game over all stop the counter for free. The text is only rewritten when the
whole-unit value changes, so it is not touched 60 times a second.

`ShowScore(bool)` became `ShowRunCounters(bool)` and now toggles both HUD counters together. The
old name would have been a lie once it handled two, and both appear and disappear at the same
moments anyway. Two call sites.

The counter is saved disabled in the scene, which is fine: `ShowRunCounters(true)` is what turns
it on when a run starts, exactly as it does for the score.

### Still to wire

`distanceTextCounter` is an empty slot until Distance Score Counter is dragged onto the
GameManager in the Inspector. `ShowDistance()` and `ShowRunCounters()` both null-check it, so the
game runs without it, just with no distance shown.

`distancePerSecond` was saved as 0 almost immediately, and not by the serialization trap this
time: the name reads as a starting value, so 0 looked like "count up from zero". It is a rate, and
at 0 the number never moves.

The field is now `unitsPerSecond`, renamed rather than reset. A saved value beats a script
default, so changing the default alone would not have helped; the rename orphans the saved 0 and
lets 5 apply with no scene edit, which also meant nothing had to be written while Unity held the
scene open. `[FormerlySerializedAs]` is deliberately absent for the same reason. `[Min(0.1f)]`
now stops 0 being entered at all, since an empty text slot is the way to turn the counter off.

Worth remembering as a general point: the serialization trap is a saved 0 beating a good default,
and the fix is always the rename. This was the same symptom from a different cause, and the same
fix worked.

## The distance counter was eating taps (2026-10-05)

Reported as the fish stuttering while swimming up and down the cave. It was not the text
rendering and not the physics: the counter was swallowing the taps.

`PlayerControllerScript.SwimPressed()` refuses to swim while
`EventSystem.current.IsPointerOverGameObject()` is true, which is correct -- a tap on Pause or
Restart must not also flap the fish. But any UI Graphic with Raycast Target ticked counts as
"pointer over UI", and every HUD text in this scene has it on. The distance counter is 1018 x 141
at the top centre of the screen, so that whole strip stopped swimming the fish. Tapping to climb
is exactly when a thumb lands there, which is why it showed up as up-and-down movement.

This was introduced by the counter itself. It had been sitting in the scene with
`m_IsActive: 0`, so its raycast target never mattered until `ShowRunCounters(true)` began
enabling it at the start of a run.

`GameManager.Awake()` now clears `raycastTarget` on all three counters it owns -- score, coins and
distance. Fixed in code rather than by unticking the box in the Inspector, so a counter added
later cannot bring it back, and in one place rather than three because the score and coin
counters had the same latent fault. They had just never been big enough to notice.

`BuildCoinIcon()` had already been doing this for the coin sprite since the day it was written,
with the comment "Never eat a tap meant for the game". The knowledge was in the project; it
simply had not been applied to the text objects.

## Two counters wired to one text (2026-10-05)

Reported as the distance text glitching while the fish swam up and down the cave. The raycast
fix above was a real bug but not this one, and the text itself was never at fault.

`GameManager` had both slots pointing at the same object:

```
scoreTextCounter     -> Distance Score Counter
distanceTextCounter  -> Distance Score Counter
```

So two writers fought over one label. `ShowDistance()` writes `Distance Count: 123` about five
times a second, and `AddScore()` writes a bare `Score.ToString()` every time a gap is cleared.
Each stamped over the other.

That is also why it tracked the fish going up and down: that is when `ScoreTrigger` fires. Every
point replaced the distance text with a plain number until the next distance tick put it back.
The symptom pointed at the text because the text was the only visible part; the fault was in the
Inspector.

Diagnosis took three wrong guesses first -- render order, layout fitters, box overlap -- because
each was checked against the scene in isolation. Dumping every `GameManager` slot with its target
object's name resolved it immediately. Worth doing that first next time a UI symptom has no
obvious cause: the wiring is data, and it can be read.

### Distance is the HUD score now

The fix was a choice, not just a rewiring: Score Text Counter is cleared and the rock count comes
off the HUD entirely. `Score` keeps counting, so nothing is lost and `ScoreTrigger` is untouched.

The rock count was the wrong number to show. This file's own rough edges already note that both
rows score, so points come at roughly twice the rate of Flappy Bird's pipes. `ScoreTrigger` sits
on all five prefabs in `Cave Walls/`, so that is live rather than theoretical. Its label had also
been abandoned in practice long before today: `Text (TMP)` is in the scene at `m_IsActive: 0` and
`sizeDelta (0, 0)` in the bottom-left corner. The duplicate wiring is how it came back.

Distance cannot double-count, and it rides `ObstacleMovement.SpeedMultiplier`, so it measures what
actually slides past and quickens with the ramp.

### Guards added

`Awake()` now reports the collision directly, rather than leaving it to look like a rendering
fault:

```
GameManager: Score Text Counter and Distance Text Counter are the same object,
so each overwrites the other.
```

`scoreTextCounter` was also the only one of the three counters dereferenced without a null check,
while `coinText` and `distanceTextCounter` both had one. It now goes through `ShowScore()` and is
guarded in `ShowRunCounters()`, so clearing the slot is a valid way to take the rock count off the
HUD rather than a crash.

## The counter was flickering because two methods wrote it (2026-10-05)

Reported as the UI text glitching while the fish swam up and down. The cause was neither
rendering nor alignment: the scene had both GameManager slots pointing at the same component.

```
scoreTextCounter:    {fileID: 678708312}   <- Distance Score Counter
distanceTextCounter: {fileID: 678708312}   <- the same component
```

`ShowDistance()` wrote `Distance Count: 143` five to eight times a second while `AddScore()`
wrote a bare `7` over it on every rock cleared. The text flipped between the two formats, and
because clearing rocks is what the fish does while swimming up and down the cave, it looked
like movement caused it.

Nothing was miswired by accident. `Distance Score Counter` is the only numeric text object in
the scene: `Text (TMP)` reads "Button", and `CurrentScore_Label`, `ScoreLabel` and
`BestScore_Label` are inactive headers. There was nothing else for the score to point at.

`Coin Counter` never had this problem because it owns one object with one writer, `ShowCoins()`.
That is the rule the HUD now follows throughout.

### What changed

The `distanceTextCounter` field is gone and `ShowDistance()` with it. `scoreTextCounter` stays,
already wired to the same component, and one writer composes the whole line:

```csharp
scoreTextCounter.text = "Score: " + Score + "    Distance Count: " + Distance;
```

`AddScore()`, `ResetScore()`, `ResetDistance()` and `Update()` all route through it, so there is
exactly one `scoreTextCounter.text` assignment in the file. The `if (whole == Distance) return;`
guard in `Update()` still means the text is only rebuilt when the number actually changes.

Keeping `scoreTextCounter` rather than `distanceTextCounter` was deliberate: both were wired to
the same object, so either name would have survived, and the one with more call sites meant the
smaller diff and no Inspector work. The scene's `distanceTextCounter` entry is now an orphan and
Unity drops it on the next save, like `graceSeconds` and `distancePerSecond` before it.

`scoreTextCounter` is also null-guarded now, in the writer and in `ShowRunCounters()`. It was the
only one of the three counters that would have thrown on an empty slot.

### The general rule

One text object, one writer. Two Inspector slots that can be filled with the same object are a
trap, and this scene has few enough text objects that it was the likely outcome rather than a
careless one. Where a second value needs showing, compose it into the one string.

### Then the score came off it again (2026-10-05)

Showing both read as `Score: 2    Distance Count: 18`, which wrapped onto two lines in the
1018-wide box, and the rock score duplicated what the coin counter already does. The line is now
`Distance Count: 18` alone, which fits on one line with room to spare.

`Score` still counts in code, so a high-score screen can use it later, but nothing displays it:
`AddScore()` no longer touches the text and `ResetScore()` no longer paints. `ShowRunScore()` is
`ShowDistance()` again, called only from `Update()` and `ResetDistance()` -- the two places the
distance actually changes.

The field stays `scoreTextCounter` even though it now carries the distance. Renaming it would
empty the Inspector slot, and this scene has only the one numeric text object to put back in it.
A comment on the field says so, because the name is otherwise misleading.

## The coin counter joins the run HUD (2026-10-05)

The two counters disagreed about when they existed. `Coin Counter` is saved with
`m_IsActive: 1` and nothing in code ever toggled it, so it sat on the main menu and the game
over screen as well as during play. `Distance Score Counter` is saved disabled and only
`ShowRunCounters()` turned it on, so it appeared with the run.

`ShowRunCounters()` now sets both, with the same `foreach` shape already used for the raycast
pass in `Awake()`. Both counters come up when PLAY is pressed and go away on the menu.

Game over deliberately leaves them up: `ShowRunCounters(false)` is only called from
`BackToMenu()`, so the final distance and coin count stay readable on the end screen.

The saved scene states no longer matter either way, since `Start()` runs `BackToMenu()` and that
settles both counters before anything is shown.

## HUD polish, a blur that did not survive, and the lead-in (2026-10-07)

### Coin counter

The `": "` in front of the number is gone; `ShowCoins()` writes `Coins.ToString()` and nothing
else. The gap from the icon never came from those characters anyway, it comes from the left
margin `BuildCoinIcon()` sets aside. Widen `coinIconTextGap` if the number sits too close.

The number was also top-aligned while the icon is centred, so it rode above the coin. The icon
anchors at pivot 0.5 in code, so `BuildCoinIcon()` now sets
`coinText.verticalAlignment = VerticalAlignmentOptions.Middle` to match. Done beside the icon
because that method already owns the text's margin: the icon's placement and the text's
alignment are one concern, not two.

### A background blur, added and removed the same night

The ask was for the paused and game over background to blur behind the dim. The 2D renderer
rules out Depth of Field, since sprites do not write the depth it works from, and everything
else in URP means a blur shader plus a renderer feature plus an asset to maintain.

What made it cheap: `timeScale` is 0 whenever that dim is up, so one still frame is
indistinguishable from a live blur. The implementation rendered the camera into a RenderTexture
at 1/8 size and stretched it back up under the dim, which is a blur for free because the detail
is simply not there. Roughly 55 lines, no shader, no new assets.

Two things came of it. The RenderTexture was built with 0 depth bits, and Unity 6's Render Graph
refuses to render a camera into a target whose Depth Stencil Format is None -- it warned on every
pause until the constructor asked for 24. Then the real verdict: a low-res stretch is blocky, not
soft, and what was actually wanted was a sharp background that is simply darker.

So all of it came out again. The file is back to where it started and `pauseBlur` appears nowhere.
Recorded because the reasoning is worth keeping even though the code is not: if a blur is ever
wanted properly, it is a two-pass gaussian in a `FullScreenPassRendererFeature`, and the
still-frame trick still applies.

### The dim went darker

`pauseDimAlpha` 0.6 became `pauseDimDarkness` 0.78. Renamed rather than retuned because Unity was
holding the scene open, so editing `SampleScene.unity` would have been overwritten on its next
save, and a saved value beats a script default. The rename orphans the old entry and lets the new
default through -- the same mechanism as `graceSeconds` and `distancePerSecond`.

That is the third time a rename has stood in for a value change, and it should be the last. It
works, but churning a field name to change a number does not scale. Now that 0.78 is saved, the
slider is the way to tune it.

### The lead-in is gone

`leadInSeconds` and the `elapsed` timer that served it are deleted, so the cave starts filling the
instant PLAY is pressed. The scene already had it at 0, so nothing changes at runtime; this just
removes the machinery.

`elapsed` existed for nothing else, and with it gone `Time.deltaTime` is used zero times in
`MyObstacleSpawner` -- the cave is position-driven off `CameraRightEdge()`, never time-driven. The
breathing room is now entirely the swim in from the right edge: about 3.5s at 16:9, 4.3s on a
19.5:9 phone.

Worth noting the arc. A timed grace existed as `graceSeconds`, was removed, came back as
`leadInSeconds`, and has now gone again. Three attempts at the same idea. The swim-in distance was
always doing the job on its own.

## Game over screen, code side (2026-10-07)

The mockup in `Assets/Graphics/GameOverScreen Ref/image.png` asks for a summary screen: background
art, the GAME OVER banner, this run's distance as `SCORE: 23m`, a persisted `BEST SCORE: 98m`, and
the coins collected.

Built as a **panel in the existing scene, not a second scene.** `MainMenu` is already exactly that
shape -- a RectTransform with three children, toggled by `SetActive()`. A separate scene would
need additive loading purely to keep the frozen game visible behind the dim, plus a static handoff
for `Coins`, `Distance` and `Score`, since Unity cannot serialize references across scenes. All of
that to replace a `SetActive`.

Split: the panel is composed in the Editor and driven from code, the same division as MainMenu.
Unity was holding the scene open, so writing objects into `SampleScene.unity` was off the table
anyway.

### Best score finally exists

`HighScore` had been a public property since the early days with nothing ever assigning it. It now
loads from `BEST_KEY` in `Awake()` beside the coin wallet, and `GameOver()` banks a new best before
the `PlayerPrefs.Save()` that was already there.

That save call matters more than it looks: `SetInt` only writes to memory, and a phone can be
killed without a clean quit.

### What the code adds

| | |
|---|---|
| `gameOverMenu` | the panel, shown on death and hidden by Restart and Home |
| `gameOverScoreText`, `gameOverBestText`, `gameOverCoinText` | the three rows |
| `BEST_KEY` | `"BestDistance"`, the project's second PlayerPrefs key |
| `ShowGameOver()` | one writer filling all three |

Every slot is null-guarded, so the game runs with the panel half-composed or not built at all.
`ShowGameOver()` writes each text exactly once, following `ShowDistance()` and `ShowCoins()` --
two methods writing one TMP_Text is what caused the counter flicker recorded above.

The coin figure is this run's `Coins`, not the lifetime `TotalCoins`. One word if that is wrong.

### The buttons do not map to the mockup

The mockup puts HOME and RESTART inside the game over panel. They cannot live there: the pause
screen uses the same two buttons plus Resume, so moving them in would mean a second pair for
pause. They stay where they are and the panel is composed around them. `ShowPauseDim()` raises
`gameOverMenu` above the dim alongside them, since UI draws in sibling order and anything not
raised reads as greyed out.

### Still to compose in the Editor

A `GameOverMenu` object under `Canvas`, holding `BG_2.jpg`, the GAME OVER banner, three text rows
and a `Frame 15.png` coin icon. Then the four slots on the GameManager. The inactive
`CurrentScore_Label`, `ScoreLabel` and `BestScore_Label` objects were pre-authored for this and
can be reused or deleted -- two of the three read "Best Score:", so one was miscopied.

## Game over panel authored into the scene (2026-10-07)

The panel objects were written straight into `SampleScene.unity`, with Unity closed. There is no
Editor-side automation here -- the `unity-mcp` server connects but exposes no tools that can
create GameObjects -- so the choice was authoring YAML or doing it by hand in the Inspector.

Built under the `GameOverMenu` object, which had been added by hand as an empty 100x100 box in the
bottom-right corner:

| Object | |
|---|---|
| `GameOverMenu` | stretched to fill the canvas, saved **inactive** |
| `BG` | `BG_2.jpg`, full stretch, raycast off |
| `CoinIcon` | `Frame 15.png` at -620, 115, preserve aspect |
| `CoinValue`, `ScoreValue`, `BestValue` | empty TMP rows at y=115, left-aligned, size 60, raycast off |
| `GameOver Text` | reparented out of the Canvas into the panel, moved to 0, 288 |

All four GameManager slots are wired, so there is nothing left to drag.

### How it was built

Nothing was written from scratch. A TextMeshProUGUI block carries dozens of serialized fields and
hand-writing one invites silent breakage, so the existing `GameOver Text` was used as the template
for all three rows and an existing button Image as the template for `BG` and `CoinIcon`: clone the
GameObject, RectTransform, CanvasRenderer and component quad, issue fresh fileIDs, repoint
`m_GameObject` and `m_Father`, then override only the handful of fields that differ.

Checked afterwards rather than assumed: every fileID in the scene is unique, each wired slot
resolves to the component type and object it should, both sprite GUIDs match the assets, and
`GameOver Text` no longer appears among the Canvas's direct children.

Positions come from the mockup's proportions scaled to the 1920x1080 reference resolution, so they
are arithmetic rather than composition. Expect to nudge them by eye -- the same caveat the Backups
table carries for the main menu's buttons.

### It did not load the first time: a stranded `[]`

Unity refused the scene with "Transform child can't be loaded". The cause was one line:

```
  - {fileID: 531923360} []
```

`GameOverMenu` had been saved with `m_Children: []`, an empty list written inline on the key's own
line. The regex that inserted the new children matched the `m_Children:` key and the child entries
after it, of which there were none -- so it appended the list and left the `[]` sitting at the end
of the last child line.

Worth remembering because of how it hid: every structural check passed. Parent and child agreed
both ways, every component was owned by the object listing it, no fileID was duplicated, no block
header was malformed. The graph was correct; one line of it was simply not parseable, and nothing
that reasons about the graph can see that. Reading the actual block is what found it.

The sweep for other stranded inline lists then false-positived on every normal pair of consecutive
child lines, because the pattern used `\s+` where it needed whitespace-except-newline. That one
failed safe -- it aborted before writing -- but it is the same class of mistake twice in a row.

### The ordering bug this surfaced

`ShowPauseDim()` raised `gameOverText` before `gameOverMenu`, which would have put the panel's
background on top of the GAME OVER text. Fixed before the panel existed to expose it: the panel now
goes up first, then the text, then the buttons, so the final draw order is dim, panel, text,
buttons.

## The canvas stopped mattering to the code (2026-10-07)

Grouping the flat Canvas into panels would have broken two things, both of which assumed every UI
element was a direct child of the Canvas.

**The dim was parented to a button's parent.** `ShowPauseDim()` did
`pauseDim.transform.SetParent(pauseButton.transform.parent, false)`. That was the Canvas only for
as long as nothing was grouped; move `PauseButton` into a `HUD` object and the dim gets built
inside `HUD`, where it cannot cover the other groups. It now parents to `CanvasRoot()`, which walks
up with `GetComponentInParent<Canvas>()`.

**Raising used six flat `SetAsLastSibling()` calls.** That only reorders an object among its own
siblings, so a button nested in a group would move within the group and the whole group would stay
under the dim. `RaiseAboveDim()` now walks up to whichever top-level object under the Canvas
contains the target, and raises that instead.

Neither was wrong when written. Both encoded "everything is a direct child of the Canvas", which
was true until it wasn't.

### Intended shape

```
Canvas
  HUD           (full-screen stretch)  Distance Score Counter, Coin Counter, PauseButton
  MainMenu
  GameOverMenu
  RunButtons    (full-screen stretch)  Restart Button, HomeButton, ResumeButton
```

`RunButtons` is its own group rather than part of either menu, because pause and game over share
those three buttons. Containers must be full-screen stretches or the children's own anchors stop
meaning what they say: a counter anchored to `(0.5, 1)` is only at the top of the screen if its
parent fills the screen.

As of this entry `MainMenu` and `GameOverMenu` exist; `HUD` and `RunButtons` do not. **Both were
added later the same day — see *The canvas grouped* below.**

### Dead objects removed

`ScoreLabel`, `CurrentScore_Label` and `BestScore_Label` are gone -- pre-authored for a score
screen that was built differently in the end. `Text (TMP)` stays: it looked like a fourth orphan
but is the label child of `Restart Button`, and deleting it would have stripped that button's text.

### Seven editors at once

"Failed to create CoreCLR, HRESULT: 0x80004005" turned out to be seven `Unity.exe` processes and
three crash handlers running together, each trying to spawn the ILPP runner that serves a gRPC
endpoint. Not a script fault: the project compiled clean throughout and `dotnet --version` was
healthy. Closing every editor and reopening one cleared it.

Worth knowing the shape of it, because the error names the runtime rather than the cause. If it
returns, count the Unity processes before touching anything else, then delete `Library/Bee` and
`Library/ScriptAssemblies`, which are generated and safe to lose.

## The canvas grouped (2026-10-07)

The two containers the previous entry called for now exist, so the Canvas matches its intended
shape:

```
Canvas
  HUD           Distance Score Counter, Coin Counter, PauseButton
  MainMenu
  GameOverMenu
  RunButtons    Restart Button, HomeButton, ResumeButton
```

Both are full-screen stretches — anchors 0,0 to 1,1, zero offsets, pivot 0.5 — and nothing else
changed. Every child kept its own anchors, `anchoredPosition` and `sizeDelta` byte for byte, which
is the whole point of the stretch: a counter anchored to `(0.5, 1)` is at the top of the screen
only while its parent fills the screen. Both containers are saved active; the children carry their
own visibility, and `GameManager` toggles those as it always did.

`RunButtons` is last under the Canvas so the three shared buttons draw above both menus, which is
the order `RaiseAboveDim()` already expects. Nothing in code needed touching: the previous entry
had already taught the dim and the raising to walk the hierarchy rather than assume a flat Canvas.

One thing the grouping makes moot. `ShowPauseDim()` raises `gameOverMenu` and then
`gameOverText.gameObject`, with a comment explaining that the panel must go up first or its
background would cover the text. `GameOver Text` is now a child of the panel, so `RaiseAboveDim()`
walks both calls up to the same object and the second is a no-op. Harmless, and still correct if
that text is ever pulled back out.

### Done on disk, with Unity closed

Same approach as the game over panel: no Editor automation exists for this — `unity-mcp` connects
and exposes nothing that can create a GameObject — so it was YAML. Two GameObject blocks and two
RectTransform blocks appended, the Canvas's `m_Children` rewritten to the four containers, and
`m_Father` repointed on the six moved transforms.

What was done differently, because of the stranded `[]`: the edit works on parsed documents, not
on regex over the whole file. Split on the `--- !u!N &id` markers, rewrite the specific blocks by
line, reassemble. Both of that night's mistakes were regexes reaching across lines they had no
business touching, and document-level surgery cannot make either of them.

It also verifies before writing rather than after, and aborts rather than saving a broken scene:
fileIDs unique, every `m_Children` entry ending in `}` (the `[]` check, which is the one thing
structural reasoning cannot see), parent and child agreeing both ways, and **every** transform in
the scene listed by the father it names — not just the ones being moved. It refuses outright if
the Canvas's children are not the eight it was written against, so a stale script cannot run
against a moved-on scene.

Checked afterwards: all fifteen wired `GameManager` slots still resolve to the right component on
the right object, and all five live `OnClick` targets still reach `GameManager`. None of them point
at a transform, which is why reparenting could not have broken them — but the previous entry's
lesson was that every structural check can pass while the scene is still wrong, so it was checked
rather than reasoned about.

### Positions were left alone

The mockup this would have been composed against, `Assets/Graphics/GameOverScreen Ref/image.png`,
is no longer in the project — that folder now holds only `BG_2.jpg`, `Frame 15.png`, `Frame 16.png`
and `Frame 9.png`. The game over rows still sit where the arithmetic put them, with `CoinIcon`
nudged to -733 and `GameOver Text` to y 318 since. Nudging the rest by eye needs the reference
back.

`GameOverMenu` is also saved **active**, where the entry above records it as inactive. It makes no
difference at runtime, since `Start()` calls `BackToMenu()` which switches it off, but it covers
the Scene view while composing anything behind it.

## The game over screen gets its own Restart and Home (2026-10-07)

One pair of buttons served both pause and game over, so composing them for one screen moved them
on the other. The entry above recorded that as the reason the mockup could not be followed. The
call now is the other way round: the panel gets its own pair, and the cost the entry named — a
second pair — is accepted.

No new abstraction for it, because the project already does this. `MainMenu` owns `Menu Play
Button` and `CharactersButton` outright; the game over panel owning its own Restart and Home is
the same shape. That also means **no new `GameManager` slots**. The panel is already shown and
hidden as a whole by `SetActive()`, and both `StartGame()` and `BackToMenu()` already switch it
off, so buttons nested inside it appear and disappear for free. They need their `OnClick` wired in
the Editor and nothing else.

### The code half, which is four lines

`GameOver()` called `ShowRunButtons(true)`, which raised the shared pair *and* the dim. It now
holds the pair down and raises the dim on its own:

```csharp
ShowRunButtons(false);
ShowPauseDim(true);
```

Splitting the dim out is the whole change. `ShowRunButtons(false)` is strictly redundant — the
pause buttons are already down during a live run, since `StartGame()` put them there and
`TogglePause()` refuses once `isGameOver` — but it states the intent where a reader of `GameOver()`
will ask the question.

`ShowRunButtons` is a pause-only method now, and its comment and the two button tooltips say so.

### What this does not change

`RaiseAboveDim()` still walks each target up to its top-level container, so the new buttons ride
up with the panel they sit in and land above the dim without being named. The pause pair is raised
too, while inactive, which is only sibling order and draws nothing.

### Composed in the Editor, same day

Both copies are in `GameOverMenu`, active, carrying Image, Button and AspectRatioFitter, and wired
to `StartGame()` and `BackToMenu()`. The `restartButton`, `homeButton` and `resumeButton` slots
still point at the originals, which is what keeps pause working. `RunButtons` is now
`Pause Buttons`.

The duplicate is named `HomeButton ` with a trailing space. Nothing reads it by name, so it is
cosmetic only.

The new buttons serialize with Width 0 because `AspectRatioFitter` is on `HeightControlsWidth` and
Unity does not serialize driven properties. Height is the knob; the width in the Inspector is
greyed out and recomputed at 1.7931.

### A container saved inactive takes the whole screen down

`Pause Buttons` came back from the Editor pass with `m_IsActive: 0`, which soft-locks pause.
`ShowRunButtons()` calls `SetActive` on the three children, but a child of an inactive parent never
renders, so all three calls silently do nothing. Pause then shows the dim and no buttons, and
`PauseButton` is deliberately under the dim with its taps swallowed — there is no way back out.

Worth keeping because of the shape of it. `HUD` and `Pause Buttons` are the only two containers no
code ever calls `SetActive` on; they are assumed on forever. Toggling either off while composing
breaks the game with nothing in the Console, and the code cannot tell — `SetActive(true)` on a
child reports no error. The fix is the checkbox, not a guard: adding one would be defending against
scene authoring in every method that shows a button.

### And the mirror of it, an hour later

Ticking the container then made its Restart and Home draw on top of the main menu in the Editor,
overlapping CHARACTERS and PLAY. Not a runtime bug: `BackToMenu()` calls `ShowRunButtons(false)`
and `mainMenu.SetActive(true)` in the same method and `StartGame()` turns the menu off, so menu
visible *and* pause buttons visible is a state no code path can reach. The Editor was drawing the
scene as saved, with Play not running.

Both children had always been saved active. The container being off was masking them, so turning
it on did not break anything -- it revealed something already wrong.

The rule the two bugs share, worth stating once:

| Container | Code toggles | Container saved | Children saved |
|---|---|---|---|
| `MainMenu`, `GameOverMenu` | the container | free | on, so they show with the panel |
| `HUD`, `Pause Buttons` | the children | **must be on** | **off** |

`HUD` already had all three children saved off, which is why it never misbehaved. `Pause Buttons`
was the only container breaking the rule, and it produced both symptoms: invisible buttons when
the container was off, stray buttons when it was on. Fixed by unticking the two children and
leaving the container ticked.

That makes two bugs in a row from the same family — the stranded `[]` and this — where the scene is
structurally fine and one serialized value makes it wrong.

---

## Game over layout, and the top-aligned rows behind it (2026-10-07)

Composed against the mockup. Four of the changes are layout; one is a bug the layout work
exposed, and one is a bug left over from the session before.

**The rows were Top-aligned.** `CoinValue`, `ScoreValue` and `BestScore` all sat
`V=Top` in a 120-tall box, so the text rode about 30 units above the box centre. `CoinIcon` had
been pushed to y 145 chasing it. That is the same bug the HUD coin counter already had and the
same fix: all three are `Middle` now and the icon is back level with the number at y 130.

Fixed on all three rows rather than just the coin. The ticket was the coin, but the other two
share the row and the misalignment, so patching only the named one leaves two siblings wrong.

**HOME and RESTART were the wrong way round** against the mockup, and neither level nor
symmetric -- y 6.8 apart, mid-point 29 off centre, heights 3 apart. Now mirrored at x -422 and
422, both y -228, both 269.07 tall. The separation they were already tuned to (844) was kept;
the mockup works out at about 868, which is inside nudging distance.

**`Pause Buttons` was still saved inactive** from the previous entry, so pause was still
soft-locked. On now.

**`HomeButton ` renamed.** Worth recording how the trailing space hid: printed through a
`%-16s` column it is invisible, and an earlier pass in this session read it back as already
fixed. `cat -A` on the raw line is what settled it -- the scene stores it as
`m_Name: 'HomeButton '`, since Unity single-quotes any name with leading or trailing space.
A name read out of this file has to be unquoted before it is compared.

### Measured, not eyeballed

The mockup and a screenshot of the build were compared by pixel, in ratios that survive the
crop, since the two images are different sizes and absolute pixels would have lied:

| | build | mockup |
|---|---|---|
| coin to number | 0.185 icon-widths | 0.25 |
| coin to SCORE | 7.5 | 4.44 |
| button separation | 902 units | about 868 |

That is what redirected the work. The coin-to-number gap was already within about 6 units of
target, so the thing that actually read as wrong was never the gap -- it was the row being
strung out, and the vertical misalignment above.

### Two things deliberately not done

**Pause still reads RESTART-left, HOME-right**, which is now mirrored against the game over
screen. Same two actions, opposite sides, on screens seconds apart. It is a real
tap-the-wrong-one risk and the fix is three coordinates, but pause was not in the ask and it
was composed by eye.

**The dim does nothing on the game over screen.** `GameOverMenu/Background` is a full-screen
image at alpha 1 and white tint, drawn above the dim, so `pauseDimDarkness` has no effect
there at all. The mockup is visibly darker than the build: that is this Image's colour tint,
not the dim slider. Recorded because the slider is the obvious place to look and it is the
wrong one.

Also measured: the mockup's coin number is gold, RGB (223, 170, 39), matching the coin, where
the build renders it cream like SCORE and BEST SCORE.

---

## The pause blur, second attempt (2026-10-07)

Asked for again after the entry above removed it. Built the way that entry said to build it if it
ever came back -- except for the shader, which turned out not to be needed.

The still-frame trick stands: `timeScale` is 0 the whole time the dim is up, so one captured
frame is indistinguishable from a live blur and costs nothing per frame. `CapturePauseBlur()`
renders `Camera.main` into a RenderTexture once, when the dim goes up.

**What is different is the downsampling.** The first attempt dropped straight to 1/8 and stretched
that back up, which keeps every 8th pixel exactly as it was -- that is why it read as blocky
rather than soft. This one halves the frame `pauseBlurSteps` times, and every halving is a 2x2
bilinear average, so detail is actually blended away rather than thrown away. A chain of box
blurs converges on a gaussian, which is the shape the eye reads as soft.

So the upgrade path the old entry recorded -- a two-pass gaussian in a
`FullScreenPassRendererFeature` -- was not needed. That would have meant a shader asset, an edit
to the URP renderer asset, and a thing to maintain. Repeated `Graphics.Blit` gets there with no
shader, no renderer feature and no new assets, because bilinear filtering is already doing the
averaging.

Kept from last time: the RenderTexture is built with **24 depth bits**, not 0. Unity 6's Render
Graph refuses to render a camera into a target whose Depth Stencil Format is None and warns on
every pause. Nothing reads the depth; it only has to exist.

### Why the capture cannot catch the UI

The Canvas is **Screen Space - Overlay**, so UI is composited after the camera and never lands in
a camera capture. That means the dim and buttons cannot blur themselves, and the capture needs no
careful ordering against `SetActive`. Checked rather than assumed -- on Screen Space - Camera this
would have needed the dim hidden before the grab.

### Two things to know

`pauseBlurSteps` is a **new serialized field**, so the scene may well write `default(int)` -- 0 --
instead of the C# initializer of 4. At 0 there is no blur at all and it looks like the feature
never worked. Check the slider in the Inspector before concluding anything, the same trap as
`CoinSpawner.wallGap`.

**The blur does not show on the game over screen**, even though the dim is raised there too.
`GameOverMenu/Background` is a full-screen image at alpha 1 drawn above the dim, so it covers the
blur completely. Pause is the only place it is visible. Dropping that image's alpha is what would
expose it.

### Not verified at runtime

`cam.Render()` is the call that fills the capture, and this was written without Unity running. The
previous attempt used the same approach in this project and the only complaint it drew was the
depth-bits one, so the path is known to work here. If the blur comes out black or the Console
objects, the Unity 6 replacement is `RenderPipeline.SubmitRenderRequest` with a
`UniversalRenderPipeline.SingleCameraRequest`.

---

## The stat row became a layout group (2026-10-07)

The coins, score and best score were three independently positioned objects, so the gaps between
them changed with the text. `SCORE: 8m` and `SCORE: 410m` are different widths, which means no set
of typed coordinates can keep the spacing even -- the layout has to be computed from the text, not
from numbers written once.

That is what `HorizontalLayoutGroup` is for, and it is already in the project; uGUI ships with
Unity. No code, no new dependency.

```
GameOverMenu
  Divider        thin Image
  StatRow        HorizontalLayoutGroup, centred, spacing 90
    CoinGroup    HorizontalLayoutGroup, spacing 22
      CoinIcon   + LayoutElement 90x90
      CoinValue
    ScoreValue
    BestScore
  GameOver Text
  Restart Button
  HomeButton
```

**Equal gaps and equal edge padding are one setting, not two.** `m_ChildAlignment: 4`
(MiddleCenter) with `m_ChildForceExpandWidth: 0` packs the three at a fixed spacing and splits
whatever is left evenly between the two edges. Expanding the children instead would have widened
each cell rather than the gaps, and since the text is left-aligned inside its cell the gaps would
have come out uneven.

**No `ContentSizeFitter` anywhere.** `TMP_Text` already implements `ILayoutElement` and reports its
own preferred width, so `m_ChildControlWidth: 1` is enough and the row reflows whenever
`ShowGameOver()` writes new text.

**`CoinIcon` needed a `LayoutElement`.** An `Image` reports no preferred size at all, so inside a
layout group it collapses to zero width. The 90x90 is on the LayoutElement now, not the
RectTransform, because the RectTransform is driven.

`CoinGroup` exists so the icon and number travel as one item in the outer row. It needs no size
fitter either: a layout group is itself an `ILayoutElement` and reports the width of its contents.

### It also fixed the order

The number was rendering to the *left* of the coin -- measured off a screenshot, text at x81..98
and the coin at x106..137, the reverse of the mockup. Sibling order inside `CoinGroup` settles
that, so it is now structural rather than a coordinate that can drift.

### Writing layout components into YAML

The component GUIDs were read off
`Library/PackageCache/com.unity.ugui@*/Runtime/UGUI/UI/Core/Layout/*.cs.meta` rather than
remembered, because a wrong GUID is a missing script -- silent, and exactly the breakage this file
keeps recording. Verified afterwards by resolving every component in the panel back through those
metas: all of them named a real class, none came back unknown.

The divider's `CanvasRenderer` and `Image` are clones of `CoinIcon`'s with the sprite cleared and
the colour set to the row's cream, following the rule from the panel-authoring entry: never
hand-write a component block when one can be copied.

All fourteen `GameManager` slots still resolve. Reparenting does not change fileIDs, so the three
text slots kept pointing at the same components.

### Knobs

`StatRow` spacing 90 and `CoinGroup` spacing 22 are the two to tune. The divider is at y 215,
3 units thick, inset 240 each side, cream at 45% alpha -- all by eye and all guesses.

Equal edge padding here comes from centring, not from a padding value. If the row should instead
push out to the edges with a fixed margin -- CSS `space-between` -- that needs two empty children
with `m_FlexibleWidth: 1` between the items.

---

## The divider got a taper (2026-10-08)

Asked for a line that is nothing at both ends and thickest in the middle. A uGUI `Image` draws a
flat quad, and nothing in uGUI gives a shape that varies along its length -- `m_FillMethod` cuts a
quad down, it does not taper one.

Three ways to get the shape: a shader, a mesh effect, or art. Art is the only one with nothing to
maintain afterwards, so `Assets/Graphics/Updated UI/DividerTaper.png` is generated rather than
drawn: 512x16, white, with alpha shaped by

```
halfThickness(x) = (H/2 - 0.5) * sin(pi * x / (W-1))
alpha(x, y)      = clamp(halfThickness(x) - |y - centre| + 0.5, 0, 1)
```

`sin` is 0 at both ends and 1 dead centre, which is the lens shape; the `+0.5` feathers the edge
so it antialiases instead of stepping.

Two things fall out of that construction. The texture is **white**, so the Image's existing colour
tint still decides the colour -- the cream at 45% alpha already on the Divider carries over
untouched. And the taper reaches the full height of the texture at its centre, so **the rect's
Height is literally the thickness at the middle** and the ends are always 0. One knob, and it
means what it says.

Kept at 16px tall rather than 64: the rect is a handful of units high, mipmaps are off on UI
sprites, and a big vertical downscale would alias.

No code and no new serialized field, which also dodges the trap where a newly added field saves as
`default` -- the one `pauseBlurSteps` is sitting in.

### Unity imported it as Multiple

The generated PNG came in with `spriteMode: 2`. A Multiple sprite has no single main Sprite
object, so it cannot be assigned to a UI `Image` at all -- the same wall the anglerfish PNG hit
back in *Look and feel*, which is the second time this project has paid for it. Anything imported
here for UI needs `spriteMode: 1` checked before it will drop into a Source Image slot.

Fixed in the `.meta` with Unity closed, one line. The stale `internalIDToNameTable` and the empty
outer `spriteID` were left alone: `Frame 23-1` carries exactly the same leftovers from its own
Multiple days and works fine, and Unity fills the spriteID in on reimport.

The sprite is referenced at `fileID: 21300000`, which is the main-Sprite id for a Single-mode
texture -- checked rather than assumed, by counting what the scene already uses: twelve sprites at
21300000 and one sub-sprite id belonging to a Multiple.

### Applied

`StatRow` spacing 90 -> 160, `Divider` height 3 -> 8, Divider sprite -> `DividerTaper`, and the
meta's spriteMode. Four lines across two files.

---

## Sound toggle, and a darker cave (2026-10-08)

Two buttons from `Graphics/Music UI.Buttons`, one on the main menu and one on pause, both showing
and setting the same mute state. **The game still has no audio at all** -- these are placed and
wired now so the control exists when sound arrives.

That is what decided the implementation. `AudioListener.volume` is the one global tap: it silences
every `AudioSource` at once, *including ones that do not exist yet*. So nothing has to be wired per
clip later and no `AudioMixer` has to be created for the toggle to keep working. A mixer group
would have been the other option and would have needed an asset, a route for every source, and a
reason to exist.

One `Muted` flag sits behind both buttons, which is what stops the menus disagreeing, and it lives
in `PlayerPrefs` under `"Muted"` -- the project's third key -- so the choice survives the app being
killed. `ApplySound()` sets the sprite on **both** buttons regardless of which menu is up: a hidden
button still has to be correct by the time its menu is shown.

### The pause button had to join the toggle list

`Pause Buttons` is always active and `ShowRunButtons()` switches its *children* on and off, so a
button dropped into that group with nothing toggling it would have sat on screen through the entire
run. It is switched with the other three now.

This is the same container rule from the entry above, seen from the other side: in a group whose
children are toggled, anything added must be toggled too, or it is simply always visible.

### Multiple again

Both sprites imported as `spriteMode: 2`, so neither could be dropped into an Image. That is the
**third** time -- the anglerfish, `DividerTaper`, and now these two. It is worth treating as the
default rather than the exception: anything imported here for UI needs Single before it will go
into a Source Image slot.

### Unity had already written the new fields

By the time the scene was patched, `menuSoundButton`, `pauseSoundButton`, `soundOnSprite` and
`soundOffSprite` were already in `SampleScene.unity` as `{fileID: 0}` -- Unity serialises new
fields the moment it reloads the script. Appending them again would have produced duplicate keys,
so the patch fills the slots in place and refuses outright if one is already pointing at something,
rather than quietly overwriting a reference.

Also checked while in there: `pauseBlurSteps` came through as **5**, not 0, so the
new-field-saves-as-default trap did not bite this time.

### Darker cave

`Global Light 2D` intensity 1 -> **0.35**. Nothing else about the lighting touched, deliberately:
the fish is going to be lit by the particle system coming from the teammate's copy, so the global
light only has to stop flattening everything.

---

## The cave darkens with depth (2026-10-09)

The global light was a flat authored value. It now runs from `startLight` to `deepLight` across
`darkestAtDistance` metres, so the cave closes in as the run goes on.

This is the one case where `GameManager` should own a value that also exists on a component. The
general rule, written up in the entry below, is that a number belongs either in the Inspector or
in code and never both -- but that rule is about *static* values. This one has to change during
the run, so the component cannot author it. `Start Light` is the number to tune now; the Light
2D's own Intensity only decides what the Editor preview looks like, and is overwritten the moment
a run starts.

### Where it hooks in

`Distance` changes in exactly two places, `Update()` and `ResetDistance()`, and both already
called `ShowDistance()`. `ApplyCaveDepth()` is called beside it at both, rather than from inside
it: `ShowDistance()` stays the only thing that writes the text, which is the rule that came out of
the counter flicker.

Driven off the whole-metre `Distance` rather than the float behind it, so it only recomputes when
the counter ticks instead of every frame. 400 steps across the fade is far finer than the eye
reads, and `Update()` already early-returns on the unchanged case, so the gating was free.

`ResetDistance()` is what puts the light back, and both `StartGame()` and `BackToMenu()` route
through it, so a new run and the menu are lit identically without either having to know about the
light.

### Two edges closed rather than left

`[Min(1f)]` on `darkestAtDistance` is what stops the divide by zero in `ApplyCaveDepth()`, and
`Mathf.Clamp01` means a run past that distance holds at `deepLight` instead of overshooting into
negative intensity. Neither is hypothetical: 0 is exactly what a new serialized field can be saved
as in this project.

### Naming, corrected before it set

These were first called `surfaceLight` and `abyssLight`, which reads as a dive from daylight.
There is no surface in this game -- the fish lives in the cave and the run only ever goes further
in. Renamed to `startLight` and `deepLight` while the fields had not yet been serialized, so the
rename cost nothing: no orphaned entries, no Inspector trap, which is the cheap moment to fix a
name and the only one.

### Still to wire

`Cave Light` is an empty slot. Drag `Global Light 2D` onto it, then **check the three numbers read
0.35, 0.08 and 400** -- a newly serialized field can come through as `default(T)`, and `startLight`
at 0 means a black screen from the first metre.

---

## The HUD ghosted through the pause blur (2026-10-09)

At `pauseBlurSteps` 5 or 6 the distance counter showed as a dark ghost over the blurred
background. At 2 or 3 it did not.

**The blur was never opaque.** The camera clears to `a: 0` with SolidColor, so the captured frame
carries alpha 0 everywhere nothing opaque drew, and `RawImage` multiplies by that alpha. Each
halving then averages the 0 regions outwards into the 1 regions, so the more steps, the more
transparent the whole image becomes. That is the entire reason the step count mattered: at 2 steps
most pixels still held alpha near 1 and it passed for solid.

What showed through was the HUD, which sits below the blur in sibling order and is never raised
above the dim -- deliberately, that is what makes the score and Pause untappable while paused.

**Why the game itself did not visibly bleed through:** `timeScale` is 0, so the live frame behind
the blur is pixel-identical to the capture. Only the UI, which is Screen Space - Overlay and
therefore never in a camera capture at all, had anything different to show.

The fix is one line of intent: the owned texture is `RGB565`, which has no alpha channel and so
samples as fully opaque. Dropping the channel beats patching the symptom -- hiding the counter
during pause would have left every other overlay element free to ghost the same way. Colour is
unaffected, since nothing here premultiplies, and 16 bits is past caring for a blurred backdrop
under a 78% black dim. `SystemInfo.SupportsRenderTextureFormat` falls back to the default rather
than assuming.

Worth keeping because of how it presented. The report was "text appears at high blur", which reads
like a blur bug. The blur was fine; the alpha channel was the bug, and the step count was only the
thing that made it visible.

---

## The lure light, and the wrong slot behind it (2026-10-09)

A Spot Light 2D was added as a child of `Player_Fish` with a `SpotLight2D` script, to flare when
the player taps. Three things were wrong with it, only one of which was the one reported.

### Cave Light was pointing at the lure

`caveLight` held `1843059529`, the Spot Light on the fish, not `619394801`, the Global Light 2D.
So `ApplyCaveDepth()` was driving the lure with the cave curve while `SpotLight2D` wrote the
same `intensity` every frame -- two writers on one value, the rule the counter flicker already
taught -- and the cave itself never darkened at all, because nothing was driving the global light.

Easy to miss: both are `Light2D`, so the slot accepts either and the Inspector shows a plausible
name. Worth checking the *component* and not the object name when a typed slot has two candidates
in the scene.

### The light pulsed on any press, not on a swim

The script polled `Pointer.current.press.wasPressedThisFrame` directly, which is every tap
anywhere: on the menu, while paused, after death, and on UI buttons. `SwimPressed()` already
carries all four guards, so the fix was to publish the swim rather than re-derive it --
`PlayerControllerScript.SwamThisFrame`, read by the lure. One flag, and the light inherits every
guard the swim will ever gain.

The light finds the player with `GetComponentInParent`, since it is already a child of the fish.
No Inspector slot means no slot to come loose.

### File name did not match the class

`SpotLight2D.cs` held `class LightTapPulse`. Unity needs those to agree or the component returns
as a missing script on the next reimport -- the same trap `MyGameManager.cs` hit in the very first
session. Renamed the file, not the class, since `SpotLight2D` is a poor name for something that
pulses and collides with Unity's own `Light2D`. The `.meta` was renamed with it, so the GUID and
the scene's reference survived.

`Assembly-CSharp.csproj` still listed the old path afterwards and the compile check failed on it.
That file is generated -- Unity rewrites it on focus -- so the entry was just corrected in place.

### Depth is published, not recomputed

`GameManager.CaveDepth` is now a public 0-to-1, set in `ApplyCaveDepth()` **before** the null
check, so the lure keeps tracking depth even with no global light wired. The lure rests at
`baseIntensity` at the mouth and `baseWhenDeep` at the darkest, so it carries more of the scene as
the cave drops away. One number behind both lights; a second copy of the distance maths would
have drifted.

### The circle in the screenshot

`m_FalloffIntensity` was 0.349, which ends the light over a short band and leaves a visible rim on
a dark background. The transform scale was also non-uniform at (2.02, 1.65), which makes a radial
light elliptical and can seam.

Also found: **there is not one `ShadowCaster2D` in the project**, so `m_ShadowIntensity` 0.719 on
both lights does nothing at all. The hard-edged dark shapes in the screenshot are background art
and rock obstacles, not shadows -- worth recording so the next person does not go hunting a
shadow bug that cannot exist.

---

## How dark the cave can afford to be (2026-10-09)

The ask was a cave dark enough that the player cannot see what is coming. The answer is not a
lower number, and the reason is worth keeping, because the tempting setting is the unfair one.

**Contrast sells darkness, not absolute darkness.** A uniformly dim screen reads as muddy; the
eye adapts within seconds and on a phone in daylight it simply vanishes. A bright lure against a
dim cave reads darker than a dim everything. So the global light goes down and the lure goes up,
and the gap between them is the whole effect.

### The lure cannot be the only light, and the numbers say so

Measured off the scene rather than guessed:

| | |
|---|---|
| Rock speed | 5 u/s, multiplied 0.7 at the start and 1.6 at full ramp (90 s) |
| With the 1.3 surge on top | up to **10.4 u/s** |
| Lure reach | Outer Radius 1.82 x scale 2 = **3.63 units** |

That gives 1.04 s of warning at the opening speed, **0.45 s at full ramp, 0.35 s during a surge**.
Reaction alone is around 0.25 s and the fish still has to travel afterwards. So past about ninety
seconds the lure is not enough on its own and the player has to be reading rock silhouettes
against the background.

Which means **the global light is not atmosphere, it is the fairness mechanism.** `deepLight`
0.08 is the setting that quietly removes it.

### Two ramps that converge by accident

Darkness runs on distance (`darkestAtDistance` 400) and speed runs on time (`rampSeconds` 90).
The distance counter climbs at `5 x SpeedMultiplier`, so 400 m arrives around 70 s -- the cave
reaches its darkest just as it reaches its fastest. Nobody chose that; two independent ramps
simply landed on top of each other, and it stacks the hardest lighting on the hardest speed.
Raising `darkestAtDistance` to 700 pulls them apart.

### Settings this lands on

`startLight` 0.30, `deepLight` **0.16** (less than half the opening, still readable), 
`darkestAtDistance` 700, lure Outer Radius 5 at scale 1, `baseWhenDeep` 1.4.

Scale stays at 1 and the radii do the sizing, because a 2D light's size is radius *times*
transform scale and two controls for one number is how it ended up elliptical earlier.

### A difficulty valve that came free

The lure flares on every swim, so a panicking player tapping rapidly gets more light exactly when
they need it, while a calm glider sees least. That fell out of the tap-pulse without being
designed, and it is worth remembering before any other assist gets added.

### Still mis-wired

`caveLight` points at the **Spot Light 2D**, the lure, not `Global Light 2D`. The component
fileID has changed once since this was first spotted, so it was re-made rather than re-pointed.
Until it is fixed the cave never darkens and two scripts write the lure's intensity every frame.

---

## Darkness pass, and a runaway the valve turned out to be (2026-10-09)

The tuning from the entry above was applied by hand in the Editor before this ran -- `caveLight`
repointed at `Global Light 2D`, 0.3 / 0.16 / 700, lure radii 1 and 5, scale back to 1, shadow
intensity 0. The patch's precondition guard is what reported that, by refusing to change a field
it did not find in the state it expected. Worth noting as the guard paying for itself: without it
the script would have written `caveLight` back to the lure it had just been rescued from.

### The difficulty valve was a bug

The previous entry called the tap flare a free difficulty valve: panic and the lure brightens.
It was worse than that. `SpotLight2D` did

```csharp
current += pulseBoost;
```

with no ceiling, so the boosts **stacked**. Tapping is the one input a player can spam without
limit, and mashing the screen drove the lure to roughly nine times its intended peak within
seconds -- enough to light the whole cave and take the darkness, which is the entire mechanic,
back out of the game.

Clamped to one flare:

```csharp
current = Mathf.Min(current + pulseBoost, resting + pulseBoost);
```

The valve survives -- a tapping player still sits nearer the peak than a gliding one -- but it now
has a ceiling. This was introduced in this session by changing an assignment into a `+=` while
adding the depth lerp; the original line was `current = baseIntensity + pulseBoost`, which was
bounded by construction. A `+=` on a value that recovers toward a floor needs a ceiling, and that
is easy to miss because it reads as the more natural form.

Checked by simulating the loop at one tap per frame for ten seconds: clamped peaks at 1.25 against
a ceiling of 1.30, unclamped reaches 11.7.

### Darker background, by tinting the sprite and not the light

`Bg_0` and the inactive `BG_1` carry a 0.45 grey tint now. Doing it on the sprite rather than
with the global light is the point: **a light dims the rocks too, a sprite tint dims only the
backdrop.** So the cave reads darker while rock-against-background contrast actually goes up,
which is the opposite of what reaching for the intensity slider would have done. Given the
warning-time numbers in the entry above, that direction matters.

`ScrollObject` clones `Bg_0` at runtime, so the copies inherit the tint and nothing else needed
doing.

---

## Two darkeners that multiplied, and the lure script renamed back (2026-10-10)

### The cave went muddy because two things darkened it

The 0.45 grey tint added to `Bg_0` was applied on top of a global light already tuned to 0.3, and
**they multiply**: the background was rendering at 0.30 x 0.45 = **0.135** on the menu and
0.16 x 0.45 = **0.072** deep. That is 2.2x darker than the 0.30 that had been tuned and liked, so
everything collapsed into one near-black band -- which is what "muddy" is. Low contrast, not low
brightness.

Tint raised to **0.75**: menu 0.225, deep 0.12. Still clearly dark, and the tint still does its
one job of knocking the backdrop down without touching the rocks, just gently instead of
catastrophically.

The lesson is the stacking, not the number. The background is the only thing in the scene that
gets hit by both the light curve and a sprite tint, so it is the only thing whose brightness is a
product rather than a value. Change one and the other's effect changes with it.

### The lure script is `SpotLight2D` again

Renamed back by request. The class went with it -- `SpotLight2D.cs` holding `class SpotLight2D`
-- because reverting the *file* name alone would have restored the original mismatch, which is
the bug this file already records twice.

Worth noting what went wrong in the middle: the class was renamed first and the file rename was
declined, leaving `LightTapPulse.cs` holding `class SpotLight2D` for a few minutes. A rename like
this is two edits that are only correct together, and stopping between them is worse than either
end state. Do the file and the class in one go, or not at all.

`Assembly-CSharp.csproj` needed its path corrected both times, since it is generated and still
listed the old file. Unity rewrites it on focus, so correcting it is only for the offline
`dotnet build` check.

---

## A background gap nobody saw, and a doc that quoted its own numbers (2026-10-10)

### `ScrollObject` recycled a copy while it was still on screen

The wrap test was `transform.position.x <= -width`, a threshold with no relationship to the
camera. It assumes the screen is no wider than one background copy. It is not: `Bg_0` measures
about **14.0 units** scaled, and the screen is **17.8** at 16:9 and **21.7** on a 19.5:9 phone.

So a copy was sent to the back of the line while its right edge was still 1.9 units inside the
viewport, and the copy behind it only reached `-width/2`. That left a bare strip at the **left
edge** of the screen, closing over about a second as the row caught up, returning every
`width / scrollSpeed` = **7 seconds**.

The defect scales with aspect ratio, which is why it survived this long. In the Editor at 16:9 it
is 1.9 units against a dark cave. On the 19.5:9 handset this project actually targets it is
**3.8 units, about 18% of the screen width**. Nothing has run on a phone since 2026-09-24, so the
only aspect anyone has looked at is the one where the bug is smallest.

Fixed by testing against the camera instead of a constant:

```csharp
if (transform.position.x + width / 2f <= leftEdge)
```

`leftEdge` is now a field, read once in `Start()` from the value that method already computed for
the row anchor. The camera never moves in this game, so once is enough, and `Update()` does not
need `Camera.main`.

`AddMissingCopies()` needed no change, which is worth recording because it looks like it should.
It makes `ceil(screenWidth / width) + 1` copies. The later threshold needs
`copyCount >= screenWidth / width + 1` for the row to stay covered through a wrap, and the
existing count satisfies that at every aspect ratio — exactly, with nothing to spare.

### The starter doc was quoting tuned values, and four had drifted

`START-NEW-CHAT.md` opened by promising that nothing in it could drift out of date, and then
quoted six lighting numbers. Against the scene: `deepLight` 0.16 and `baseWhenDeep` 1.4 matched;
`startLight` (0.3 vs **0.35**), `baseIntensity` (0.5 vs **0.35**) and `darkestAtDistance`
(700 vs **400**) did not.

The lighting section no longer carries any of them. It names the fields and says to read the
settings off the Inspector. The argument underneath was rebuilt on figures that are derived
rather than transcribed — lure radius 4.9, closing speed `speed` × `SpeedMultiplier`, so 0.61 s
of warning and 0.47 s mid-surge — so a retune changes the inputs and anyone can recompute it.

This is trap 2 applied to documents. A number copied into a `.md` file is a second copy with
nothing keeping it in step, and it rots silently because no compiler reads it. The trap section
says so now.

### `darkestAtDistance` 700 is not doc drift, it is an unapplied decision

Worth separating from the rest. *Two ramps that converge by accident* above works the problem
through and *Settings this lands on* commits to 700, with the reasoning: at 400 the cave reaches
its darkest just as it reaches its fastest, stacking the hardest lighting on the hardest speed.

The scene still holds 400. The decision was made, written down, and never typed into the
Inspector — and a saved value beats a script default, so editing the script would not have done
it either. Recomputed at today's values: 400 m arrives at about **75 s**, `rampSeconds` is 90, so
darkness lands roughly 15 s *before* full speed. At 700 it would be about 111 s, 21 s *after*.

Left at 400 for now, because 400 is what every recent play session actually felt like and nothing
about it has been reported as unfair. It is now item 3 of *Do these first* in the starter doc
rather than a settled line in this one.

### Two stale names, one that matters and one that does not

`SpotLight2D`'s two `Debug.LogError` calls still opened with `LightTapPulse:`. Changed. An error
naming a script that does not exist sends the next person looking for a file.

The scene also carries `m_EditorClassIdentifier: Assembly-CSharp::LightTapPulse` on the lure.
That one is harmless and was left alone: resolution goes through the `m_Script` GUID, which
matches `SpotLight2D.cs.meta`, and Unity rewrites the cached identifier on its next save. The
starter doc previously warned this would show as a missing script. It will not.

### `caveLight` is wired correctly now

*Still mis-wired* above is stale and should be read as closed. `caveLight` is fileID 619394801,
which is the component with `m_LightType: 4` — the Global Light 2D. The cave does darken, and
only `SpotLight2D` writes the lure.

### `BG_1` is inert, so half a to-do was pointless

The starter doc asked for `Bg_0`'s tint to be matched on `BG_1` for consistency. There is no
consistency to keep: `BG_1` is inactive in the scene, `FindCopies()` filters on
`activeInHierarchy`, and `AddMissingCopies()` clones `Bg_0`. Every copy on screen inherits
`Bg_0`'s tint at runtime. `BG_1` never renders and its white tint cannot be seen.

The `Bg_0` half was already done, by eye rather than by typing 0.75: the Sprite Renderer reads
`0.706, 0.638, 0.638` — slightly warm rather than neutral grey, and a touch under the 0.75 this
log settled on. Close enough to leave, and worth knowing it is a colour and not a grey.

### The pause blur showed flat blocks, and two things were doing it

Reported as "distorted pixels" on pause and game over. Two independent causes, both in
`CapturePauseBlur()`, neither of them the blur algorithm itself.

**The first halving was decimating, not averaging.** `RenderTexture.GetTemporary()` returns a
pooled texture carrying whatever `filterMode` its last user left on it, and the filter a `Blit`
uses is the **source's**, not the destination's. The loop sets `filterMode` on each `smaller`
it creates — which covers steps 2, 3 and 4, because each one becomes the next step's source —
but nothing ever set it on the full-resolution capture. So the first and most important halving
read one texel in four instead of averaging a 2x2 block. That is nearest-neighbour decimation,
and the hard edges it produces are still hard edges after three more halvings.

The log already records fixing a blocky result once, by replacing a single jump to 1/8 with
successive halvings. The halvings were right; one of them just was not filtering.

**RGB565 was banding the dark frames.** The format was chosen for one property: no alpha
channel, so the alpha-0 clear could not ghost the HUD through. The property is real and the
reasoning held. The cost was not costed: 5 bits of red and blue, so **32 levels** across the
whole range — and the cave lives in the bottom tenth of it, which leaves roughly three or four
levels to describe an entire dark gradient. It collapses into flat patches with hard edges,
which is exactly what the screenshot shows.

Fixed at the source instead. The capture now forces an opaque clear for the one frame it
renders:

```csharp
Color wasBackground = cam.backgroundColor;
cam.backgroundColor = new Color(wasBackground.r, wasBackground.g, wasBackground.b, 1f);
cam.targetTexture = frame;
cam.Render();
cam.targetTexture = wasTargeting;
cam.backgroundColor = wasBackground;
```

With alpha guaranteed 1 there is nothing for a halving to average outwards, so the format no
longer has to drop the channel, and `RenderTextureFormat.Default` goes back in. The
`SupportsRenderTextureFormat` branch went with it — the fix got shorter, not longer.

The lesson is that the alpha workaround was one layer too high. Picking a format to avoid a
channel meant accepting whatever else that format gave up, and what it gave up was the colour
depth the content actually needed.

### The camera clears to a mid blue with alpha 0, and nothing dims it

`m_ClearFlags: 2` (Solid Color), `m_BackGroundColor: {0.192, 0.302, 0.475, a: 0}`.

Two consequences, and the second is why it is written down here.

The alpha 0 is what the blur workaround above existed to dodge. That is handled in code now.

The **colour** is the one still live. A Light 2D lights sprites; it does not touch the camera's
clear. The cave renders at roughly 0.1 to 0.25 once the tint and the global light have both had
their go, while any sliver of clear colour renders at the full 0.19/0.30/0.47. So anywhere the
background fails to cover the screen — for a frame, for a pixel column — what shows through is
not a dark gap. It is brighter than everything around it, and it appears and disappears as the
row wraps.

That is the likely shape of the reported "white flash lines on the bg", and it is the same
defect as the wrap gap fixed above wearing a different face: the gap explains *where*, the clear
colour explains *why it is bright rather than dark*.

**Setting Background to opaque black on `Main Camera` makes any residual seam invisible**, and
costs nothing — nothing in this game is ever meant to show camera clear. It is a scene value, so
it has to be typed into the Inspector.

If a bright line survives that, the cause is not geometry and the next two suspects are on the
texture, not the camera:

- `BG_4.jpg` imports with `filterMode: 0` (**Point**) and no mipmaps, and it is *downscaled* on
  screen — 1280 px of sprite across roughly 790. Point sampling a downscale drops pixels rather
  than averaging them, and a scrolling downscaled texture shimmers for the same reason the blur
  did. Bilinear is the one-value fix; mipmaps are the thorough one. Left alone because it changes
  how the art looks and that is a call to make deliberately, not a bug to fix quietly.
- It is a **JPG**. JPEG ringing concentrates on hard edges, including the image's own border, and
  `spriteExtrude: 1` extrudes that border texel. A bright edge row would read as a bright line at
  every tile boundary, travelling across the screen once every 7 seconds. A PNG re-export is the
  fix if it comes to that.

### Four findings left alone on purpose

All four are real and none earns a change today. Parked here so the next pass does not have to
re-find them:

- **`Camera.main` per obstacle per frame.** `ObstacleMovement.Update()` calls it for every rock,
  coin and debris piece, every frame — roughly 18 objects — and there are 10 more call sites
  across the spawners and `PlayerHit`. Caching it is a few lines. No profiler has complained, and
  the phone build is the thing that would tell us, so measure on device first.
- **`GameManager.Awake()` line 181** calls `obstacleSpawner.GetComponent<TrashSpawner>()` with no
  null check, while every later use of that field is guarded. An empty Inspector slot throws
  before the guards it sets up can help.
- **`TrashSpawner.StartSpawning()`** runs `ResetRun()` before `HasValidTrash()`. With an empty
  prefab list it leaves `SpeedMultiplier` at `startSpeedMultiplier` 0.7 and never sets
  `spawning`, so nothing ramps it: the whole cave runs permanently at 0.7x, distance counter
  included. Only reachable by unwiring the prefabs.
- **`Score` is write-only.** `ScoreTrigger` is live on all five rock prefabs and calls
  `AddScore()`, `ResetScore()` zeroes it, and nothing has read it since distance replaced it on
  the HUD. Deleting it properly means removing a component from five prefabs, which is Editor
  work, and deleting `ScoreTrigger.cs` without that would leave five missing scripts.

---

## Clean-code pass over all 13 scripts (2026-10-10)

Applied `clean-code-guidelines.txt` (Code Monkey, distilled). Audited every rule against every
script first, because most of them turned out to be satisfied already and the two that are not
are the expensive kind.

**Already clean, checked not assumed:** rule 8 (no strings as identifiers) — no `GameObject.Find`,
no `Transform.Find`, no tags, no `SendMessage`, and the three PlayerPrefs keys are already
`const`. Rule 2 (default to private) — every serialized field is `private` with
`[SerializeField]`, and every property is `public … { get; private set; }`. Rule 1 (naming) — the
names are already verbose and specific. Nothing to do on any of them.

### Rule 10: the same two lines lived in four files

`CameraRightEdge()`, `CameraLeftEdge()` and `CameraTopEdge()` were private copies in
`MyObstacleSpawner`, `TrashSpawner`, `CoinSpawner` and `ObstacleMovement`, with the same maths
inlined again in `GameManager.PlayerStart()`. Five copies of `orthographicSize * aspect`.

Now one `CameraEdges` static: `Left`, `Right`, `Top`, `HalfWidth`. It also caches the camera,
which closes the `Camera.main`-per-obstacle-per-frame finding parked in the entry above — that
call was in `ObstacleMovement.Update()`, running for every rock, coin and debris piece every
frame. The cache re-fetches if the camera is ever destroyed, because Unity's `== null` is true
for a destroyed object and not only a missing one.

`PlayerHit` and `ScrollObject` kept their own camera handling. Both guard against a null camera
and fall back; the four that were consolidated never did. Folding guarded code into unguarded
code to remove a duplicate would have been a behaviour change smuggled in as tidying.

### Rules 2 and 5: two public static mutable fields

`ObstacleMovement.SpeedMultiplier` and `Surge` were `public static float`, writable by any
script in the project. Both rules land on this at once: rule 2 because public means the bug could
be anywhere, rule 5 because a value anyone can write has no single owner.

They are now `public static float … { get; private set; }` with one writer:

```csharp
public static void SetPace(float speedMultiplier, float surge)
```

`TrashSpawner` owns the ramp and is the only caller. The compiler now enforces what was
previously only a convention — and this is not hypothetical, the log already records these
statics surviving into the next run because the reset lived somewhere a reader would not look.

One behaviour change, deliberate: `ResetRun()` now sets `Surge` back to 1 as well as the
multiplier. `Update()` overwrote it on the next frame anyway, so it is a no-op in practice, but
leaving a stale swell in a field named "the current surge" is how a reader stops trusting both of
them.

### Rule 7: five magic numbers named

`SPAWN_MARGIN` (1f, in both spawners), `ROW_START_MARGIN` (0.5f), `FORGET_MARGIN` (5f),
`DESPAWN_MARGIN` (1f). All `const`, not `[SerializeField]` — they are structural offsets rather
than tuning knobs, and a const cannot be orphaned by the scene the way a renamed serialized field
can. `0f` and `1f` used as identities were left alone; rule 7 is about a bare 5 in the middle of
logic, not about arithmetic identity.

### Rule 1, locally: `Rock.transform`

The private nested `Rock` class had a field called `transform`, and `SpawnRock()` uses
`rock.transform` two lines from `row.last.transform` meaning something else entirely. Renamed to
`rockTransform`. Safe to do because `Rock` is a private nested class and nothing is serialized.

### Rule 6: left almost entirely alone, on purpose

The guidelines want comments rare and about "why". Read against what rule 6 actually asks for,
these files already comply: the comments are design decisions, bug-avoidance notes and ordering
constraints, which it lists as the things to *do* comment. Very few restate code.

The density is high, and that is a deliberate response to this project's situation rather than a
failure to follow the rule. There is no useful git history here. A comment explaining why
`pauseDimDarkness` is *not* `[FormerlySerializedAs]`, or why the blur drops a channel, is the only
copy of that reasoning outside this log. Rule 0 settles it: if following a rule makes the code
harder to understand or change, break the rule. Stripping these would delete the project's memory
to satisfy a guideline whose entire stated purpose is understanding.

### Rules 4, 11 and class renames: not done, and why

Three real findings, all left for a deliberate decision because each one costs Inspector work
that a compile cannot verify:

- **Rule 4, single responsibility.** `GameManager` is ~730 lines doing scoring, coins, distance,
  lighting, pause and menu state, runtime UI construction, blur capture and audio. It is the
  clearest violation in the project. Splitting it moves `[SerializeField]` fields to new
  components, which orphans every Inspector reference on the GameManager object — roughly twenty
  slots to re-drag, with nothing but play-testing to confirm they are right. Worth doing, worth
  doing on purpose, with the scene closed and a full backup.
- **Rule 11, dead code.** `Score`, `AddScore()` and `ResetScore()` are write-only, and
  `ScoreTrigger` is live on all five rock prefabs feeding them. Deleting it means editing those
  five prefabs — sanctioned, prefabs are safe to edit on disk — but it also throws away a scoring
  system that may be wanted back. Not a tidying decision.
- **Rule 1, class names.** `PlayerControllerScript` carries a meaningless `Script` suffix and
  `MyObstacleSpawner` a meaningless `My`. Both are rule-1 violations. A MonoBehaviour rename is
  the file, the class and the `.meta` together, and this log already records that exact rename
  going wrong twice in one session. Not worth doing quietly.

`Assembly-CSharp.csproj` needed `CameraEdges.cs` added by hand for the offline `dotnet build`
check, same as the renames before it. Unity rewrites the file on focus.

---

## The lure was on Multiply, and the three refactors (2026-10-11)

### One wrong dropdown explained all three lighting symptoms

Reported as: the lure does not activate on a tap, the light is hollow, the middle is too dim,
and raising Intensity by hand changes nothing.

`Renderer2D.asset` defines the blend styles in order — **0 is Multiply, 1 is Additive**. Both
lights were on index 0.

Multiply is correct for `Global Light 2D`. That light *is* the darkening mechanism; the whole
cave-depth model is a multiply against the background tint, and the log above works through what
that product does.

Multiply is wrong for the lure, and wrongly in a way that looks like four separate faults:

- **Hollow.** Multiply gives `sprite x light`. The fish is a near-black sprite, so no intensity
  can lift it: black times anything is black. The background around it has colour to multiply up,
  and at Intensity 8.1 it blew out into a bright ring. A bright ring around a black centre is
  what "hollow" was.
- **Dim in the middle.** Same sentence from the other end. The lure could never add light, only
  fail to remove it.
- **No flare on tap.** The flare was firing the whole time. `pulseBoost` adds 0.8 to a multiplier
  that is multiplying a black fish, so the result of the flare was black either way.
- **Manual tuning did nothing.** This one is not the blend mode. `SpotLight2D.Update()` writes
  `lure.intensity` every frame from `baseIntensity`/`baseWhenDeep`, so the 8.1 typed into the
  Inspector was overwritten on the first frame of play. Exactly the trap already recorded for the
  global light's own slider, hit a second time on the other light.

Fixed: lure to `m_BlendStyleIndex: 1` (Additive), `baseIntensity` 0.35 to 0.55, and the Light 2D's
own `m_Intensity` set from 8.1 to 0.55 so the Scene view stops previewing a value the game will
never render. The global light was not touched.

**The knobs that work are `Base Intensity` and `Base When Deep` on the `SpotLight2D` component,
not the Intensity slider on the Light 2D.** Worth saying plainly, because the slider has now cost
two sessions.

### Rule 11: the score is gone

`Score`, `AddScore()` and `ResetScore()` deleted from GameManager, `ScoreTrigger.cs` deleted, and
the `ScoreZone` child removed from all five rock prefabs — GameObject, Transform and BoxCollider2D
together, not just the script. Leaving the collider would have left every rock carrying a trigger
that fires into nothing.

Checked before cutting: `m_IsTrigger: 1`, so it was never part of the rocks' physical shape and
removing it cannot change what kills the fish. `HighScore` is unrelated and stays — it is the best
*distance*, and the game over panel reads it.

### Rule 1: two class names

`PlayerControllerScript` to `PlayerController`, `MyObstacleSpawner` to `ObstacleSpawner`. The
`Script` suffix and the `My` prefix said nothing.

Done the way this log has twice recorded going wrong: the `.cs` and the `.cs.meta` renamed
together so the GUID travels with the file, then the class, then the 24 references, in one pass.
GUIDs verified identical before and after — `623e7c4f…` and `0df512d5…` — which is what keeps
every scene reference pointing at the right script. The serialized field in GameManager is still
named `obstacleSpawner`; only its type changed, and Unity keys saved values by field name, so the
Inspector slot survives.

### Rule 4: the blur moved out, and nothing else did

`PauseBlur.cs` now owns the dim, the blur, the render textures and the stacking order. GameManager
is 726 lines down to 562.

It is a plain class, not a MonoBehaviour, and that is the whole reason this split was safe. A new
component means new serialized fields, and moving `pauseDimDarkness` and `pauseBlurSteps` onto it
would have orphaned both saved values — including a `darkestAtDistance` of 700 applied only
yesterday. Instead the two fields stay serialized on GameManager and arrive through the
constructor. **Zero serialized fields moved**, verified by diffing the `[SerializeField]` set
before and after: nothing lost, nothing added. No Inspector slot to re-drag.

What GameManager kept is the part that decides *what* goes above the dim; `PauseBlur.Show()` takes
them as `params GameObject[]` and decides only the stacking. It never has to know what a game over
panel is.

The rest of rule 4 is still open and still worth doing — the cave lighting, the coin wallet and the
HUD writers are all separable. All of them own serialized fields with tuned values, so each one
needs the scene closed and the values hand-carried into the new component. That is the expensive
half, and it is expensive for a reason worth remembering: **the cheap half of a single-responsibility
split is the code, the costly half is the serialized state.**

### Still to verify in the Editor

Nothing here has been run. `dotnet build` is clean and the scene and prefabs verified structurally
(no dangling fileIDs, no references to the deleted script, every GUID resolving), but the lighting
change in particular is a judgement call that needs eyes on it. `PauseBlur.cs` has no `.meta` yet;
Unity writes one on import, and nothing references it by GUID.

---

## Tuning the lure from a screen recording (2026-10-11)

Diagnosed off `1007.mp4`, a 21-second Editor capture of an actual run, rather than from the
values. Worth recording that it was the right call: the frames showed the opposite of what the
numbers implied.

**What the frames showed.** At Distance 5 the lure is a reasonable soft pool. By Distance 58 it
is a bright cyan ball with the fish a silhouette inside it, and the cave around it is mid-teal
rather than dark. The run gets *brighter* as it goes, which is backwards.

Three separate causes, none of them the blend mode that was fixed earlier the same day:

- **`pulseBoost` 0.8 on an additive light is a floodlight, not a flare.** Under Multiply it was
  invisible, so it had never actually been seen doing anything. The moment the blend mode was
  right, every tap added 0.8 across a 3.93-unit radius. Tapping is continuous in this game, so
  `recoverSpeed` 4 never got the chance to bring it down.
- **`m_PointLightInnerRadius: 0.47` is a full-intensity disc.** Inner radius in URP 2D is where
  the falloff *starts*, so 0.47 was a hard plate of maximum light sitting on the fish's head.
  That is most of what "covering the entire fish" was.
- **The cave is not dark.** With `darkestAtDistance` at 700, `CaveDepth` is 0.08 at Distance 58,
  so the global light is still essentially at `startLight`. A lure cannot read as a light source
  in a cave that is already lit — there is nothing for it to reveal.

**Changed.** Lure: outer radius 3.93 to 3.0, inner radius 0.47 to 0.12, colour from 0.909 grey to
a pale cyan (0.78, 0.95, 1.0), `baseIntensity` 0.55 to 0.45, `baseWhenDeep` 1.4 to 0.95,
`pulseBoost` 0.8 to 0.3, `recoverSpeed` 4 to 7. Cave: `startLight` 0.35 to 0.20, `deepLight` 0.16
to 0.11.

Grey was worth changing on its own. An additive grey light over a teal cave has no hue to
contribute, so it desaturates whatever it touches — it bleaches rather than illuminates, which is
why the pool read as haze. A tinted light adds a colour and the lit area separates from the
background instead of washing into it.

**Added: the lure wavers.** `flickerDepth` 0.16 and `flickerSpeed` 6.5 on `SpotLight2D`, driven by
`Mathf.PerlinNoise`. Perlin rather than `Random` because it wanders between values instead of
snapping, which is the difference between something alive and a loose connection. Applied to the
output rather than to `current`, or the waver feeds back into the flare's recovery and the two
fight each other. `Time.unscaledTime`, so the lure still breathes on the menu where timeScale is 0.

### This moves the fairness numbers, and they were already measured

The entry above measured the warning window off a 4.9-unit lure reach. It is 3.0 now, and the
global light is down a third as well — both of the levers that make rocks readable moved in the
same direction in one pass. At 8 u/s, 3.0 units is **0.375 s** of warning against a ~0.25 s
reaction, where it used to be 0.61 s. Inside a surge it is **0.29 s**, which is essentially no
warning at all.

That is a real risk and it is unverified: this is tuning done from a recording, not from playing
it. If deaths start feeling unfair, the DEVLOG's own rule still applies — **raise `Deep Light`,
not the lure.** The lure is atmosphere; the global light is the fairness mechanism.

### A comment line was damaged and restored

The rename pass earlier today blanked a line in `SpotLight2D.cs`'s header comment — the one
naming the `MyGameManager.cs` precedent. Caught by reading the file rather than by the compiler,
which is the point: nothing automated guards the comments, and they are this project's memory.
Restored from the `1957` backup.

---

## The lure rebuilt as two lights, and a day lost to an open Editor (2026-10-11)

### The real reason three rounds of tuning did nothing

`Temp/UnityLockfile` dated 20:09. Scene edited on disk at 20:38. The Editor had the scene in
memory the whole time and the two never met. "Scene is closed" was taken to mean the Editor was
closed; it meant the scene tab was not open while the project still was.

Worth stating as a rule, because it cost most of a session: **an open Editor is not a passive
reader of these files.** It holds its own copy, it re-serializes components whenever a script
changes, and it writes the whole scene back on save. Editing `SampleScene.unity` on disk while it
runs is not "risky", it is a race, and the Editor usually wins.

The tell is cheap and should be checked before any scene edit from now on:

```
ls Temp/UnityLockfile
```

Present means stop.

### Multiply reveals, Additive hazes, and one light cannot do both

The blend styles are fixed in `Renderer2D.asset`: index 0 Multiply, index 1 Additive.

- **Multiply** accumulates into a factor the sprite is multiplied by. It reveals *surfaces* and
  leaves empty water alone, because there is nothing there to multiply. That is what reads as
  lighting in a cave.
- **Additive** is painted over everything inside the radius, empty water included. It is the only
  way to show a visible glowing source, and it is also why a single additive lure reads as a haze
  blob at every radius and intensity that was tried. Hazing is not a misconfiguration, it is what
  additive does.

Switching the lure from Multiply to Additive earlier today traded one failure for the other: under
Multiply it could not glow, under Additive it could not stop fogging. Neither was a tuning
problem, which is why three passes of tuning moved nothing.

Two rigs were on the table. The two-light version — a wide Multiply pool plus a tiny Additive
bulb — gets both halves, because each light does the one job its blend style is good at.

**Chose one Multiply light.** The additive bulb buys a visible glowing dot and costs a second
object, a second set of radii to keep in step, and a script with two references that can come
loose. The pool is what makes the cave readable and what the complaint was actually about; the
glowing dot is decoration. One object, one slider, no wiring.

The tradeoff is real and accepted: **a Multiply light cannot make the lure appear to emit
anything.** The fish stays a silhouette and there is no bright bulb at the esca. If that is ever
wanted, the cheap answer is a small additive *sprite* on the lure tip — not a second Light 2D,
which brings back the radius-pairing problem for the same picture.

Settings on the existing `Spot Light 2D`, which was kept rather than rebuilt: Blend Style
Multiply, Intensity 1.0, Inner Radius 1.0, Outer Radius 7.0, Falloff 0.8, shadows off, colour a
cool near-white.

**`Player_Fish` is scaled 0.5, and a Light 2D's radius is its value times the parent scale**, so
Outer Radius 7.0 draws as 3.5 world units. This log already recorded that catching someone once;
it is written here again because it caught the maths a second time today.

### Intensity ownership inverted

The old script wrote `lure.intensity` from its own `baseIntensity` field every frame, so the
Intensity slider on the Light 2D did nothing at runtime. Two sessions were spent typing values
into that slider and watching them vanish on the first frame of play.

`SpotLight2D` now reads each light's authored Intensity once in `Awake` and treats it as that
light's 100%, modulating both by one shared level:

```csharp
revealBase = revealLight.intensity;   // the slider is the knob again
...
revealLight.intensity = revealBase * lit;
glowLight.intensity   = glowBase   * lit;
```

Eight tuning fields become five, and brightness lives on the light where a person looks for it.
The cost, which is in the comments: the bases are captured in `Awake`, so dragging Intensity
*during play* still does nothing. Tune in edit mode, then press Play.

### Left for the Editor on purpose

The light settings were not written into the scene file. With the Editor open that edit would be
overwritten, and hand-written scene YAML fails silently in a way only Unity reports. Changing a
dropdown and five numbers on an object that already exists is faster and safer than a scripted
edit plus a restart, so the values were specified and handed over.

Nothing was deleted either. The brief from the frustration of the day was "remove it and start
afresh", but the object that exists *is* one Multiply light once its Blend Style is changed.
Deleting and recreating it would have been churn with an identical result.

---

## The fish shop behind the Characters button (2026-10-11)

Five blocks on a horizontal shelf, bought with coins and equipped one at a time. Code and import
settings only — the shelf itself is Inspector work, deliberately.

### Nine more sprites caught by trap 3

Every file in `Fish Inventory/` and `UI Inventory Buttons/` imported as `spriteMode: 2`
(Multiple), so none of them could be assigned to a UI `Image`. All nine flipped to Single in their
`.meta` files, which is safe to do on disk with the Editor running.

The trap note in `START-NEW-CHAT.md` says "this has caught four assets; treat it as the default".
It is nine more now. It is not an occasional accident, it is what this project's import settings
do to every new sprite.

### What the art decided

`PEIXE1`-`PEIXE5` and `PEIXE BLOQUEADO` are all **353x410** finished cards, frame and artwork
baked in. The three button images are all **167x44**. Two things follow:

- The cards cannot double as the in-game fish sprite, so **equipping cannot change the fish on
  screen yet**. `FishInventory.EquippedIndex` records the choice and is public for the day five
  in-game sprites exist. The swap will be one line: the `Animator` on `Player_Fish/Animator`
  points at a controller GUID that no longer exists as an asset, so nothing overwrites
  `SpriteRenderer.sprite` each frame.
- Uniform card sizes mean the shelf can use a plain `HorizontalLayoutGroup` with no per-card
  handling.

### No scrolling code

`ScrollRect` set to horizontal already does dragging, momentum, clamping and touch. How many
blocks are visible is decided by their width and spacing, not by logic. The only thing worth
writing down is that three-and-a-bit visible is better than exactly three: the fourth block
peeking off the edge is what tells a player the shelf moves at all.

### The wallet got its second writer, next to its first

`TotalCoins` had exactly one writer, `AddCoin()`. Spending is now `GameManager.TrySpendCoins(int)`
sitting directly beside it, and the shop never touches the wallet itself.

Checking the balance and subtracting it are **one** call on purpose. As two, every caller has to
remember to do both, and the caller that forgets hands out a fish for free. It returns false and
writes nothing when the player is short, so the early return happens before any mutation —
a partial spend here is real money to the player.

`PlayerPrefs.Save()` is called immediately on a purchase rather than left to the next natural
save. A lost point is an annoyance; a bought fish that does not survive the app being killed is
300 stolen coins.

### Blocks are found, not wired

`FishInventory` reads the blocks as the children of `content`, takes each block's own `Image` as
its card and `GetComponentInChildren<Button>()` as its button. Fifteen Inspector drags avoided,
and a sixth fish later means adding a block rather than editing the script.

Ownership is a bitmask in one PlayerPrefs int rather than five keys — PlayerPrefs has no array,
and five parallel keys is five chances to write one and not the others. Index 0 is forced owned on
load, so a corrupt save cannot leave a player with no fish and nothing affordable to fix it.

One `Refresh()` redraws every block from `EquippedIndex`, which is what makes "only one equipped"
true by construction instead of by remembering to clear the previous one.

### No automated check on the money path, again

Stated plainly because this is the first code here that can take something from a player. There is
no test assembly in this project and the logic only runs inside Unity; a mirrored test would
verify a copy of the arithmetic rather than the code that ships. The check is manual and it is two
cases: buy with 300+ coins and confirm the balance falls by exactly 300 and survives a relaunch,
then buy with fewer than 300 and confirm the balance is untouched and nothing is granted.

---

## The shelf authored straight into the scene (2026-10-11)

67 YAML documents written into `SampleScene.unity` by script, with the Editor closed: the
`CharactersMenu` panel, a horizontal `ScrollRect` with masked viewport, five blocks each carrying
an `ActionButton`, a close button, and the `FishInventory` component wired to all of it. Canvas
adopted the panel, `GameManager.charactersMenu` points at it, and `CharactersButton` was repointed
from the `BackToMenu` placeholder to `ShowCharacters(true)`.

### Nothing was written from memory

Every GUID came out of this project before a line was generated. The seven UGUI script GUIDs were
read from `Library/PackageCache/com.unity.ugui@bb329a87fcdc` — and `HorizontalLayoutGroup` was
cross-checked against the two instances already in the scene, which is what proved the method
sound before trusting it for `ScrollRect`, `Mask` and `ContentSizeFitter`, none of which the scene
had an example of. Component bodies were templated from real objects in this scene rather than
written from the docs.

### The dry run earned its keep

The generator was run against a **copy** first and a verifier checked it. It failed, and on
something that would not have been obvious:

```
880100047 lists child 880100007 but that child names father 0
```

Building bottom-up meant every child RectTransform was created before its parent existed, so all
fourteen were written with `m_Father: {fileID: 0}`. Unity would have opened the scene with the
whole shelf flattened to the root — not an error, not a missing script, just a hierarchy quietly
wrong in a way that looks like a layout bug.

Fixed by reserving the parent transform ids up front so a child can name its father at the moment
it is written. The rule worth keeping: **in scene YAML the parent/child link is stored twice**,
once as the parent's `m_Children` and once as the child's `m_Father`, and nothing checks that they
agree except Unity refusing to make sense.

The verifier stays in the scratchpad pattern: unique anchors, every local `fileID` resolving,
parent and child agreeing in both directions, and the specific references this change was supposed
to make. It is the only reason this was safe to do at all.

### Button art is a guess

Three 167x44 images existed and nothing says which is which, so: `Group 26` to buy, `Select
Button` to select, `Group 26-1` to equipped. One Inspector drag each to correct, and the slots are
already filled so the states are at least distinguishable on sight.

### Sizing

Blocks 470x546 (the cards' 353x410 ratio), spacing 55, viewport 1700 wide. That is 3.2 blocks
visible at the 1920 reference — three and a peek, because the fourth block showing at the edge is
what tells a player the shelf moves.

---

## Wired the wrong button, and why the verifier missed it (2026-10-11)

Reported as: the Characters button greys slightly and nothing else happens.

The grey was the Button's own pressed tint. `CharactersButton` had **no OnClick at all**, because
the rewire landed on a different object: `Canvas / Pause Buttons / HomeButton`, whose Button
component happened to sit near `CharactersButton`'s GameObject in the file.

Scene YAML is not grouped by object. Components appear in whatever order Unity wrote them, so a
`grep` over a line range that shows `m_Name: CharactersButton` and a Button component a few lines
later says nothing about whether they belong together. The Button at `&516573829` has
`m_GameObject: {fileID: 833734024}`, and `CharactersButton` is `506712601`. Two different objects.

The same mistake earlier in the day produced the claim that `CharactersButton` was "wired to
`BackToMenu` as a placeholder". It never was. *Known rough edges* has said since September that
it "has no OnClick, so it silently does nothing", and that was correct; the grep was not.

**Resolve components by `m_GameObject`, never by proximity in the file.**

### The verifier passed a broken scene

It checked unique anchors, every local fileID resolving, parent and child agreeing in both
directions, and that the expected components existed. All true, all useless here. It also checked
`ShowCharacters calls == 2` and found two — the close button and the hijacked HomeButton — so the
count was right and the target was wrong.

Structural checks prove the file will load. They say nothing about whether it does what was asked.
A semantic check for this would have been one line: assert the OnClick lives on the GameObject
named `CharactersButton`. Worth adding to the scratchpad verifier before the next scene edit.

### Second bug, introduced not found

The pause screen's Home button was left opening the fish shop instead of returning to the menu.
Recorded because it was a silent side effect of a change that appeared to succeed, and would
otherwise have been found later as "pause Home is broken" with no obvious cause.

---

## The verifier now checks meaning, not just shape (2026-10-12)

`CharactersButton` is wired and the pause `HomeButton` has `BackToMenu` back. Both were patched by
resolving the Button through its owning GameObject's **name and hierarchy path**, which is the
thing that went wrong the first time.

### Why the first attempt passed every check and still did nothing

The old verifier proved the scene would load: unique anchors, every local `fileID` resolving,
parent and child agreeing in both directions, expected components present. All true. It also
asserted there were two `ShowCharacters` calls, and found two — the close button and a hijacked
`HomeButton`. Right count, wrong object, green tick.

A structural check cannot tell a correct scene from a scene that merely parses. The missing half
was one assertion: that the OnClick lives on the GameObject it was supposed to. `Tools/verify_scene.py`
now carries an `EXPECTED` table keyed by hierarchy path:

```
Canvas/MainMenu/CharactersButton      -> ShowCharacters(true)
Canvas/Pause Buttons/HomeButton       -> BackToMenu
Canvas/GameOverMenu/HomeButton        -> BackToMenu
Canvas/CharactersMenu/CloseButton     -> ShowCharacters(false)
```

It also asserts the five shelf `ActionButton`s have **no** persistent OnClick, because
`FishInventory` adds those listeners in code — a drag into one of them in the Inspector would
double-fire every purchase, and nothing else would notice.

### Tools kept in the project

`Tools/` at the project root, outside `Assets` so Unity never imports it:

| File | What it does |
|---|---|
| `verify_scene.py` | Structure + wiring check. Run before and after any scripted scene edit |
| `gen_inventory.py` | The generator that authored the CharactersMenu shelf |
| `fix_buttons.py` | Repoints the two buttons, resolved by hierarchy path |

Kept in the repo rather than a scratch directory because this project has no useful git history and
the next scene edit is months away from remembering any of this. `python Tools/verify_scene.py
Assets/Scenes/SampleScene.unity` from the project root, with the Editor closed.

### The rule that came out of it

**Resolve a component by its `m_GameObject`, never by what it sits next to in the file.** Scene YAML
is written in whatever order Unity felt like. A grep over a line range that shows a name and a
component together is evidence of nothing, and it produced two wrong conclusions in one day: that
`CharactersButton` was wired to `BackToMenu` (it had no OnClick at all, exactly as *Known rough
edges* had said since September), and that the rewire had landed on it.

---

## The shelf runs, and the button art was backwards (2026-10-12)

Characters opens, the shelf scrolls, three blocks visible, cards and prices drawing. The wiring
fix landed.

Two things wrong, both found by **looking at the assets instead of their filenames**:

- `Group 26` is the green **EQUIPPED** button and `Group 26-1` is the gold **BUY** button. They
  were assigned the other way round from the filenames alone, which is why every locked card read
  EQUIPPED. Two Inspector drags to swap.
- `PEIXE BLOQUEADO` is a single generic locked card reused for all four locked blocks, so the
  shelf reads as four copies of one unknown fish.

The filename guess has now cost two rounds in two days -- the button art here, and `Group 26` /
`Group 26-1` before it. `Read` renders a PNG. Open the file.

### The price is in the art

`PEIXE BLOQUEADO.png` has **"Unlock with: 300" painted into it**, and `FishInventory.price` is a
separate 300 in the Inspector. Two copies of one number with nothing keeping them in step, which
is the rule the clean-code pass spent a whole session on. Change the field and every card still
promises 300 -- and the card is what the player believes.

Not resolved yet; it is a real decision about whether prices will ever differ per fish, and that
belongs to whoever sets the economy. Written up with both options in `SHOP-AGENDA.md`.

### `SHOP-AGENDA.md`

New doc at the project root, linked from `START-NEW-CHAT.md`. It carries the open shop work in the
order worth doing it in: the button swap, the invisible wallet, the silent failure when a player
cannot afford a fish, the price-in-two-places decision, per-fish locked art, and an empty table to
fill in with real coin totals before 300 is treated as final.

It exists because the shop is now several sessions of small decisions deep and `DEVLOG.md` is a
history, not a to-do list. The agenda is the living half; the log stays the record.

---

## PlayerPrefs are not where you look for them (2026-10-12)

A fish was bought by accident and needed locking back. Finding the save data took four attempts,
so it is written down.

In a **built** player, Unity puts PlayerPrefs under `HKCU\Software\<company>\<product>`. In the
**Editor** it does not: it uses

```
HKCU\Software\Unity\UnityEditor\<company>\<product>
```

For this project that is `...\Unity\UnityEditor\Mobile Game Project\Abyssal Fish`. The player path
does not exist at all yet, because nothing has been built since the keys were added. Every key name
is also suffixed with `_h<hash>`, so a search for a bare `Coins` finds nothing.

Found: `FishOwned = 3` (bitmask 11 -- starter plus fish 2), `FishEquipped = 1`, `Coins = 846`.
Set `FishOwned` to 1 and `FishEquipped` to 0. The 300 spent was left spent; 846 is still plenty to
test a purchase, and inventing a coin balance is not mine to do.

The reset procedure is in `SHOP-AGENDA.md` rather than here, because it will be needed repeatedly
while the shop is being tested. *Edit -> Clear All PlayerPrefs* is the wrong tool: it takes the
wallet and the best distance with it.

### The display was lying in both directions

The Corsair Chomper card showed full fish art with a **BUY** button, which is impossible from
`Refresh()` -- owned draws the fish, unowned draws BUY, never both. The explanation was the swapped
button sprites: the card was genuinely owned and equipped, and `equippedArt` was pointing at the
BUY graphic.

So while those two slots are crossed, an unowned card reads EQUIPPED and the equipped card reads
BUY. It presents as a buying-logic bug and it is a sprite in the wrong slot. Worth remembering
before debugging the purchase path on the strength of what the screen says.

The swap is still unsaved in the scene -- quite possibly done during Play mode, which throws edits
away on stop.

---

## Known rough edges

- `Play Button` wears Frame 12 (PLAY art) while acting as the restart. Frame 9 (RESTART) is unused and is probably what belongs there.
- Button positions were set by typing coordinates into the scene file, not by eye. They work but are not composed.
- Scoring is one point per rock, and both rows score, so points come roughly twice as fast as with pipes.
- `Rock.png` is unused and can be deleted.

---

## Backups

Restore points, newest first. All live next to `Assets`, never inside it — copies of scripts inside `Assets` cause "already contains a definition" compile errors.

Most folders also carry the `CHAT-HANDOFF.md` and `DEVLOG.md` of their moment. The rows name the project files only, so check the folder itself before assuming a doc is not in there.

| Folder | Holds |
|---|---|
| `2026-10-11_2331_inventory-generation-applied/` | `SampleScene.unity` — the exact state Unity left on quit, before the shelf was authored in |
| `2026-10-11_2301_before-characters-menu/` | All scripts, `DEVLOG.md` and the 9 inventory `.meta` files — before the fish shop and the Single-mode flip |
| `2026-10-11_1957_before-lure-additive-and-refactors/` | All 13 scripts, `SampleScene.unity`, the 5 rock prefabs and `DEVLOG.md` — before the lure went Additive and the three refactors landed |
| `2026-10-10_1516_before-clean-code-pass/` | All 13 scripts and `DEVLOG.md` — before the clean-code audit consolidated the camera helpers and closed the pace statics |
| `2026-10-10_0134_before-scroll-recycle-fix/` | `ScrollObject.cs`, `SpotLight2D.cs` and `START-NEW-CHAT.md` — before the background wrap was moved off a fixed threshold |
| `2026-10-10_0108_handoff-rename-and-starter/` | **Full restore point.** All 13 scripts, the scene and all three docs, with the lure script back to `SpotLight2D` |
| `2026-10-10_0101_before-script-rename-back/` | The lure script and both docs — before it went back to `SpotLight2D` |
| `2026-10-09_2103_handoff-darkness-pass-complete/` | **Full restore point.** All 13 scripts, `SampleScene.unity` and both docs, as left at the end of the 10-09 session |
| `2026-10-09_2008_before-darkness-pass/` | `SampleScene.unity`, the lure script and both docs — before the tap flare was clamped and the background tinted |
| `2026-10-09_1459_before-lure-light/` | `SpotLight2D.cs`, `GameManager.cs`, `PlayerControllerScript.cs` — before the lure keyed off the swim and cave depth |
| `2026-10-09_1021_before-blur-alpha-fix/` | `GameManager.cs` — before the blur texture dropped its alpha channel |
| `2026-10-09_1007_before-depth-darkening/` | `GameManager.cs` — before the cave darkened with distance |
| `2026-10-08_2341_before-sound-buttons/` | `SampleScene.unity` and both sprite metas — before the sound toggles and the darker light |
| `2026-10-08_2319_before-sound-toggle/` | `GameManager.cs` — before ToggleSound and the Muted flag |
| `2026-10-08_2230_before-divider-taper/` | `SampleScene.unity` and `DividerTaper.png.meta` — before the tapered divider and the wider row spacing |
| `2026-10-07_1825_before-statrow-layout/` | `SampleScene.unity` — before the stat row became a HorizontalLayoutGroup and the divider went in |
| `2026-10-07_1803_before-pause-blur/` | `GameManager.cs` — before the pause blur came back, this time by repeated halving |
| `2026-10-07_1630_before-gameover-layout/` | `SampleScene.unity` — before the panel rows went Middle-aligned and the buttons swapped |
| `2026-10-07_1248_before-gameover-own-buttons/` | `GameManager.cs` — before the game over panel stopped sharing the pause screen's Restart and Home. **Reconstructed, not copied:** the file was edited before the backup was taken, and git's copy is 101 lines behind. Verified by diff against the edited file |
| `2026-10-07_1108_before-canvas-grouping/` | `SampleScene.unity` — before `HUD` and `RunButtons` were added and the six elements moved in |
| `2026-10-07_1046_before-canvas-structure-independence/` | `GameManager.cs` — before the dim and sibling ordering stopped assuming a flat Canvas |
| `2026-10-07_0824_before-gameover-panel-authoring/` | `SampleScene.unity` — before the game over panel objects were written in |
| `2026-10-07_0743_before-game-over-screen/` | `GameManager.cs` — before the game over screen was wired |
| `2026-10-07_0011_before-remove-lead-in/` | All 12 scripts — before the lead-in went, and the only copy carrying the darker dim |
| `2026-10-05_0213_before-coin-counter-gating/` | `GameManager.cs` — before the coin counter was tied to the run |
| `2026-10-05_0205_before-distance-only/` | `GameManager.cs` — before the rock score came off the counter line |
| `2026-10-05_0157_before-counter-flicker-fix/` | `GameManager.cs` — before the two counter slots became one |
| `2026-10-05_0136_before-raycast-fix/` | `GameManager.cs` — before the HUD counters stopped swallowing taps |
| `2026-10-05_0115_before-distance-counter/` | `GameManager.cs` — before the distance counter was added |
| `2026-10-05_0044_before-comment-shorten/` | All 12 scripts — before comments were cut back and the four guard bugs fixed |
| `2026-10-05_0018_before-remove-get-ready/` | All 12 scripts — before the get-ready state was taken out |
| `2026-10-04_2359_before-comment-trim/` | All 12 scripts — before a comment and tooltip trim that was never applied |
| `2026-10-04_2323_before-lead-in/` | `MyObstacleSpawner.cs` — before `leadInSeconds` was added |
| `2026-10-04_2133_before-tmp-essentials/` | The whole `TextMesh Pro/` folder and `Graphics/Fonts/BBH_Hegarty/` — before TMP Essential Resources were reimported |
| `2026-10-04_1037_before-handoff-refresh/` | The two docs only |
| `2026-10-04_0843_before-shadow-fix/` | `ObstacleDrift.cs`, `PlayerControllerScript.cs` and `TrashSpin.cs` — before the two self-assignments left by the naming pass were fixed |
| `2026-10-04_0822_before-clean-code/` | All 12 scripts — before the clean-code naming pass |
| `2026-10-03_0247_before-drop-grace/` | `MyObstacleSpawner.cs` — before `graceSeconds` was deleted |
| `2026-10-03_0239_before-get-ready/` | `GameManager.cs` and `PlayerControllerScript.cs` — before the get-ready state existed |
| `2026-10-03_0232_before-trash-colliders/` | The 9 trash prefabs and `TrashSpawner.cs` — before the polygon colliders were refitted. The script is saved at `Assets/TrashSpawner.cs`, not under `My Scripts`, so restore it to the right folder |
| `2026-10-03_0221_before-margin-revert/` | `ObstacleDrift.cs` |
| `2026-10-03_0144_before-coin-wallgap/` | `CoinSpawner.cs` and `ObstacleDrift.cs` — before coins were kept clear of the rock faces |
| `2026-10-03_0000_before-brief-grace/` | `MyObstacleSpawner.cs` |
| `2026-10-02_2313_before-coin-current/` | `CoinSpawner.cs`, a `CoinSpawner.cs.pre-bump` copy, and `ObstacleDrift.cs` |
| `2026-10-02_2303_before-trash-forward/` | `ObstacleDrift.cs` and `TrashSpawner.cs` |
| `2026-10-02_2257_before-current-push/` | `GameManager.cs`, `ObstacleMovement.cs`, `PlayerControllerScript.cs` and `TrashSpawner.cs` — before the current could shove the fish |
| `2026-10-02_2240_before-vertical-only-drift/` | `CoinSpawner.cs`, `ObstacleDrift.cs` and `TrashSpawner.cs` |
| `2026-10-02_2232_before-currents/` | `TrashSpawner.cs` |
| `2026-10-02_2230_before-drift-revert/` | `CoinSpawner.cs`, `ObstacleDrift.cs` and `TrashSpawner.cs` |
| `2026-10-02_2213_before-adaptive-drift/` | `CoinSpawner.cs`, `ObstacleDrift.cs` and `TrashSpawner.cs` |
| `2026-10-02_2201_before-icon-inside/` | `GameManager.cs` — before the coin icon moved inside the counter's box |
| `2026-10-02_2156_before-coin-icon/` | `CoinSpawner.cs` and `GameManager.cs` — before the coin icon was drawn in code |
| `2026-10-02_2142_before-coins/` | `GameManager.cs` — before the coin wallet |
| `2026-10-02_2051_before-coin-prefab/` | `Graphics/Coins/Frame 15.png.meta` — before the coin sprite's import settings changed |
| `2026-09-30_2000_before-remove-grace/` | `MyObstacleSpawner.cs` |
| `2026-09-29_1835_before-dim-pause-button/` | `GameManager.cs` |
| `2026-09-29_1828_before-pause-dim/` | `GameManager.cs` — before the pause backdrop |
| `2026-09-29_1814_before-cave-gap-fix/` | `MyObstacleSpawner.cs` |
| `2026-09-25_1211_before-grace-period/` | `MyObstacleSpawner.cs` — before `graceSeconds` was first added |
| `2026-09-25_1152_before-fish-center/` | `GameManager.cs` — before the start position became a fraction of the screen width |
| `2026-09-25_1112_before-pause-button-lock/` | `GameManager.cs` |
| `2026-09-25_1053_before-handoff-fixes/` | `CHAT-HANDOFF.md`, `DEVLOG.md` and `GameManager.cs` — before the handoff review corrections and the TrashSpawner null guards |
| `2026-09-24_1501_before-bg-anchor/` | `ScrollObject.cs` — before the row was pulled to the screen's left edge |
| `2026-09-24_1259_before-android-export/` | `GameManager.cs`, `ProjectSettings.asset` and `CHAT-HANDOFF.md` — before the Android player settings were switched |
| `2026-09-24_0109_before-remove-wobble/` | All 10 scripts — before drift was taken back off the rocks |
| `2026-09-24_0053_before-rock-drift/` | All scripts and the scene — before rocks began wandering |
| `2026-09-24_0043_before-trash-wiring/` | `SampleScene.unity` — before TrashSpawner was wired onto SpawnObstacles |
| `2026-09-24_0019_before-trash-debris/` | All 7 scripts and the scene — before the floating trash system |
| `2026-09-24_0002_before-aspect-locks/` | `SampleScene.unity` — before AspectRatioFitters were added to the 7 UI elements |
| `2026-09-23_2356_before-resume-button-build/` | `SampleScene.unity` — before ResumeButton was authored into the scene |
| `2026-09-23_2345_before-resume-button/` | `GameManager.cs` and scene — before the resume button existed |
| `2026-09-23_2256_before-ui-normalize/` | `SampleScene.unity` — before UI sizes were normalized and aspect-locked |
| `2026-09-23_2232_before-fall-death/` | `PlayerHit.cs` and `CHAT-HANDOFF.md` — before the fish could die by falling off screen |
| `2026-09-23_0410_before-score-visibility/` | `GameManager.cs` and the 5 rock prefabs — before the counter was shown by code |
| `2026-09-22_2100_before-scroll-align/` | `ScrollObject.cs` — before copies were aligned and auto-created |
| `2026-09-22_2010_before-gap-tuning/` | All 7 scripts — before rocks were made flush and the gap tightened |
| `2026-09-22_1950_before-rock-spawner/` | All scripts, scene, rock prefabs, untrimmed rock PNGs and DEVLOG — before the rock rows |
| `2026-09-22_1935_before-rock-prefabs/` | Rock import settings and `Obstacles.prefab` (the pipe version) |
| `2026-09-22_1400_before-rock-obstacles/` | Obstacle prefab, scene and all scripts — before the first rock |
| `2026-09-22_1140_before-new-background/` | Background import settings, scene and `ScrollObject.cs` |
| `2026-09-20_2240_before-score-fixes/` | All 7 scripts, scene, prefab and DEVLOG — before the score and guard fixes |
| `2026-09-20_2230_before-home-pause-ui/` | Scene, before HomeButton and the pause conversion |
| `2026-09-20_2215_before-pause-home/` | GameManager and PlayerControllerScript |
| `2026-09-20_2205_before-play-gate/` | The 3 scripts, before the game was gated behind PLAY |
| `2026-09-20_2155_before-revert/` | The menu-era scripts, kept when reverting to the 09-19 originals |
| `2026-09-20_2105_before-title-and-pause/` | Scene, before the title image |
| `2026-09-20_2055_before-menu-sprites/` | Scene, before sprites were assigned |
| `2026-09-20_2050_before-menu-buttons/` | Scene, before the first menu buttons |
| `2026-09-20_2045_before-mainmenu-stretch/` | Scene, before MainMenu was stretched |
| `2026-09-19_1916_before-underwater-physics/` | Scripts, scene and prefab — the last pre-menu state |

Note that `2026-09-19_1916` predates the underwater physics work. Its `PlayerControllerScript.cs` is the old version, so don't restore that one file from it without checking.
