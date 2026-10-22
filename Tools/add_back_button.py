# -*- coding: utf-8 -*-
"""Add a bottom-left back arrow to the Characters screen by cloning CloseButton.

RUN WITH UNITY CLOSED. The Editor holds the scene in memory and writes it back on its next save,
so a patch applied underneath a running Editor is thrown away. Unity Hub runs its own
"unity.exe serve" helper -- that one is harmless; it is the Editor that matters.

Cloned rather than built from parts, because CloseButton's Button already carries the persistent
call GameManager.ShowCharacters(false). Copying it means the new button closes the shop with
nothing to rewire, and no chance of wiring it to the wrong method or the wrong bool.

Objects are resolved by NAME, never by position in the file, the same rule as add_block.py.

Run once. Running it twice would add a second button; it refuses if BackButton already exists.
"""
import io
import os
import re
import shutil
import sys
import time

SCENE = sys.argv[1]

SOURCE = 'CloseButton'        # cloned from
PARENT = 'CharactersMenu'     # cloned into, as the LAST child so it draws over the shelf
NEW_NAME = 'BackButton'

ART = 'Assets/Graphics/Updated UI/Back Button 13.png'

# Height matches the sound buttons. Width is that height times the art's own 151x112 aspect
# (1.348), so the arrow is not stretched -- the mistake Buy.png's lettering once suffered.
SIZE = (197, 146)
POS = (60, 60)
CORNER = (0, 0)               # anchorMin, anchorMax and pivot: bottom-left


def sub_sprite(png):
    """The guid and the fileID of a Sprite Multiple texture's single sub-sprite."""
    meta = io.open(png + '.meta', encoding='utf-8').read()
    guid = re.search(r'(?m)^guid: (\w+)$', meta).group(1)

    table = re.search(r'(?ms)^    nameFileIdTable:\n(.*?)(?=^  \w)', meta).group(1)
    ids = re.findall(r'(?m)^      .+?: (-?\d+)$', table)

    if len(ids) != 1:
        raise SystemExit('%s has %d sub-sprites, expected exactly 1' % (png, len(ids)))

    return guid, ids[0]


s = io.open(SCENE, encoding='utf-8', newline='').read()
NEWLINE = '\r\n' if '\r\n' in s else '\n'
s = s.replace('\r\n', '\n')

if re.search(r'(?m)^  m_Name: %s$' % NEW_NAME, s):
    raise SystemExit('%s already exists in the scene -- nothing to do.' % NEW_NAME)

backup = os.path.join('Backups', time.strftime('%Y-%m-%d_%H%M') + '_before-back-button')
if not os.path.isdir(backup):
    os.makedirs(backup)
shutil.copy2(SCENE, os.path.join(backup, os.path.basename(SCENE)))
print('backed up to %s' % backup)

DOC = re.compile(r'(?ms)^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)')
docs = {m.group(2): (m.group(1), m.group(0)) for m in DOC.finditer(s)}
taken = set(docs)

names = {}
for anchor, (kind, text) in docs.items():
    if kind != '1':
        continue
    m = re.search(r'(?m)^  m_Name: (.*)$', text)
    if m:
        names[anchor] = m.group(1).strip()


def find(name, kind='1'):
    for anchor, (k, _) in docs.items():
        if k == kind and names.get(anchor) == name:
            return anchor
    raise SystemExit('no GameObject named %s in the scene' % name)


def rect_of(go):
    """The RectTransform anchor belonging to this GameObject."""
    for anchor, (kind, text) in docs.items():
        if kind == '224' and 'm_GameObject: {fileID: %s}' % go in text:
            return anchor
    raise SystemExit('GameObject %s has no RectTransform' % go)


src_go = find(SOURCE)
parent_go = find(PARENT)
parent_rt = rect_of(parent_go)

src_comps = re.findall(r'component: \{fileID: (\d+)\}', docs[src_go][1])
print('cloning %s (%d components) into %s' % (SOURCE, len(src_comps), PARENT))

# Fresh anchors. Unity's are arbitrary positive integers, so any unused one is valid; counting up
# from the source keeps the new block near its sibling in the file, which only helps a human diff.
new_id = {}
next_free = int(src_go)
for old in [src_go] + src_comps:
    while True:
        next_free += 1
        if str(next_free) not in taken:
            break
    new_id[old] = str(next_free)
    taken.add(str(next_free))

guid, fid = sub_sprite(ART)
new_rt = new_id[rect_of(src_go)]

blocks = []
for old in [src_go] + src_comps:
    kind, text = docs[old]

    # The anchor line itself, then every reference between the cloned documents
    text = re.sub(r'^--- !u!%s &%s$' % (kind, old), '--- !u!%s &%s' % (kind, new_id[old]),
                  text, count=1, flags=re.M)
    for a, b in new_id.items():
        text = text.replace('fileID: %s}' % a, 'fileID: %s}' % b)

    if kind == '1':
        text = re.sub(r'(?m)^  m_Name: .*$', '  m_Name: %s' % NEW_NAME, text)

    elif kind == '224':
        text = re.sub(r'm_AnchorMin: \{x: [-\d.e]+, y: [-\d.e]+\}',
                      'm_AnchorMin: {x: %g, y: %g}' % CORNER, text)
        text = re.sub(r'm_AnchorMax: \{x: [-\d.e]+, y: [-\d.e]+\}',
                      'm_AnchorMax: {x: %g, y: %g}' % CORNER, text)
        text = re.sub(r'm_Pivot: \{x: [-\d.e]+, y: [-\d.e]+\}',
                      'm_Pivot: {x: %g, y: %g}' % CORNER, text)
        text = re.sub(r'm_AnchoredPosition: \{x: [-\d.e]+, y: [-\d.e]+\}',
                      'm_AnchoredPosition: {x: %g, y: %g}' % POS, text)
        text = re.sub(r'm_SizeDelta: \{x: [-\d.e]+, y: [-\d.e]+\}',
                      'm_SizeDelta: {x: %g, y: %g}' % SIZE, text)
        text = re.sub(r'm_Father: \{fileID: \d+\}',
                      'm_Father: {fileID: %s}' % parent_rt, text)

    elif 'UnityEngine.UI.Image' in text:
        text = re.sub(r'm_Sprite: \{fileID: -?\d+, guid: \w+, type: \d+\}',
                      'm_Sprite: {fileID: %s, guid: %s, type: 3}' % (fid, guid), text)

    blocks.append(text if text.endswith('\n') else text + '\n')

s = s.rstrip('\n') + '\n' + ''.join(blocks)

# Last in m_Children, so it draws over the Scroll View and takes the tap there
pk, ptext = docs[parent_rt]
kids = re.search(r'(?ms)^  m_Children:\n(.*?)(?=^  m_Father)', ptext)
patched = ptext[:kids.end(1)] + '  - {fileID: %s}\n' % new_rt + ptext[kids.end(1):]
s = s.replace(ptext, patched, 1)

io.open(SCENE, 'w', encoding='utf-8', newline='').write(s.replace('\n', NEWLINE))

print('  %s  %gx%g at (%g, %g), bottom-left' % (NEW_NAME, SIZE[0], SIZE[1], POS[0], POS[1]))
print('  sprite %s' % os.path.basename(ART))
print('  onClick inherited from %s' % SOURCE)
