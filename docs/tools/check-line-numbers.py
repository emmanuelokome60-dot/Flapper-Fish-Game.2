#!/usr/bin/env python3
"""Re-point every line number in CODE-EXPLAINED.md at the real source.

    python fix_line_numbers.py <project root> [--check]

The guide tells the reader to look at <file>.cs line N, so a number that has
drifted sends them to the wrong place. Rewrapping the tooltips moved the
methods down, which is exactly how this drifts without anyone noticing.

Numbers are read fresh from the .cs files rather than nudged by an offset, so
this is also the check: --check reports drift and exits 1 without writing.
"""
import glob
import os
import re
import sys

# A member declaration: optional modifiers, a return type, then the name.
DECL = re.compile(
    r"^\s+(?:public|private|protected|internal)\s+"
    r"(?:static\s+|override\s+|virtual\s+|async\s+|readonly\s+)*"
    r"(?:[\w<>\[\],.?]+\s+)?(\w+)\s*(?:\(|=>|$|\s*\{)")

SECTION = re.compile(r"^#+\s*\d+\.\s*(\w+)\.cs")
# `Name()`, `Name(int amount)` or a bare property like `CoinSprite`, then a
# line number in the same sentence. Only names the section's script actually
# declares get substituted, so the loose match cannot hit ordinary prose.
PROSE = re.compile(r"(`(\w+)(?:\([^`]*\))?`[^`\n]{0,40}?\bline )(\d+)")
ROW = re.compile(r"^\|([^|]*`[^|]*)\|([^|]*\d[^|]*)\|")


def members(path):
    """{name: line} for every method and property in one .cs file."""
    out = {}
    cls = os.path.basename(path)[:-3]
    for i, line in enumerate(open(path, encoding="utf-8").read().split("\n"), 1):
        found = DECL.match(line)
        if not found:
            continue
        name = found.group(1)
        # "public LegalNotice(" is the constructor, not a method called by name.
        if name == cls:
            name = "constructor"
        # First declaration wins: overloads and the odd re-match stay put.
        out.setdefault(name, i)
    return out


def source_map(root):
    return {os.path.basename(p)[:-3]: members(p)
            for p in glob.glob(os.path.join(root, "Assets", "**", "*.cs"), recursive=True)}


def fix_row(line, known, drift):
    """A table row: names in the first cell, numbers in the second, in order.

    ponytail: assumes names sit in column 1. The FishShopDebug table puts them
    in column 2 and is skipped; it holds the only three-column table in the
    guide, and that file has no tooltips to push its methods around. Walk the
    cells looking for the one with backticks if a second such table appears.
    """
    if not ROW.match(line):
        return line
    parts = line.split("|")          # parts[1] = names, parts[2] = numbers
    if len(parts) < 3:
        return line

    names = re.findall(r"`(\w+)\(", parts[1])
    if not names and "constructor" in parts[1]:
        names = ["constructor"]
    numbers = re.findall(r"\d+", parts[2])
    if len(names) != len(numbers):
        return line

    cell, at = parts[2], 0
    for name, old in zip(names, numbers):
        at = cell.index(old, at)
        new = known.get(name)
        if new is not None and str(new) != old:
            cell = cell[:at] + str(new) + cell[at + len(old):]
            drift.append((name, old, new))
            old = str(new)
        at += len(old)
    parts[2] = cell
    return "|".join(parts)


def main():
    root = sys.argv[1]
    check = "--check" in sys.argv
    doc = os.path.join(root, "docs", "CODE-EXPLAINED.md")
    by_script = source_map(root)

    lines = open(doc, encoding="utf-8").read().split("\n")
    known, drift = {}, []

    for i, line in enumerate(lines):
        heading = SECTION.match(line)
        if heading:
            known = by_script.get(heading.group(1), {})
            continue
        if not known:
            continue

        def prose(m):
            new = known.get(m.group(2))
            if new is not None and str(new) != m.group(3):
                drift.append((m.group(2), m.group(3), new))
                return m.group(1) + str(new)
            return m.group(0)

        lines[i] = fix_row(PROSE.sub(prose, line), known, drift)

    for name, old, new in drift:
        print("  %-22s %s -> %s" % (name, old, new))

    if check:
        print("%d line numbers have drifted." % len(drift))
        return 1 if drift else 0

    if drift:
        open(doc, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
    print("%d line numbers corrected." % len(drift))
    return 0


if __name__ == "__main__":
    sys.exit(main())
