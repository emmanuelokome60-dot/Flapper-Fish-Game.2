# -*- coding: utf-8 -*-
"""Point CharactersButton at ShowCharacters(true) and give the pause HomeButton BackToMenu back.

Components are resolved by their owning GameObject's NAME, never by position in the file. That is
the mistake that caused this: a Button sitting near CharactersButton's GameObject in the YAML
belonged to HomeButton.
"""
import io
import re
import sys

SCENE = sys.argv[1]
GAMEMANAGER = 706398570
BUTTON_GUID = '4e29b1a8efbd4b44bb3f3716e73f07ff'

s = io.open(SCENE, encoding='utf-8', newline='').read()
NEWLINE = '\r\n' if '\r\n' in s else '\n'
s = s.replace('\r\n', '\n')

# name every GameObject, and record each one's parent so the two HomeButtons can be told apart
names = {}
for m in re.finditer(r'(?ms)^--- !u!1 &(\d+)\nGameObject:(.*?)(?=^--- !u!)', s + '--- !u!0 &0\n'):
    n = re.search(r'(?m)^  m_Name: (.*)$', m.group(2))
    names[m.group(1)] = n.group(1).strip() if n else '?'

rect_of = {}
parent_of = {}
for m in re.finditer(r'(?ms)^--- !u!224 &(\d+)\nRectTransform:(.*?)(?=^--- !u!)', s + '--- !u!0 &0\n'):
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', m.group(2)).group(1)
    fa = re.search(r'm_Father: \{fileID: (\d+)\}', m.group(2)).group(1)
    rect_of[go] = m.group(1)
    parent_of[m.group(1)] = fa


def path_of(go):
    out, rt = [], rect_of.get(go)
    while rt and rt in parent_of:
        owner = next((g for g, r in rect_of.items() if r == rt), None)
        out.append(names.get(owner, '?'))
        rt = parent_of[rt]
        if rt == '0':
            break
    return '/'.join(reversed(out))


def call(method, mode, boolarg):
    return (
        '\n      - m_Target: {fileID: %d}\n'
        '        m_TargetAssemblyTypeName: GameManager, Assembly-CSharp\n'
        '        m_MethodName: %s\n'
        '        m_Mode: %d\n'
        '        m_Arguments:\n'
        '          m_ObjectArgument: {fileID: 0}\n'
        '          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine\n'
        '          m_IntArgument: 0\n'
        '          m_FloatArgument: 0\n'
        '          m_StringArgument: \n'
        '          m_BoolArgument: %d\n'
        '        m_CallState: 2' % (GAMEMANAGER, method, mode, boolarg))


# what each button should end up with, keyed by hierarchy path
wanted = {
    'Canvas/MainMenu/CharactersButton': call('ShowCharacters', 6, 1),
    'Canvas/Pause Buttons/HomeButton': call('BackToMenu', 1, 0),
}

done = []
for m in list(re.finditer(r'(?ms)^--- !u!114 &(\d+)\nMonoBehaviour:(.*?)(?=^--- !u!)',
                          s + '--- !u!0 &0\n')):
    bid, body = m.group(1), m.group(2)
    if 'guid: ' + BUTTON_GUID not in body:
        continue
    go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body).group(1)
    p = path_of(go)
    if p not in wanted:
        continue

    whole = m.group(0)
    head, sep, _ = whole.partition('  m_OnClick:\n')
    assert sep, '%s has no m_OnClick' % p
    replacement = head + '  m_OnClick:\n    m_PersistentCalls:\n      m_Calls:%s\n' % wanted[p]
    s = s.replace(whole, replacement, 1)
    done.append((p, bid))

io.open(SCENE, 'w', encoding='utf-8', newline=NEWLINE).write(s)
for p, bid in done:
    print('patched %-36s (Button &%s)' % (p, bid))
assert len(done) == 2, 'expected to patch 2 buttons, patched %d' % len(done)
