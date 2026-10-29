# -*- coding: utf-8 -*-
"""Shrink the Characters shelf from six cards to five, and centre the wallet counter.

RUN WITH UNITY CLOSED. The Editor holds the scene in memory and writes it back on its next
save, so a patch applied underneath a running Editor is thrown away. Unity Hub runs its own
"unity.exe serve" helper -- that one is harmless; it is the Editor that matters.

Why card 6 goes: FishInventory.Load() makes the LAST card the paid one, worked out from how
many cards there are rather than written down as a number. fishArt only holds five sprites,
so card 6 had no art: 300 coins bought a card that stayed grey and an in-game fish that fell
back to the default. Deleting card 6 makes slot 4 the paid one -- PEIXE5 "METAL CHOMPER",
which is the FISH3 skin -- and that slot has art.

Nothing here resizes Content. It carries a HorizontalLayoutGroup and a ContentSizeFitter set
to PreferredSize, so the shelf re-measures itself for five cards at run time.

Objects are resolved by their owning GameObject's NAME, never by position in the file, the
same rule as fix_shop.py and skins_and_size.py.
"""
import io
import os
import re
import shutil
import sys
import time

SCENE = sys.argv[1]

DOOMED = 'Block 6'
WALLET = 'WalletText'

backup = os.path.join('Backups', time.strftime('%Y-%m-%d_%H%M') + '_shop-five-cards')
if not os.path.isdir(backup):
    os.makedirs(backup)
shutil.copy2(SCENE, os.path.join(backup, os.path.basename(SCENE)))
print('backed up to %s' % backup)

s = io.open(SCENE, encoding='utf-8', newline='').read()
NEWLINE = '\r\n' if '\r\n' in s else '\n'
s = s.replace('\r\n', '\n')

head, sep, rest = s.partition('--- !u!')
assert sep, 'no YAML documents in %s' % SCENE
rest = sep + rest

DOC = r'(?ms)^--- !u!(\d+) &(\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)'

names = {}
for m in re.finditer(r'(?ms)^--- !u!1 &(\d+)\nGameObject:(.*?)(?=^--- !u!|\Z)', rest):
    n = re.search(r'(?m)^  m_Name: (.*)$', m.group(2))
    names[m.group(1)] = n.group(1).strip() if n else '?'

rect_of = {}
for m in re.finditer(r'(?ms)^--- !u!224 &(\d+)\nRectTransform:(.*?)(?=^--- !u!|\Z)', rest):
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', m.group(2)).group(1)
    rect_of[go] = m.group(1)


def go_named(name):
    hits = [g for g, n in names.items() if n == name]
    assert len(hits) == 1, '%s matched %d GameObjects' % (name, len(hits))
    return hits[0]


def doc_body(fid):
    m = re.search(r'(?ms)^--- !u!\d+ &%s(?: stripped)?\n(.*?)(?=^--- !u!|\Z)' % fid, rest)
    assert m, 'no document &%s in the scene' % fid
    return m.group(1)


# ------------------------------------------------------- 1. collect card 6 and everything under it
#
# Walked from the RectTransform children rather than from the ID range. Consecutive IDs are a
# convention the authoring script happened to follow, not a guarantee, and deleting a block by
# number is how you orphan a component that was renumbered on some earlier reimport.
doomed_go = go_named(DOOMED)
doomed_rect = rect_of[doomed_go]

subtree_rects = []
queue = [doomed_rect]
while queue:
    r = queue.pop()
    subtree_rects.append(r)
    body = doc_body(r)
    kids = re.search(r'(?ms)^  m_Children:\n(.*?)^  m_Father:', body)
    if kids:
        queue.extend(re.findall(r'\{fileID: (\d+)\}', kids.group(1)))

drop = set()
for r in subtree_rects:
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', doc_body(r)).group(1)
    drop.add(go)
    comps = re.search(r'(?ms)^  m_Component:\n(.*?)^  m_Layer:', doc_body(go))
    assert comps, 'GameObject &%s has no m_Component list' % go
    drop.update(re.findall(r'\{fileID: (\d+)\}', comps.group(1)))

print('%s is %d GameObjects / %d documents' % (DOOMED, len(subtree_rects), len(drop)))

# ------------------------------------------------------- 2. unhook it from the shelf, then delete
content_rect = rect_of[go_named('Content')]
before = doc_body(content_rect)
after = before.replace('  - {fileID: %s}\n' % doomed_rect, '', 1)
assert after != before, 'Content does not list %s as a child' % DOOMED
rest = rest.replace(before, after, 1)

kept = []
for m in re.finditer(DOC, rest):
    if m.group(2) not in drop:
        kept.append(m.group(0))
rest = ''.join(kept)

# Nothing may still point at a document that has gone, or Unity logs a missing reference and
# the shelf comes up one button short of its blocks.
for fid in sorted(drop):
    assert 'fileID: %s}' % fid not in rest, 'something still references &%s' % fid

# ------------------------------------------------------- 3. centre the wallet counter
#
# Was pinned to the top-right corner. Anchor, pivot and X all move together: with the pivot left
# at 1 the box would grow leftwards from the centre line instead of straddling it, because
# FishInventory adds a ContentSizeFitter that shrinks the box to its own text.
wallet_rect = rect_of[go_named(WALLET)]
body = doc_body(wallet_rect)
moved = body
for key, value in (('m_AnchorMin', '{x: 0.5, y: 1}'),
                   ('m_AnchorMax', '{x: 0.5, y: 1}'),
                   ('m_Pivot', '{x: 0.5, y: 1}')):
    moved, n = re.subn(r'(?m)^  %s: .*$' % key, '  %s: %s' % (key, value), moved)
    assert n == 1, '%s: expected one %s, found %d' % (WALLET, key, n)

moved, n = re.subn(r'(?m)^  m_AnchoredPosition: \{x: [-\d.]+, (y: [-\d.]+)\}$',
                   r'  m_AnchoredPosition: {x: 0, \1}', moved)
assert n == 1, '%s: expected one m_AnchoredPosition, found %d' % (WALLET, n)

rest = rest.replace(body, moved, 1)

out = (head + rest).replace('\n', NEWLINE)
io.open(SCENE, 'w', encoding='utf-8', newline='').write(out)
print('deleted %s, centred %s' % (DOOMED, WALLET))
