# -*- coding: utf-8 -*-
"""Structural AND semantic check on the scene.

The structural half proves the file will load. The semantic half proves it does what was asked --
which is the half that was missing when a rewire landed on HomeButton instead of CharactersButton
and every structural check still passed.
"""
import collections
import io
import re
import sys

path = sys.argv[1]
s = io.open(path, encoding='utf-8').read()
fail = []

BUTTON_GUID = '4e29b1a8efbd4b44bb3f3716e73f07ff'

# ---------------------------------------------------------------- structure
anchors = re.findall(r'(?m)^--- !u!\d+ &(\d+)', s)
print('documents                : %d' % len(anchors))
dupes = [a for a, n in collections.Counter(anchors).items() if n > 1]
if dupes:
    fail.append('duplicate anchors: %s' % dupes[:10])
defined = set(anchors)

dangling = set()
for m in re.finditer(r'\{fileID: (-?\d+)(, guid: [a-f0-9]+, type: \d+)?\}', s):
    fid, guid = m.group(1), m.group(2)
    if guid or fid == '0':
        continue
    if fid not in defined:
        dangling.add(fid)
if dangling:
    fail.append('dangling local fileIDs: %s' % sorted(dangling)[:10])

names = {}
for m in re.finditer(r'(?ms)^--- !u!1 &(\d+)\nGameObject:(.*?)(?=^--- !u!)', s + '--- !u!0 &0\n'):
    n = re.search(r'(?m)^  m_Name: (.*)$', m.group(2))
    names[m.group(1)] = n.group(1).strip() if n else '?'

rects, owner_of, parent_of = {}, {}, {}
for m in re.finditer(r'(?ms)^--- !u!224 &(\d+)\nRectTransform:(.*?)(?=^--- !u!)', s + '--- !u!0 &0\n'):
    rid, body = m.group(1), m.group(2)
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body).group(1)
    fa = re.search(r'm_Father: \{fileID: (\d+)\}', body).group(1)
    kids = re.findall(r'(?m)^  - \{fileID: (\d+)\}$', body.split('m_Father')[0])
    rects[rid] = (go, kids, fa)
    owner_of[rid] = go
    parent_of[rid] = fa

for rid, (go, kids, fa) in rects.items():
    for k in kids:
        if k not in rects:
            fail.append('%s lists child %s which is not a RectTransform' % (rid, k))
        elif rects[k][2] != rid:
            fail.append('%s lists child %s but it names father %s' % (rid, k, rects[k][2]))
    if fa != '0' and fa in rects and rid not in rects[fa][1]:
        fail.append('%s names father %s but is not among its children' % (rid, fa))


def path_of(go):
    out, rid = [], None
    for r, o in owner_of.items():
        if o == go:
            rid = r
            break
    while rid:
        out.append(names.get(owner_of[rid], '?'))
        rid = parent_of.get(rid)
        if rid == '0' or rid not in owner_of:
            break
    return '/'.join(reversed(out))


# ---------------------------------------------------------------- semantics
# Every Button resolved by the NAME of the GameObject it is actually on, never by its position
# in the file. Proximity in scene YAML means nothing.
wiring = {}
for m in re.finditer(r'(?ms)^--- !u!114 &(\d+)\nMonoBehaviour:(.*?)(?=^--- !u!)', s + '--- !u!0 &0\n'):
    body = m.group(2)
    if 'guid: ' + BUTTON_GUID not in body:
        continue
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body).group(1)
    methods = re.findall(r'm_MethodName: (\S+)', body)
    args = re.findall(r'm_BoolArgument: (\d)', body)
    wiring[path_of(go)] = (methods, args)

EXPECTED = {
    'Canvas/MainMenu/CharactersButton': (['ShowCharacters'], ['1']),
    'Canvas/Pause Buttons/HomeButton': (['BackToMenu'], ['0']),
    'Canvas/GameOverMenu/HomeButton': (['BackToMenu'], ['0']),
    'Canvas/CharactersMenu/BackButton': (['ShowCharacters'], ['0']),
}
print()
for p, (methods, args) in sorted(EXPECTED.items()):
    got = wiring.get(p)
    if got is None:
        print('%-42s MISSING' % p)
        fail.append('%s not found' % p)
        continue
    ok = got[0] == methods and got[1] == args
    print('%-42s %s %s' % (p, ','.join(got[0]) or '(none)', 'OK' if ok else '<-- WRONG'))
    if not ok:
        fail.append('%s wired to %s arg %s, expected %s arg %s'
                    % (p, got[0], got[1], methods, args))

# the shelf buttons must stay unwired: FishInventory adds their listeners at runtime
shelf = [p for p in wiring if p.endswith('/ActionButton')]
print('\nshelf ActionButtons      : %d (all unwired)' % len(shelf))
for p in shelf:
    if wiring[p][0]:
        fail.append('%s has a persistent OnClick; it should be wired in code' % p)

# ---------------------------------------------------------------- shop wiring
# The button sprites were assigned from filenames before anyone looked at them, and the two that
# matter say the opposite of what their names suggest. Pinned by GUID so the swap cannot quietly
# come back: a locked card reading EQUIPPED is the symptom.
FISHINV_GUID = 'f0ba9fafd4ed22c4e84b8dfc86465dba'
SPRITE_NAME = {
    'edc04efc0b2d5f3489d548cbebdc6aa9': 'Equipped.png (reads EQUIPPED)',
    '8a105be730dae2d4d840e5f511a3202a': 'Buy.png (reads BUY)',
    '3a8aa385fbb21184d90cf5011a902c9e': 'Select Button.png (reads SELECT)',
}
WANT_SPRITE = {
    'buyArt': '8a105be730dae2d4d840e5f511a3202a',
    'equippedArt': 'edc04efc0b2d5f3489d548cbebdc6aa9',
    'selectArt': '3a8aa385fbb21184d90cf5011a902c9e',
}

inv = None
for m in re.finditer(r'(?ms)^--- !u!114 &(\d+)\nMonoBehaviour:(.*?)(?=^--- !u!)', s + '--- !u!0 &0\n'):
    if 'guid: ' + FISHINV_GUID in m.group(2):
        inv = m.group(2)
        break

print()
if inv is None:
    fail.append('FishInventory component not found in the scene')
else:
    for field, want in sorted(WANT_SPRITE.items()):
        hit = re.search(r'(?m)^  %s: \{fileID: \d+, guid: (\w+)' % field, inv)
        got = hit.group(1) if hit else None
        ok = got == want
        print('%-26s %-28s %s' % (field, SPRITE_NAME.get(got, got), 'OK' if ok else '<-- WRONG'))
        if not ok:
            fail.append('%s is %s, expected %s'
                        % (field, SPRITE_NAME.get(got, got), SPRITE_NAME[want]))

    # the run counters are hidden on the menu, so this is the only balance the shop screen shows
    wallet = re.search(r'(?m)^  walletText: \{fileID: (-?\d+)\}', inv)
    wired = bool(wallet) and wallet.group(1) != '0'
    print('%-26s %-28s %s' % ('walletText', 'wired' if wired else 'EMPTY',
                              'OK' if wired else '<-- WRONG'))
    if not wired:
        fail.append('walletText is not wired, so the shop screen shows no balance')

# One price label per block. Found at runtime by GetComponentInChildren<TMP_Text>, so the check is
# that each block has exactly one TMP descendant -- two would make which one wins a coin toss.
tmp_owners = set()
for m in re.finditer(r'(?ms)^--- !u!114 &\d+\nMonoBehaviour:(.*?)(?=^--- !u!)', s + '--- !u!0 &0\n'):
    if 'm_fontAsset' in m.group(1):
        tmp_owners.add(re.search(r'm_GameObject: \{fileID: (\d+)\}', m.group(1)).group(1))

rid_of = dict((go, rid) for rid, go in owner_of.items())


def descendants(go):
    out, stack = [], list(rects.get(rid_of.get(go), (None, [], None))[1])
    while stack:
        rid = stack.pop()
        if rid not in rects:
            continue
        out.append(rects[rid][0])
        stack.extend(rects[rid][1])
    return out


blocks = sorted((names[go], go) for go in names if names[go].startswith('Block '))
print()
for name, go in blocks:
    n = sum(1 for d in descendants(go) if d in tmp_owners)
    print('%-26s %d price label(s)%s' % (name, n, '' if n == 1 else '  <-- WRONG'))
    if n != 1:
        fail.append('%s has %d TMP_Text descendants, expected exactly 1' % (name, n))
# Counts are relative, never pinned: the shelf grows when a fish is added and this check should
# not need editing for that. FishInventory frees every block but the last, so two is the minimum
# that still leaves something to buy.
if len(blocks) < 2:
    fail.append('expected at least 2 blocks (one free, one to buy), found %d' % len(blocks))
if len(shelf) != len(blocks):
    fail.append('%d blocks but %d ActionButtons' % (len(blocks), len(shelf)))

print()
if fail:
    print('FAILED')
    for f in fail:
        print('  - ' + f)
    sys.exit(1)
print('ALL CHECKS PASSED (structure + wiring)')
