# Abyssal Fish

A one-tap underwater swimmer for Android. You are an anglerfish with a single light source on you,
swimming deeper into a cave that gets darker and narrower the further you go. Dodge the rocks and
the drifting trash, collect only the coins, and ride out the currents that shove you sideways when
you can least afford it.

Built by **ABYSSAL TEAM** for the **EUAS — Mobile Game Development** course.

> **This is an unfinished student project.** It is free, it is not optimised, and a large part of
> it was written with AI assistance. The game shows its full Terms of Use and Privacy Policy on
> first launch, before you can play. It collects nothing — no internet, no accounts, no ads, no
> tracking.

---

## How to play

| Input | Action |
|---|---|
| Tap anywhere | Swim up |
| Do nothing | Sink slowly |

Everything except coins ends the run. There is no tutorial because there is nothing else to learn.

---

## Running it

1. Install **Unity 6000.3.8f1** (Unity 6.3 LTS) with the Android Build Support module.
2. Open this folder from Unity Hub.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press Play. Space, a mouse click or a tap all swim.

**To build the APK:** File → Build Settings → Android → Build. The player settings are already
configured (see the table further down).

---

## What is in here

| Folder | What it holds |
|---|---|
| `Assets/My Scripts/` | All 17 gameplay scripts. The whole game logic, 1,880 lines. |
| `Assets/Editor/` | A Tools menu for resetting the save while testing. |
| `Assets/Graphics/` | 58 sprites — fish, rocks, trash, backgrounds, UI. |
| `Assets/Music/` | 5 audio clips. |
| `Assets/Resources/Legal/` | The Terms and Privacy text shown in-game. |
| `Assets/Scenes/` | `SampleScene.unity` — the entire game is one scene. |
| `Tools/` | Python scripts that edit the scene file safely while Unity is closed. |
| `docs/` | The written history of the project. Start with `docs/START-NEW-CHAT.md`. |

---

## How the game is put together

The whole game is **one scene**. Nothing loads, nothing streams.

**One shared speed.** `ObstacleMovement` holds a single number for how fast the cave is moving.
Rocks, trash and coins all read that same number, so none of them can drift out of step with the
others. `TrashSpawner` owns that number and is the only thing that changes it.

**One shared depth.** `GameManager` publishes how deep the run is, as a value from 0 to 1. The
cave light dims on it *and* the gap between the rocks narrows on it. One signal the player reads,
rather than two systems to balance against each other.

**The cave builds itself ahead of you.** `ObstacleSpawner` keeps two rows of rocks filled out past
the right edge, each rock measured against the opposite row so the gap never closes up. Spawners
ask it where the gap is before dropping anything into the cave.

| Script | Job |
|---|---|
| `GameManager` | Menus, runs, score, coins, sound, cave lighting |
| `PlayerController` | The fish: input, swimming, tilt, skins |
| `PlayerHit` | Ends the run — a bump, or sinking off screen |
| `ObstacleSpawner` | Builds the two rock walls and owns the gap |
| `ObstacleMovement` | Slides one object left, then deletes it |
| `TrashSpawner` | Drops trash, and drives the whole difficulty curve |
| `CoinSpawner` / `CoinPickup` | Drops and collects coins |
| `ObstacleDrift` / `TrashSpin` | Makes debris float and turn |
| `ScrollObject` | The endless scrolling background |
| `SpotLight2D` | The lamp on the fish's head |
| `PauseBlur` | The frozen, blurred pause backdrop |
| `FishInventory` | The fish shop |
| `LegalNotice` | The Terms and Privacy screens |
| `CameraEdges` | Where the screen edges are |
| `ButtonPressSound` | Click on finger-down, not finger-up |

---

## Build settings

| Setting | Value | Why |
|---|---|---|
| Engine | Unity 6000.3.8f1, 2D URP | — |
| Package | `com.emmanuelokome.abyssalfish` | — |
| Version | 1.6 (version code 2) | — |
| Min SDK | 25 (Android 7.1) | Covers almost every device still in use |
| Target SDK | 35 (Android 15) | Meets the Play Store requirement |
| Architecture | ARM64 only | Required by Play; keeps the APK small |
| Output | APK | Switch to AAB only if publishing to Play |
| Orientation | Portrait, 9:16 | — |

---

## Where the project stands

**Working end to end:** menu → run → pause → game over → fish shop. Coins persist between runs and
sessions. Five fish skins, buyable and wearable. Terms and Privacy gate on first launch. Tuned and
tested on a real phone, not just in the Editor.

**Known gaps:**

- The shop has a sixth slot with no artwork behind it. It can be bought for 300 coins and gives
  back the default fish. Needs art, or the slot removing.
- No sprite atlas yet, so the game issues more draw calls than it needs to.
- Tested on one phone. Wider device testing still to do.

---

## The documents

Everything written during development lives in `docs/`. They cross-reference each other, so
they are kept together in one folder.

| File | What it is |
|---|---|
| `docs/START-NEW-CHAT.md` | **Read this first.** The short orientation. |
| `docs/DEVLOG.md` | The full history and the reasoning behind every decision. Large. |
| `docs/CHAT-HANDOFF.md` | A longer catch-up document than START-NEW-CHAT. |
| `docs/SHOP-AGENDA.md` | What is still open on the fish shop. |
| `docs/SESSION-LOG-2026-10-12_13.md` | What was done on those days, and why. |
| `docs/SESSION-LOG-2026-10-14_15.md` | The same, for the following sessions. |
| `docs/PRESENTATION.md` | Source text for the course presentation deck. |

`CLAUDE.md` and `AGENTS.md` sit in the root rather than in `docs/` because AI coding tools read
them from there by name. They hold the project's working rules and are identical to each other.

---

## Working on the scene safely

Two rules this project learned the hard way:

1. **Never edit the scene file while the Unity Editor is open.** Unity holds the scene in memory
   and will overwrite your changes when it saves. Close the Editor first, or make the change in
   the Inspector instead.
2. **A value saved in the scene always beats the default written in a script.** Changing a
   `[SerializeField]` default in code moves nothing if the scene already has a value for it.

`Tools/verify_scene.py` checks the scene's structure and button wiring:

```
python Tools/verify_scene.py Assets/Scenes/SampleScene.unity
```

---

## Licence

No licence granted. This is coursework, shared so it can be read and marked. Please do not
redistribute it as your own.
