# RESIDUUM · 残响

A first-person horror investigation game. You walk into an apartment at night with three
instruments, work out which of three ghosts is in the building from the evidence it leaves,
and get out alive.

Unity 6 · URP · single-player · a seven-day vertical slice.

---

## Play it

**[→ Download the Windows build from Releases](../../releases)**

Unzip the whole folder and run `RESIDUUM.exe`. Windows may show a
"Windows protected your PC" warning because the build is not code-signed —
click **More info → Run anyway**. This is normal for student builds.

Requires Windows 10/11 64-bit and about 2 GB of free disk space.

---

## How it plays

A round takes five to ten minutes.

```
1  Pick up tools from the rack in the lobby (you can carry all three)
2  Go upstairs and find evidence
3  Press Tab to open the journal and rule ghosts out
4  Submit your verdict once one ghost remains
5  Return to the lobby, press E at the glass door to leave
```

### Three evidence types, three ghosts

Every ghost holds **exactly two** of the three evidence types, and no two ghosts share the
same pair — so **collecting two pieces always identifies the ghost uniquely**. There is no
luck involved.

| | EMF-5 | UV Fingerprints | Ghost Writing |
|---|:---:|:---:|:---:|
| **Spirit** 怨灵 | ✓ | ✓ | — |
| **Wraith** 幽影 | ✓ | — | ✓ |
| **Poltergeist** 骚灵 | — | ✓ | ✓ |

- **EMF-5** — take out the reader, left-click to power it on. The reading climbs as you
  approach the ghost and the beeping speeds up. Only a reading of **5** counts as evidence.
- **UV Fingerprints** — shine the UV light on door handles the ghost has touched.
  Handprints glow under ultraviolet.
- **Ghost Writing** — place the book on the floor and walk away. The ghost may write in it.
  You will hear the pen before you see the page.

### The hunt

Once your sanity drops below half, the ghost starts hunting. It moves at roughly your speed —
and **the Poltergeist is faster than you** — so running is not the answer.

The reliable move is **breaking line of sight**: turn a corner, close a door behind you.
Once the ghost loses you it searches where it last saw you, and when that fails it moves to
another room and keeps looking.

During a hunt every light in the house turns red and flickers, and sometimes the power
cuts out entirely. Your heartbeat gets faster and louder the closer the ghost gets.

There is a 100-second grace period after each hunt.

### Controls

| | |
|---|---|
| `W A S D` / Mouse | Move / Look |
| `Shift` | Sprint (about 4 seconds of stamina) |
| `C` | Crouch (toggle) |
| `E` | Interact / pick up |
| `1` `2` `3` | Switch evidence tool |
| `T` | Flashlight |
| `G` | Drop current tool |
| Left mouse | Use current tool |
| `Tab` | Deduction journal |
| `ESC` | Pause menu / return to title |

---

## Design decisions that did not move

**A 3×3 deduction table, no luck.** The table above is the skeleton of the whole game.
Three ghosts, two evidence types each, all three pairs distinct — so two pieces of evidence
always give a unique answer. Every instrument has a hard gate: if the ghost of the round does
not hold a given evidence type, that evidence **can never appear**. There is no
"I just got unlucky and it never showed up."

**Ghosts are data, not three classes.** All three share a single `GhostAI`. Everything that
differs — speed, hunt duration, which two evidence types it holds, whether it leaves
footprints — lives in a `GhostDefinition` ScriptableObject. Adding a fourth ghost means
creating one asset.

**All cross-module communication goes through a static event bus.** `GameEvents` is the only
channel between modules; no module is allowed to hold a reference to another
(the single exception is `GameManager`, which owns initialization). This rule is enforced by
a static scanner on every change, not by convention.

**No jumping, no multiplayer.** Jumping breaks level containment and the navmesh;
multiplayer would have eaten three of the seven days.

---

## Building from source

**You do not need this section to play the game — grab the build from Releases.**

To open the project, three things must be done first or the scene will come up empty:

### 👉 [`Docs/10_Windows上手指南.md`](Docs/10_Windows上手指南.md) (Chinese)

The three-minute version:

| # | Required | What happens if you skip it |
|---|---|---|
| 1 | Install Unity **`6000.5.8f1`** (not in the Hub list — use the deep link `unityhub://6000.5.8f1/5cb7df797b7d`) | The Hub adds the project but will not open it |
| 2 | Install **Git LFS**, run `git lfs install`, and use `git clone` (**not Download ZIP**) | Textures, audio and models arrive as text pointers and the import fails |
| 3 | Claim the free **Apartment Kit** from the Asset Store, import it, convert to URP, then run `python tools/check_kit.py` | 171 references in the Blockout scene break; you get wireframes and floating name labels |

> **Scene opens empty with a few wireframe boxes?** That is step 3.
> `Blockout.unity` has 1756 prefab instances; 171 external references point at that
> asset pack, which is not redistributed with this repository.
> **The repo is not broken.** Run `python tools/check_kit.py` and it will tell you exactly
> what is missing.

Main scene: `Assets/_Project/Scenes/Blockout.unity`

---

## Environment

| | |
|---|---|
| Unity | `6000.5.8f1` (revision `5cb7df797b7d`) — **pinned, do not upgrade or downgrade** |
| Render pipeline | URP 17.5.0 |
| Input | Input System 1.20.0 |
| Navigation | AI Navigation 2.0.14 |

---

## Layout

```
Assets/_Project/
  Scripts/          48 source files (Core / Evidence / Ghost / Items / Player / World / UI / Audio)
  Scenes/           Blockout.unity — the main scene
  ScriptableObjects/Ghosts/   the three ghost definitions
  Art/  Audio/      models, textures, sound effects
Docs/               design documents, indexed below
tools/codexctl/     task dispatcher and static gate toolchain
tools/check_kit.py  Apartment Kit reconciliation tool (run this first if the scene is empty)
```

## How this was built

Seven days, one person, and an automation pipeline.

`tools/codexctl/` is a dispatcher written for this project. Every module starts as a task
specification (there are 76 of them under `tools/codexctl/tasks/`) and then runs through:

```
implementation  →  static rule gates  →  headless Unity compile  →  repair rounds (≤2)
                →  structured self-audit  →  review package
```

The gates are regex scans that hard-check 18 items: cross-module references, deprecated APIs,
missing tooltips, and so on. Compilation runs in headless Unity, and **"compile skipped" is
explicitly treated as a failure** — code that was never verified must never count as passing.
Every round produces a review package, and a human reads the diff before anything merges.

Seven contract files (the event bus, four interfaces, the ghost data definition) are
human-owned. The automated path is forbidden from touching them, and any attempt is rejected
by the gate.

The design documents in [`Docs/`](Docs/) carry the full numeric tables, the architectural
rules, the 18-item audit checklist, and a per-module acceptance path for testing in the editor.

---

## Documents

Most design documents are written in Chinese.

| Document | Contents |
|---|---|
| [`01_GDD_残响.md`](Docs/01_GDD_残响.md) | Gameplay and every number: speed baselines, sanity decay, hunt probability, evidence rules |
| [`02_技术架构.md`](Docs/02_技术架构.md) | Directory layout, the four interfaces, five architectural rules |
| [`03_Codex任务包.md`](Docs/03_Codex任务包.md) | Original T01–T17 task definitions |
| [`04_七天排期.md`](Docs/04_七天排期.md) | Seven-day schedule and delivery checklist |
| [`05_美术协作规范.md`](Docs/05_美术协作规范.md) | Greybox contract for art handoff |
| [`06_审查流程.md`](Docs/06_审查流程.md) | The 18-item self-audit checklist |
| [`09_验收路径.md`](Docs/09_验收路径.md) | How to verify each module by hand in the editor |
| [`10_Windows上手指南.md`](Docs/10_Windows上手指南.md) | Required reading for new contributors on Windows |
| [`13_README_for_Instructor.txt`](Docs/13_README_for_Instructor.txt) | Build instructions and controls, in English |
| [`ASSET_LICENSES.md`](Docs/ASSET_LICENSES.md) | Third-party asset licenses |

---

## Team conventions

1. **Only one person edits the scene at a time.** `Blockout.unity` is 5.7 MB; two concurrent
   edits will conflict and cannot be merged.
2. **Commit your work before pulling.** Never `git pull` with a dirty working tree.
3. **`Library/`, `Temp/`, `Assets/Brick Project Studio/` and build output never enter the
   repository.**
4. Glance at `git status --short` before every commit.

---

## Assets

Third-party asset sources and licenses are listed in
[`Docs/ASSET_LICENSES.md`](Docs/ASSET_LICENSES.md).

The apartment geometry uses Brick Project Studio's Apartment Kit (free on the Unity Asset
Store). It is **not redistributed with this repository** — claim and import it yourself.
