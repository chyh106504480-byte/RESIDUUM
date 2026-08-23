# RESIDUUM — English Pitch Script

**Deck**: https://claude.ai/code/artifact/d7f38f22-ea16-4021-9f7f-4a51214a6185
Arrow keys / space / click to advance. 9 slides. Core = 90 seconds, full = ~3 minutes.

---

## Say these out loud a few times first

| Word | Say it like | Not |
|------|-------------|-----|
| **Residuum** | ri-ZID-yoo-um | "resi-doom" |
| **Wraith** | RAYTH (one syllable, rhymes with *faith*) | "wray-ith" |
| **Poltergeist** | POLE-ter-guyst | "polter-gee-st" |
| **Phasmophobia** | faz-mo-FOH-bee-uh | — |
| **Sanity** | SAN-i-tee | — |
| **Deduction** | dee-DUK-shun | — |

The only three you'll say more than once are **Wraith**, **Poltergeist**, and **sanity**. Get those three comfortable and the rest takes care of itself.

---

## Slide-by-slide

### 1 · Cover — 10 sec

> "Our game is called **Residuum**. It's a first-person horror investigation game. Unity 6, ten minutes a run, two people, one week to a playable vertical slice."

Don't linger. Advance.

---

### 2 · The scene — 15 sec

**Slow down here.** One line, one beat. This is your only chance to put them inside the game.

> "You're standing in a dark hallway."　*[beat]*
> "Your flashlight is the only light."　*[beat]*
> "You know something is in here."　*[beat]*
> "You don't know what."

Hold one full second before you advance.

---

### 3 · The matrix — 25 sec ★ CORE

> "Your job is to figure out what it is."
>
> "There are three ghosts. And three kinds of evidence — an EMF reading, ultraviolet fingerprints, and ghost writing."
>
> "Here's the design. **Every ghost has exactly two.**"

**If there's a whiteboard, draw this instead of showing it.** Three rows, three columns, fill the checkmarks in one at a time while you talk. Watching you build it lands far harder than watching a finished slide.

---

### 4 · Uniqueness — 20 sec ★ CORE

> "Pick two out of three, and there are exactly three combinations. Three combinations, three ghosts — one to one."
>
> "So **one piece of evidence is never enough** — every piece is shared by two ghosts. But find two, and the answer is certain."　*[beat]*
>
> "No luck. Pure deduction."

**This is the peak of the whole pitch.** Stop after "pure deduction." Let it sit.

---

### 5 · Sanity — 20 sec

> "So why not just take your time? Because your sanity is dropping."
>
> "Stay in the dark, and it falls. The lower it gets, the more likely the ghost hunts you. Below fifty percent it starts rolling. At zero, it's guaranteed."
>
> "So every extra piece of evidence costs you. **That trade is the whole game.**"

---

### 6 · What we're not building — 20 sec

> "Phasmophobia has twenty-four ghosts, seven kinds of evidence, forty tools. We're not copying the content. We're copying the logic."
>
> "So — no multiplayer. No voice recognition. No second map. No fourth ghost."
>
> "One finished room beats three grey boxes."

> **This slide buys you credibility.** In a one-week project, the person who can say what they're *not* building is ten times more believable than the person listing features.

---

### 7 · Seven days — 15 sec · skip if short on time

> "The schedule is planned day by day. Evidence loop closes on day three. Ghost AI on day four. Full playable cycle on day five — and at that point it's still all white boxes. We don't touch art until day six."
>
> "The two red days are risk gates. If we're not there on time, we cut features. We don't push through."

---

### 8 · Recruiting — 20 sec ★ DO NOT SKIP

**Look at the room, not the screen.**

> "Last thing. I need one teammate — and specifically, someone whose strength is art."
>
> "Ninety percent of horror is atmosphere, not mechanics. The design is already finished. The code is handled — I'm working with AI on it."
>
> "You won't write a line of code, and you won't wait on me. Day one you start in your own scene, doing lighting, materials, atmosphere."
>
> "How this game feels is entirely your call."

---

### 9 · Close — 10 sec

> "Find the evidence. Name what's hunting you. Get out alive."　*[beat]*
>
> "Design document, seven-day schedule, seventeen build modules — all written. The project is set up, and the first module shipped today. We're not starting from zero. We're starting from day two."

That last line is what they'll remember.

---

## Time cuts

| You have | Show |
|----------|------|
| **30 sec** | 3 → 4, then one sentence of 5 |
| **90 sec** | 2 → 3 → 4 → 5 → 6 → 8 |
| **3 min** | All nine |
| **5 min+** | All nine, draw the matrix live, leave room for questions |

---

## Q&A

**"Isn't this just a Phasmophobia clone?"**

> "We're borrowing its core loop, and we're not hiding that. The difference is the deduction system. In Phasmophobia, a lot of the twenty-four ghosts share overlapping evidence sets, so players often end up guessing from behaviour. Ours has zero ambiguity — two pieces of evidence always identify exactly one ghost. That's a design decision, not a simplification."

**"Can you actually finish this in a week?"**

> "The parts we couldn't finish are already cut. What's left is one map, three ghosts, three kinds of evidence, four tools. It's broken into seventeen modules, each with a defined interface and acceptance criteria. And we've set two risk gates — day three and day four. If we're not on target, we cut more. We don't push through."

**"Why no multiplayer? That's the best part of Phasmophobia."**

> "Agreed, it is. But network synchronisation costs at least three days, and in a one-week project those three days would eat everything else. We'd rather ship a single-player slice with a complete loop than a multiplayer build with nothing in it. Multiplayer is the obvious next step — it's just not this week."

**"How are two people going to make it look scary?"**

> "We're not authoring assets. Everything comes from free libraries — Poly Haven is CC-zero, Mixamo is free, and there are free realistic interior packs on the Unity store. Consistency matters far more than per-asset quality. And the cheapest thing about horror is that you don't have to build what people can't see. Our ghost doesn't even render most of the time."

**"Three ghosts — won't players get bored after two runs?"**

> "For a slice, three is enough — a run is ten minutes, so you need at least three runs to see them all. And the structure scales cleanly: four kinds of evidence supports six ghosts, five supports ten. The expansion path is obvious. We're just not doing it this week."

**"You're using AI to write the code — do you actually know how to build this?"**

> "The code is generated, but the architecture is mine. I wrote the event bus, the interfaces, and the data structures first, and locked them. The AI only fills in implementations inside that contract, and every file goes through an eighteen-point review before it lands. The AI can write every script in this project in a day — but only I can tell whether it *feels* right. My job is design and review, not typing."

---

## Before you go on

- [ ] Open the deck, press **F for fullscreen**, click through once to check the projector
- [ ] Whiteboard marker, in case you can draw the matrix live
- [ ] This script open on your phone
- [ ] Decide now who you'll walk up to after the pitch — don't wait for the artist to find you

---

## One last thing

**Slow down.** Nerves make everyone speak faster, and in a second language they make you speak much faster. Take the full ninety seconds for ninety seconds of material.

The two places to deliberately stop: after **"You don't know what."** and after **"No luck. Pure deduction."** Both are supposed to land in silence.

---

*Chinese version: `07_Pitch讲稿.md` · [中文 deck](https://claude.ai/code/artifact/4431b61f-ebe2-425f-9bbd-81e565ad4e00)*
