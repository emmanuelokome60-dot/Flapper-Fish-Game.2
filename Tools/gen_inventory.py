# -*- coding: utf-8 -*-
"""Author the CharactersMenu shelf into SampleScene.unity.

Every component GUID below was read out of this project (the UGUI package cache and the scene
itself), never from memory. Run only with the Editor closed.
"""
import io
import re
import sys

SCENE = sys.argv[1]

# --- anchors that already exist in the scene -------------------------------------------------
CANVAS_RT = 2031526770          # Canvas RectTransform, the new panel's parent
GAMEMANAGER = 706398570         # GameManager MonoBehaviour, the OnClick target
CHARBTN_BUTTON = 516573829      # CharactersButton's Button, whose OnClick gets repointed

# --- script GUIDs, verified against Library/PackageCache/com.unity.ugui@bb329a87fcdc ---------
G_IMAGE = 'fe87c0e1cc204ed48ad3b37840f39efc'
G_BUTTON = '4e29b1a8efbd4b44bb3f3716e73f07ff'
G_SCROLLRECT = '1aa08ab6e0800fa44ae55d278d1423e3'
G_MASK = '31a19414c41e5ae4aae2af33fee712f6'
G_HLG = '30649d3a9faa99c48a7b1166b86bf2a0'
G_CSF = '3245ec927659c4140ac4f8d17403cc18'
G_FISHINV = 'f0ba9fafd4ed22c4e84b8dfc86465dba'

# --- sprite GUIDs, from the .meta files ------------------------------------------------------
S_LOCKED = 'fc3c544e8eec1fc46a7b24cfa193f9de'
S_FISH = [
    '8f79f92a83b431344afede47150248a8',   # PEIXE1 - Abyssal Chomper, owned from the start
    '9a690c9ad412c604eb179b572dd63635',
    '4079667aa729ce247b560cee1777cdb2',
    '8a34eba5a3e7f9c4ca0e73ddcc2b082e',
    'ea3832eea5f8a1141be0a22cdc550c30',
]
S_BUY = 'edc04efc0b2d5f3489d548cbebdc6aa9'        # Group 26
S_SELECT = '3a8aa385fbb21184d90cf5011a902c9e'     # Select Button
S_EQUIPPED = '8a105be730dae2d4d840e5f511a3202a'   # Group 26-1

text = io.open(SCENE, encoding='utf-8', newline='').read()
NEWLINE = '\r\n' if '\r\n' in text else '\n'
text = text.replace('\r\n', '\n')

used = set(re.findall(r'(?m)^--- !u!\d+ &(\d+)', text))
counter = [880100000]


def new_id():
    while True:
        counter[0] += 1
        if str(counter[0]) not in used:
            used.add(str(counter[0]))
            return counter[0]


docs = []


def children_block(ids):
    if not ids:
        return ' []'
    return '\n' + ''.join('  - {fileID: %d}\n' % i for i in ids).rstrip('\n')


def add_gameobject(gid, name, components, active=1):
    docs.append(
        '--- !u!1 &%d\nGameObject:\n'
        '  m_ObjectHideFlags: 0\n'
        '  m_CorrespondingSourceObject: {fileID: 0}\n'
        '  m_PrefabInstance: {fileID: 0}\n'
        '  m_PrefabAsset: {fileID: 0}\n'
        '  serializedVersion: 6\n'
        '  m_Component:\n%s'
        '  m_Layer: 5\n'
        '  m_Name: %s\n'
        '  m_TagString: Untagged\n'
        '  m_Icon: {fileID: 0}\n'
        '  m_NavMeshLayer: 0\n'
        '  m_StaticEditorFlags: 0\n'
        '  m_IsActive: %d\n'
        % (gid, ''.join('  - component: {fileID: %d}\n' % c for c in components), name, active))


def add_rect(rid, gid, father, kids, amin, amax, pivot, pos, size):
    docs.append(
        '--- !u!224 &%d\nRectTransform:\n'
        '  m_ObjectHideFlags: 0\n'
        '  m_CorrespondingSourceObject: {fileID: 0}\n'
        '  m_PrefabInstance: {fileID: 0}\n'
        '  m_PrefabAsset: {fileID: 0}\n'
        '  m_GameObject: {fileID: %d}\n'
        '  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n'
        '  m_LocalPosition: {x: 0, y: 0, z: 0}\n'
        '  m_LocalScale: {x: 1, y: 1, z: 1}\n'
        '  m_ConstrainProportionsScale: 0\n'
        '  m_Children:%s\n'
        '  m_Father: {fileID: %d}\n'
        '  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
        '  m_AnchorMin: {x: %s, y: %s}\n'
        '  m_AnchorMax: {x: %s, y: %s}\n'
        '  m_AnchoredPosition: {x: %s, y: %s}\n'
        '  m_SizeDelta: {x: %s, y: %s}\n'
        '  m_Pivot: {x: %s, y: %s}\n'
        % (rid, gid, children_block(kids), father,
           amin[0], amin[1], amax[0], amax[1], pos[0], pos[1], size[0], size[1],
           pivot[0], pivot[1]))


def add_canvas_renderer(cid, gid):
    docs.append(
        '--- !u!222 &%d\nCanvasRenderer:\n'
        '  m_ObjectHideFlags: 0\n'
        '  m_CorrespondingSourceObject: {fileID: 0}\n'
        '  m_PrefabInstance: {fileID: 0}\n'
        '  m_PrefabAsset: {fileID: 0}\n'
        '  m_GameObject: {fileID: %d}\n'
        '  m_CullTransparentMesh: 1\n' % (cid, gid))


def add_behaviour(bid, gid, guid, cls, body):
    docs.append(
        '--- !u!114 &%d\nMonoBehaviour:\n'
        '  m_ObjectHideFlags: 0\n'
        '  m_CorrespondingSourceObject: {fileID: 0}\n'
        '  m_PrefabInstance: {fileID: 0}\n'
        '  m_PrefabAsset: {fileID: 0}\n'
        '  m_GameObject: {fileID: %d}\n'
        '  m_Enabled: 1\n'
        '  m_EditorHideFlags: 0\n'
        '  m_Script: {fileID: 11500000, guid: %s, type: 3}\n'
        '  m_Name: \n'
        '  m_EditorClassIdentifier: %s\n%s' % (bid, gid, guid, cls, body))


def sprite_ref(guid):
    return '{fileID: 21300000, guid: %s, type: 3}' % guid if guid else '{fileID: 0}'


def image_body(sprite, alpha=1, raycast=1, preserve=0):
    return (
        '  m_Material: {fileID: 0}\n'
        '  m_Color: {r: 1, g: 1, b: 1, a: %s}\n'
        '  m_RaycastTarget: %d\n'
        '  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
        '  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'
        '  m_Sprite: %s\n'
        '  m_Type: 0\n'
        '  m_PreserveAspect: %d\n'
        '  m_FillCenter: 1\n'
        '  m_FillMethod: 4\n'
        '  m_FillAmount: 1\n'
        '  m_FillClockwise: 1\n'
        '  m_FillOrigin: 0\n'
        '  m_UseSpriteMesh: 0\n'
        '  m_PixelsPerUnitMultiplier: 1\n' % (alpha, raycast, sprite_ref(sprite), preserve))


def bool_call(value):
    return (
        '\n      - m_Target: {fileID: %d}\n'
        '        m_TargetAssemblyTypeName: GameManager, Assembly-CSharp\n'
        '        m_MethodName: ShowCharacters\n'
        '        m_Mode: 6\n'
        '        m_Arguments:\n'
        '          m_ObjectArgument: {fileID: 0}\n'
        '          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine\n'
        '          m_IntArgument: 0\n'
        '          m_FloatArgument: 0\n'
        '          m_StringArgument: \n'
        '          m_BoolArgument: %d\n'
        '        m_CallState: 2' % (GAMEMANAGER, value))


def button_body(target_graphic, calls=''):
    return (
        '  m_Navigation:\n    m_Mode: 3\n    m_WrapAround: 0\n'
        '    m_SelectOnUp: {fileID: 0}\n    m_SelectOnDown: {fileID: 0}\n'
        '    m_SelectOnLeft: {fileID: 0}\n    m_SelectOnRight: {fileID: 0}\n'
        '  m_Transition: 1\n'
        '  m_Colors:\n'
        '    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}\n'
        '    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}\n'
        '    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}\n'
        '    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}\n'
        '    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}\n'
        '    m_ColorMultiplier: 1\n    m_FadeDuration: 0.1\n'
        '  m_SpriteState:\n'
        '    m_HighlightedSprite: {fileID: 0}\n    m_PressedSprite: {fileID: 0}\n'
        '    m_SelectedSprite: {fileID: 0}\n    m_DisabledSprite: {fileID: 0}\n'
        '  m_AnimationTriggers:\n'
        '    m_NormalTrigger: Normal\n    m_HighlightedTrigger: Highlighted\n'
        '    m_PressedTrigger: Pressed\n    m_SelectedTrigger: Selected\n'
        '    m_DisabledTrigger: Disabled\n'
        '  m_Interactable: 1\n'
        '  m_TargetGraphic: {fileID: %d}\n'
        '  m_OnClick:\n    m_PersistentCalls:\n      m_Calls:%s\n'
        % (target_graphic, calls if calls else ' []'))


# ---------------------------------------------------------------------------------------------
# Build bottom-up: a parent needs its children's RectTransform ids before it can be written.
# ---------------------------------------------------------------------------------------------

# Transform ids are reserved before their children are written, because a child has to name its
# father at the moment it is created. Built bottom-up without this, every child ends up with
# m_Father 0 and Unity opens the scene with the hierarchy flattened to the root.
menu_rt = new_id()
scroll_rt = new_id()
view_rt = new_id()
content_rt = new_id()

# Five blocks, each with an ActionButton child.
block_rects = []
for n in range(5):
    blk_rt = new_id()
    btn_go, btn_rt, btn_cr, btn_img, btn_btn = (new_id() for _ in range(5))
    add_canvas_renderer(btn_cr, btn_go)
    add_behaviour(btn_img, btn_go, G_IMAGE, 'UnityEngine.UI::UnityEngine.UI.Image',
                  image_body(S_SELECT))
    add_behaviour(btn_btn, btn_go, G_BUTTON, 'UnityEngine.UI::UnityEngine.UI.Button',
                  button_body(btn_img))
    add_rect(btn_rt, btn_go, blk_rt, [], (0.5, 0), (0.5, 0), (0.5, 0), (0, 45), (220, 58))
    add_gameobject(btn_go, 'ActionButton', [btn_rt, btn_cr, btn_img, btn_btn])

    blk_go, blk_cr, blk_img = (new_id() for _ in range(3))
    add_canvas_renderer(blk_cr, blk_go)
    # Raycast off: the card is scenery, the button on it is what takes the tap. Left on, a drag
    # that starts on the card would still scroll, but the card would also swallow stray taps.
    add_behaviour(blk_img, blk_go, G_IMAGE, 'UnityEngine.UI::UnityEngine.UI.Image',
                  image_body(S_LOCKED, raycast=0, preserve=1))
    add_rect(blk_rt, blk_go, content_rt, [btn_rt],
             (0.5, 0.5), (0.5, 0.5), (0.5, 0.5), (0, 0), (470, 546))
    add_gameobject(blk_go, 'Block %d' % (n + 1), [blk_rt, blk_cr, blk_img])
    block_rects.append((blk_rt, btn_rt))

# Content: the layout group spaces the blocks, the fitter grows it to hold them.
content_go, content_hlg, content_csf = (new_id() for _ in range(3))
add_behaviour(content_hlg, content_go, G_HLG,
              'UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup',
              '  m_Padding:\n    m_Left: 40\n    m_Right: 40\n    m_Top: 0\n    m_Bottom: 0\n'
              '  m_ChildAlignment: 4\n'
              '  m_Spacing: 55\n'
              '  m_ChildForceExpandWidth: 0\n  m_ChildForceExpandHeight: 0\n'
              '  m_ChildControlWidth: 0\n  m_ChildControlHeight: 0\n'
              '  m_ChildScaleWidth: 0\n  m_ChildScaleHeight: 0\n'
              '  m_ReverseArrangement: 0\n')
add_behaviour(content_csf, content_go, G_CSF,
              'UnityEngine.UI::UnityEngine.UI.ContentSizeFitter',
              '  m_HorizontalFit: 2\n  m_VerticalFit: 0\n')
add_rect(content_rt, content_go, view_rt, [b[0] for b in block_rects],
         (0, 0), (0, 1), (0, 0.5), (0, 0), (0, 0))
add_gameobject(content_go, 'Content', [content_rt, content_hlg, content_csf])

# Viewport: the mask. Its Image is the mask shape, so it is invisible but must exist.
view_go, view_cr, view_img, view_mask = (new_id() for _ in range(4))
add_canvas_renderer(view_cr, view_go)
add_behaviour(view_img, view_go, G_IMAGE, 'UnityEngine.UI::UnityEngine.UI.Image',
              image_body(None))
add_behaviour(view_mask, view_go, G_MASK, 'UnityEngine.UI::UnityEngine.UI.Mask',
              '  m_ShowMaskGraphic: 0\n')
add_rect(view_rt, view_go, scroll_rt, [content_rt], (0, 0), (1, 1), (0, 1), (0, 0), (0, 0))
add_gameobject(view_go, 'Viewport', [view_rt, view_cr, view_img, view_mask])

# Scroll View. Alpha 0 on its own Image: the blur belongs behind it, not a grey panel.
scroll_go, scroll_cr, scroll_img, scroll_sr = (new_id() for _ in range(4))
add_canvas_renderer(scroll_cr, scroll_go)
add_behaviour(scroll_img, scroll_go, G_IMAGE, 'UnityEngine.UI::UnityEngine.UI.Image',
              image_body(None, alpha=0))
add_behaviour(scroll_sr, scroll_go, G_SCROLLRECT, 'UnityEngine.UI::UnityEngine.UI.ScrollRect',
              '  m_Content: {fileID: %d}\n'
              '  m_Horizontal: 1\n  m_Vertical: 0\n'
              '  m_MovementType: 1\n  m_Elasticity: 0.1\n'
              '  m_Inertia: 1\n  m_DecelerationRate: 0.135\n'
              '  m_ScrollSensitivity: 1\n'
              '  m_Viewport: {fileID: %d}\n'
              '  m_HorizontalScrollbar: {fileID: 0}\n  m_VerticalScrollbar: {fileID: 0}\n'
              '  m_HorizontalScrollbarVisibility: 0\n  m_VerticalScrollbarVisibility: 0\n'
              '  m_HorizontalScrollbarSpacing: 0\n  m_VerticalScrollbarSpacing: 0\n'
              '  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'
              % (content_rt, view_rt))
add_rect(scroll_rt, scroll_go, menu_rt, [view_rt], (0.5, 0.5), (0.5, 0.5), (0.5, 0.5), (0, 0),
         (1700, 620))
add_gameobject(scroll_go, 'Scroll View', [scroll_rt, scroll_cr, scroll_img, scroll_sr])

# Close button, top-right.
close_go, close_rt, close_cr, close_img, close_btn = (new_id() for _ in range(5))
add_canvas_renderer(close_cr, close_go)
add_behaviour(close_img, close_go, G_IMAGE, 'UnityEngine.UI::UnityEngine.UI.Image',
              image_body(S_SELECT))
add_behaviour(close_btn, close_go, G_BUTTON, 'UnityEngine.UI::UnityEngine.UI.Button',
              button_body(close_img, bool_call(0)))
add_rect(close_rt, close_go, menu_rt, [], (1, 1), (1, 1), (1, 1), (-60, -60), (220, 58))
add_gameobject(close_go, 'CloseButton', [close_rt, close_cr, close_img, close_btn])

# The panel itself, inactive until ShowCharacters opens it.
menu_go, menu_fi = (new_id() for _ in range(2))
add_behaviour(menu_fi, menu_go, G_FISHINV, 'Assembly-CSharp::FishInventory',
              '  content: {fileID: %d}\n'
              '  lockedArt: %s\n'
              '  fishArt:\n%s'
              '  buyArt: %s\n'
              '  selectArt: %s\n'
              '  equippedArt: %s\n'
              '  price: 300\n'
              '  walletText: {fileID: 0}\n'
              % (content_rt, sprite_ref(S_LOCKED),
                 ''.join('  - %s\n' % sprite_ref(g) for g in S_FISH),
                 sprite_ref(S_BUY), sprite_ref(S_SELECT), sprite_ref(S_EQUIPPED)))
add_rect(menu_rt, menu_go, CANVAS_RT, [scroll_rt, close_rt],
         (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
add_gameobject(menu_go, 'CharactersMenu', [menu_rt, menu_fi], active=0)

# --- splice the new documents in, then repoint the three existing references -----------------
text = text.rstrip('\n') + '\n' + ''.join(docs)

# Canvas adopts the panel.
canvas_rect = re.search(r'(?ms)^--- !u!224 &%d\nRectTransform:.*?(?=^--- !u!)' % CANVAS_RT, text)
assert canvas_rect, 'Canvas RectTransform not found'
block = canvas_rect.group(0)
assert '  m_Children:\n' in block, 'Canvas has no children list'
patched = block.replace('  m_Children:\n', '  m_Children:\n  - {fileID: %d}\n' % menu_rt, 1)
text = text[:canvas_rect.start()] + patched + text[canvas_rect.end():]

# GameManager learns about the panel. The field is declared right after mainMenu.
gm = re.search(r'(?ms)^--- !u!114 &%d\nMonoBehaviour:.*?(?=^--- !u!)' % GAMEMANAGER, text)
assert gm, 'GameManager MonoBehaviour not found'
body = gm.group(0)
assert re.search(r'(?m)^  mainMenu: \{fileID: \d+\}$', body), 'mainMenu field not found'
if 'charactersMenu:' in body:
    body = re.sub(r'(?m)^  charactersMenu: \{fileID: \d+\}$',
                  '  charactersMenu: {fileID: %d}' % menu_go, body)
else:
    body = re.sub(r'(?m)^(  mainMenu: \{fileID: \d+\})$',
                  r'\1\n  charactersMenu: {fileID: %d}' % menu_go, body, count=1)
text = text[:gm.start()] + body + text[gm.end():]

# CharactersButton opens the shop instead of going home.
btn = re.search(r'(?ms)^--- !u!114 &%d\nMonoBehaviour:.*?(?=^--- !u!)' % CHARBTN_BUTTON, text)
assert btn, 'CharactersButton Button not found'
body = btn.group(0)
head, sep, _ = body.partition('  m_OnClick:\n')
assert sep, 'CharactersButton has no m_OnClick'
body = head + '  m_OnClick:\n    m_PersistentCalls:\n      m_Calls:%s\n' % bool_call(1)
text = text[:btn.start()] + body + text[btn.end():]

io.open(SCENE, 'w', encoding='utf-8', newline=NEWLINE).write(text)

print('CharactersMenu GameObject : %d' % menu_go)
print('  RectTransform           : %d' % menu_rt)
print('  FishInventory           : %d' % menu_fi)
print('  Content RectTransform   : %d' % content_rt)
print('documents added           : %d' % len(docs))
