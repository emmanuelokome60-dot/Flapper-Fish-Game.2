# Flapper Fish Game.2 — Development Log

Unity 6.3 LTS (6000.3.8f1) · URP 2D · New Input System only
Last updated: 2026-09-25

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

---

## Known rough edges

- `Play Button` wears Frame 12 (PLAY art) while acting as the restart. Frame 9 (RESTART) is unused and is probably what belongs there.
- `CharactersButton` is visible on the menu but has no OnClick, so it silently does nothing.
- Button positions were set by typing coordinates into the scene file, not by eye. They work but are not composed.
- Scoring is one point per rock, and both rows score, so points come roughly twice as fast as with pipes.
- `Rock.png` is unused and can be deleted.

---

## Backups

Restore points, newest first. All live next to `Assets`, never inside it — copies of scripts inside `Assets` cause "already contains a definition" compile errors.

Most folders also carry the `CHAT-HANDOFF.md` and `DEVLOG.md` of their moment. The rows name the project files only, so check the folder itself before assuming a doc is not in there.

| Folder | Holds |
|---|---|
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
