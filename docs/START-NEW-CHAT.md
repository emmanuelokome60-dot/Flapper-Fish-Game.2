# Start here — Flapper Fish Game.2

Paste this file into a new chat. It is the short version on purpose; it points at the two
documents that hold the detail rather than repeating them, so there is nothing here to drift
out of date.

Unity 6.3 LTS (6000.3.8f1) · URP 2D · New Input System only · Android, landscape only
Written 2026-10-10.

---

## Read these first

| File | What it is |
|---|---|
| `DEVLOG.md` | Every decision and why, newest entries at the bottom before *Known rough edges*. The real memory of this project. |
| `CHAT-HANDOFF.md` | Current state, the lighting model, scripts, scene setup, Android notes. |
| `SHOP-AGENDA.md` | The fish shop: what works, what is open, and the order to do it in. Live. |

**This project has no useful git history** — the branches are years behind and must never be
merged. `Backups/` holds timestamped folders and is the only real undo. Make one before editing
anything, and there is a table of every restore point at the bottom of `DEVLOG.md`.

---

## The three traps this project keeps setting

1. **Unity is usually open, and `SampleScene.unity` cannot be edited on disk while it is.**
   Unity's in-memory copy overwrites the file on its next save. Scripts, prefabs and `.meta`
   files are safe to edit any time; the scene is not. Check for a running editor before touching
   it, and ask for it to be closed.
2. **A value saved in the scene beats a script default.** Editing `= 5f` in a script changes
   nothing for an object that already exists. Decide per value: serialize it and tune in the
   Inspector, or keep it `const` and tune in code — never both. To push a new default onto an
   existing object use the component's gear menu → **Reset**, not a field rename.
   A newly added `[SerializeField]` can also be written as `default(T)`, so check the Inspector
   after adding one.
   **This trap catches documents too.** Any tuned number copied into a `.md` file is a second
   copy that nothing keeps in step. Name the field, not its setting.
3. **Imported sprites arrive as `spriteMode: 2` (Multiple)** and will not drop into a UI Image
   slot until switched to Single. This has caught four assets; treat it as the default.

Compile without opening Unity: `dotnet build Assembly-CSharp.csproj` — catches errors and
warnings in seconds.

---

## Where things stand

**Nothing since the 2026-09-24 Android APK has run on a phone.** Two and a half weeks of work now
sits behind that, and it is the single biggest open risk.

Recently added: the game over panel; the Canvas grouped into HUD / MainMenu / GameOverMenu /
Pause Buttons; a pause blur; a sound toggle; and depth-based cave darkening with a lure light on
the fish.

Fixed 2026-10-10: `ScrollObject` was recycling a background copy while part of it was still on
screen, which left a bare strip at the left edge for about a second on every wrap. It scaled
with aspect ratio, so it was worst on exactly the device nobody has tested. See `DEVLOG.md`.

### Do these first

1. **Open Unity and check the Console.** The scene and scripts were last edited with the editor
   closed. `Spot Light 2D` is not the worry it looks like — its script GUID resolves; only the
   scene's cached class name is stale, and Unity rewrites that on its next save.
2. **Build an APK and test on a phone.** The lighting especially: `Deep Light` on a desktop
   monitor in a dark room is nothing like `Deep Light` on a handset outdoors. Watch the left
   edge of the background too — that is where today's fix has to hold, and a phone is wider
   than the Editor's default aspect.
3. **Decide `Darkest At Distance`.** `DEVLOG.md` settled on 700 and the scene still holds 400,
   so a reasoned decision never landed. It is one number in `GameManager`'s Inspector. See the
   lighting section at the bottom.

### Known rough edges worth knowing up front

- Pause and game over disagree on button order: game over is HOME-left / RESTART-right, pause is
  the mirror. Three coordinates to fix.
- The project has **no audio at all**. The mute toggle exists and drives `AudioListener.volume`,
  so anything added later is covered with no extra wiring.
- A teammate is building a **light particle system** on a copy of the project. Ask for it as a
  **prefab in a `.unitypackage`** with Include dependencies — never a zipped folder (the `.meta`
  files carry the GUIDs) and never their `SampleScene.unity`.
- `BG_1` is inert. It is inactive in the scene, and `ScrollObject` clones `Bg_0` at runtime and
  skips inactive siblings, so `BG_1` never reaches the screen. Tint `Bg_0` only.
- Four smaller findings are parked at the bottom of `DEVLOG.md` rather than fixed: a dead score
  counter, two unguarded Inspector slots, and `Camera.main` called per obstacle per frame.

---

## The lighting model, because it has two owners

- **`Global Light 2D` is driven from code.** `GameManager.ApplyCaveDepth()` lerps it from
  `Start Light` to `Deep Light` across `Darkest At Distance`. The Light 2D's own Intensity
  slider does nothing at runtime — tune `Start Light` instead.
- **The fish's `Spot Light 2D` is the lure**, driven by the `SpotLight2D` script. It rests
  between `Base Intensity` and `Base When Deep` by the same depth and flares on each swim,
  clamped to one boost so mashing the screen cannot wash the cave out.
- **`GameManager.CaveDepth`** (0 to 1) is the single depth number both lights read.
- The background sprite carries a tint *as well*, so its brightness is light × tint. That
  multiply is what made it muddy.
- **The five numbers live in the Inspector, not here.** Read them off `GameManager` and the
  lure. An earlier version of this file quoted them and four had already drifted — one of them
  by 1.75×, which quietly inverted the paragraph below. What does not drift is the shape:
  five fields, one depth number, two owners.

**The lure cannot be the only light.** The lure's outer radius is 4.9 units, and obstacles close
at `speed` × `ObstacleMovement.SpeedMultiplier` — 8 u/s at full ramp, and 10.4 u/s inside a
surge, which `TrashSpawner` warns about in a comment. That is 0.61 s of warning, or 0.47 s
mid-surge, against a reaction time of about 0.25 s. The global light is what makes rock
silhouettes readable, so it is a fairness mechanism and not atmosphere — if a death feels
unfair, raise `Deep Light`, never the lure.

**The cave goes fully dark before the game reaches full speed.** At the values saved on
2026-10-10, `Darkest At Distance` is reached at roughly 75 s, about 15 s before `TrashSpawner`'s
90 s difficulty ramp finishes — so the hardest, fastest part of every run happens at
`Deep Light`. Two independent ramps landed on top of each other; nobody chose it.

**This one is an open decision, not a fact.** `DEVLOG.md` works the convergence through and
settles on raising `Darkest At Distance` to **700** to pull the two ramps apart. That change was
never applied — the scene still holds 400, and a saved value beats a script default (trap 2), so
it has to be typed into the Inspector on `GameManager`. Decide it deliberately: 700 is the
documented intent, 400 is what every recent play session actually felt like.
