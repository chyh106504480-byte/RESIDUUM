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
  s.addText("A first-person horror investigation game", {
    x: ML, y: 3.3, w: 7.2, h: 0.44, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 21, color: DIM,
  });
  s.addText("You don't know what it is. That is the entire game.", {
    x: ML, y: 3.94, w: 7.2, h: 0.44, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 17, italic: true, color: MOONBR,
  });

  s.addText("Unity 6000.5.8f1   ·   URP 17.5   ·   Single-player   ·   Vertical slice", {
    x: ML, y: 5.86, w: 8.0, h: 0.32, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 11.5, color: DIM2,
  });
  s.addText("Lead designer: Henry   ·   Final presentation   ·   August 2026", {
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

  s.addNotes("Ten seconds. Don't linger — say the title and the premise, then advance.");
}

/* ══════════════ 02 · Premise ══════════════ */
{
  const s = newSlide("PREMISE");
  title(s, "The game in one sentence", "If you only get one line, say this one");

  card(s, ML, 2.02, CW, 1.5);
  s.addText(
    "You walk into a house you shouldn't be in with three instruments. Before your sanity runs out, you must identify what is haunting it — and get back out alive.",
    {
      x: ML + 0.5, y: 2.3, w: CW - 1.0, h: 1.0, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 20, color: TEXT, lineSpacing: 32,
    }
  );

  const stats = [
    ["8–12", "minutes per run"],
    ["3", "ghosts, three data assets"],
    ["3", "kinds of evidence, three fears"],
    ["2", "of them settle it for good"],
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

  footNote(s, "Benchmarked against Phasmophobia — we copy its deductive structure, not its content volume.");
  s.addNotes("This slide is the what. The four numbers are the most memorable thing in the deck. Say them one at a time, with a beat between.");
}

/* ══════════════ 03 · Why we cut to 3x3 ══════════════ */
{
  const s = newSlide("SCOPE");
  title(s, "Decision one: cut it down to 3 × 3", "Content volume is not where the fun comes from");

  const cw2 = (CW - 0.5) / 2;
  card(s, ML, 2.06, cw2, 2.35);
  s.addText("Phasmophobia", {
    x: ML + 0.34, y: 2.26, w: cw2 - 0.68, h: 0.36, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 18, bold: true, color: DIM,
  });
  s.addText(
    [
      { text: "24 ghost types", options: { bullet: true, breakLine: true } },
      { text: "7 kinds of evidence", options: { bullet: true, breakLine: true } },
      { text: "40+ items", options: { bullet: true, breakLine: true } },
      { text: "Evidence sets overlap; players end up guessing", options: { bullet: true } },
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
      { text: "3 ghost types", options: { bullet: true, breakLine: true } },
      { text: "3 kinds of evidence", options: { bullet: true, breakLine: true } },
      { text: "4 items plus one deduction journal", options: { bullet: true, breakLine: true } },
      { text: "Zero overlap; any two clues are decisive", options: { bullet: true } },
    ],
    {
      x: ML + cw2 + 0.84, y: 2.76, w: cw2 - 0.68, h: 1.5, isTextBox: true, margin: 0,
      fontFace: F, fontSize: 14, color: TEXT, paraSpaceAfter: 7,
    }
  );

  s.addText("That game's appeal really comes down to three things — and all three survive at minimum scale:", {
    x: ML, y: 4.72, w: CW, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15, color: TEXT,
  });

  const three = [
    ["Asymmetric information", "You know it's there. You don't know what, or where."],
    ["Deduction under constraint", "Elimination, using a limited toolkit"],
    ["Risk pulling against reward", "One more second is one more clue, and one more chance to die"],
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

  s.addNotes("This slide buys credibility. We didn't cut because we ran out of time — we cut because we worked out what mattered.");
}

/* ══════════════ 04 · The matrix ══════════════ */
{
  const s = newSlide("THE MATRIX");
  title(s, "The core: a 3 × 3 deduction matrix", "★ The most important slide in the deck");

  const cols = ["", "EMF-5 reading", "UV fingerprints", "Ghost writing"];
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
  s.addText("Every ghost has exactly two — and the three ghosts fill all three pairs.", {
    x: ML + 0.4, y: 5.48, w: CW - 0.8, h: 0.42, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 19, bold: true, color: MOONBR, align: "center",
  });
  s.addText("This matrix is the most elegant thing in the project, and the source of every constraint in it.", {
    x: ML + 0.4, y: 5.94, w: CW - 0.8, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, color: DIM, align: "center",
  });

  s.addNotes("If there is a whiteboard, draw this instead of showing it — three rows, three columns, one checkmark at a time. Watching you build it lands far harder. Beat before advancing.");
}

/* ══════════════ 05 · Uniqueness ══════════════ */
{
  const s = newSlide("PROOF");
  title(s, "Why this matrix is airtight", "★ The peak of the talk");

  card(s, ML, 2.06, 3.9, 2.5, PANEL2);
  s.addText("C(3, 2)  =  3", {
    x: ML, y: 2.5, w: 3.9, h: 0.8, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 44, bold: true, color: MOONBR, align: "center",
  });
  s.addText("Choose two out of three and\nthere are exactly three pairs —\none for each ghost, one to one", {
    x: ML + 0.3, y: 3.34, w: 3.3, h: 1.0, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13.5, color: DIM, align: "center", lineSpacing: 22,
  });

  const facts = [
    ["One clue is never enough", "Every kind of evidence is shared by two ghosts, so one rules out only one"],
    ["Two clues are always decisive", "Pairs map one-to-one onto ghosts. There is no second solution"],
    ["Elimination works just as well", "Proving a kind of evidence absent advances the deduction too"],
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

  s.addText("No redundancy.    No ambiguity.    No luck.", {
    x: ML, y: 5.1, w: CW, h: 0.62, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 30, bold: true, color: MOONBR, align: "center", charSpacing: 2,
  });
  s.addText("When a player wins a run, they won it by deduction and nothing else.", {
    x: ML, y: 5.82, w: CW, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 14, color: DIM, align: "center",
  });

  s.addNotes("Stop dead after 'No luck.' Let it sit for two full seconds before you advance.");
}

/* ══════════════ 06 · Sanity ══════════════ */
{
  const s = newSlide("PACING");
  title(s, "Sanity is the pacing engine", "The answer to “why not just take your time?”");

  const lw = 6.4;
  card(s, ML, 2.06, lw, 4.0);
  s.addText("How sanity moves", {
    x: ML + 0.36, y: 2.24, w: lw - 0.72, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 16, bold: true, color: MOON,
  });
  const sanity = [
    ["Starting value", "100 %"],
    ["Standing in darkness", "−0.12 %/s"],
    ["In a lit room", "−0.06 %/s"],
    ["Holding a lit flashlight", "rate × 0.5"],
    ["Witnessing a ghost event", "−15 % once"],
    ["While being hunted", "−0.5 %/s"],
    ["Back in the entrance safe zone", "+1.0 %/s"],
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
  s.addText("Hunt probability", {
    x: rx + 0.36, y: 2.22, w: rw - 0.72, h: 0.3, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15, bold: true, color: MOON,
  });
  s.addText("P  =  ( 50 − sanity ) ÷ 50", {
    x: rx + 0.3, y: 2.62, w: rw - 0.6, h: 0.5, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 25, bold: true, color: BLOODBR, align: "center",
  });
  s.addText("Rolled once every 25 seconds, once sanity drops below 50 %", {
    x: rx + 0.36, y: 3.18, w: rw - 0.72, h: 0.44, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12.5, color: DIM, align: "center",
  });

  const pts = [["50 %", "0 %"], ["25 %", "50 %"], ["0 %", "certain"]];
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

  footNote(s, "Design intent: the player is permanently doing the arithmetic on how much longer they can stay. That anxiety is the gameplay.");
  s.addNotes("Deliver the line 'every extra piece of evidence costs you' slowly. That is the thesis of the whole design.");
}

/* ══════════════ 07 · Hunt speeds ══════════════ */
{
  const s = newSlide("THE HUNT");
  title(s, "Running is not the answer", "These numbers were re-tuned by hand, not guessed");

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
    ["No adrenaline bonus", "A hunt sprint is 3.5, same as any other sprint. No free crutch."],
    ["The Poltergeist is faster than you", "Spirit 3.3 and Wraith 3.4 you can just outrun. 3.6 you cannot."],
    ["Stamina lasts 4.2 seconds", "Then 3.5 seconds to refill, and you are back to walking at 2.0."],
    ["Break the line of sight", "Lose it and the ghost only searches your last known position. Turn a corner. Shut the door."],
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

  s.addNotes("This slide is about feel, not features. All six numbers were re-benchmarked on day seven.");
}

/* ══════════════ 08 · Three instruments ══════════════ */
{
  const s = newSlide("INSTRUMENTS");
  title(s, "Three instruments, three different fears", "They are not three ways of getting the same thing");

  const tools = [
    ["EMF Reader", "T R A C K", "You follow the reading. Higher means closer to where it just was.", "It pulls you toward it"],
    ["UV Flashlight", "S E A R C H", "You stand still and sweep handles and switches. Your flashlight is forced off.", "It takes your light away"],
    ["Ghost Writing Book", "W A I T", "You set it down in the ghost room, leave, and have to come back to read it.", "It sends you back in"],
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
  s.addText("Only three equipment slots, and the flashlight permanently occupies one. Every run forces a trade. That pressure is deliberate.", {
    x: ML + 0.5, y: 5.66, w: CW - 1.0, h: 0.9, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15.5, color: TEXT, valign: "middle",
  });

  s.addNotes("Track, search, wait — three behaviour patterns that do not overlap. That is the answer to 'why these three items'.");
}

/* ══════════════ 09 · The three ghosts ══════════════ */
{
  const s = newSlide("ENTITIES");
  title(s, "Three ghosts, one AI", "Every difference is expressed as data, not as a subclass");

  const ghosts = [
    ["Spirit", "Slow · stubborn", ["Lingers in the ghost room", "Heavy, distinct footsteps", "Longest hunt duration"], "Hunt 3.3 m/s"],
    ["Wraith", "Drifting · traceless", ["Leaves no floor prints", "Blinks short distances closer", "Periodic bursts while chasing"], "Hunt 3.4 m/s"],
    ["Poltergeist", "Violent · destructive", ["Throws interactable objects", "Flings everything at hunt start", "Drains sanity 1.5× faster"], "Hunt 3.6 m/s"],
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
  s.addText("Three ghosts  =  one GhostAI  +  three ScriptableObject data assets", {
    x: ML + 0.5, y: 5.48, w: CW - 1.0, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 18, bold: true, color: MOONBR,
  });
  s.addText("Adding a fourth ghost means authoring one asset file — not one line of code changes.", {
    x: ML + 0.5, y: 5.9, w: CW - 1.0, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, color: DIM,
  });

  s.addNotes("If asked 'aren't three ghosts too few', answer with this slide: four kinds of evidence would support six ghosts. We simply chose not to this week.");
}

/* ══════════════ 10 · Level and lighting ══════════════ */
{
  const s = newSlide("LEVEL");
  title(s, "One apartment, eight zones", "One polished map beats three grey-boxed ones");

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
  s.addText("Seven rooms are ghost-room candidates — one is drawn at random each run. The lobby is the safe zone.", {
    x: px, y: py + 4 * (zh + zg) + 0.16, w: pw, h: 0.5, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12, color: DIM,
  });

  const rx = ML + pw + 0.6, rw = CW - pw - 0.6;
  const items = [
    ["Cold blue moonlight is the only ambient", "A directional light through the windows. The interior is almost black."],
    ["Your flashlight is the only warm source", "4200 K, 12 m range, 45° cone. That is all the light you get."],
    ["Every room has a switchable ceiling light", "Light slows sanity loss but makes the ghost likelier to notice you."],
    ["Hunts shift the whole house red and flicker it", "The flashlight dies. There is a chance the power cuts entirely."],
    ["The corridor breaks up sightlines", "You rarely see the whole space at once — footsteps tend to arrive before vision."],
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

  s.addNotes("Eight RoomVolumes in the scene, seven of them ghost-room candidates. The cheapest thing about a horror game: you don't have to build what nobody can see.");
}

/* ══════════════ 11 · Architecture ══════════════ */
{
  const s = newSlide("ARCHITECTURE");
  title(s, "Zero direct references between modules", "So several AI sessions can write code in parallel without colliding");

  card(s, 4.55, 3.5, 4.22, 1.08, PANEL2);
  s.addText("Core.GameEvents", {
    x: 4.55, y: 3.62, w: 4.22, h: 0.42, isTextBox: true, margin: 0,
    fontFace: FM, fontSize: 19, bold: true, color: MOONBR, align: "center",
  });
  s.addText("static event bus · 23 events", {
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
    ["Everything crosses via the bus", "Module files may not import another module's namespace"],
    ["Four interfaces fixed up front", "IInteractable · IHoldable · IEvidenceSource · GhostDefinition"],
    ["The contract has one author", "7 contract files owned by the lead. The AI cannot touch them"],
    ["Tunable values are serialized", "Designers tune in the Inspector, never in code"],
    ["Ghosts are data, not classes", "Behavioural differences live in ScriptableObjects"],
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

  s.addNotes("The payoff: any two modules can be written simultaneously by two sessions that know nothing about each other, and it will still compile.");
}

/* ══════════════ 12 · Pipeline ══════════════ */
{
  const s = newSlide("PIPELINE");
  title(s, "How the code actually got written", "★ The pipeline is a deliverable in its own right");

  const steps = [
    ["01", "Task spec", "Written by a\nhuman. Files,\nvalues, limits,\nacceptance tests"],
    ["02", "Implement", "Codex fills in\nbehind the\ncontract. It\ncannot edit it"],
    ["03", "Static gates", "8 regex rules.\nAny hit sends\nit straight back"],
    ["04", "Compile", "Headless batch\nmode. Real\ncompiler errors"],
    ["05", "Self-audit", "18-point list,\nanswered line\nby line"],
    ["06", "Human review", "A person reads\nthe diff and\npasses or rejects"],
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

  s.addText("A failure at step 3 or 4 returns automatically to step 2 carrying the raw error text. Two rounds maximum.", {
    x: ML, y: 4.56, w: CW, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, italic: true, color: DIM, align: "center",
  });

  card(s, ML, 5.08, CW, 1.44, PANEL2);
  s.addText("Humans own design, contract and acceptance. The AI owns implementation.", {
    x: ML + 0.45, y: 5.24, w: CW - 0.9, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 17, bold: true, color: TEXT,
  });
  s.addText("The author and the reviewer have to be two independent viewpoints. The AI generated the code, but a human wrote the architecture — the event bus, the interfaces, the data structures were fixed as contracts first. An AI can write every script in this project in a day; only a person can tell whether it feels right.", {
    x: ML + 0.45, y: 5.68, w: CW - 0.9, h: 0.74, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12.5, color: DIM,
  });

  s.addNotes("This is the differentiator. Other teams hand in a game. We hand in a game plus a production line that transfers to the next project.");
}

/* ══════════════ 13 · Gates ══════════════ */
{
  const s = newSlide("GATES");
  title(s, "Eight hard gates. One hit and it goes back.", "An AI never gets tired — but it will make the same mistake a hundred times");

  const gates = [
    ["G01", "Deprecated Find APIs are banned", "error"],
    ["G02", "Rigidbody.velocity → linearVelocity", "error"],
    ["G03", "No legacy Input Manager", "error"],
    ["G04", "CinemachineVirtualCamera was renamed", "error"],
    ["G05", "No direct imports between modules", "error"],
    ["G06", "Tunables must be SerializeField private", "warn"],
    ["G07", "No GameObject.Find by name", "warn"],
    ["G08", "Strip debug logging before commit", "warn"],
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
  s.addText("Plus an 18-point self-audit", {
    x: rx + 0.3, y: 2.22, w: rw - 0.6, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 16, bold: true, color: MOONBR,
  });
  s.addText("The AI answers every point on delivery and separately lists anything it decided on its own. Those are usually where the next module's landmines are.", {
    x: rx + 0.3, y: 2.62, w: rw - 0.6, h: 0.82, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12, color: DIM,
  });

  card(s, rx, 3.72, rw, 1.32);
  s.addText("Gates outrank the self-audit", {
    x: rx + 0.3, y: 3.88, w: rw - 0.6, h: 0.34, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 15, bold: true, color: TEXT,
  });
  s.addText("A self-audit is what the AI says about itself. A gate is a fact about the source text.", {
    x: rx + 0.3, y: 4.26, w: rw - 0.6, h: 0.66, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 12, color: DIM,
  });

  card(s, rx, 5.2, rw, 1.32);
  s.addText("A gate must never accuse the innocent", {
    x: rx + 0.3, y: 5.34, w: rw - 0.6, h: 0.36, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 14.5, bold: true, color: BLOODBR,
  });
  s.addText("Our first rule set flagged 2 errors and 15 warnings on a clean tree. All false positives — that doesn't add noise, it burns a whole round.", {
    x: rx + 0.3, y: 5.74, w: rw - 0.6, h: 0.68, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 11.5, color: DIM,
  });

  s.addNotes("If someone asks 'if the AI wrote it, is it yours?', the answer lives on this slide and the previous one.");
}

/* ══════════════ 14 · Numbers ══════════════ */
{
  const s = newSlide("NUMBERS");
  title(s, "Eight days, measured", "Every figure is verifiable in the repository");

  const nums = [
    ["59", "task specifications", "Each one fixes the files, the values and the acceptance criteria"],
    ["58", "pipeline runs", "Gates, compile and self-audit, start to finish"],
    ["182", "commits", "Contract, implementation, scene and docs kept separate"],
    ["43", "C# scripts", "34 runtime plus 9 editor assembly tools"],
    ["16,467", "lines of C#", "All module code produced by the AI behind the contract"],
    ["3,017", "lines of design docs", "GDD, architecture, schedule, review process, onboarding"],
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

  s.addNotes("Do not read all six. Say 59 task specs and 16,467 lines, and let them read the rest.");
}

/* ══════════════ 15 · Schedule and risk gates ══════════════ */
{
  const s = newSlide("SCHEDULE");
  title(s, "Seven days, two risk gates", "Miss the gate and features get cut. No heroics.");

  const days = [
    ["Day 0", "Pitch and setup", "Project stands up; both members can clone and run it", false],
    ["Day 1", "Walking and looking", "First-person controller, interaction, NavMesh bake", false],
    ["Day 2", "Instruments in hand", "Equipment slots, flashlight, room system, HUD", false],
    ["Day 3", "Evidence loop closed", "EMF, UV fingerprints, ghost writing, evidence manager", true],
    ["Day 4", "The ghost arrives", "Ghost AI and hunt scheduling — the hardest day of the project", true],
    ["Day 5", "Full loop running", "Round manager, deduction journal, results screen", false],
    ["Day 6", "Art lands", "Swap assets, re-bake NavMesh, replay the whole loop", false],
    ["Day 7", "Polish and hand-off", "Audio, lighting, main menu, acceptance walkthrough", false],
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
      s.addText("RISK GATE", {
        x: ML + CW - 2.1, y, w: 1.84, h: dh, isTextBox: true, margin: 0,
        fontFace: FM, fontSize: 11.5, bold: true, color: BLOODBR, align: "right", valign: "middle",
      });
    }
  });

  footNote(s, "If evidence hadn't closed by day 3, ghost writing was to be cut down to a 2 × 3 table. If the AI hadn't worked by day 4, the art day moved. Neither gate ever fired.", MOONBR);
  s.addNotes("In a one-week project, being able to state your abort conditions is worth ten times being able to state your goals.");
}

/* ══════════════ 16 · Non-goals ══════════════ */
{
  const s = newSlide("NON-GOALS");
  title(s, "The seven things we deliberately did not build", "Written down so the week couldn't be eaten alive");

  const nos = [
    ["Multiplayer", "Network sync costs three days minimum. It would have eaten the slice."],
    ["Voice recognition", "Depends on a third-party SDK. Unbounded risk."],
    ["A second map", "One polished map beats three grey-boxed ones."],
    ["A fourth ghost or clue", "It would break the mathematical closure of the 3 × 3 matrix."],
    ["Saves and progression", "A vertical slice does not need it."],
    ["Jumping", "If you can jump you can climb furniture — it breaks level closure and navmesh."],
    ["Original art assets", "All free-licensed. Consistency of style beats fidelity of any one piece."],
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
  s.addText("A team that can tell you what it isn't building is ten times more credible.", {
    x: ML + 0.3, y: 5.98, w: cw3 - 0.6, h: 0.84, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 15, bold: true, italic: true, color: MOONBR, valign: "middle",
  });

  s.addNotes("This slide exists purely to buy credibility. Pause after 'jumping' — that one is the most counter-intuitive.");
}

/* ══════════════ 17 · Demo ══════════════ */
{
  const s = newSlide("DEMO");
  title(s, "What you're about to watch", "Eight to twelve minutes, one complete loop");

  const flow = [
    ["01", "Enter", "The entrance is the safe zone. Three instruments; the flashlight takes a slot"],
    ["02", "Find the ghost room", "One of seven, drawn at random. Temperature drop plus EMF"],
    ["03", "Gather evidence", "Chase the reading · sweep for prints · plant the book and return"],
    ["04", "Sanity drops below 50 %", "Rolled every 25 seconds. Lights start flickering. The heartbeat comes up"],
    ["05", "The hunt", "The house goes red, the flashlight dies. Corner, door, break line of sight"],
    ["06", "Evacuate and call it", "Back to the entrance, tick two clues in the journal, name the ghost"],
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

  const grades = [["S", "Correct + both clues + sanity above 30 %"], ["A", "Correct call"], ["C", "Wrong call, walked out alive"], ["F", "Caught during a hunt"]];
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

  s.addNotes("Leave this slide up while you switch to Unity. During the demo, narrate what is happening, never the mechanics — those are already explained.");
}

/* ══════════════ 18 · Close ══════════════ */
{
  idx++;
  const s = pres.addSlide();
  s.background = { color: BG };

  s.addText("Zero to a playable run, in eight days.", {
    x: ML, y: 1.5, w: CW, h: 0.9, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 42, bold: true, color: TEXT,
  });
  s.addText("We didn't build a bigger game. We built one that closes.", {
    x: ML, y: 2.44, w: CW, h: 0.46, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 19, color: MOON,
  });

  const take = [
    ["A deduction core that closes mathematically", "3 ghosts × 3 clues. Two clues decide it. No luck involved."],
    ["A production pipeline that transfers", "Spec → AI → gates → compile → self-audit → human review"],
    ["An architecture contract fixed in advance", "Zero direct references, so any two modules can be built in parallel"],
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

  s.addText("Next: a thermometer as a fourth clue, expanding to 4 × 4 and supporting six ghosts · a second map · incense and crucifix · local co-op", {
    x: ML, y: 5.36, w: CW, h: 0.36, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 13, color: DIM,
  });

  s.addText("RESIDUUM", {
    x: ML, y: 6.1, w: 5.5, h: 0.5, isTextBox: true, margin: 0,
    fontFace: FD, fontSize: 24, bold: true, color: TEXT, charSpacing: 4,
  });
  s.addText("Thank you  ·  Questions", {
    x: W - MR - 4.6, y: 6.16, w: 4.6, h: 0.4, isTextBox: true, margin: 0,
    fontFace: F, fontSize: 17, color: MOON, align: "right",
  });

  s.addNotes("Say 'thank you' and stop. Do not explain anything after it. Let the last line hang and wait for questions.");
}

pres.writeFile({ fileName: process.argv[2] || "RESIDUUM_Final_EN.pptx" })
  .then((f) => console.log("written:", f));
