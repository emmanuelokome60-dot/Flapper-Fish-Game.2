#!/usr/bin/env python3
"""Check the numbers in docs/CODE-EXPLAINED.md against the saved scene.

    python docs/tools/check-numbers.py

The saved scene always beats a script default, so every tuned number in the doc
has to come from SampleScene.unity. Run this after tuning anything in Unity: it
reports every setting where the script and the scene disagree, and whether the
doc is quoting the scene's value.

Exit code 1 if the doc is quoting a number the game does not use.
"""
import glob
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
SCENE = os.path.join(ROOT, "Assets", "Scenes", "SampleScene.unity")
DOC = os.path.join(ROOT, "docs", "CODE-EXPLAINED.md")

FIELD = re.compile(
    r"\[SerializeField\]\s*(?:private|public|protected)?\s*"
    r"[\w\.\<\>\[\]\?]+\s+(\w+)\s*(?:=\s*([^;]+))?;")


def read(path):
    with open(path, encoding="utf-8", errors="replace") as handle:
        return handle.read()


def number(text):
    text = (text or "").strip().rstrip("f")
    try:
        return float(text)
    except ValueError:
        return None


def unity_label(field):
    """swimForce -> Swim Force, the way Unity shows it in the Inspector."""
    spaced = re.sub(r"(?<!^)(?=[A-Z])", " ", field)
    return " ".join(w.capitalize() if w.islower() else w for w in spaced.split())


def script_defaults():
    """{script name: {field: default}} from the .cs files."""
    out = {}
    for path in glob.glob(os.path.join(ROOT, "Assets", "**", "*.cs"), recursive=True):
        found = {m.group(1): (m.group(2) or "").strip()
                 for m in FIELD.finditer(read(path))}
        if found:
            out[os.path.basename(path)[:-3]] = found
    return out


def scene_values():
    """{script name: {field: value}} from the saved scene."""
    guids = {}
    for meta in glob.glob(os.path.join(ROOT, "Assets", "**", "*.cs.meta"), recursive=True):
        found = re.search(r"^guid:\s*([0-9a-f]+)", read(meta), re.M)
        if found:
            guids[found.group(1)] = os.path.basename(meta)[:-8]

    out = {}
    for document in re.split(r"^--- ", read(SCENE), flags=re.M):
        if not document.startswith("!u!114"):
            continue
        guid = re.search(r"m_Script:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+)", document)
        if not guid or guid.group(1) not in guids:
            continue
        fields = {}
        for line in document.splitlines():
            pair = re.match(r"^  ([A-Za-z_]\w*):\s*(.*)$", line)
            if pair and not pair.group(1).startswith("m_"):
                fields[pair.group(1)] = pair.group(2).strip()
        out.setdefault(guids[guid.group(1)], {}).update(fields)
    return out


def main():
    defaults, live = script_defaults(), scene_values()
    doc = read(DOC) if os.path.exists(DOC) else ""

    mismatches, stale = [], []
    for script in sorted(defaults):
        for field, default in defaults[script].items():
            in_script = number(default)
            in_scene = number(live.get(script, {}).get(field))
            if in_script is None or in_scene is None:
                continue
            if abs(in_script - in_scene) < 1e-9:
                continue

            shown = "%g" % in_scene
            mismatches.append((script, unity_label(field), "%g" % in_script, shown))
            if doc and shown not in doc:
                stale.append((script, unity_label(field), shown))

    width = max((len(m[1]) for m in mismatches), default=10)
    current = None
    for script, label, in_script, in_scene in mismatches:
        if script != current:
            print("\n" + script)
            current = script
        print("  %-*s  script %-7s  game uses %s" % (width, label, in_script, in_scene))

    print("\n%d settings differ between the scripts and the scene." % len(mismatches))

    if not doc:
        print("No %s to check against." % os.path.basename(DOC))
        return 0

    if stale:
        print("\nThe doc does not quote the scene value for:")
        for script, label, in_scene in stale:
            print("  %s.%s should say %s" % (script, label, in_scene))
        return 1

    print("All %d are quoted correctly in %s." % (len(mismatches), os.path.basename(DOC)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
