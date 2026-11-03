#!/usr/bin/env python3
"""Check the numbers in docs/CODE-EXPLAINED.md against the saved scene.

    python docs/tools/check-numbers.py

The saved scene always beats a script default, so every tuned number in the doc
has to come from SampleScene.unity. Run this after tuning anything in Unity: it
reports every setting where the script and the scene disagree, and whether the
doc is quoting the scene's value.

Each value is matched to its OWN setting, by name, in both the per-script table
and the one in Part 3. Asking only whether a number appears somewhere in the
document is not enough: Start Light drifted from 0.03 to 0.05 and went
unreported, because 0.05 was already in the doc as Movement Pop Volume.

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

# Part 3's table: | Script | Setting | script value | **game value** |
DOC_ROW = re.compile(
    r"^\|\s*(\w+)\s*\|\s*([\w /]+?)\s*\|\s*(-?[\d.]+)\s*\|\s*\*\*(-?[\d.]+)\*\*\s*\|")


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


def doc_rows(doc):
    """{(script, setting): (script value, game value)} from Part 3's table."""
    out = {}
    for line in doc.split("\n"):
        found = DOC_ROW.match(line)
        if found:
            out[(found.group(1), found.group(2))] = (found.group(3), found.group(4))
    return out


def body_value(doc, label):
    """The bold value beside **Label** in a per-script table, if it has a row.

    Settings that share a row with another, like "Drift Min / Max Speed", have
    no row of their own. Those return None and rest on Part 3's table.
    """
    found = re.search(r"\|\s*\*\*%s\*\*\s*\|\s*\*\*([^*]+)\*\*" % re.escape(label), doc)
    return found.group(1).strip() if found else None


def main():
    defaults, live = script_defaults(), scene_values()
    doc = read(DOC) if os.path.exists(DOC) else ""
    rows = doc_rows(doc)

    mismatches, stale = [], []
    for script in sorted(defaults):
        for field, default in defaults[script].items():
            in_script = number(default)
            in_scene = number(live.get(script, {}).get(field))
            if in_script is None or in_scene is None:
                continue
            if abs(in_script - in_scene) < 1e-9:
                continue

            shown, was = "%g" % in_scene, "%g" % in_script
            label = unity_label(field)
            mismatches.append((script, label, was, shown))
            if not doc:
                continue

            # Checked against this setting's OWN row, not the whole file. The
            # same number turning up beside some other setting is not evidence.
            quoted = rows.get((script, label))
            if quoted is None:
                stale.append((script, label, shown, "missing from the Part 3 table"))
            elif quoted != (was, shown):
                stale.append((script, label, shown,
                              "Part 3 says %s / %s" % quoted))
            else:
                beside = body_value(doc, label)
                if beside is not None and beside != shown:
                    stale.append((script, label, shown,
                                  "its own table says %s" % beside))

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
        print("\nThe doc does not quote the game's value for:")
        for script, label, in_scene, why in stale:
            print("  %s.%s should say %s -- %s" % (script, label, in_scene, why))
        return 1

    print("All %d are quoted correctly in %s." % (len(mismatches), os.path.basename(DOC)))
    return 0


def self_check():
    """The parsing is the whole point, so prove it before trusting a pass.

    The bug this guards against: the old version asked whether the number
    appeared anywhere in the document. Start Light drifted to 0.05 and it
    reported no problem, because 0.05 was sitting beside Movement Pop Volume.
    """
    doc = ("| **Start Light** | **0.03** (script 0.35) | How bright. |\n"
           "| **Movement Pop Volume** | **0.05** (script 0.35) | The pop. |\n"
           "| GameManager | Start Light | 0.35 | **0.03** |\n"
           "| GameManager | Movement Pop Volume | 0.35 | **0.05** |\n")
    rows = doc_rows(doc)
    assert rows[("GameManager", "Start Light")] == ("0.35", "0.03"), rows
    assert body_value(doc, "Start Light") == "0.03"
    # A value belonging to another setting must not vouch for this one.
    assert body_value(doc, "Start Light") != "0.05"
    assert body_value(doc, "Drift Min Speed") is None, "shared rows have none"


if __name__ == "__main__":
    self_check()
    sys.exit(main())
