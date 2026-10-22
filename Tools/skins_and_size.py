# -*- coding: utf-8 -*-
"""Wire the five fish skins onto PlayerController, and scale the Characters shelf for phones.

RUN WITH UNITY CLOSED. The Editor holds the scene in memory and writes it back on its next save,
so a patch applied underneath a running Editor is thrown away. Unity Hub runs its own
"unity.exe serve" helper -- that one is harmless; it is the Editor that matters.

Objects are resolved by their owning GameObject's NAME, never by position in the file, the same
rule as add_block.py and fix_shop.py.

Pass 1 writes the skins array straight into the scene. A new [SerializeField] on a component
already saved in the scene otherwise comes back EMPTY in the Inspector, so this is what stops the
array being a thing to remember to drag. Each sprite's guid and sub-sprite fileID are read from
the .meta at run time rather than hardcoded, because a reimport can change a fileID.

Pass 2 scales the shelf by 1.15. Content has a HorizontalLayoutGroup and a ContentSizeFitter, so
resizing the blocks re-spaces the shelf on its own -- nothing here repositions anything.
"""
import io
import os
import re
import shutil
import sys
import time

SCENE = sys.argv[1]

# One bare fish per shelf block, in SHELF order -- which is not the order the files are numbered
# in. FISH1-4 do not line up with PEIXE1-5, so each line names the card it belongs to; matching on
# the filenames alone is exactly what put the wrong fish in the cave the first time.
SKIN_PNGS = [
    'Assets/Graphics/My Assets/Frame 1 (1).png',   # PEIXE1  Abyssal Chomper -- dark purple
    'Assets/Graphics/Fish Skins/FISH1.png',        # PEIXE2  Corsair Chomper -- eyepatch, hoops
    'Assets/Graphics/Fish Skins/FISH4.png',        # PEIXE3  Crimson Hunter -- skeletal fins
    'Assets/Graphics/Fish Skins/FISH2.png',        # PEIXE4  Mossy Scavenger -- rust and sea moss
    'Assets/Graphics/Fish Skins/FISH3.png',        # PEIXE5  Metal Chomper -- pale silver
]
# FISH5.png is the blue fish, the old FISH4 renamed once the red Crimson Hunter replaced it. It
# has no card on the shelf, so it stays deliberately unused rather than squeezed onto a card that
# describes a different fish. Block 6 still has neither card art nor a skin.
#
# Entries may be None: ApplySkin treats a null the same as an out-of-range index and shows the
# ship fish. Keep the slot rather than dropping it, or every fish after it shifts onto the wrong
# card -- which is exactly the bug this list already caused once.

PLAYER_CONTROLLER_GUID = '623e7c4ff6f4e73428b8438a41d68b58'

# name -> (width, height). None keeps that axis.
SIZES = {
    'Block 1': (620, 722),
    'Block 2': (620, 722),
    'Block 3': (620, 722),
    'Block 4': (620, 722),
    'Block 5': (620, 722),
    'Block 6': (620, 722),
    'ActionButton': (330, 87),
    'PriceText': (397, 106),
    # Height only, now. FishInventory puts a ContentSizeFitter on this box so its width follows
    # the digits -- a fixed width left the number floating in from the right edge by whatever the
    # balance did not use. The 300 is a starting value for Edit mode.
    'WalletText': (300, 92),
    # 280x76 was aspect 3.684 against Buy.png's 3.795, so its lettering was stretched. 3.793.
    'CloseButton': (330, 87),
    'Scroll View': (1820, 810),
    # Both copies, menu and pause. None keeps the width: an AspectRatioFitter in
    # HeightControlsWidth mode drives it from the height, so a width written here is overwritten
    # on the first frame and only misleads whoever reads the Inspector next.
    'SoundButton': (None, 146),
}

# One margin from the frame edge for every corner widget. 60 is not a new number: both
# SoundButtons and WalletText's x already used it, while BackButton had drifted to 66.2/37.5,
# PauseButton to 51/66, and both run counters sat flush against the top edge at 0.
PADDING = 60

# Signs follow each object's own pivot: a left anchor takes +PADDING, a right anchor -PADDING,
# and a top anchor -PADDING. An object pivoted at its centre carries half its own height too.
MOVES = {
    # The shelf drops so the cards clear the back arrow. Blocks are 722 tall, centre-aligned in
    # an 810 viewport, so a card's top is 44 below the viewport's: BackButton's bottom sits at
    # +334, the card top wants +274, so the viewport centre is 274 + 44 - 405.
    'Scroll View': (0, -87),
    # y is overwritten below from BackButton's real position. Kept here so the key exists and
    # the x is visible; the number is a placeholder.
    'WalletText': (-PADDING, None),
    # Was 66.2/37.5 -- two different margins on one object, neither of them anyone's decision.
    'BackButton': (PADDING, -PADDING),
    # The two sound buttons sat at (60, 39.4) and (52.9, 35.9), so the button jumped when the
    # player paused. One margin from the left and bottom edges on both screens.
    'SoundButton': (PADDING, PADDING),
    # The run HUD. Both of these were flush against the top edge, which is also where a phone
    # puts its notch.
    'Coin Counter': (PADDING, -PADDING),
    'PauseButton': (-PADDING, -PADDING),
    # Pivot (0.5, 0.5) on a top anchor, so half its own 141.02 height is part of the offset.
    # x is a deliberate off-centre nudge, not a margin, and is left alone.
    'Distance Score Counter': (36, -(PADDING + 141.02 / 2)),
}

# Unity's default of 10 is a mouse-era number. On a touchscreen a finger drifts further than that
# during an ordinary tap, the tap is reclassified as a drag, and the click never fires -- which is
# every button on the phone feeling dead at random, not just the sound one.
DRAG_THRESHOLD = 30

# name -> the corner that anchorMin, anchorMax and pivot all share. Set together on purpose: an
# anchor moved without its pivot shifts the object by half its own size, which reads as a nudge
# nobody asked for.
ANCHORS = {
    'BackButton': (0, 1),   # top-left
    'WalletText': (1, 1),   # top-right, opposite the arrow
}

# TMP HorizontalAlignmentOptions: Left 1, Center 2, Right 4.
#
# Left, even though the label is anchored to the RIGHT. Right-aligning it pushed the digits to
# the box's right edge while BuildCoinIcon kept the coin pinned to the left edge -- 397 units of
# empty space between the two. The box is narrowed instead, which keeps the pair together.
ALIGN = {'WalletText': 1}

# Removed outright: GameObject, its components, and its entry in the parent's child list. Nothing
# else in the scene referenced CloseButton -- it was built from Select Button.png, so it rendered
# the word SELECT and read like a card action rather than the way out. BackButton replaces it.
DELETE = ['CloseButton']

# WalletText matches the run's Coin Counter. FishInventory re-applies it at runtime from that
# counter, so the two cannot drift; this is what keeps the Inspector honest in the meantime.
FONT_SIZES = {'PriceText': 78, 'WalletText': 50}

LAYOUT_SPACING = 69   # was 60
LAYOUT_PADDING = 46   # was 40


def sub_sprite(png):
    """The guid and the fileID of a Sprite Multiple texture's single sub-sprite."""
    meta = io.open(png + '.meta', encoding='utf-8').read()
    guid = re.search(r'(?m)^guid: (\w+)$', meta).group(1)

    table = re.search(r'(?ms)^    nameFileIdTable:\n(.*?)(?=^  \w)', meta).group(1)
    ids = re.findall(r'(?m)^      .+?: (-?\d+)$', table)

    if len(ids) != 1:
        raise SystemExit('%s has %d sub-sprites, expected exactly 1' % (png, len(ids)))

    return guid, ids[0]


backup = os.path.join('Backups', time.strftime('%Y-%m-%d_%H%M') + '_before-skins-and-size')
if not os.path.isdir(backup):
    os.makedirs(backup)
shutil.copy2(SCENE, os.path.join(backup, os.path.basename(SCENE)))
print('backed up to %s' % backup)

s = io.open(SCENE, encoding='utf-8', newline='').read()
NEWLINE = '\r\n' if '\r\n' in s else '\n'
s = s.replace('\r\n', '\n')

DOC = re.compile(r'(?ms)^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)')
docs = [(m.group(1), m.group(2), m.group(3), m.start(), m.end()) for m in DOC.finditer(s)]

names = {}
for kind, anchor, body, _, _ in docs:
    if kind != '1':
        m = re.search(r'(?m)^  m_Name: (.*)$', body)
        continue
    m = re.search(r'(?m)^  m_Name: (.*)$', body)
    if m:
        names[anchor] = m.group(1).strip()

# Both passes rewrite document bodies, and more than one may land on the same document. They
# share this map so the second pass builds on the first rather than throwing it away; the file
# is rewritten from it once, back to front, so each offset stays valid.
pending = {}   # start -> (end, kind, anchor, body)
changed = []


def body_of(start, kind, anchor, original):
    return pending[start][3] if start in pending else original


def owner_name(body):
    m = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
    return names.get(m.group(1)) if m else None


# ------------------------------------------------- derive: wallet centred on the back button
# Both sit under a top anchor with pivot y = 1, so a rect's centre is anchoredPosition.y minus
# half its height. Matching the two centres is what puts them on one visual line.
#
# Read from the scene rather than hardcoded: BackButton's y has already been nudged in the Editor
# once between runs, and a number typed here would silently drift out of step with it again.
def _rect_of(name):
    for kind, anchor, body, start, end in docs:
        if kind != '224':
            continue
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        if go and names.get(go.group(1)) == name:
            y = float(re.search(r'm_AnchoredPosition: \{x: [-\d.e]+, y: ([-\d.e]+)\}', body).group(1))
            h = float(re.search(r'm_SizeDelta: \{x: [-\d.e]+, y: ([-\d.e]+)\}', body).group(1))
            p = float(re.search(r'm_Pivot: \{x: [-\d.e]+, y: ([-\d.e]+)\}', body).group(1))
            return y, h, p
    return None


_btn = _rect_of('BackButton')
if _btn is not None and 'WalletText' in MOVES:
    by, bh, bp = _btn

    # This run may be moving the button itself, and _rect_of reads the scene as it is on disk --
    # pass 2 has not run yet. Take the y about to be written, or the wallet lines up with where
    # the button used to be. (It did, once: centre -110.5 instead of -133.)
    if MOVES.get('BackButton', (None, None))[1] is not None:
        by = MOVES['BackButton'][1]

    centre = by + (0.5 - bp) * bh
    wh = SIZES['WalletText'][1]                     # the height this run is about to write
    wp = ANCHORS['WalletText'][1]
    x = MOVES['WalletText'][0]
    MOVES['WalletText'] = (x, centre - (0.5 - wp) * wh)
    changed.append('WalletText centred on BackButton (centre %.1f)' % centre)


# ---------------------------------------------------------------- pass 1: skins
# A None keeps the slot and writes an empty reference, so the indices stay lined up with the
# shelf. Dropping the entry instead would shift every fish after it back onto the wrong card.
entries = [sub_sprite(p) if p else None for p in SKIN_PNGS]
array = '  skins:\n' + ''.join(
    '  - {fileID: 0}\n' if e is None else '  - {fileID: %s, guid: %s, type: 3}\n' % (e[1], e[0])
    for e in entries
)

for kind, anchor, body, start, end in docs:
    if kind != '114' or PLAYER_CONTROLLER_GUID not in body:
        continue

    # Both forms must be matched. Unity writes an empty array inline as "skins: []", and a regex
    # that only knew the block form appended a SECOND skins key next to it -- duplicate YAML keys,
    # and the scene silently kept the wrong one.
    if re.search(r'(?m)^  skins:', body):
        new_body = re.sub(r'(?ms)^  skins:(?: \[\]\n|\n(?:  - \{.*?\}\n)*)', array, body)
    else:
        new_body = body.rstrip('\n') + '\n' + array

    pending[start] = (end, kind, anchor, new_body)
    changed.append('PlayerController skins: %d sprites' % len(entries))

# ---------------------------------------------------------------- pass 2: sizes
for kind, anchor, body, start, end in docs:
    body = body_of(start, kind, anchor, body)
    new_body = body

    if kind == '224':  # RectTransform
        name = owner_name(body)

        if name in SIZES:
            w, h = SIZES[name]
            keep = re.search(r'm_SizeDelta: \{x: ([-\d.e]+), y: ([-\d.e]+)\}', new_body)
            if w is None:
                w = float(keep.group(1))
            new_body = re.sub(
                r'm_SizeDelta: \{x: [-\d.e]+, y: [-\d.e]+\}',
                'm_SizeDelta: {x: %g, y: %g}' % (w, h), new_body)
            changed.append('%s size -> %gx%g' % (name, w, h))

        if name in ANCHORS:
            c = ANCHORS[name]
            for field in ('m_AnchorMin', 'm_AnchorMax', 'm_Pivot'):
                new_body = re.sub(r'%s: \{x: [-\d.e]+, y: [-\d.e]+\}' % field,
                                  '%s: {x: %g, y: %g}' % (field, c[0], c[1]), new_body)
            changed.append('%s anchor/pivot -> (%g, %g)' % (name, c[0], c[1]))

        if name in MOVES and MOVES[name][1] is not None:
            x, y = MOVES[name]
            new_body = re.sub(
                r'm_AnchoredPosition: \{x: [-\d.e]+, y: [-\d.e]+\}',
                'm_AnchoredPosition: {x: %g, y: %g}' % (x, y), new_body)
            changed.append('%s pos -> (%g, %g)' % (name, x, y))

    elif kind == '114':  # MonoBehaviour
        name = owner_name(body)

        if 'm_DragThreshold:' in body:
            new_body = re.sub(r'(?m)^  m_DragThreshold: \d+$',
                              '  m_DragThreshold: %d' % DRAG_THRESHOLD, new_body)
            changed.append('EventSystem drag threshold -> %d' % DRAG_THRESHOLD)

        if name in ALIGN and 'm_HorizontalAlignment:' in body:
            new_body = re.sub(r'm_HorizontalAlignment: \d+',
                              'm_HorizontalAlignment: %d' % ALIGN[name], new_body)
            changed.append('%s text alignment -> %d' % (name, ALIGN[name]))

        if name in FONT_SIZES and 'm_fontSize:' in body:
            size = FONT_SIZES[name]
            new_body = re.sub(r'm_fontSize: [\d.]+', 'm_fontSize: %g' % size, new_body)
            # Auto-size reads these two; left out of step they clamp the new size straight back
            new_body = re.sub(r'm_fontSizeBase: [\d.]+', 'm_fontSizeBase: %g' % size, new_body)
            changed.append('%s font -> %g' % (name, size))

        # Scoped to the shelf by name. StatRow (spacing 160) and CoinGroup (spacing 22) are
        # HorizontalLayoutGroups too, and an unscoped match silently flattened both.
        if name == 'Content' and 'HorizontalLayoutGroup' in body:
            new_body = re.sub(r'(?m)^  m_Spacing: [\d.]+$',
                              '  m_Spacing: %g' % LAYOUT_SPACING, new_body)
            new_body = re.sub(r'(?m)^    m_Left: \d+$',
                              '    m_Left: %d' % LAYOUT_PADDING, new_body)
            new_body = re.sub(r'(?m)^    m_Right: \d+$',
                              '    m_Right: %d' % LAYOUT_PADDING, new_body)
            changed.append('shelf spacing -> %g, padding -> %d' % (LAYOUT_SPACING, LAYOUT_PADDING))

    if new_body != body:
        pending[start] = (end, kind, anchor, new_body)

# ---------------------------------------------------------------- pass 3: deletions
# Last, so the passes above still see the object if it is also listed there. Spans marked here
# are dropped from the file; the parent's child entry goes with them, or the scene loads with a
# child pointing at nothing.
dropped = set()

for name in DELETE:
    victim = None
    for kind, anchor, body, start, end in docs:
        if kind == '1' and names.get(anchor) == name:
            victim = (anchor, body, start, end)
            break

    if victim is None:
        continue        # already gone; re-running the script is a no-op

    go, go_body, go_start, go_end = victim
    ids = {go} | set(re.findall(r'component: \{fileID: (\d+)\}', go_body))

    for kind, anchor, body, start, end in docs:
        if anchor in ids:
            dropped.add(start)
            pending.pop(start, None)

    # The parent's m_Children entry. Found by scanning rather than assumed, because the child
    # list lives on the parent's RectTransform, which this loop has no other handle on.
    for kind, anchor, body, start, end in docs:
        if kind != '224':
            continue
        hit = [i for i in ids if '- {fileID: %s}' % i in body]
        if not hit:
            continue
        cur = body_of(start, kind, anchor, body)
        for i in hit:
            drop = '- {fileID: %s}' % i
            cur = ''.join(ln for ln in cur.splitlines(True) if drop not in ln)
        pending[start] = (end, kind, anchor, cur)

    changed.append('%s deleted (%d documents)' % (name, len(ids)))

for start in sorted(set(pending) | dropped, reverse=True):
    if start in dropped:
        end = next(e for k, a, b, st, e in docs if st == start)
        s = s[:start] + s[end:]
        continue

    end, kind, anchor, new_body = pending[start]
    s = s[:start] + '--- !u!%s &%s\n%s' % (kind, anchor, new_body) + s[end:]

io.open(SCENE, 'w', encoding='utf-8', newline='').write(s.replace('\n', NEWLINE))

for line in changed:
    print('  ' + line)
print('%d edits written to %s' % (len(changed), SCENE))
