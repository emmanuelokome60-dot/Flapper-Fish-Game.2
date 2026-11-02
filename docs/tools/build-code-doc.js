// Markdown -> .docx for the Abyssal Fish code guide.
// Handles the subset the guide actually uses: #..#### headings, paragraphs,
// - and 1. lists, | tables |, ``` code fences, > quotes, --- rules,
// and inline **bold** / `code` / *italic*.
const fs = require("fs");
const path = require("path");
const {
  Document, Packer, Paragraph, TextRun, HeadingLevel, Table, TableRow, TableCell,
  WidthType, BorderStyle, AlignmentType, ShadingType, PageBreak,
} = require(process.argv[4]);

const SRC = process.argv[2];
const OUT = process.argv[3];

const INK = "1A1A1A";
const RULE = "C8CED2";
const CODEBG = "F2F5F6";
const HEADBG = "DCE6E8";
const QUOTEBG = "EAF2F1";
const TEAL = "1A6B66";

// ---- inline: **bold**, `code`, *italic* -------------------------------------
function runs(text, opts = {}) {
  const base = { size: opts.size || 22, color: opts.color || INK, font: "Calibri" };
  const out = [];
  // split on the three inline markers, keeping delimiters
  const re = /(\*\*[^*]+\*\*|`[^`]+`|\*[^*\s][^*]*\*)/g;
  let last = 0, m;
  const push = (t, extra) => { if (t) out.push(new TextRun({ ...base, ...extra, text: t })); };
  while ((m = re.exec(text)) !== null) {
    push(text.slice(last, m.index), {});
    const tok = m[0];
    if (tok.startsWith("**")) push(tok.slice(2, -2), { bold: true });
    else if (tok.startsWith("`")) {
      out.push(new TextRun({ text: tok.slice(1, -1), font: "Consolas", size: base.size - 2, color: "0B4F4A" }));
    } else push(tok.slice(1, -1), { italics: true });
    last = m.index + tok.length;
  }
  push(text.slice(last), {});
  return out.length ? out : [new TextRun({ ...base, text: "" })];
}

// ---- block builders ---------------------------------------------------------
const heading = (text, level) => {
  const map = {
    1: { h: HeadingLevel.HEADING_1, size: 40, before: 420, after: 200, color: TEAL },
    2: { h: HeadingLevel.HEADING_2, size: 32, before: 380, after: 160, color: TEAL },
    3: { h: HeadingLevel.HEADING_3, size: 26, before: 300, after: 120, color: INK },
    4: { h: HeadingLevel.HEADING_4, size: 23, before: 260, after: 100, color: INK },
  }[level];
  return new Paragraph({
    heading: map.h,
    spacing: { before: map.before, after: map.after },
    keepNext: true,
    pageBreakBefore: level === 1 && !/^Part 0/.test(text),
    children: [new TextRun({ text, bold: true, size: map.size, color: map.color, font: "Calibri" })],
  });
};

const body = (text) => new Paragraph({ spacing: { after: 140, line: 288 }, children: runs(text) });

const bullet = (text, n) => new Paragraph({
  bullet: { level: 0 }, spacing: { after: 70, line: 276 },
  indent: { left: 360 + (n || 0) * 360 }, children: runs(text),
});

const numbered = (text, i) => new Paragraph({
  spacing: { after: 70, line: 276 }, indent: { left: 720, hanging: 360 },
  children: [new TextRun({ text: i + ".  ", bold: true, size: 22, font: "Calibri", color: TEAL }), ...runs(text)],
});

const codeBlock = (lines) => new Paragraph({
  shading: { type: ShadingType.CLEAR, fill: CODEBG },
  spacing: { before: 120, after: 160 },
  border: { left: { style: BorderStyle.SINGLE, size: 18, color: TEAL, space: 8 } },
  indent: { left: 180, right: 120 },
  children: lines.flatMap((l, i) => [
    ...(i ? [new TextRun({ break: 1 })] : []),
    new TextRun({ text: l || " ", font: "Consolas", size: 19, color: "102A28" }),
  ]),
});

const quote = (lines) => new Paragraph({
  shading: { type: ShadingType.CLEAR, fill: QUOTEBG },
  spacing: { before: 140, after: 180 },
  border: { left: { style: BorderStyle.SINGLE, size: 24, color: TEAL, space: 10 } },
  indent: { left: 180, right: 180 },
  children: runs(lines.join(" ")),
});

const rule = () => new Paragraph({
  spacing: { before: 120, after: 120 },
  border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: RULE, space: 6 } },
  children: [new TextRun({ text: "" })],
});

const cells = (row) => row.replace(/^\|/, "").replace(/\|$/, "").split("|").map((c) => c.trim());

function table(rows) {
  const head = cells(rows[0]);
  const bodyRows = rows.slice(2).map(cells);
  const mk = (txt, isHead) => new TableCell({
    shading: isHead ? { type: ShadingType.CLEAR, fill: HEADBG } : undefined,
    margins: { top: 90, bottom: 90, left: 130, right: 130 },
    children: [new Paragraph({
      spacing: { after: 0, line: 264 },
      children: isHead
        ? [new TextRun({ text: txt, bold: true, size: 21, font: "Calibri", color: INK })]
        : runs(txt, { size: 21 }),
    })],
  });
  return new Table({
    width: { size: 100, type: WidthType.PERCENTAGE },
    borders: {
      top: { style: BorderStyle.SINGLE, size: 4, color: RULE },
      bottom: { style: BorderStyle.SINGLE, size: 4, color: RULE },
      left: { style: BorderStyle.SINGLE, size: 4, color: RULE },
      right: { style: BorderStyle.SINGLE, size: 4, color: RULE },
      insideHorizontal: { style: BorderStyle.SINGLE, size: 4, color: RULE },
      insideVertical: { style: BorderStyle.SINGLE, size: 4, color: RULE },
    },
    rows: [
      new TableRow({ tableHeader: true, children: head.map((h) => mk(h, true)) }),
      ...bodyRows.map((r) => new TableRow({
        children: head.map((_, i) => mk(r[i] === undefined ? "" : r[i], false)),
      })),
    ],
  });
}

// ---- parse ------------------------------------------------------------------
const lines = fs.readFileSync(SRC, "utf8").split(/\r?\n/);
const out = [];
let i = 0, para = [], num = 0;

const flush = () => { if (para.length) { out.push(body(para.join(" "))); para = []; } };

while (i < lines.length) {
  const L = lines[i];

  if (/^\s*$/.test(L)) { flush(); num = 0; i++; continue; }

  if (/^```/.test(L)) {
    flush();
    const buf = [];
    i++;
    while (i < lines.length && !/^```/.test(lines[i])) buf.push(lines[i++]);
    i++;
    out.push(codeBlock(buf));
    continue;
  }

  if (/^(---+|\*\*\*+)\s*$/.test(L)) { flush(); out.push(rule()); i++; continue; }

  const h = L.match(/^(#{1,4})\s+(.*)$/);
  if (h) { flush(); out.push(heading(h[2].trim(), h[1].length)); i++; continue; }

  if (/^\s*\|/.test(L)) {
    flush();
    const buf = [];
    while (i < lines.length && /^\s*\|/.test(lines[i])) buf.push(lines[i++].trim());
    if (buf.length >= 2) out.push(table(buf)); else buf.forEach((b) => out.push(body(b)));
    out.push(new Paragraph({ spacing: { after: 180 }, children: [new TextRun("")] }));
    continue;
  }

  if (/^\s*>/.test(L)) {
    flush();
    const buf = [];
    while (i < lines.length && /^\s*>/.test(lines[i])) buf.push(lines[i++].replace(/^\s*>\s?/, ""));
    out.push(quote(buf.filter((x) => x.trim())));
    continue;
  }

  const b = L.match(/^(\s*)[-*]\s+(.*)$/);
  if (b) {
    flush();
    let txt = b[2];
    while (i + 1 < lines.length && /^\s{2,}\S/.test(lines[i + 1]) && !/^\s*[-*|>#]/.test(lines[i + 1])) {
      txt += " " + lines[++i].trim();
    }
    out.push(bullet(txt, Math.floor(b[1].length / 2)));
    i++;
    continue;
  }

  const n = L.match(/^\s*(\d+)\.\s+(.*)$/);
  if (n) {
    flush();
    let txt = n[2];
    while (i + 1 < lines.length && /^\s{3,}\S/.test(lines[i + 1]) && !/^\s*[-*|>#]/.test(lines[i + 1])) {
      txt += " " + lines[++i].trim();
    }
    out.push(numbered(txt, ++num));
    i++;
    continue;
  }

  para.push(L.trim());
  i++;
}
flush();

// ---- title page -------------------------------------------------------------
const centre = (text, opts) => new Paragraph({
  alignment: AlignmentType.CENTER, spacing: { after: opts.after || 160 },
  children: [new TextRun({ text, font: "Calibri", ...opts })],
});

const title = [
  new Paragraph({ spacing: { after: 2400 }, children: [new TextRun("")] }),
  centre("ABYSSAL FISH", { bold: true, size: 64, color: TEAL, after: 120 }),
  centre("The code, explained", { size: 36, color: INK, after: 600 }),
  centre("A plain-English guide to all 18 scripts", { size: 24, color: "4A5A5C", after: 100 }),
  centre("Every variable. Every method. What each one does in the game.", { size: 24, color: "4A5A5C", after: 1400 }),
  centre("Unity 6000.3.8f1  ·  2D URP  ·  18 scripts  ·  3,163 lines", { size: 20, color: "6A7A7C", after: 100 }),
  centre("Numbers read from SampleScene.unity on 2 November 2026", { size: 20, color: "6A7A7C", italics: true, after: 0 }),
  new Paragraph({ children: [new PageBreak()] }),
];

const doc = new Document({
  creator: "Abyssal Team",
  title: "Abyssal Fish - The code, explained",
  description: "Every variable and method in all 18 scripts, in plain English.",
  styles: { default: { document: { run: { font: "Calibri", size: 22, color: INK } } } },
  sections: [{
    properties: { page: { margin: { top: 1000, bottom: 1000, left: 1100, right: 1100 } } },
    children: [...title, ...out],
  }],
});

Packer.toBuffer(doc).then((buf) => {
  fs.writeFileSync(OUT, buf);
  console.log("wrote " + OUT + "  (" + (buf.length / 1024).toFixed(0) + " KB, " + out.length + " blocks)");
});
