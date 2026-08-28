const pptxgen = require("pptxgenjs");

// ── Palette: cold moonlight + hunt red. Taken from the game's own lighting design ──
const BG      = "0A0D13";
const PANEL   = "161D2A";
const PANEL2  = "222C3E";
const MOON    = "7FA6CC";
const MOONBR  = "A9C6E4";
const BLOOD   = "B3202E";
const BLOODBR = "E0505E";
const TEXT    = "E9EDF3";
const DIM     = "8D9AAD";
const DIM2    = "5E6B7E";

const F = "Calibri";       // body
const FD = "Cambria";      // display numerals / serif accents
const FM = "Courier New";  // monospace labels

const W = 13.333, H = 7.5;
const ML = 0.75, MR = 0.75;
const CW = W - ML - MR; // 11.833

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE";
pres.author = "Henry";
pres.title = "RESIDUUM — Final Presentation";

let idx = 0;
const TOTAL = 18;

function pad(n) { return String(n).padStart(2, "0"); }

function newSlide(kicker) {
  idx++;
  const s = pres.addSlide();
  s.background = { color: BG };
  s.addText(`FILE ${pad(idx)} — ${kicker}`, {
    x: ML, y: 0.34, w: 7.5, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10.5, color: DIM2, charSpacing: 2,
  });
  s.addText(`${pad(idx)} / ${TOTAL}`, {
    x: W - MR - 1.4, y: 6.92, w: 1.4, h: 0.28, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 10.5, color: DIM2, align: "right",
  });
  return s;
}

function title(s, t, sub) {
  s.addText(t, {
    x: ML, y: 0.7, w: CW, h: 0.64, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 34, bold: true, color: TEXT,
  });
  if (sub) {
    s.addText(sub, {
      x: ML, y: 1.38, w: CW, h: 0.34, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14.5, color: MOON,
    });
  }
}

function card(s, x, y, w, h, fill) {
  s.addShape(pres.ShapeType.roundRect, {
    x, y, w, h, rectRadius: 0.06,
    fill: { color: fill || PANEL },
    line: { color: PANEL2, width: 0.75 },
  });
}

function footNote(s, t, color) {
  s.addText(t, {
    x: ML, y: 6.7, w: CW - 1.7, h: 0.42, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, italic: true, color: color || DIM,
  });
}

/* ══════════════ 01 · Cover ══════════════ */
{
  idx++;
  const s = pres.addSlide();
  s.background = { color: BG };

  s.addText("RESIDUUM", {
    x: ML, y: 1.9, w: 7.2, h: 1.3, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 66, bold: true, color: TEXT, charSpacing: 6,
  });
  s.addText("A first-person horror game about what is in the house", {
    x: ML, y: 3.3, w: 7.2, h: 0.44, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 17, color: DIM,
  });
  s.addText("You don't know what it is. That is the whole game.", {
    x: ML, y: 3.94, w: 7.2, h: 0.44, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 17, italic: true, color: MOONBR,
  });

  s.addText("Unity 6   ·   Single player   ·   A playable build made in 8 days", {
    x: ML, y: 5.86, w: 8.0, h: 0.32, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 11.5, color: DIM2,
  });
  s.addText("Made by 2 students   ·   Our first 3D game   ·   August 2026", {
    x: ML, y: 6.24, w: 8.0, h: 0.32, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 11.5, color: DIM2,
  });

  // Visual motif: the 3x3 evidence matrix, abstracted
  const gx = 8.6, gy = 2.05, cell = 0.84, gap = 0.16;
  const grid = [[1, 1, 0], [1, 0, 1], [0, 1, 1]];
  for (let r = 0; r < 3; r++) {
    for (let c = 0; c < 3; c++) {
      const on = grid[r][c] === 1;
      s.addShape(pres.ShapeType.roundRect, {
        x: gx + c * (cell + gap), y: gy + r * (cell + gap), w: cell, h: cell,
        rectRadius: 0.08,
        fill: { color: on ? MOON : BG },
        line: { color: on ? MOON : PANEL2, width: on ? 0 : 1 },
      });
    }
  }
  s.addText("3 ghosts  ×  3 kinds of evidence\nEvery ghost has exactly two", {
    x: gx - 0.3, y: gy + 3 * cell + 2 * gap + 0.24, w: 4.2, h: 0.72, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12.5, color: DIM, align: "center", lineSpacing: 19,
  });

  s.addNotes("Ten seconds. Do not stop here. Say the name and the one-line idea, then move on.");
}

/* ══════════════ 02 · Premise ══════════════ */
{
  const s = newSlide("PREMISE");
  title(s, "The game in one sentence", "If we only get to say one thing, this is it");

  card(s, ML, 2.02, CW, 1.5);
  s.addText(
    "You go into a house you should not be in, carrying three tools. Before your sanity runs out, you have to work out what is haunting it and get back out alive.",
    {
      x: ML + 0.5, y: 2.32, w: CW - 1.0, h: 1.0, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 19, color: TEXT, lineSpacing: 30,
    }
  );

  const stats = [
    ["8–12", "minutes per game"],
    ["3", "ghosts"],
    ["3", "kinds of evidence"],
    ["2", "clues give you the answer"],
  ];
  const sw = (CW - 3 * 0.32) / 4;
  stats.forEach((st, i) => {
    const x = ML + i * (sw + 0.32);
    card(s, x, 3.92, sw, 1.72);
    s.addText(st[0], {
      x: x, y: 4.06, w: sw, h: 0.86, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 46, bold: true, color: MOONBR, align: "center",
    });
    s.addText(st[1], {
      x: x + 0.16, y: 4.94, w: sw - 0.32, h: 0.6, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: DIM, align: "center",
    });
  });

  footNote(s, "We looked at Phasmophobia. We copied the way it makes you think, not the amount of stuff in it.");
  s.addNotes("The four numbers are the easiest thing to remember. Say them one at a time and stop between each one.");
}

/* ══════════════ 03 · Why we cut to 3x3 ══════════════ */
{
  const s = newSlide("SCOPE");
  title(s, "Choice one: keep it small on purpose", "More content does not mean more fun");

  const cw2 = (CW - 0.5) / 2;
  card(s, ML, 2.06, cw2, 2.35);
  s.addText("Phasmophobia", {
    x: ML + 0.34, y: 2.26, w: cw2 - 0.68, h: 0.36, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 18, bold: true, color: DIM,
  });
  s.addText(
    [
      { text: "24 ghosts", options: { bullet: true, breakLine: true } },
      { text: "7 kinds of evidence", options: { bullet: true, breakLine: true } },
      { text: "More than 40 tools", options: { bullet: true, breakLine: true } },
      { text: "Ghosts share clues, so players still guess", options: { bullet: true } },
    ],
    {
      x: ML + 0.34, y: 2.76, w: cw2 - 0.68, h: 1.5, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, color: DIM2, paraSpaceAfter: 7,
    }
  );

  card(s, ML + cw2 + 0.5, 2.06, cw2, 2.35, PANEL2);
  s.addText("Residuum", {
    x: ML + cw2 + 0.84, y: 2.26, w: cw2 - 0.68, h: 0.36, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 18, bold: true, color: MOONBR,
  });
  s.addText(
    [
      { text: "3 ghosts", options: { bullet: true, breakLine: true } },
      { text: "3 kinds of evidence", options: { bullet: true, breakLine: true } },
      { text: "4 tools and one notebook", options: { bullet: true, breakLine: true } },
      { text: "No sharing problem. Two clues, one answer", options: { bullet: true } },
    ],
    {
      x: ML + cw2 + 0.84, y: 2.76, w: cw2 - 0.68, h: 1.5, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, color: TEXT, paraSpaceAfter: 7,
    }
  );

  s.addText("We think that game is fun for three reasons. All three still work when the game is small:", {
    x: ML, y: 4.72, w: CW, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15, color: TEXT,
  });

  const three = [
    ["You don't know what it is", "You know something is in the house. You just don't know what, or where."],
    ["You have to work it out", "You use clues to rule ghosts out, one at a time."],
    ["Staying is dangerous", "One more second is one more clue — and one more chance to die."],
  ];
  const tw = (CW - 2 * 0.32) / 3;
  three.forEach((t, i) => {
    const x = ML + i * (tw + 0.32);
    card(s, x, 5.2, tw, 1.32);
    s.addText(t[0], {
      x: x + 0.26, y: 5.34, w: tw - 0.52, h: 0.32, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 15.5, bold: true, color: MOONBR,
    });
    s.addText(t[1], {
      x: x + 0.26, y: 5.72, w: tw - 0.52, h: 0.66, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: DIM,
    });
  });

  s.addNotes("The point of this slide: we did not make it small because we ran out of time. We made it small on purpose.");
}

/* ══════════════ 04 · The matrix ══════════════ */
{
  const s = newSlide("THE TABLE");
  title(s, "The heart of the game: a 3 × 3 table", "★ The most important slide");

  const cols = ["", "EMF reading", "UV fingerprints", "Ghost writing"];
  const rows = [
    ["Spirit", 1, 1, 0],
    ["Wraith", 1, 0, 1],
    ["Poltergeist", 0, 1, 1],
  ];

  const tx = 1.6, ty = 2.15;
  const c0 = 3.0, cN = 2.35, rh = 0.82, hh = 0.62;

  cols.forEach((c, i) => {
    if (i === 0) return;
    const x = tx + c0 + (i - 1) * cN;
    s.addText(c, {
      x: x, y: ty, w: cN, h: hh, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14.5, bold: true, color: MOON, align: "center", valign: "middle",
    });
  });

  rows.forEach((r, ri) => {
    const y = ty + hh + ri * rh;
    s.addShape(pres.ShapeType.roundRect, {
      x: tx, y: y, w: c0 + 3 * cN, h: rh - 0.1, rectRadius: 0.05,
      fill: { color: ri % 2 === 0 ? PANEL : "121926" },
      line: { color: PANEL2, width: 0.75 },
    });
    s.addText(r[0], {
      x: tx + 0.3, y: y, w: c0 - 0.3, h: rh - 0.1, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 19, bold: true, color: TEXT, valign: "middle",
    });
    for (let ci = 1; ci <= 3; ci++) {
      const x = tx + c0 + (ci - 1) * cN;
      const on = r[ci] === 1;
      s.addText(on ? "✓" : "—", {
        x: x, y: y, w: cN, h: rh - 0.1, isTextBox: true, margin: 0,
        fontFace: F, fontSize: on ? 26 : 20, bold: on,
        color: on ? MOONBR : DIM2, align: "center", valign: "middle",
      });
    }
  });

  card(s, ML, 5.28, CW, 1.16, PANEL2);
  s.addText("Every ghost has exactly two. The three ghosts use up all three pairs.", {
    x: ML + 0.4, y: 5.48, w: CW - 0.8, h: 0.42, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 19, bold: true, color: MOONBR, align: "center",
  });
  s.addText("This small table is the best idea in our project. Everything else is built around it.", {
    x: ML + 0.4, y: 5.94, w: CW - 0.8, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, color: DIM, align: "center",
  });

  s.addNotes("If there is a whiteboard, draw this instead of showing it. Three rows, three columns, one tick at a time. People trust something they watch you build. Stop for a second before you move on.");
}

/* ══════════════ 05 · Uniqueness ══════════════ */
{
  const s = newSlide("PROOF");
  title(s, "Why this table always works", "★ The best moment in the talk");

  card(s, ML, 2.06, 3.9, 2.5, PANEL2);
  s.addText("C(3, 2)  =  3", {
    x: ML, y: 2.5, w: 3.9, h: 0.8, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 44, bold: true, color: MOONBR, align: "center",
  });
  s.addText("Pick 2 things out of 3.\nThere are only 3 ways to do it.\nOne way for each ghost.", {
    x: ML + 0.3, y: 3.34, w: 3.3, h: 1.0, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13.5, color: DIM, align: "center", lineSpacing: 22,
  });

  const facts = [
    ["One clue is never enough", "Two ghosts share every clue, so one clue only rules out one ghost"],
    ["Two clues always give the answer", "Each pair belongs to one ghost only. There is no second answer"],
    ["Ruling things out works too", "Showing that a clue is NOT there also helps you"],
  ];
  const fx = ML + 4.3, fw = CW - 4.3;
  facts.forEach((f, i) => {
    const y = 2.06 + i * 0.88;
    card(s, fx, y, fw, 0.76);
    s.addShape(pres.ShapeType.ellipse, {
      x: fx + 0.24, y: y + 0.2, w: 0.36, h: 0.36,
      fill: { color: MOON }, line: { color: MOON, width: 0 },
    });
    s.addText(String(i + 1), {
      x: fx + 0.24, y: y + 0.2, w: 0.36, h: 0.36, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 14, bold: true, color: BG, align: "center", valign: "middle",
    });
    s.addText(f[0], {
      x: fx + 0.76, y: y + 0.1, w: fw - 1.0, h: 0.3, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 15.5, bold: true, color: TEXT,
    });
    s.addText(f[1], {
      x: fx + 0.76, y: y + 0.4, w: fw - 1.0, h: 0.3, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12, color: DIM,
    });
  });

  s.addText("No guessing.    No luck.    Just logic.", {
    x: ML, y: 5.1, w: CW, h: 0.62, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 30, bold: true, color: MOONBR, align: "center", charSpacing: 2,
  });
  s.addText("If a player wins, it is because they worked it out.", {
    x: ML, y: 5.82, w: CW, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 14, color: DIM, align: "center",
  });

  s.addNotes("Stop after 'Just logic.' Count two seconds before you move on. Do not fill the silence.");
}

/* ══════════════ 06 · Sanity ══════════════ */
{
  const s = newSlide("PACING");
  title(s, "Sanity sets the pace", "This is why you cannot just take your time");

  const lw = 6.4;
  card(s, ML, 2.06, lw, 4.0);
  s.addText("How sanity changes", {
    x: ML + 0.36, y: 2.24, w: lw - 0.72, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 16, bold: true, color: MOON,
  });
  const sanity = [
    ["You start at", "100 %"],
    ["Standing in the dark", "−0.12 %/s"],
    ["In a room with the light on", "−0.06 %/s"],
    ["Holding a lit flashlight", "half as fast"],
    ["Seeing the ghost do something", "−15 % at once"],
    ["While it is hunting you", "−0.5 %/s"],
    ["Back in the lobby", "+1.0 %/s"],
  ];
  sanity.forEach((r, i) => {
    const y = 2.72 + i * 0.46;
    s.addText(r[0], {
      x: ML + 0.36, y: y, w: lw - 2.6, h: 0.36, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, color: TEXT, valign: "middle",
    });
    s.addText(r[1], {
      x: ML + lw - 2.15, y: y, w: 1.79, h: 0.36, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12,
      color: r[1].startsWith("+") ? MOONBR : (i === 0 ? DIM : BLOODBR),
      align: "right", valign: "middle",
    });
  });

  const rx = ML + lw + 0.45, rw = CW - lw - 0.45;
  card(s, rx, 2.06, rw, 1.66, PANEL2);
  s.addText("Chance of a hunt", {
    x: rx + 0.36, y: 2.22, w: rw - 0.72, h: 0.3, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15, bold: true, color: MOON,
  });
  s.addText("P  =  ( 50 − sanity ) ÷ 50", {
    x: rx + 0.3, y: 2.62, w: rw - 0.6, h: 0.5, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 25, bold: true, color: BLOODBR, align: "center",
  });
  s.addText("The game checks every 25 seconds, once sanity is under 50 %", {
    x: rx + 0.36, y: 3.18, w: rw - 0.72, h: 0.44, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12.5, color: DIM, align: "center",
  });

  const pts = [["50 %", "0 %"], ["25 %", "50 %"], ["0 %", "always"]];
  pts.forEach((p, i) => {
    const y = 3.94 + i * 0.72;
    card(s, rx, y, rw, 0.6);
    s.addText(`Sanity ${p[0]}`, {
      x: rx + 0.36, y: y, w: rw / 2 - 0.36, h: 0.6, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, color: TEXT, valign: "middle",
    });
    s.addText(`→   ${p[1]}`, {
      x: rx + rw / 2, y: y, w: rw / 2 - 0.36, h: 0.6, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, bold: true, color: BLOODBR, align: "right", valign: "middle",
    });
  });

  footNote(s, "The player is always asking the same question: how much longer can I stay? That worry is the game.");
  s.addNotes("Say 'every extra clue costs you' slowly. That one line is the whole design.");
}

/* ══════════════ 07 · Hunt speeds ══════════════ */
{
  const s = newSlide("THE HUNT");
  title(s, "Running away does not work", "We set these speeds by hand and tested them");

  const chartData = [{
    name: "Movement speed (m/s)",
    labels: ["Player crouch", "Player walk", "Spirit hunt", "Wraith hunt", "Player sprint", "Poltergeist hunt"],
    values: [1.4, 2.0, 3.3, 3.4, 3.5, 3.6],
  }];
  s.addChart(pres.ChartType.bar, chartData, {
    x: ML, y: 2.02, w: 7.3, h: 4.05,
    barDir: "col",
    chartColors: [MOON, MOON, BLOOD, BLOOD, MOONBR, BLOODBR],
    varyColors: true,
    showTitle: false,
    showLegend: false,
    showValue: true,
    dataLabelPosition: "outEnd",
    dataLabelColor: TEXT,
    dataLabelFontSize: 12,
    dataLabelFontFace: FM,
    catAxisLabelColor: DIM,
    catAxisLabelFontSize: 10.5,
    catAxisLabelFontFace: F,
    valAxisLabelColor: DIM2,
    valAxisLabelFontSize: 10,
    valAxisMaxVal: 4.2,
    valGridLine: { color: PANEL2, size: 0.75 },
    catGridLine: { style: "none" },
    barGapWidthPct: 55,
    plotArea: { fill: { color: BG } },
    chartArea: { fill: { color: BG } },
  });

  const rx = ML + 7.6, rw = CW - 7.6;
  const notes = [
    ["No speed boost in a hunt", "Sprinting is 3.5, the same as normal. We give you no free help."],
    ["The Poltergeist is faster than you", "You can just outrun 3.3 and 3.4. You cannot outrun 3.6."],
    ["You can only sprint 4.2 seconds", "Then 3.5 seconds to get it back, and you walk at 2.0."],
    ["Get out of its sight instead", "If it cannot see you, it only checks where you were. Turn a corner. Close the door."],
  ];
  notes.forEach((n, i) => {
    const y = 2.02 + i * 1.05;
    card(s, rx, y, rw, 0.96);
    s.addText(n[0], {
      x: rx + 0.26, y: y + 0.1, w: rw - 0.52, h: 0.32, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, bold: true, color: i === 3 ? MOONBR : BLOODBR,
    });
    s.addText(n[1], {
      x: rx + 0.26, y: y + 0.42, w: rw - 0.52, h: 0.48, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM,
    });
  });

  s.addNotes("This slide is about how the game feels, not what it has. We re-tested all six numbers on day seven.");
}

/* ══════════════ 08 · Three instruments ══════════════ */
{
  const s = newSlide("TOOLS");
  title(s, "Three tools, three kinds of fear", "They are not just three ways to get the same thing");

  const tools = [
    ["EMF Reader", "F O L L O W", "You follow the number. A higher number means it was just here.", "It pulls you toward it"],
    ["UV Flashlight", "S E A R C H", "You stand still and check door handles and switches. Your flashlight turns off.", "It takes your light away"],
    ["Ghost Writing Book", "W A I T", "You put it down, walk away, and have to go back in to read it.", "It sends you back in"],
  ];
  const tw = (CW - 2 * 0.4) / 3;
  tools.forEach((t, i) => {
    const x = ML + i * (tw + 0.4);
    card(s, x, 2.06, tw, 3.32);
    s.addText(t[1], {
      x: x + 0.3, y: 2.26, w: tw - 0.6, h: 0.34, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12.5, color: MOON,
    });
    s.addText(t[0], {
      x: x + 0.3, y: 2.7, w: tw - 0.6, h: 0.46, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 24, bold: true, color: TEXT,
    });
    s.addText(t[2], {
      x: x + 0.3, y: 3.3, w: tw - 0.6, h: 1.1, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, color: DIM, lineSpacing: 21,
    });
    s.addText(`“${t[3]}”`, {
      x: x + 0.3, y: 4.58, w: tw - 0.6, h: 0.62, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 17, bold: true, italic: true, color: BLOODBR,
    });
  });

  card(s, ML, 5.66, CW, 0.9, PANEL2);
  s.addText("You only get 3 tool slots, and the flashlight always takes one of them. So you have to choose. We did that on purpose.", {
    x: ML + 0.5, y: 5.66, w: CW - 1.0, h: 0.9, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15.5, color: TEXT, valign: "middle",
  });

  s.addNotes("Follow, search, wait. Three different things to do. That is why these three tools and not others.");
}

/* ══════════════ 09 · The three ghosts ══════════════ */
{
  const s = newSlide("ENTITIES");
  title(s, "Three ghosts, one AI", "The differences live in data files, not in three sets of code");

  const ghosts = [
    ["Spirit", "Slow and stubborn", ["Stays in its own room", "Loud, clear footsteps", "Hunts for the longest"], "Hunts at 3.3 m/s"],
    ["Wraith", "Quiet and hard to follow", ["Leaves no footprints", "Jumps a short way closer", "Speeds up while chasing"], "Hunts at 3.4 m/s"],
    ["Poltergeist", "Angry and loud", ["Throws things around", "Throws everything at hunt start", "Drains sanity 1.5× faster"], "Hunts at 3.6 m/s"],
  ];
  const gw = (CW - 2 * 0.4) / 3;
  ghosts.forEach((g, i) => {
    const x = ML + i * (gw + 0.4);
    card(s, x, 2.06, gw, 3.0);
    s.addText(g[0], {
      x: x + 0.3, y: 2.22, w: gw - 0.6, h: 0.44, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 24, bold: true, color: TEXT,
    });
    s.addText(g[1], {
      x: x + 0.3, y: 2.7, w: gw - 0.6, h: 0.32, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, color: MOON,
    });
    s.addText(
      g[2].map((t, j) => ({ text: t, options: { bullet: true, breakLine: j < g[2].length - 1 } })),
      {
        x: x + 0.3, y: 3.14, w: gw - 0.6, h: 1.32, isTextBox: true, margin: 0,
        fontFace: F, fontSize: 13, color: DIM, paraSpaceAfter: 8,
      }
    );
    s.addText(g[3], {
      x: x + 0.3, y: 4.52, w: gw - 0.6, h: 0.36, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 13, bold: true, color: BLOODBR,
    });
  });

  card(s, ML, 5.34, CW, 1.14, PANEL2);
  s.addText("Three ghosts  =  one GhostAI script  +  three data files", {
    x: ML + 0.5, y: 5.48, w: CW - 1.0, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 18, bold: true, color: MOONBR,
  });
  s.addText("To add a fourth ghost we make one more data file. We do not change any code.", {
    x: ML + 0.5, y: 5.9, w: CW - 1.0, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, color: DIM,
  });

  s.addNotes("If someone says three ghosts is too few, use this slide. Four kinds of clue would give six ghosts. We just chose not to do it this week.");
}

/* ══════════════ 10 · Level and lighting ══════════════ */
{
  const s = newSlide("LEVEL");
  title(s, "One apartment, eight areas", "One good map beats three empty ones");

  const px = ML, py = 2.14, pw = 6.4;
  const zones = [
    ["Main Room", "70 m²"], ["Kitchen", "22 m²"],
    ["Corridor", "22 m²"], ["Washroom", "22 m²"],
    ["Host Bedroom", "52 m²"], ["Guest Room", "22 m²"],
    ["Double Room", "46 m²"], ["Lobby", "safe zone"],
  ];
  const zw = (pw - 0.16) / 2, zh = 0.8, zg = 0.16;
  zones.forEach((z, i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const x = px + col * (zw + zg);
    const y = py + row * (zh + zg);
    const safe = z[1] === "safe zone";
    s.addShape(pres.ShapeType.roundRect, {
      x, y, w: zw, h: zh, rectRadius: 0.05,
      fill: { color: safe ? PANEL2 : PANEL },
      line: { color: safe ? MOON : PANEL2, width: safe ? 1.25 : 0.75 },
    });
    s.addText(z[0], {
      x: x + 0.26, y, w: zw - 1.62, h: zh, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, bold: true, color: safe ? MOONBR : TEXT, valign: "middle",
    });
    s.addText(z[1], {
      x: x + zw - 1.32, y, w: 1.06, h: zh, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 11, color: safe ? MOON : DIM2, align: "right", valign: "middle",
    });
  });
  s.addText("Seven rooms can be the ghost room. The game picks one at random each time. The lobby is safe.", {
    x: px, y: py + 4 * (zh + zg) + 0.16, w: pw, h: 0.5, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12, color: DIM,
  });

  const rx = ML + pw + 0.6, rw = CW - pw - 0.6;
  const items = [
    ["Cold blue moonlight is the only room light", "It comes in through the windows. Inside the flat it is almost black."],
    ["Your flashlight is the only warm light", "4200 K, 12 m, a 45° cone. That is all the light you get."],
    ["Every room has a light switch", "Lights slow down sanity loss, but the ghost notices you more."],
    ["A hunt turns the whole house red", "Your flashlight stops working. Sometimes the power goes out."],
    ["The corridor blocks your view", "You rarely see the whole flat at once. You hear it before you see it."],
  ];
  items.forEach((it, i) => {
    const y = 2.14 + i * 0.9;
    s.addShape(pres.ShapeType.ellipse, {
      x: rx, y: y + 0.08, w: 0.2, h: 0.2,
      fill: { color: i === 3 ? BLOODBR : MOON }, line: { color: BG, width: 0 },
    });
    s.addText(it[0], {
      x: rx + 0.36, y: y - 0.02, w: rw - 0.36, h: 0.32, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, bold: true, color: TEXT,
    });
    s.addText(it[1], {
      x: rx + 0.36, y: y + 0.3, w: rw - 0.36, h: 0.52, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM,
    });
  });

  s.addNotes("Eight room volumes in the scene, seven of them can be the ghost room. The cheapest part of a horror game: you do not have to build what nobody can see.");
}

/* ══════════════ 11 · Architecture ══════════════ */
{
  const s = newSlide("HOW THE CODE FITS");
  title(s, "No part of the code talks to another part directly", "So two people, or two AI sessions, can write code at the same time");

  card(s, 4.55, 3.5, 4.22, 1.08, PANEL2);
  s.addText("Core.GameEvents", {
    x: 4.55, y: 3.62, w: 4.22, h: 0.42, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 19, bold: true, color: MOONBR, align: "center",
  });
  s.addText("a message board · 23 messages", {
    x: 4.55, y: 4.06, w: 4.22, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12.5, color: DIM, align: "center",
  });

  const mods = ["Player", "Ghost", "Evidence", "Items", "World", "UI"];
  const mw = 1.72, gapM = 0.29;
  const startX = (W - (6 * mw + 5 * gapM)) / 2;
  mods.forEach((m, i) => {
    const x = startX + i * (mw + gapM);
    card(s, x, 2.16, mw, 0.86);
    s.addText(m, {
      x, y: 2.16, w: mw, h: 0.86, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 13.5, color: TEXT, align: "center", valign: "middle",
    });
    s.addShape(pres.ShapeType.line, {
      x: x + mw / 2, y: 3.02, w: 0, h: 0.48,
      line: { color: PANEL2, width: 1.25 },
    });
  });
  s.addShape(pres.ShapeType.line, {
    x: startX + mw / 2, y: 3.5, w: 5 * (mw + gapM), h: 0,
    line: { color: PANEL2, width: 1.25 },
  });

  const rules = [
    ["Everything goes through the board", "One part of the code may not import another part"],
    ["Four interfaces agreed first", "IInteractable · IHoldable · IEvidenceSource · GhostDefinition"],
    ["Only one person edits the rules", "7 files. The AI is not allowed to open them"],
    ["Numbers live in the Inspector", "We tune the game without touching any code"],
    ["Ghosts are data, not code", "Their differences live in data files"],
  ];
  const rw2 = (CW - 2 * 0.3) / 3;
  rules.forEach((r, i) => {
    const col = i % 3, row = Math.floor(i / 3);
    const x = ML + col * (rw2 + 0.3);
    const y = 4.94 + row * 0.9;
    s.addText(`${String(i + 1).padStart(2, "0")}   ${r[0]}`, {
      x, y, w: rw2, h: 0.3, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13.5, bold: true, color: MOONBR,
    });
    s.addText(r[1], {
      x: x + 0.44, y: y + 0.3, w: rw2 - 0.44, h: 0.46, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11, color: DIM,
    });
  });

  s.addNotes("Why it matters: two parts of the game can be written at the same time by people who never talk to each other, and it still builds.");
}

/* ══════════════ 12 · Pipeline ══════════════ */
{
  const s = newSlide("PIPELINE");
  title(s, "How we actually wrote the code", "★ We built this tool, and we can use it again");

  const steps = [
    ["01", "Task spec", "A person writes\nit. Which files,\nwhich numbers,\nwhat not to do"],
    ["02", "AI writes it", "Codex fills in\nthe code. It\ncannot change\nthe rules"],
    ["03", "Auto check", "8 text rules.\nOne hit and it\ngoes back"],
    ["04", "Build it", "Unity builds it\nwith no window.\nReal errors"],
    ["05", "Self check", "The AI answers\n18 questions\nabout its work"],
    ["06", "We read it", "A person reads\nthe changes and\nsays yes or no"],
  ];
  const sw2 = (CW - 5 * 0.24) / 6;
  steps.forEach((st, i) => {
    const x = ML + i * (sw2 + 0.24);
    const last = i === 5;
    card(s, x, 2.1, sw2, 2.3, last ? PANEL2 : PANEL);
    s.addText(st[0], {
      x: x + 0.2, y: 2.24, w: sw2 - 0.4, h: 0.32, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12.5, color: last ? MOONBR : DIM2,
    });
    s.addText(st[1], {
      x: x + 0.2, y: 2.62, w: sw2 - 0.4, h: 0.42, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 15, bold: true, color: last ? MOONBR : TEXT,
    });
    s.addText(st[2], {
      x: x + 0.2, y: 3.08, w: sw2 - 0.4, h: 1.28, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 10.5, color: DIM, lineSpacing: 15,
    });
  });

  s.addText("If step 3 or step 4 fails, it goes back to step 2 with the error text. Two tries, then it stops.", {
    x: ML, y: 4.56, w: CW, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, italic: true, color: DIM, align: "center",
  });

  card(s, ML, 5.08, CW, 1.44, PANEL2);
  s.addText("People decide what to build and whether it is good. The AI writes it.", {
    x: ML + 0.45, y: 5.24, w: CW - 0.9, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 17, bold: true, color: TEXT,
  });
  s.addText("The person who writes it and the person who checks it cannot be the same. We are new to 3D games, so we made the rules very strict before we started: the message board, the interfaces and the data files were all agreed first, and the AI could only fill in behind them. An AI can write every script in this project in a day. Only a person can tell whether the game feels right.", {
    x: ML + 0.45, y: 5.68, w: CW - 0.9, h: 0.74, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12.5, color: DIM,
  });

  s.addNotes("This is what makes us different. Other teams hand in a game. We hand in a game and a way of working we can use again.");
}

/* ══════════════ 13 · Gates ══════════════ */
{
  const s = newSlide("GATES");
  title(s, "Eight rules the code has to pass", "An AI never gets tired, but it makes the same mistake again and again");

  const gates = [
    ["G01", "The old Find functions are not allowed", "error"],
    ["G02", "Rigidbody.velocity is now linearVelocity", "error"],
    ["G03", "Do not use the old Input Manager", "error"],
    ["G04", "CinemachineVirtualCamera has a new name", "error"],
    ["G05", "No part may import another part", "error"],
    ["G06", "Numbers must be SerializeField private", "warn"],
    ["G07", "Do not use GameObject.Find by name", "warn"],
    ["G08", "Take the debug logs out before saving", "warn"],
  ];
  const gw2 = 6.9;
  gates.forEach((g, i) => {
    const y = 2.06 + i * 0.56;
    s.addShape(pres.ShapeType.roundRect, {
      x: ML, y, w: gw2, h: 0.48, rectRadius: 0.04,
      fill: { color: PANEL }, line: { color: PANEL2, width: 0.75 },
    });
    s.addText(g[0], {
      x: ML + 0.24, y, w: 0.7, h: 0.48, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12.5, bold: true,
      color: g[2] === "error" ? BLOODBR : MOON, valign: "middle",
    });
    s.addText(g[1], {
      x: ML + 1.02, y, w: gw2 - 2.1, h: 0.48, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 13, color: TEXT, valign: "middle",
    });
    s.addText(g[2], {
      x: ML + gw2 - 1.0, y, w: 0.76, h: 0.48, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 10.5, color: DIM2, align: "right", valign: "middle",
    });
  });

  const rx = ML + gw2 + 0.5, rw = CW - gw2 - 0.5;
  card(s, rx, 2.06, rw, 1.5, PANEL2);
  s.addText("Plus 18 questions to answer", {
    x: rx + 0.3, y: 2.22, w: rw - 0.6, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 16, bold: true, color: MOONBR,
  });
  s.addText("The AI answers all 18 when it hands work in, and lists anything it decided by itself. Those decisions are usually where the next bug comes from.", {
    x: rx + 0.3, y: 2.62, w: rw - 0.6, h: 0.82, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12, color: DIM,
  });

  card(s, rx, 3.72, rw, 1.32);
  s.addText("The auto check wins", {
    x: rx + 0.3, y: 3.88, w: rw - 0.6, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15, bold: true, color: TEXT,
  });
  s.addText("The self check is what the AI says about itself. The auto check reads the real code.", {
    x: rx + 0.3, y: 4.26, w: rw - 0.6, h: 0.66, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12, color: DIM,
  });

  card(s, rx, 5.2, rw, 1.32);
  s.addText("A check must not blame good code", {
    x: rx + 0.3, y: 5.34, w: rw - 0.6, h: 0.36, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 14.5, bold: true, color: BLOODBR,
  });
  s.addText("Our first version found 2 errors and 15 warnings in code that was fine. All of them were wrong, and that wastes a whole round of work.", {
    x: rx + 0.3, y: 5.74, w: rw - 0.6, h: 0.68, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 11.5, color: DIM,
  });

  s.addNotes("If someone asks whether the work is really ours, the answer is on this slide and the one before it.");
}

/* ══════════════ 14 · Numbers ══════════════ */
{
  const s = newSlide("NUMBERS");
  title(s, "Eight days, in numbers", "You can check every one of these in our repository");

  const nums = [
    ["59", "task specs", "Each one lists the files, the numbers, and how we would test it"],
    ["58", "tool runs", "Auto check, build and self check, all the way through"],
    ["182", "commits", "We kept code, scene and documents in separate commits"],
    ["43", "C# scripts", "34 in the game, plus 9 editor tools we wrote to help us"],
    ["16,467", "lines of C#", "The AI wrote the game code, inside the rules we set"],
    ["3,017", "lines of documents", "Design, plan, review steps, and a setup guide for the team"],
  ];
  const nw = (CW - 2 * 0.36) / 3;
  nums.forEach((n, i) => {
    const col = i % 3, row = Math.floor(i / 3);
    const x = ML + col * (nw + 0.36);
    const y = 2.1 + row * 2.18;
    card(s, x, y, nw, 1.96);
    s.addText(n[0], {
      x: x + 0.3, y: y + 0.18, w: nw - 0.6, h: 0.86, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 48, bold: true, color: MOONBR,
    });
    s.addText(n[1], {
      x: x + 0.3, y: y + 1.04, w: nw - 0.6, h: 0.34, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 15.5, bold: true, color: TEXT,
    });
    s.addText(n[2], {
      x: x + 0.3, y: y + 1.4, w: nw - 0.6, h: 0.46, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM,
    });
  });

  s.addNotes("Do not read all six out loud. Say 59 task specs and 16,467 lines. Let people read the rest.");
}

/* ══════════════ 15 · Schedule and risk gates ══════════════ */
{
  const s = newSlide("SCHEDULE");
  title(s, "Seven days, and two cut-off days", "If we missed a date, we cut features. We did not stay up all night.");

  const days = [
    ["Day 0", "Pitch and setup", "The project runs, and both of us can open it", false],
    ["Day 1", "Walk and look", "First-person movement, doors, and the AI path map", false],
    ["Day 2", "Tools in hand", "Tool slots, flashlight, rooms, and the on-screen display", false],
    ["Day 3", "Clues work", "EMF, UV prints, ghost writing, and the clue tracker", true],
    ["Day 4", "The ghost", "Ghost AI and hunts — our hardest day by far", true],
    ["Day 5", "The whole game runs", "Round manager, notebook, and the results screen", false],
    ["Day 6", "Art goes in", "Swap in models, rebuild the AI path map, play it all again", false],
    ["Day 7", "Polish", "Sound, lights, main menu, and a full test pass", false],
  ];
  const dh = 0.53;
  days.forEach((d, i) => {
    const y = 2.06 + i * (dh + 0.055);
    s.addShape(pres.ShapeType.roundRect, {
      x: ML, y, w: CW, h: dh, rectRadius: 0.04,
      fill: { color: d[3] ? "2A1519" : PANEL },
      line: { color: d[3] ? BLOOD : PANEL2, width: d[3] ? 1.1 : 0.75 },
    });
    s.addText(d[0], {
      x: ML + 0.28, y, w: 0.95, h: dh, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12.5, bold: true, color: d[3] ? BLOODBR : MOON, valign: "middle",
    });
    s.addText(d[1], {
      x: ML + 1.35, y, w: 2.5, h: dh, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, bold: true, color: TEXT, valign: "middle",
    });
    s.addText(d[2], {
      x: ML + 4.0, y, w: CW - 6.3, h: dh, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12.5, color: DIM, valign: "middle",
    });
    if (d[3]) {
      s.addText("CUT-OFF DAY", {
        x: ML + CW - 2.1, y, w: 1.84, h: dh, isTextBox: true, margin: 0,
        fontFace: FM, fontSize: 11.5, bold: true, color: BLOODBR, align: "right", valign: "middle",
      });
    }
  });

  footNote(s, "If the clues did not work by Day 3, we would drop ghost writing and use a 2 × 3 table. If the ghost AI did not work by Day 4, the art day would move. We did not have to do either.", MOONBR);
  s.addNotes("In a one-week project, saying when you would give up on something is worth more than saying what you want to build.");
}

/* ══════════════ 16 · Non-goals ══════════════ */
{
  const s = newSlide("NON-GOALS");
  title(s, "Seven things we chose not to build", "We wrote this list down so the week would not disappear");

  const nos = [
    ["Online multiplayer", "Network code costs 3 days at least. It would have eaten the whole project."],
    ["Voice input", "It needs software from someone else. Too risky for one week."],
    ["A second map", "One good map beats three empty ones."],
    ["A fourth ghost or clue", "The 3 × 3 table would stop working."],
    ["Save files", "A demo does not need them."],
    ["Jumping", "If you can jump you can climb on furniture. That breaks the level and the AI paths."],
    ["Our own 3D models", "We used free ones. One clear style beats one nice-looking chair."],
  ];
  const cw3 = (CW - 0.4) / 2;
  nos.forEach((n, i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const x = ML + col * (cw3 + 0.4);
    const y = 2.1 + row * 0.96;
    card(s, x, y, cw3, 0.84);
    s.addText("✕", {
      x: x + 0.26, y, w: 0.4, h: 0.84, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 17, bold: true, color: BLOODBR, valign: "middle",
    });
    s.addText(n[0], {
      x: x + 0.74, y: y + 0.12, w: cw3 - 1.0, h: 0.3, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 15.5, bold: true, color: TEXT,
    });
    s.addText(n[1], {
      x: x + 0.74, y: y + 0.43, w: cw3 - 1.0, h: 0.34, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM,
    });
  });

  card(s, ML, 5.98, cw3, 0.84, PANEL2);
  s.addText("A team that can tell you what it is NOT making is easier to trust.", {
    x: ML + 0.3, y: 5.98, w: cw3 - 0.6, h: 0.84, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 15, bold: true, italic: true, color: MOONBR, valign: "middle",
  });

  s.addNotes("This slide is here to build trust. Stop for a second after 'jumping'. People never expect that one.");
}

/* ══════════════ 17 · Demo ══════════════ */
{
  const s = newSlide("DEMO");
  title(s, "What you are about to watch", "About 8 to 12 minutes, one full game");

  const flow = [
    ["01", "Go in", "The lobby is safe. Three tools, and the flashlight takes one slot"],
    ["02", "Find the ghost room", "One of seven, picked at random. Cold air and the EMF reader"],
    ["03", "Get the clues", "Follow the number · look for prints · leave the book and come back"],
    ["04", "Sanity drops under 50 %", "Checked every 25 seconds. Lights flicker. A heartbeat starts"],
    ["05", "The hunt", "The house turns red, the flashlight dies. Corner, door, get out of sight"],
    ["06", "Leave and answer", "Back to the lobby, tick two clues, and pick a ghost"],
  ];
  const fw2 = (CW - 2 * 0.32) / 3;
  flow.forEach((f, i) => {
    const col = i % 3, row = Math.floor(i / 3);
    const x = ML + col * (fw2 + 0.32);
    const y = 2.06 + row * 1.48;
    card(s, x, y, fw2, 1.32, i >= 4 ? "231419" : PANEL);
    s.addText(f[0], {
      x: x + 0.26, y: y + 0.12, w: 0.7, h: 0.3, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12.5, color: i >= 4 ? BLOODBR : MOON,
    });
    s.addText(f[1], {
      x: x + 0.26, y: y + 0.42, w: fw2 - 0.52, h: 0.34, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 17, bold: true, color: TEXT,
    });
    s.addText(f[2], {
      x: x + 0.26, y: y + 0.8, w: fw2 - 0.52, h: 0.46, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM,
    });
  });

  const grades = [["S", "Right answer, both clues, sanity over 30 %"], ["A", "Right answer"], ["C", "Wrong answer, but got out alive"], ["F", "Caught during a hunt"]];
  const gw3 = (CW - 3 * 0.32) / 4;
  grades.forEach((g, i) => {
    const x = ML + i * (gw3 + 0.32);
    card(s, x, 5.18, gw3, 1.28, PANEL2);
    s.addText(g[0], {
      x: x + 0.26, y: 5.28, w: 0.8, h: 0.56, isTextBox: true, margin: 0,
      fontFace: FD, fontSize: 32, bold: true, color: i === 3 ? BLOODBR : MOONBR,
    });
    s.addText(g[1], {
      x: x + 0.26, y: 5.86, w: gw3 - 0.52, h: 0.5, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 11.5, color: DIM,
    });
  });

  s.addNotes("Leave this slide up while you switch to Unity. While you play, only say what is happening. Do not explain the rules again.");
}

/* ══════════════ 18 · Close ══════════════ */
{
  idx++;
  const s = pres.addSlide();
  s.background = { color: BG };

  s.addText("Eight days, from nothing to a playable game.", {
    x: ML, y: 1.5, w: CW, h: 0.9, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 36, bold: true, color: TEXT,
  });
  s.addText("We did not make a bigger game. We made one that finishes.", {
    x: ML, y: 2.44, w: CW, h: 0.46, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 19, color: MOON,
  });

  const take = [
    ["A clue table that always works", "3 ghosts × 3 clues. Two clues give one answer. No luck."],
    ["A way of working we can use again", "Spec → AI → auto check → build → self check → a person reads it"],
    ["Rules we agreed before we started", "No part touches another, so we could all work at the same time"],
  ];
  const tw3 = (CW - 2 * 0.36) / 3;
  take.forEach((t, i) => {
    const x = ML + i * (tw3 + 0.36);
    card(s, x, 3.3, tw3, 1.66);
    s.addText(String(i + 1).padStart(2, "0"), {
      x: x + 0.3, y: 3.42, w: 1.0, h: 0.32, isTextBox: true, margin: 0,
      fontFace: FM, fontSize: 12.5, color: DIM2,
    });
    s.addText(t[0], {
      x: x + 0.3, y: 3.76, w: tw3 - 0.6, h: 0.62, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 15, bold: true, color: MOONBR,
    });
    s.addText(t[1], {
      x: x + 0.3, y: 4.36, w: tw3 - 0.6, h: 0.56, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 12, color: DIM,
    });
  });

  s.addText("This was our first 3D game, and most of the eight days went into deciding what NOT to build.\nNext: a thermometer as a fourth clue, which gives a 4 × 4 table and six ghosts · a second map · two players on one PC", {
    x: ML, y: 5.24, w: CW, h: 0.72, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, color: DIM, lineSpacing: 20,
  });

  s.addText("RESIDUUM", {
    x: ML, y: 6.1, w: 5.5, h: 0.5, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 24, bold: true, color: TEXT, charSpacing: 4,
  });
  s.addText("Thank you  ·  Questions", {
    x: W - MR - 4.6, y: 6.16, w: 4.6, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 17, color: MOON, align: "right",
  });

  s.addNotes("Say thank you and stop talking. Do not add anything after it. Wait for questions.");
}

pres.writeFile({ fileName: process.argv[2] || "RESIDUUM_Final_EN.pptx" })
  .then((f) => console.log("written:", f));
