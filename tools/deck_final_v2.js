/*
 * RESIDUUM — Final Presentation deck (v2)
 * 16 slides · 16:9 · ~8 minutes spoken at a calm pace, no live demo.
 *
 * Design brief: the deck should look like the game. The game is one warm
 * flashlight cone inside a cold, near-black house. So: almost every slide is
 * mostly empty black, one idea sits inside a soft amber pool of light, cold
 * blue carries all the structure (labels, tables, rules), and RED appears on
 * exactly two slides — the hunt, and the moment the gate rejects the AI's work.
 * Same colour, same meaning both times: something is pushing back at you.
 *
 * The thin bar along the bottom is the player's sanity meter. It drains as the
 * talk goes on. It is the progress bar, and it is also the joke.
 *
 * Build:  npm install --no-save pptxgenjs && node tools/deck_final_v2.js
 * Out:    Docs/decks/RESIDUUM_Final_v2.pptx
 */

const pptxgen = require("pptxgenjs");
const path = require("path");

/* ── Palette ─────────────────────────────────────────────────────────────── */
const BG      = "06080B";  // the house with the lights off
const PANEL   = "0E141D";
const PANEL2  = "141C28";
const LINE    = "1E2836";
const MOON    = "6E96C4";  // cold moonlight — structure, labels, rules
const MOONBR  = "A6C6E8";
const AMBER   = "D89A45";  // the flashlight — the human, the player, the point
const AMBERBR = "F0C57E";
const RED     = "B4232F";  // the hunt. used twice. that is the whole budget.
const REDBR   = "FF5A66";
const TEXT    = "EDF1F6";
const DIM     = "8E9BAD";
const FAINT   = "4E5A6B";

const FD = "Georgia";      // display + numerals
const F  = "Calibri";      // body
const FM = "Courier New";  // instrument readouts, file paths, labels

const W = 13.333, H = 7.5;
const ML = 0.8, MR = 0.8;
const CW = W - ML - MR;    // 11.733

/* Sanity per slide. Drains 100 → 11. Colour flips at 50 and 20, same as the
 * game: cold while you are fine, warm while you are worried, red at the end. */
const SANITY = [100, 95, 89, 82, 74, 65, 55, 48, 42, 37, 32, 26, 21, 17, 14, 11];
const TOTAL = SANITY.length;

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE";
pres.author = "Henry";
pres.company = "RESIDUUM";
pres.title = "RESIDUUM — Final Presentation";
pres.subject = "A first-person horror deduction game, and how we built it in one week.";

let idx = 0;

/* ── Helpers ─────────────────────────────────────────────────────────────── */

// A soft pool of light. Three stacked ellipses at high transparency fake the
// falloff; PowerPoint gradients are not portable enough to rely on.
function glow(s, cx, cy, w, h, color) {
  const c = color || AMBER;
  // Many faint layers, not few strong ones — three rings looked like a target.
  [1.0, 0.88, 0.76, 0.64, 0.53, 0.43, 0.34, 0.26, 0.19].forEach(k => {
    const t = 97;
    s.addShape(pres.ShapeType.ellipse, {
      x: cx - (w * k) / 2, y: cy - (h * k) / 2, w: w * k, h: h * k,
      fill: { color: c, transparency: t }, line: { type: "none" },
    });
  });
}

function sanityBar(s, n) {
  const v = SANITY[n - 1];
  const col = v >= 50 ? MOON : v >= 20 ? AMBER : RED;
  const trackW = CW - 1.9;
  s.addShape(pres.ShapeType.rect, {
    x: ML, y: 7.02, w: trackW, h: 0.055,
    fill: { color: LINE }, line: { type: "none" },
  });
  s.addShape(pres.ShapeType.rect, {
    x: ML, y: 7.02, w: trackW * (v / 100), h: 0.055,
    fill: { color: col }, line: { type: "none" },
  });
  s.addText(`SANITY ${String(v).padStart(3, " ")}%`, {
    x: W - MR - 1.75, y: 6.9, w: 1.75, h: 0.3, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 9.5, color: col, align: "right", charSpacing: 1,
  });
}

function newSlide(label) {
  idx++;
  const s = pres.addSlide();
  s.background = { color: BG };
  s.addText(label.toUpperCase(), {
    x: ML, y: 0.36, w: 8.5, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10, color: FAINT, charSpacing: 2.2,
  });
  s.addShape(pres.ShapeType.rect, {
    x: ML, y: 0.68, w: CW, h: 0.012, fill: { color: LINE }, line: { type: "none" },
  });
  sanityBar(s, idx);
  return s;
}

function title(s, t, opts) {
  const o = opts || {};
  s.addText(t, {
    x: ML, y: o.y || 1.02, w: o.w || CW, h: 0.72, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: o.size || 36, bold: true,
    color: o.color || TEXT, charSpacing: -0.2,
  });
}

function sub(s, t, opts) {
  const o = opts || {};
  s.addText(t, {
    x: ML, y: o.y || 1.78, w: o.w || CW - 1.6, h: o.h || 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: o.size || 15, color: o.color || MOON, lineSpacing: 21,
  });
}

function card(s, x, y, w, h, fill, stroke) {
  s.addShape(pres.ShapeType.roundRect, {
    x, y, w, h, rectRadius: 0.05,
    fill: { color: fill || PANEL },
    line: { color: stroke || LINE, width: 1 },
  });
}

// The one line at the bottom that people should still remember in the lift.
function punch(s, t, color, y) {
  s.addText(t, {
    x: ML, y: y || 6.14, w: CW - 2.0, h: 0.5, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 19, bold: true, color: color || AMBERBR,
  });
}

function foot(s, t, color, y) {
  s.addText(t, {
    x: ML, y: y || 6.42, w: CW - 2.0, h: 0.42, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12.5, italic: true, color: color || DIM,
  });
}

/* ══════════════════════ 01 · Cover ══════════════════════ */
{
  idx++;
  const s = pres.addSlide();
  s.background = { color: BG };
  glow(s, 4.4, 3.35, 9.0, 5.0, AMBER);

  s.addText("RESIDUUM", {
    x: ML, y: 2.28, w: 9.6, h: 1.5, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 78, bold: true, color: TEXT, charSpacing: 3,
  });
  s.addShape(pres.ShapeType.rect, {
    x: ML, y: 3.86, w: 2.1, h: 0.03, fill: { color: AMBER }, line: { type: "none" },
  });
  s.addText("A first-person horror game about knowing\nwhat you cannot see.", {
    x: ML, y: 4.1, w: 7.4, h: 0.9, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 18, color: MOONBR, lineSpacing: 27,
  });

  const meta = [
    ["ENGINE", "Unity 6 · URP"],
    ["PLAYERS", "Single player"],
    ["A ROUND", "8–12 minutes"],
    ["BUILT IN", "One week"],
  ];
  meta.forEach(([k, v], i) => {
    const x = ML + i * 2.62;
    s.addText(k, {
      x, y: 5.42, w: 2.4, h: 0.24, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 9, color: FAINT, charSpacing: 1.6,
    });
    s.addText(v, {
      x, y: 5.68, w: 2.4, h: 0.3, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, color: TEXT,
    });
  });

  s.addText("Final presentation", {
    x: W - MR - 3.2, y: 0.62, w: 3.2, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10, color: FAINT, align: "right", charSpacing: 1.6,
  });
  sanityBar(s, idx);
}

/* ══════════════════════ 02 · One sentence ══════════════════════ */
{
  const s = newSlide("the game · 01");
  title(s, "The whole game, in one sentence");

  card(s, ML, 1.95, CW, 1.65, PANEL, LINE);
  s.addShape(pres.ShapeType.rect, {
    x: ML, y: 1.95, w: 0.045, h: 1.65, fill: { color: AMBER }, line: { type: "none" },
  });
  s.addText(
    "You break into a house you should not be in, carrying three tools.\n" +
    "Before your sanity runs out, work out what is haunting it — and get back out alive.",
    {
      x: ML + 0.45, y: 2.16, w: CW - 0.9, h: 1.25, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 22, color: TEXT, lineSpacing: 36,
    });

  const stats = [
    ["3", "ghosts"],
    ["3", "kinds of evidence"],
    ["4", "tools, 3 slots"],
    ["2", "clues = 1 answer"],
  ];
  stats.forEach(([n, t], i) => {
    const x = ML + i * 2.98;
    card(s, x, 4.15, 2.72, 1.5, PANEL2, LINE);
    s.addText(n, {
      x: x + 0.3, y: 4.32, w: 2.1, h: 0.72, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 44, bold: true,
      color: i === 3 ? AMBERBR : MOONBR,
    });
    s.addText(t, {
      x: x + 0.32, y: 5.06, w: 2.3, h: 0.4, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: DIM,
    });
  });

  punch(s, "The last one is the whole design.", AMBERBR, 6.05);
}

/* ══════════════════════ 03 · The problem ══════════════════════ */
{
  const s = newSlide("the game · 02");
  title(s, "Most ghost games end in a guess");
  sub(s, "We played them. Here is the thing that always spoiled it for us.");

  card(s, ML, 2.5, 5.55, 3.1, PANEL, LINE);
  s.addText("What we saw", {
    x: ML + 0.42, y: 2.76, w: 4.7, h: 0.3, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10, color: FAINT, charSpacing: 1.6,
  });
  [
    "24 ghosts. 7 kinds of evidence. 40+ tools.",
    "Many ghosts share the same clue sets.",
    "So you collect everything you can…",
    "…and then you still guess between two.",
  ].forEach((t, i) => {
    s.addText(t, {
      x: ML + 0.42, y: 3.16 + i * 0.34, w: 4.85, h: 0.32, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, color: i === 3 ? REDBR : TEXT,
    });
  });

  card(s, ML + 6.18, 2.5, 5.55, 3.1, PANEL, LINE);
  s.addShape(pres.ShapeType.rect, {
    x: ML + 6.18, y: 2.5, w: 0.045, h: 3.1, fill: { color: AMBER }, line: { type: "none" },
  });
  s.addText("What we wanted instead", {
    x: ML + 6.6, y: 2.76, w: 4.9, h: 0.3, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10, color: FAINT, charSpacing: 1.6,
  });
  s.addText("A game where winning is never luck.", {
    x: ML + 6.6, y: 3.14, w: 4.9, h: 0.9, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 21, bold: true, color: AMBERBR, lineSpacing: 30,
  });
  s.addText(
    "If a player names the right ghost, it should be because they worked it out.\n" +
    "Not because they picked the more likely of two.",
    {
      x: ML + 6.6, y: 4.16, w: 4.85, h: 1.0, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, color: DIM, lineSpacing: 21,
    });

  punch(s, "We made the game smaller on purpose, until the guessing was gone.", TEXT, 6.02);
}

/* ══════════════════════ 04 · The table ══════════════════════ */
{
  const s = newSlide("the game · 03  ·  the centre of the design");
  title(s, "Three ghosts. Three clues. Two each.");
  sub(s, "Every ghost holds exactly two kinds of evidence. Not one. Not three.");

  const C0X = ML, C0W = 3.6;
  const CX = [4.62, 7.33, 10.04], CGW = 2.45;
  const HDR = ["EMF-5 READING", "UV FINGERPRINTS", "GHOST WRITING"];
  const ROWS = [
    ["Spirit",      "slow, stubborn, loud",   [1, 1, 0]],
    ["Wraith",      "silent, leaves no steps", [1, 0, 1]],
    ["Poltergeist", "throws things, faster than you", [0, 1, 1]],
  ];

  HDR.forEach((h, i) => {
    s.addText(h, {
      x: CX[i], y: 2.52, w: CGW, h: 0.3, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 10, color: MOON, align: "center", charSpacing: 1.2,
    });
  });
  s.addShape(pres.ShapeType.rect, {
    x: ML, y: 2.94, w: CW, h: 0.012, fill: { color: LINE }, line: { type: "none" },
  });

  ROWS.forEach(([name, flavour, marks], r) => {
    const y = 3.1 + r * 0.92;
    card(s, C0X, y, C0W, 0.78, PANEL, LINE);
    s.addText(name, {
      x: C0X + 0.28, y: y + 0.08, w: C0W - 0.5, h: 0.36, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 20, bold: true, color: TEXT,
    });
    s.addText(flavour, {
      x: C0X + 0.3, y: y + 0.45, w: C0W - 0.4, h: 0.26, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11, color: FAINT,
    });
    marks.forEach((m, c) => {
      card(s, CX[c], y, CGW, 0.78, m ? PANEL2 : PANEL, m ? "3A3020" : LINE);
      if (m) {
        s.addShape(pres.ShapeType.ellipse, {
          x: CX[c] + CGW / 2 - 0.21, y: y + 0.18, w: 0.42, h: 0.42,
          fill: { color: AMBER }, line: { type: "none" },
        });
      } else {
        s.addShape(pres.ShapeType.rect, {
          x: CX[c] + CGW / 2 - 0.22, y: y + 0.385, w: 0.44, h: 0.025,
          fill: { color: FAINT }, line: { type: "none" },
        });
      }
    });
  });

  punch(s, "Read any row: two dots. Read any column: two ghosts.", TEXT, 6.02);
  foot(s, "This is the one slide we would keep if we only had thirty seconds.", DIM, 6.46);
}

/* ══════════════════════ 05 · Why it always works ══════════════════════ */
{
  const s = newSlide("the game · 04  ·  why it cannot fail");
  title(s, "Pick 2 out of 3. There are only 3 ways.");
  sub(s, "Three ways, three ghosts, one each. Nothing repeated, nothing left over.");

  const PAIRS = [
    ["EMF  +  FINGERPRINTS", "Spirit"],
    ["EMF  +  WRITING",      "Wraith"],
    ["FINGERPRINTS  +  WRITING", "Poltergeist"],
  ];
  PAIRS.forEach(([pair, ghost], i) => {
    const x = ML + i * 4.0;
    card(s, x, 2.5, 3.68, 1.24, PANEL, LINE);
    s.addText(pair, {
      x: x + 0.28, y: 2.68, w: 3.2, h: 0.3, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 11, color: MOONBR, charSpacing: 0.6,
    });
    s.addText("→   " + ghost, {
      x: x + 0.26, y: 3.06, w: 3.2, h: 0.44, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 21, bold: true, color: AMBERBR,
    });
  });

  const POINTS = [
    ["One clue is never enough.",
     "Each clue is shared by two ghosts. One EMF reading only rules the Poltergeist out."],
    ["Two clues are always enough.",
     "There is never a second answer that fits. The table has no ties in it."],
    ["Proving a clue is absent counts too.",
     "Ruling out is as strong as finding. A careful player can win without the third tool."],
  ];
  POINTS.forEach(([h, b], i) => {
    const y = 4.06 + i * 0.68;
    s.addText(String(i + 1), {
      x: ML, y: y, w: 0.4, h: 0.36, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 17, bold: true, color: AMBER,
    });
    s.addText(h, {
      x: ML + 0.42, y: y - 0.02, w: 4.1, h: 0.36, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14.5, bold: true, color: TEXT,
    });
    s.addText(b, {
      x: ML + 4.6, y: y - 0.01, w: 7.1, h: 0.5, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: DIM,
    });
  });

  s.addText("NO GUESSING.   NO LUCK.   JUST LOGIC.", {
    x: ML, y: 6.12, w: CW - 2.0, h: 0.5, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 24, bold: true, color: AMBERBR, charSpacing: 1.2,
  });
}

/* ══════════════════════ 06 · Sanity ══════════════════════ */
{
  const s = newSlide("the game · 05  ·  the clock");
  title(s, "So why not just take your time?");
  sub(s, "Because the house is charging you for every second you stay in it.");

  card(s, ML, 2.5, 5.55, 3.42, PANEL, LINE);
  s.addText("SANITY, PER SECOND", {
    x: ML + 0.42, y: 2.72, w: 4.7, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10, color: FAINT, charSpacing: 1.6,
  });
  const DRAIN = [
    ["Standing in the dark", "−0.12", MOONBR],
    ["A room with the light on", "−0.06", MOONBR],
    ["Holding a lit flashlight", "× 0.5", AMBERBR],
    ["Seeing the ghost do something", "−15  once", REDBR],
    ["While it is hunting you", "−0.50", REDBR],
    ["Back in the lobby", "+1.00", MOONBR],
  ];
  DRAIN.forEach(([k, v, c], i) => {
    const y = 3.14 + i * 0.44;
    s.addText(k, {
      x: ML + 0.42, y, w: 3.7, h: 0.34, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: TEXT,
    });
    s.addText(v, {
      x: ML + 3.9, y, w: 1.4, h: 0.34, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 13, color: c, align: "right",
    });
  });

  card(s, ML + 6.18, 2.5, 5.55, 3.42, PANEL, LINE);
  s.addText("BELOW 50%, EVERY 25 SECONDS", {
    x: ML + 6.6, y: 2.72, w: 4.9, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10, color: FAINT, charSpacing: 1.6,
  });
  s.addText("P(hunt)  =  ( 50 − sanity ) / 50", {
    x: ML + 6.6, y: 3.12, w: 4.9, h: 0.56, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 23, bold: true, color: AMBERBR,
  });
  const P = [["sanity 50", "0%", MOON], ["sanity 25", "50%", AMBER], ["sanity 0", "certain", REDBR]];
  P.forEach(([k, v, c], i) => {
    const x = ML + 6.6 + i * 1.68;
    card(s, x, 3.94, 1.5, 1.02, PANEL2, LINE);
    s.addText(v, {
      x: x + 0.1, y: 4.06, w: 1.3, h: 0.44, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 19, bold: true, color: c, align: "center",
    });
    s.addText(k, {
      x: x + 0.1, y: 4.53, w: 1.3, h: 0.3, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 9.5, color: FAINT, align: "center",
    });
  });
  s.addText(
    "The player is never asking \"where is it?\" for long.\nThey are asking \"how much longer can I stay?\"",
    {
      x: ML + 6.6, y: 5.1, w: 4.9, h: 0.7, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: DIM, lineSpacing: 20,
    });

  punch(s, "Knowing more always costs you something. That trade is the game.", AMBERBR, 6.14);
}

/* ══════════════════════ 07 · Running (RED #1) ══════════════════════ */
{
  const s = newSlide("the game · 06  ·  the hunt");
  glow(s, 10.6, 4.0, 6.4, 5.2, RED);
  title(s, "When it hunts, running does not save you");
  sub(s, "We tuned six numbers by hand until fleeing became a choice with a price.", { color: REDBR });

  const BARX = ML + 2.6, BARMAX = 4.0, VMAX = 4.0;
  const SPEEDS = [
    ["You, walking", 2.0, MOON, false],
    ["You, sprinting", 3.5, AMBER, true],
    ["Spirit, hunting", 3.3, "7A3038", false],
    ["Wraith, hunting", 3.4, "97303C", false],
    ["Poltergeist, hunting", 3.6, RED, true],
  ];
  SPEEDS.forEach(([k, v, c, mark], i) => {
    const y = 2.62 + i * 0.6;
    s.addText(k, {
      x: ML, y: y - 0.03, w: 2.8, h: 0.34, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: mark ? TEXT : DIM,
    });
    s.addShape(pres.ShapeType.rect, {
      x: BARX, y: y + 0.04, w: BARMAX * (v / VMAX), h: 0.26,
      fill: { color: c }, line: { type: "none" },
    });
    s.addText(v.toFixed(1) + " m/s", {
      x: BARX + BARMAX * (v / VMAX) + 0.14, y: y - 0.02, w: 1.3, h: 0.32, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12, color: mark ? TEXT : FAINT,
    });
  });
  // "your ceiling" marker at 3.5 m/s
  s.addShape(pres.ShapeType.rect, {
    x: BARX + BARMAX * (3.5 / VMAX), y: 2.56, w: 0.014, h: 3.2,
    fill: { color: AMBERBR }, line: { type: "none" },
  });
  s.addText("your ceiling", {
    x: BARX + BARMAX * (3.5 / VMAX) - 0.75, y: 5.78, w: 1.6, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 9.5, color: AMBERBR, align: "center",
  });

  card(s, ML + 7.9, 2.5, 3.83, 2.9, PANEL, "3A1A1E");
  s.addText("AND THERE IS NO ADRENALINE", {
    x: ML + 8.22, y: 2.72, w: 3.3, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 9.5, color: REDBR, charSpacing: 1.2,
  });
  [
    "Hunt sprint is 3.5 — the same as any\nother sprint. No panic bonus.",
    "Your sprint lasts 4.2 seconds, then\n3.5 seconds to recharge. You walk\nthrough all of it.",
    "So the only thing that works is\nbreaking its line of sight. Turn a\ncorner, shut the door, and you are\na rumour again.",
  ].forEach((t, i) => {
    s.addText(t, {
      x: ML + 8.22, y: 3.12 + i * 0.78, w: 3.3, h: 0.72, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: i === 2 ? TEXT : DIM, lineSpacing: 15,
    });
  });

  punch(s, "Two ghosts you can just outrun. One of them you cannot.", REDBR, 6.1);
}

/* ══════════════════════ 08 · Three tools ══════════════════════ */
{
  const s = newSlide("the game · 07  ·  the tools");
  title(s, "Three tools, three different fears");
  sub(s, "They are not three ways to get the same thing. Each one pushes you somewhere you would rather not go.");

  const TOOLS = [
    ["EMF READER", "FOLLOW", "A higher reading means it was standing here a moment ago.", "It pulls you toward it.", MOONBR],
    ["UV LIGHT",   "SEARCH", "Fingerprints only glow in the dark, so holding UV kills your flashlight.", "It takes your light away.", MOONBR],
    ["GHOST BOOK", "WAIT",   "You put it on the floor, you leave, and the writing happens without you.", "It sends you back in.", MOONBR],
  ];
  TOOLS.forEach(([name, verb, body, cost], i) => {
    const x = ML + i * 4.0;
    card(s, x, 2.52, 3.68, 3.0, PANEL, LINE);
    s.addShape(pres.ShapeType.rect, {
      x, y: 2.52, w: 3.68, h: 0.035, fill: { color: AMBER }, line: { type: "none" },
    });
    s.addText(name, {
      x: x + 0.32, y: 2.78, w: 3.1, h: 0.3, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 10.5, color: FAINT, charSpacing: 1.4,
    });
    s.addText(verb, {
      x: x + 0.3, y: 3.1, w: 3.1, h: 0.55, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 30, bold: true, color: TEXT,
    });
    s.addText(body, {
      x: x + 0.32, y: 3.78, w: 3.06, h: 0.94, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: DIM, lineSpacing: 18,
    });
    s.addText(cost, {
      x: x + 0.32, y: 4.86, w: 3.06, h: 0.44, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, bold: true, color: AMBERBR,
    });
  });

  card(s, ML, 5.72, CW, 0.68, PANEL2, LINE);
  s.addText(
    "Three belt slots. The flashlight permanently owns one of them. " +
    "So you leave one tool behind, every single round.",
    {
      x: ML + 0.42, y: 5.86, w: CW - 0.8, h: 0.42, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, color: TEXT,
    });
  foot(s, "Choosing wrong is not a bug. It is the round starting badly, and that is allowed.", DIM, 6.52);
}

/* ══════════════════════ 09 · Art direction ══════════════════════ */
{
  const s = newSlide("the game · 08  ·  the look");
  glow(s, 9.9, 4.2, 5.4, 4.4, AMBER);
  title(s, "One warm light in a cold house");
  sub(s, "The art direction is a budget decision that we are genuinely proud of.");

  const LOOK = [
    ["THE ONLY AMBIENT LIGHT", "Cold blue moonlight through the windows. Indoors is close to black."],
    ["THE ONLY WARM LIGHT", "The flashlight in your hand. 12 metres, a 45° cone. Nothing else is warm."],
    ["EIGHT AREAS, ONE HAUNTED", "Seven of them can hold the ghost. The game picks one at random each round."],
    ["WHEN THE HUNT STARTS", "The whole house goes red and flickers, your torch dies, and sometimes the power fails."],
  ];
  LOOK.forEach(([k, v], i) => {
    const y = 2.52 + i * 0.86;
    s.addText(k, {
      x: ML, y, w: 3.5, h: 0.3, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 9.5, color: MOON, charSpacing: 1.2,
    });
    s.addText(v, {
      x: ML + 3.7, y: y - 0.05, w: 5.4, h: 0.66, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: i === 3 ? REDBR : TEXT, lineSpacing: 19,
    });
  });

  punch(s, "The cheapest thing in a horror game is everything nobody can see.", AMBERBR, 6.06);
  foot(s, "Our ghost is invisible most of the round. That was an art decision before it was a design one.", DIM, 6.5);
}

/* ══════════════════════ 10 · Divider ══════════════════════ */
{
  const s = newSlide("how we built it");
  glow(s, 6.0, 3.9, 10.0, 4.6, AMBER);
  s.addText("We had never made a 3D game.", {
    x: ML, y: 2.5, w: 11.0, h: 0.9, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 46, bold: true, color: TEXT,
  });
  s.addShape(pres.ShapeType.rect, {
    x: ML, y: 3.62, w: 2.1, h: 0.03, fill: { color: AMBER }, line: { type: "none" },
  });
  s.addText(
    "None of us. Not one shipped scene between us. So we did the only thing that\n" +
    "seemed safe: before we wrote a single line of game code, we wrote the rules\n" +
    "that the code would have to obey.",
    {
      x: ML, y: 3.92, w: 9.6, h: 1.5, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 17, color: MOONBR, lineSpacing: 30,
    });
  s.addText("Everything after this slide comes out of that one decision.", {
    x: ML, y: 5.6, w: 9.6, h: 0.44, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 14, italic: true, color: DIM,
  });
}

/* ══════════════════════ 11 · The contract ══════════════════════ */
{
  const s = newSlide("how we built it · 01  ·  the contract");
  title(s, "Nothing talks to anything directly");
  sub(s, "Six systems. None of them knows the other five exist. Everything goes through one noticeboard.");

  const MODS = ["PLAYER", "GHOST", "EVIDENCE", "ITEMS", "WORLD", "UI"];
  const MX = [ML, ML + 4.0, ML + 8.0];
  const BUSY = 4.12;

  MODS.forEach((m, i) => {
    const row = i < 3 ? 0 : 1;
    const x = MX[i % 3];
    const y = row === 0 ? 2.62 : 5.06;
    card(s, x, y, 3.68, 0.64, PANEL, LINE);
    s.addText(m, {
      x: x + 0.1, y: y + 0.16, w: 3.48, h: 0.34, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12.5, color: MOONBR, align: "center", charSpacing: 1.6,
    });
    s.addShape(pres.ShapeType.rect, {
      x: x + 1.83, y: row === 0 ? y + 0.64 : BUSY + 0.62,
      w: 0.014, h: row === 0 ? BUSY - (y + 0.64) : y - (BUSY + 0.62),
      fill: { color: LINE }, line: { type: "none" },
    });
  });

  card(s, ML, BUSY, CW, 0.62, PANEL2, "3A3020");
  s.addText("GameEvents  ·  the static event bus  ·  23 messages", {
    x: ML, y: BUSY + 0.15, w: CW, h: 0.34, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 13, color: AMBERBR, align: "center", charSpacing: 1.2,
  });

  const RULES = [
    ["4", "interfaces agreed before day one"],
    ["7", "contract files only one person may edit"],
    ["0", "direct references between systems"],
  ];
  RULES.forEach(([n, t], i) => {
    const x = ML + i * 4.0;
    s.addText(n, {
      x, y: 5.96, w: 0.55, h: 0.42, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 22, bold: true, color: AMBER,
    });
    s.addText(t, {
      x: x + 0.6, y: 6.04, w: 3.1, h: 0.4, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: DIM,
    });
  });
  foot(s, "Two people can build two systems at the same time, never read each other's code, and it still compiles.", MOONBR, 6.5);
}

/* ══════════════════════ 12 · The pipeline ══════════════════════ */
{
  const s = newSlide("how we built it · 02  ·  the production line");
  title(s, "How a feature actually got made");
  sub(s, "Same six steps, every single task. The AI writes the code. It never decides what the code is for.");

  const STEPS = [
    ["01", "HUMAN", "writes the task spec:\nwhich files, which\nnumbers, what is\nout of scope", AMBER],
    ["02", "AI", "writes the code\ninside those walls", MOON],
    ["03", "GATE", "8 text rules run\nover the diff", TEXT],
    ["04", "BUILD", "Unity compiles it\nwith no window\nopen", TEXT],
    ["05", "AI", "answers 18 fixed\nquestions about\nits own work", MOON],
    ["06", "HUMAN", "reads the diff and\nsays yes or no", AMBER],
  ];
  const BW = 1.78, BG_ = 0.21;
  STEPS.forEach(([n, who, what, c], i) => {
    const x = ML + i * (BW + BG_);
    card(s, x, 2.5, BW, 2.42, PANEL, c === AMBER ? "3A3020" : LINE);
    if (c === AMBER) {
      s.addShape(pres.ShapeType.rect, {
        x, y: 2.5, w: BW, h: 0.035, fill: { color: AMBER }, line: { type: "none" },
      });
    }
    s.addText(n, {
      x: x + 0.18, y: 2.68, w: 0.7, h: 0.28, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 10, color: FAINT,
    });
    s.addText(who, {
      x: x + 0.18, y: 2.98, w: BW - 0.3, h: 0.36, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 16, bold: true, color: c,
    });
    s.addText(what, {
      x: x + 0.18, y: 3.42, w: BW - 0.32, h: 1.4, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 10.5, color: DIM, lineSpacing: 14,
    });
    if (i < 5) {
      s.addText("›", {
        x: x + BW - 0.02, y: 3.42, w: 0.25, h: 0.3, isTextBox: true, margin: 0,
        fontFace: FD, fontSize: 16, color: FAINT, align: "center",
      });
    }
  });

  // rejection loop: 03 / 04 fail → back to 02
  const LY = 5.2;
  const XL = ML + 1 * (BW + BG_) + BW / 2;   // under step 02
  const XR = ML + 3 * (BW + BG_) + BW / 2;   // under step 04
  [XL, XR].forEach(x => {
    s.addShape(pres.ShapeType.rect, {
      x, y: 4.92, w: 0.016, h: LY - 4.92, fill: { color: RED }, line: { type: "none" },
    });
  });
  s.addShape(pres.ShapeType.rect, {
    x: XL, y: LY, w: XR - XL, h: 0.016, fill: { color: RED }, line: { type: "none" },
  });
  s.addShape(pres.ShapeType.triangle, {
    x: XL - 0.09, y: 4.86, w: 0.19, h: 0.15, fill: { color: RED }, line: { type: "none" },
  });
  s.addText("fail  →  back to step 02, with the error text pasted in.\nTwo attempts, then it stops and a human looks.", {
    x: ML + 7.9, y: LY - 0.34, w: 3.83, h: 0.6, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 11.5, color: REDBR, lineSpacing: 16,
  });

  card(s, ML, 5.72, CW, 0.72, PANEL2, LINE);
  s.addText(
    "The one who writes the code and the one who checks it are never the same. " +
    "That was the point of the whole machine.",
    {
      x: ML + 0.42, y: 5.88, w: CW - 0.8, h: 0.44, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 15, bold: true, color: AMBERBR,
    });
  foot(s, "An AI can write every script in this project in a day. Only a person can tell you whether the game feels right.", DIM, 6.56);
}

/* ══════════════════════ 13 · The gate + the lesson (RED #2) ══════════════════════ */
{
  const s = newSlide("how we built it · 03  ·  what the machine gets wrong");
  title(s, "It never gets tired, and it never learns");
  sub(s, "Eight text rules over every diff. Five reject the work outright; three raise a flag.");

  card(s, ML, 2.5, 5.55, 3.36, PANEL, LINE);
  s.addText("WHAT THE RULES ACTUALLY CATCH", {
    x: ML + 0.42, y: 2.72, w: 4.7, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 9.5, color: FAINT, charSpacing: 1.4,
  });
  [
    ["Four function names Unity 6 deleted", "one rule, four spellings"],
    ["A physics property that was renamed", "velocity → linearVelocity"],
    ["The old input system", "banned project-wide"],
    ["One system importing another", "the contract, enforced"],
    ["Tunable numbers hard-coded in C#", "flagged, not rejected"],
  ].forEach(([a, b], i) => {
    const y = 3.12 + i * 0.52;
    s.addText(a, {
      x: ML + 0.42, y, w: 4.9, h: 0.28, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: TEXT,
    });
    s.addText(b, {
      x: ML + 0.42, y: y + 0.24, w: 4.9, h: 0.26, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 9.5, color: FAINT,
    });
  });

  card(s, ML + 6.18, 2.5, 5.55, 3.36, PANEL, "3A1A1E");
  s.addShape(pres.ShapeType.rect, {
    x: ML + 6.18, y: 2.5, w: 0.045, h: 3.36, fill: { color: RED }, line: { type: "none" },
  });
  s.addText("THE ONE WE LEARNED THE HARD WAY", {
    x: ML + 6.6, y: 2.72, w: 4.9, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 9.5, color: REDBR, charSpacing: 1.4,
  });
  s.addText("A check must never blame good code.", {
    x: ML + 6.6, y: 3.06, w: 4.9, h: 0.86, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 22, bold: true, color: TEXT, lineSpacing: 30,
  });
  s.addText(
    "Our first version of these rules ran on a clean repository.\n" +
    "It reported 2 errors and 15 warnings. All seventeen were wrong.",
    {
      x: ML + 6.6, y: 4.02, w: 4.9, h: 0.6, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: DIM, lineSpacing: 19,
    });
  s.addText(
    "A false alarm is worse than silence: it burns both retries\n" +
    "making the AI \"fix\" code that was never broken. Every new\n" +
    "rule is now tested on clean code before it may reject anything.",
    {
      x: ML + 6.6, y: 4.72, w: 4.9, h: 0.9, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: REDBR, lineSpacing: 19,
    });

  punch(s, "An instruction can be ignored. A text search cannot.", TEXT, 6.06);
  foot(s, "Unity 6 deleted a whole family of functions. You can ban them ten times in the prompt and they still come back — and a regex catches every one, for free.", DIM, 6.5);
}

/* ══════════════════════ 14 · The numbers ══════════════════════ */
{
  const s = newSlide("what came out of it · 01");
  title(s, "Eight days, counted honestly");
  sub(s, "All of it is in one repository. Every number here is something you can go and check.");

  const STATS = [
    ["8", "days, day zero to hand-in", AMBERBR],
    ["72", "task specs, written before the code", AMBERBR],
    ["46", "C# scripts", MOONBR],
    ["17,406", "lines of C#", MOONBR],
    ["23", "messages on the event bus", MOONBR],
    ["8", "automatic rules per diff", MOONBR],
    ["18", "self-audit questions per task", MOONBR],
    ["206", "commits", MOONBR],
  ];
  STATS.forEach(([n, t, c], i) => {
    const x = ML + (i % 4) * 3.0;
    const y = 2.52 + Math.floor(i / 4) * 1.62;
    card(s, x, y, 2.72, 1.42, PANEL, LINE);
    s.addText(n, {
      x: x + 0.26, y: y + 0.16, w: 2.3, h: 0.64, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: n.length > 4 ? 30 : 38, bold: true, color: c,
    });
    s.addText(t, {
      x: x + 0.28, y: y + 0.86, w: 2.3, h: 0.46, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM, lineSpacing: 15,
    });
  });

  punch(s, "Ten of those scripts are editor tools we wrote to build the scene itself.", TEXT, 5.92);
  foot(s, "5,298 lines that never ship to a player — they exist so the level can be rebuilt from a menu instead of by hand.", DIM, 6.34);
}

/* ══════════════════════ 15 · What we did not build ══════════════════════ */
{
  const s = newSlide("what came out of it · 02");
  title(s, "The list we are proudest of is the one we cut");
  sub(s, "All four of these were decided on day zero, in writing, before anyone could fall in love with them.");

  const CUTS = [
    ["No multiplayer", "Network code costs three days minimum. In a one-week project, that is the project."],
    ["No second map", "One map that is properly lit beats three that are empty."],
    ["No fourth ghost", "Choose-2-of-3 stops being exact. The table is the game; we do not break the table."],
    ["No jumping", "You would climb the furniture, and that breaks both the level and the paths the ghost walks."],
  ];
  CUTS.forEach(([h, b], i) => {
    const x = ML + (i % 2) * 6.0;
    const y = 2.5 + Math.floor(i / 2) * 1.48;
    card(s, x, y, 5.73, 1.3, PANEL, LINE);
    s.addText(h, {
      x: x + 0.36, y: y + 0.16, w: 5.0, h: 0.4, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 19, bold: true, color: TEXT,
    });
    s.addText(b, {
      x: x + 0.38, y: y + 0.62, w: 5.0, h: 0.56, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12, color: DIM, lineSpacing: 17,
    });
  });

  card(s, ML, 5.5, CW, 0.86, PANEL2, LINE);
  s.addText("TWO CUT-OFF DATES, ALSO WRITTEN ON DAY ZERO", {
    x: ML + 0.42, y: 5.6, w: 6.0, h: 0.26, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 9.5, color: FAINT, charSpacing: 1.4,
  });
  s.addText(
    "If evidence did not work by end of day 3, ghost writing was to be dropped. " +
    "If the ghost AI failed by day 4, the art day moved.",
    {
      x: ML + 0.42, y: 5.88, w: 7.7, h: 0.42, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: TEXT,
    });
  s.addText("Neither was needed.", {
    x: W - MR - 3.3, y: 5.86, w: 3.3, h: 0.42, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 16, bold: true, color: AMBERBR, align: "right",
  });

  punch(s, "A team that can tell you what it is not building is easier to believe.", AMBERBR, 6.5);
}

/* ══════════════════════ 16 · Close ══════════════════════ */
{
  const s = newSlide("thank you");
  glow(s, 5.4, 3.7, 9.4, 4.4, AMBER);
  s.addText("We did not build a bigger game.\nWe built one that finishes.", {
    x: ML, y: 1.9, w: 10.4, h: 1.7, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 40, bold: true, color: TEXT, lineSpacing: 56,
  });

  const TAKE = [
    ["A table that cannot lie", "Two clues, one answer, every round. No luck anywhere in it."],
    ["A way of working we can reuse", "One Python file. Point it at the next project and change three things."],
    ["Rules agreed before line one", "Which is the only reason four people could build six systems at once."],
  ];
  TAKE.forEach(([h, b], i) => {
    const x = ML + i * 4.0;
    card(s, x, 4.02, 3.68, 1.46, PANEL, LINE);
    s.addShape(pres.ShapeType.rect, {
      x, y: 4.02, w: 3.68, h: 0.035, fill: { color: AMBER }, line: { type: "none" },
    });
    s.addText(h, {
      x: x + 0.3, y: 4.22, w: 3.2, h: 0.36, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, bold: true, color: TEXT,
    });
    s.addText(b, {
      x: x + 0.32, y: 4.62, w: 3.14, h: 0.76, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM, lineSpacing: 16,
    });
  });

  s.addText(
    "Next, if there is a next: a thermometer as a fourth clue. Choose two from four is six ghosts, " +
    "and a ghost here is a data file, not a class. That is one afternoon, not one week.",
    {
      x: ML, y: 5.7, w: 11.0, h: 0.66, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: MOONBR, lineSpacing: 19,
    });
  s.addText("Thank you.", {
    x: ML, y: 6.42, w: 4.0, h: 0.44, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 20, bold: true, color: AMBERBR,
  });
}

/* ── Write ───────────────────────────────────────────────────────────────── */
const OUT = path.join(__dirname, "..", "Docs", "decks", "RESIDUUM_Final_v2.pptx");
pres.writeFile({ fileName: OUT }).then(() => {
  console.log("wrote " + OUT + "  (" + TOTAL + " slides)");
});
