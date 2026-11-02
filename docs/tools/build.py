#!/usr/bin/env python3
"""Rebuild docs/CODE-EXPLAINED.docx from docs/CODE-EXPLAINED.md.

    python docs/tools/build.py

The .md is the source. Edit that, run this, and the .docx follows.

Needs Node, plus the docx package once:   npm install -g docx
"""
import os
import shutil
import subprocess
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
MD = os.path.join(ROOT, "docs", "CODE-EXPLAINED.md")
DOCX = os.path.join(ROOT, "docs", "CODE-EXPLAINED.docx")
GEN = os.path.join(HERE, "build-code-doc.js")

CONTENT_TYPES = "[Content_Types].xml"


def npm_root_global():
    """Where `npm install -g` put the docx package."""
    npm = shutil.which("npm") or "npm"
    out = subprocess.run([npm, "root", "-g"], capture_output=True, text=True,
                         check=True, shell=(os.name == "nt"))
    return out.stdout.strip()


def opc_reorder(path):
    """Move [Content_Types].xml to the front of the archive.

    OPC (ECMA-376 part 2) requires it to be the first part. Word reads the
    central directory so it never noticed, but Google Docs streams the zip and
    rejects the file as an unsupported format. docx-js writes it last, so every
    build has to be repacked.
    """
    with zipfile.ZipFile(path) as z:
        parts = [(info, z.read(info.filename)) for info in z.infolist()]

    if not any(info.filename == CONTENT_TYPES for info, _ in parts):
        sys.exit("error: %s has no %s" % (path, CONTENT_TYPES))

    # False sorts before True, so the content types land at index 0.
    parts.sort(key=lambda part: part[0].filename != CONTENT_TYPES)

    tmp = path + ".tmp"
    with zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as z:
        for info, data in parts:
            z.writestr(info, data)
    os.replace(tmp, path)


def first_part(path):
    with zipfile.ZipFile(path) as z:
        return z.namelist()[0]


def main():
    if not os.path.exists(MD):
        sys.exit("error: %s is missing" % MD)

    docx_module = os.path.join(npm_root_global(), "docx")
    if not os.path.exists(docx_module):
        sys.exit("error: docx package not found. Run:  npm install -g docx")

    node = shutil.which("node") or "node"
    subprocess.run([node, GEN, MD, DOCX, docx_module], check=True)

    opc_reorder(DOCX)

    # The check: if the repack ever stops working, the build fails here rather
    # than handing over a file Google Docs quietly refuses.
    got = first_part(DOCX)
    assert got == CONTENT_TYPES, "first part is %r, expected %r" % (got, CONTENT_TYPES)

    print("%s is first part, %.0f KB" % (got, os.path.getsize(DOCX) / 1024))


if __name__ == "__main__":
    main()
