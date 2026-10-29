# Abyssal Fish

**A one-tap swimming game for Android.**

You are an anglerfish, deep down in the sea. You carry the only light.

You swim into a cave. The cave gets darker and tighter the further you go.

Grab the coins. Touch anything else and your run is over.

Made by **ABYSSAL TEAM** for the **EUAS — Mobile Game Development** course.

> **This game is not finished.** It is a student project. It is free, it is not
> built to run fast, and a lot of it was written with help from AI.
>
> **It does not collect anything about you.** No internet. No account. No ads.
> No tracking. The game shows you its full Terms of Use and Privacy Policy the
> first time you open it, before you can play.

---

## How to play

| What you do | What happens |
|---|---|
| Tap anywhere | The fish swims up |
| Nothing | The fish sinks |

That is the whole game. There is no tutorial, because there is nothing else to
learn.

---

## What makes it hard

- **It gets dark.** Your light is the only one down there, and it fades as you
  go deeper.
- **It gets tight.** The gap between the rocks shrinks the further you get.
- **The water pushes you.** Currents shove you sideways, usually at the worst
  moment.
- **Rubbish floats past.** Old junk drifts through the cave and spins as it
  goes. Touching it ends your run.

Coins are the only thing that is safe to touch. Save them up and spend them on
new fish in the shop.

---

## Try it yourself

You need **Unity** first. That is the free program this game was built in.

1. Install **Unity 6000.3.8f1**. Tick **Android Build Support** while you install it.
2. Open Unity Hub and add this folder.
3. Open the file `Assets/Scenes/SampleScene.unity`.
4. Press **Play**.

On a computer, the space bar, a mouse click and a tap all swim. They do the
same thing.

To put the game on a phone, choose **File → Build Settings → Android → Build**.
The settings are already filled in for you.

---

## How far along it is

**All of this works:** the menu, a run, pausing, the game-over screen and the
fish shop. Your coins are saved, so they are still there the next time you open
the game. There are five fish you can buy and wear. The Terms and Privacy
screens show up the first time you play. It has been tested on a real phone,
not just on a computer.

**These parts are not done:**

- The shop has a sixth slot with no fish drawn for it yet. You can buy it for
  300 coins, but you just get the normal fish back. It needs a picture, or it
  needs taking out.
- The pictures are not packed together yet, so the game works harder to draw
  them than it needs to.
- It has only been tested on one phone. More phones still need trying.

---

## For developers

Everything below is for anyone who wants to build the game or read the code.

### What is in here

| Folder | What it holds |
|---|---|
| `Assets/My Scripts/` | All 17 gameplay scripts — the whole game logic, 1,880 lines of code. |
| `Assets/Editor/` | A Tools menu for resetting the save while testing. |
| `Assets/Graphics/` | 57 sprites — fish, rocks, trash, backgrounds, UI. |
| `Assets/Music/` | 5 audio clips. |
| `Assets/Resources/Legal/` | The Terms and Privacy text shown in-game. |
| `Assets/Scenes/` | `SampleScene.unity` — the entire game is one scene. |
| `Tools/` | Python scripts that edit the scene file safely while Unity is closed. |
| `docs/` | The written history of the project. Start with `docs/START-NEW-CHAT.md`. |

### How the game is put together

The whole game is **one scene**. Nothing loads, nothing streams.

**One shared speed.** `ObstacleMovement` holds a single number for how fast the
cave is moving. Rocks, trash and coins all read that same number, so none of
them can drift out of step with the others. `TrashSpawner` owns that number and
is the only thing that changes it.

**One shared depth.** `GameManager` publishes how deep the run is, as a value
from 0 to 1. The cave light dims on it *and* the gap between the rocks narrows
on it. One signal the player reads, rather than two systems to balance against
each other.

**The cave builds itself ahead of you.** `CaveSpawner` keeps two rows of
rocks filled out past the right edge, each rock measured against the opposite
row so the gap never closes up. Spawners ask it where the gap is before
dropping anything into the cave.

| Script | Job |
|---|---|
| `GameManager` | Menus, runs, score, coins, sound, cave lighting |
| `PlayerController` | The fish: input, swimming, tilt, skins |
| `PlayerHit` | Ends the run — a bump, or sinking off screen |
| `CaveSpawner` | Builds the two rock walls and owns the gap |
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

### Build settings

| Setting | Value | Why |
|---|---|---|
| Engine | Unity 6000.3.8f1, 2D URP | — |
| Package | `com.emmanuelokome.abyssalfish` | — |
| Version | 1.7 (version code 2) | — |
| Min SDK | 25 (Android 7.1) | Covers almost every device still in use |
| Target SDK | 35 (Android 15) | Meets the Play Store requirement |
| Architecture | ARM64 only | Required by Play; keeps the APK small |
| Output | APK | Switch to AAB only if publishing to Play |
| Orientation | Portrait, 9:16 | — |

### Working on the scene safely

Two rules this project learned the hard way:

1. **Never edit the scene file while the Unity Editor is open.** Unity holds the
   scene in memory and will overwrite your changes when it saves. Close the
   Editor first, or make the change in the Inspector instead.
2. **A value saved in the scene always beats the default written in a script.**
   Changing a `[SerializeField]` default in code moves nothing if the scene
   already has a value for it.

`Tools/verify_scene.py` checks the scene's structure and button wiring:

```
python Tools/verify_scene.py Assets/Scenes/SampleScene.unity
```

### The documents

Everything written during development lives in `docs/`. They cross-reference
each other, so they are kept together in one folder.

| File | What it is |
|---|---|
| `docs/START-NEW-CHAT.md` | **Read this first.** The short orientation. |
| `docs/DEVLOG.md` | The full history and the reasoning behind every decision. Large. |
| `docs/CHAT-HANDOFF.md` | A longer catch-up document than START-NEW-CHAT. |
| `docs/SHOP-AGENDA.md` | What is still open on the fish shop. |
| `docs/SESSION-LOG-2026-10-12_13.md` | What was done on those days, and why. |
| `docs/SESSION-LOG-2026-10-14_15.md` | The same, for the following sessions. |
| `docs/SESSION-LOG-2026-10-26_29.md` | The same again — swim tuning, the repo, Android settings, the failed APK build and the pause menu. |
| `docs/PRESENTATION.md` | Source text for the course presentation deck. |

`CLAUDE.md` and `AGENTS.md` sit in the root rather than in `docs/` because AI
coding tools read them from there by name. They hold the project's working rules
and are identical to each other.

---

## Licence

No licence granted. This is coursework, shared so it can be read and marked.
Please do not redistribute it as your own.
