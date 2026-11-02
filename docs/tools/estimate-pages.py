import sys, re
# Letter page 15840 twips tall, margins 1000+1000 -> 13840 usable.
# Body line spacing 288 twips => ~48 lines/page. Text width ~6.97in @11pt Calibri ~= 95 chars.
USABLE, LINE, CPL = 13840, 288, 95
lines = open(sys.argv[1], encoding="utf-8").read().split("\n")
units = 0.0   # in twips
i = 0
incode = False
while i < len(lines):
    L = lines[i]
    if L.startswith("```"):
        incode = not incode; units += LINE*0.5; i += 1; continue
    if incode:
        units += 230; i += 1; continue          # 19 half-pt mono, tighter
    if not L.strip():
        i += 1; continue
    if re.match(r"^#{1,4} ", L):
        lvl = len(L.split()[0])
        units += {1:900, 2:760, 3:560, 4:460}[lvl]; i += 1; continue
    if re.match(r"^(---+)\s*$", L):
        units += 240; i += 1; continue
    if L.lstrip().startswith("|"):
        # table row: header/sep + content; wrap long rows
        txt = L.strip("| ").replace("|", " ")
        rows = max(1, -(-len(txt)//CPL))
        units += 0 if re.match(r"^\|[\s:|-]+\|$", L.strip()) else rows*300
        i += 1; continue
    # paragraph / bullet / quote: gather the wrapped block
    units += max(1, -(-len(L)//CPL)) * LINE
    i += 1
print("estimated pages: %.1f" % (units/USABLE))
