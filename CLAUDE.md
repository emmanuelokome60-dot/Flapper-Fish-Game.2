# Flapper Fish — project rules

## Replies
- Answer in lists. No long prose paragraphs.
- Short lines, concrete numbers, no restating context.

## Unity
- Never patch scene YAML while the Unity Editor is open. Inspector edits instead.
- The saved scene always beats a script default. Changing a `[SerializeField]` default moves nothing.
- Back up to `Backups/<timestamp>_<what>/` before any destructive change.
- Renaming a script? Move the `.cs.meta` with it. The GUID is the scene's only link.

## Comments
Follow `Documents/Claude Code/clean-code-guidelines.txt` rule 6. Target reading age 9–10.
- One line per method saying what it does **in the game**, not what the code does.
  "The fish is dead." not "Ends the run." Never just repeat the method name.
- Keep a "why" note only where deleting it could bring a real bug back. Two lines max.
- Max ~12 words a line, ~75 characters. Plain words, present tense, active voice.
- Class overview at the top of each file: 2–3 short lines.
- Leave `[Tooltip]` and `[Header]` text alone — that is Inspector UI, not comments.
- Measured on 30 Oct 2026 at Flesch–Kincaid 4.9. Check with the script in the session log.
