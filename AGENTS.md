# Flapper Fish — project rules

## Replies
- Answer in lists. No long prose paragraphs.
- Short lines, concrete numbers, no restating context.

## Unity
- Never patch scene YAML while the Unity Editor is open. Inspector edits instead.
- The saved scene always beats a script default. Changing a `[SerializeField]` default moves nothing.
- Back up to `Backups/<timestamp>_<what>/` before any destructive change.
