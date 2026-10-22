# -*- coding: utf-8 -*-
"""Close shop agenda 1, 2 and 4 in the scene file: swap the two button sprites, add the wallet
readout, add one price label per block.

RUN WITH UNITY CLOSED. The Editor holds the scene in memory and writes it back on its next save,
so a patch applied underneath a running Editor is thrown away exactly like a Play-mode edit.

Components are resolved by their owning GameObject's NAME, never by position in the file -- the
same rule as fix_buttons.py, and for the same reason.

The three text objects are cloned from the Coin Counter rather than written from scratch: its
TextMeshProUGUI already points at the font asset this project actually accepts, and a hand-written
m_fontAsset reference is how you get a text object that renders nothing.
"""
import io
import os
import re
import shutil
import sys
import time

SCENE = sys.argv[1]

FISHINV_GUID = 'f0ba9fafd4ed22c4e84b8dfc86465dba'
BUY_SPRITE = '8a105be730dae2d4d840e5f511a3202a'    # "Group 26-1", reads BUY
EQUIPPED_SPRITE = 'edc04efc0b2d5f3489d548cbebdc6aa9'  # "Group 26", reads EQUIPPED

# Coin Counter: GameObject, RectTransform, CanvasRenderer, TextMeshProUGUI
TEMPLATE = ('921328869', '921328872', '921328871', '921328870')

# Card art is 353x410 drawn into a 470x546 block, so source pixels scale by ~1.331. The painted
# price strip sits at about (205, 277) from the art's top-left, which lands here in block-local
# coordinates. A first guess: nudge it in the Inspector once it is visible in the Game view.
PRICE_POS = (-3, -99)

backup = os.path.join('Backups', time.strftime('%Y-%m-%d_%H%M') + '_shop-wiring')
if not os.path.isdir(backup):
    os.makedirs(backup)
shutil.copy2(SCENE, os.path.join(backup, os.path.basename(SCENE)))
print('backed up to %s' % backup)

s = io.open(SCENE, encoding='utf-8', newline='').read()
NEWLINE = '\r\n' if '\r\n' in s else '\n'
s = s.replace('\r\n', '\n')

taken = set(re.findall(r'(?m)^--- !u!\d+ &(\d+)', s))

names = {}
for m in re.finditer(r'(?ms)^--- !u!1 &(\d+)\nGameObject:(.*?)(?=^--- !u!|\Z)', s):
    n = re.search(r'(?m)^  m_Name: (.*)$', m.group(2))
    names[m.group(1)] = n.group(1).strip() if n else '?'

rect_of = {}
for m in re.finditer(r'(?ms)^--- !u!224 &(\d+)\nRectTransform:(.*?)(?=^--- !u!|\Z)', s):
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', m.group(2)).group(1)
    rect_of[go] = m.group(1)


def go_named(name):
    hits = [g for g, n in names.items() if n == name]
    assert len(hits) == 1, '%s matched %d GameObjects' % (name, len(hits))
    return hits[0]


def doc(cls, fid):
    m = re.search(r'(?ms)^--- !u!%s &%s\n\w+:.*?(?=^--- !u!|\Z)' % (cls, fid), s)
    assert m, 'no !u!%s &%s in the scene' % (cls, fid)
    return m.group(0)


# ------------------------------------------------------------------ 1. swap the button sprites
inv_go = go_named('CharactersMenu')
inv = None
for m in re.finditer(r'(?ms)^--- !u!114 &\d+\nMonoBehaviour:.*?(?=^--- !u!|\Z)', s):
    if 'guid: ' + FISHINV_GUID in m.group(0):
        inv = m.group(0)
        break
assert inv, 'FishInventory component not found'

patched = inv
for field, want in (('buyArt', BUY_SPRITE), ('equippedArt', EQUIPPED_SPRITE)):
    patched = re.sub(r'(?m)^(  %s: \{fileID: \d+, guid: )\w+' % field,
                     lambda mm, w=want: mm.group(1) + w, patched, count=1)

# ------------------------------------------------------------------ 2/4. the text objects
tgo, trt, tcr, ttmp = TEMPLATE
template = '\n'.join(doc(c, f).rstrip('\n')
                     for c, f in (('1', tgo), ('224', trt), ('222', tcr), ('114', ttmp)))

next_id = 770000001
new_docs = []
child_adds = []


def alloc():
    global next_id
    out = []
    while len(out) < 4:
        while str(next_id) in taken:
            next_id += 1
        taken.add(str(next_id))
        out.append(str(next_id))
    return out


def add_text(name, parent_rid, pos, size, font, halign, valign, anchor_centre):
    """Clone the Coin Counter into a new text object under parent_rid. Returns its TMP fileID."""
    go, rt, cr, tmp = alloc()
    t = template
    for old, new in ((tgo, go), (trt, rt), (tcr, cr), (ttmp, tmp)):
        t = t.replace('&' + old, '&' + new).replace('{fileID: %s}' % old, '{fileID: %s}' % new)

    t = re.sub(r'(?m)^  m_Name: Coin Counter$', '  m_Name: ' + name, t, count=1)
    t = re.sub(r'(?m)^  m_IsActive: 0$', '  m_IsActive: 1', t, count=1)
    t = re.sub(r'(?m)^  m_Father: \{fileID: \d+\}$',
               '  m_Father: {fileID: %s}' % parent_rid, t, count=1)
    t = re.sub(r'(?m)^  m_AnchoredPosition: \{[^}]*\}$',
               '  m_AnchoredPosition: {x: %s, y: %s}' % pos, t, count=1)
    t = re.sub(r'(?m)^  m_SizeDelta: \{[^}]*\}$',
               '  m_SizeDelta: {x: %s, y: %s}' % size, t, count=1)
    if anchor_centre:
        for field in ('m_AnchorMin', 'm_AnchorMax', 'm_Pivot'):
            t = re.sub(r'(?m)^  %s: \{[^}]*\}$' % field,
                       '  %s: {x: 0.5, y: 0.5}' % field, t, count=1)
    # a label that eats raycasts turns any button it is nudged over into a dead button
    t = re.sub(r'(?m)^  m_RaycastTarget: 1$', '  m_RaycastTarget: 0', t, count=1)
    t = re.sub(r'(?m)^  m_fontSize: [\d.]+$', '  m_fontSize: %d' % font, t, count=1)
    t = re.sub(r'(?m)^  m_fontSizeBase: [\d.]+$', '  m_fontSizeBase: %d' % font, t, count=1)
    t = re.sub(r'(?m)^  m_HorizontalAlignment: \d+$',
               '  m_HorizontalAlignment: %d' % halign, t, count=1)
    t = re.sub(r'(?m)^  m_VerticalAlignment: \d+$',
               '  m_VerticalAlignment: %d' % valign, t, count=1)

    new_docs.append(t)
    child_adds.append((parent_rid, rt))
    return tmp


# the wallet: top-left of the panel, mirroring CloseButton's 60px inset at top-right
wallet_tmp = add_text('WalletText', rect_of[inv_go], (60, -60), (300, 70), 50, 1, 512, False)

# one price label per block, centred over the painted price strip
for i in range(1, 6):
    add_text('PriceText', rect_of[go_named('Block %d' % i)], PRICE_POS, (300, 80), 50, 2, 512, True)

patched = re.sub(r'(?m)^  walletText: \{fileID: -?\d+\}$',
                 '  walletText: {fileID: %s}' % wallet_tmp, patched, count=1)

assert s.count(inv) == 1
s = s.replace(inv, patched, 1)

# ------------------------------------------------------------------ reparent and write
for parent_rid, child_rid in child_adds:
    body = doc('224', parent_rid)
    if 'm_Children: []' in body:
        nb = body.replace('m_Children: []',
                          'm_Children:\n  - {fileID: %s}' % child_rid, 1)
    else:
        nb = re.sub(r'(?ms)^(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)',
                    lambda mm: mm.group(1) + '  - {fileID: %s}\n' % child_rid, body, count=1)
    assert nb != body, 'could not add child to RectTransform &%s' % parent_rid
    s = s.replace(body, nb, 1)

s = s.rstrip('\n') + '\n' + '\n'.join(new_docs) + '\n'
io.open(SCENE, 'w', encoding='utf-8', newline=NEWLINE).write(s)

print('buyArt      -> Group 26-1 (BUY)')
print('equippedArt -> Group 26 (EQUIPPED)')
print('walletText  -> WalletText &%s' % wallet_tmp)
print('added %d text objects' % len(new_docs))
assert len(new_docs) == 6, 'expected 6 new text objects, made %d' % len(new_docs)
