# -*- coding: utf-8 -*-
"""Add one more fish block to the Characters shelf by deep-cloning the last one.

RUN WITH UNITY CLOSED. The Editor holds the scene in memory and writes it back on its next save,
so a patch applied underneath a running Editor is thrown away. Unity Hub runs its own
"unity.exe serve" helper -- that one is harmless; it is the Editor that matters.

The block is resolved by its owning GameObject's NAME, never by position in the file, the same
rule as fix_buttons.py and fix_shop.py.

Cloning walks the block's RectTransform tree and copies every document it finds rather than
hardcoding "Image, Button, TMP". A block that gains a child later still clones correctly.

FishInventory frees every block but the last, so the new block becomes the one that costs coins
and the old last block becomes free. That is the point: one run of this adds a fish to buy.
"""
import io
import os
import re
import shutil
import sys
import time

SCENE = sys.argv[1]

backup = os.path.join('Backups', time.strftime('%Y-%m-%d_%H%M') + '_before-add-block')
if not os.path.isdir(backup):
    os.makedirs(backup)
shutil.copy2(SCENE, os.path.join(backup, os.path.basename(SCENE)))
print('backed up to %s' % backup)

s = io.open(SCENE, encoding='utf-8', newline='').read()
NEWLINE = '\r\n' if '\r\n' in s else '\n'
s = s.replace('\r\n', '\n')

DOC = re.compile(r'(?ms)^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)')
docs = {m.group(2): (m.group(1), m.group(0)) for m in DOC.finditer(s)}
taken = set(docs)

names = {m.group(1): re.search(r'(?m)^  m_Name: (.*)$', m.group(2)).group(1).strip()
         for m in re.finditer(r'(?ms)^--- !u!1 &(\d+)\nGameObject:(.*?)(?=^--- !u!|\Z)', s)}

# RectTransform id -> (owning GameObject, child RectTransform ids)
rects = {}
for m in re.finditer(r'(?ms)^--- !u!224 &(\d+)\nRectTransform:(.*?)(?=^--- !u!|\Z)', s):
    body = m.group(2)
    kids = re.findall(r'(?m)^  - \{fileID: (\d+)\}',
                      body.split('m_Children:')[1].split('m_Father:')[0]) if 'm_Children:' in body else []
    rects[m.group(1)] = (re.search(r'm_GameObject: \{fileID: (\d+)\}', body).group(1), kids)


def rect_of(go):
    return next(r for r, (g, _) in rects.items() if g == go)


blocks = sorted((names[g], g) for g in names if re.match(r'^Block \d+$', names[g]))
assert blocks, 'no Block N GameObjects found'
last_name, last_go = blocks[-1]
new_index = max(int(n.split()[1]) for n, _ in blocks) + 1
print('cloning %s -> Block %d' % (last_name, new_index))

content_rt = rect_of(next(g for g in names if names[g] == 'Content'))
assert rect_of(last_go) in rects[content_rt][1], '%s is not a child of Content' % last_name


def subtree(rt, out):
    """Every document id belonging to this RectTransform's GameObject and its descendants."""
    go = rects[rt][0]
    out.append(go)
    gobody = docs[go][1]
    out.extend(re.findall(r'component: \{fileID: (\d+)\}', gobody))
    for k in rects[rt][1]:
        subtree(k, out)
    return out


ids = subtree(rect_of(last_go), [])
assert len(ids) == len(set(ids)), 'duplicate ids while collecting the subtree'

next_id = 880200001
remap = {}
for old in ids:
    while str(next_id) in taken:
        next_id += 1
    taken.add(str(next_id))
    remap[old] = str(next_id)

cloned = []
for old in ids:
    cls, body = docs[old]
    # Every id in the subtree is rewritten; references pointing outside it are left alone, which
    # is what keeps the sprite, font and material links intact.
    for a, b in remap.items():
        body = body.replace('&' + a + '\n', '&' + b + '\n')
        body = body.replace('{fileID: %s}' % a, '{fileID: %s}' % b)
    cloned.append(body.rstrip('\n'))

new_go = remap[last_go]
new_rt = remap[rect_of(last_go)]

# rename only the block itself, not its children
block_doc = next(c for c in cloned if c.startswith('--- !u!1 &' + new_go + '\n'))
renamed = re.sub(r'(?m)^  m_Name: .*$', '  m_Name: Block %d' % new_index, block_doc, count=1)
assert renamed != block_doc
cloned[cloned.index(block_doc)] = renamed

# hang it off Content, after the current last block
cbody = docs[content_rt][1]
newc = re.sub(r'(?ms)^(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)',
              lambda m: m.group(1) + '  - {fileID: %s}\n' % new_rt, cbody, count=1)
assert newc != cbody, 'could not append to Content children'
s = s.replace(cbody, newc, 1)

s = s.rstrip('\n') + '\n' + '\n'.join(cloned) + '\n'
io.open(SCENE, 'w', encoding='utf-8', newline=NEWLINE).write(s)
print('added Block %d  (%d documents cloned)' % (new_index, len(cloned)))
