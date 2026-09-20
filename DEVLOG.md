# Flapper Fish Game.2 — Development Log

Unity 6.3 LTS (6000.3.8f1) · URP 2D · New Input System only
Last updated: 2026-09-20

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
| `PlayerHit.cs` | Calls Game Over when the fish hits something |
| `GameManager.cs` | Singleton, game over, restart, clears obstacles, AddScore |
| `MyObstacleSpawner.cs` | Spawns obstacles on a timer as its own children |
| `ObstacleMovement.cs` | Moves obstacles left, deletes them off screen |
| `ScoreTrigger.cs` | Scores when the fish passes through a gap |
| `ScrollObject.cs` | Endless scrolling background |

### Scene setup (`Assets/Scenes/SampleScene.unity`)

- **Player_Fish** — Rigidbody 2D (Dynamic), Circle Collider 2D, `PlayerControllerScript`, `PlayerHit`. Starts at X = -7.
- **GameManager** — `GameManager`, with Game Over Text, Play Button, Player and Obstacle Spawner all filled in.
- **SpawnObstacles** — `MyObstacleSpawner`, at X = 10. Spawned obstacles become its children.
- **Background** — background copies with `ScrollObject`.
- **Ground** — Box Collider 2D.
- **Canvas** — GameOver Text, Play Button (anglerfish image), EventSystem using the Input System UI module.
- **Prefabs/Obstacles.prefab** — Top_Obs and Bottom_Obs at Y 3.8 / -3.8 with solid colliders, plus ScoreZone with a trigger collider and `ScoreTrigger`.

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

## Where the tutorial is up to

Followed to roughly **1:15:00**. The code is ahead of the video in places and deliberately behind in others.

Done: scrolling background, obstacle spawning and movement, collision and game over, restart, score trigger, real scoring with the counter, the score reset, and the menu/pause/home flow above.

**Not done:** `HighScore` is declared in `GameManager` but nothing reads or writes it. The tutorial implements it properly with `PlayerPrefs` between 1:11:34 and 1:12:55 — read once in `Awake()`, then compared and saved in an `UpdateHighScore()` called from `GameOver()`. The two game-over labels (`CurrentScore_Label`, `BestScore_Label`) already exist in the scene but are unused.

Also not done: sound effects, and `CharactersButton` does nothing.

**Next:** a Resume button for the pause screen.

---

## Known rough edges

- `Play Button` wears Frame 12 (PLAY art) while acting as the restart. Frame 9 (RESTART) is unused and is probably what belongs there.
- `CharactersButton` is visible on the menu but has no OnClick, so it silently does nothing.
- There is no dimmed overlay behind the pause screen. The game just freezes with buttons over it.
- Button positions were set by typing coordinates into the scene file, not by eye. They work but are not composed.

---

## Backups

Restore points, newest first. All live next to `Assets`, never inside it — copies of scripts inside `Assets` cause "already contains a definition" compile errors.

| Folder | Holds |
|---|---|
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
