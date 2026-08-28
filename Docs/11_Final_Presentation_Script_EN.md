# RESIDUUM — Final Presentation Script (English)

**Deck**: `Docs/decks/RESIDUUM_Final_EN.pptx` — 18 slides, 16:9
**Target**: 12 minutes of talk + 8 minutes of live demo + 5 minutes of questions
**Speaker**: Henry

---

## Say these out loud a few times first

| Word | Say it like | Not |
|---|---|---|
| **Residuum** | ri-ZID-yoo-um | "resi-doom" |
| **Wraith** | RAYTH — one syllable, rhymes with *faith* | "wray-ith" |
| **Poltergeist** | POLE-ter-guyst | "polter-gee-st" |
| **Phasmophobia** | faz-mo-FOH-bee-uh | — |
| **Sanity** | SAN-i-tee | — |
| **Deduction** | dee-DUK-shun | — |
| **NavMesh** | NAV-mesh | "nav-em-esh" |
| **Scriptable Object** | SCRIPT-a-bul OB-ject | — |

The four you'll say more than once are **Wraith**, **Poltergeist**, **sanity**,
and **deduction**. Get those comfortable and the rest takes care of itself.

---

## Three things to hold on to

**One. The 3 × 3 matrix is still your strongest weapon.** That hasn't changed since
the pitch. Slides 4 and 5 are the peak; every other slide is either setting them up
or backing them up. If you get cut to three minutes, do 4, 5 and 6 and nothing else.

**Two. A final presentation is a different job from a pitch.** A pitch asks *is this
worth building*. A final asks *did you do what you said*. That's why slides 14 and 15
matter — they are the only evidence in the deck that the promises were kept. Say those
numbers with confidence.

**Three. Slides 12 and 13 are what separates you from every other team.** Other teams
hand in a game. You hand in a game *and* a production line. Present it as an
engineering decision, not as a party trick.

---

## Slide by slide

### 1 · Cover — 15 sec

> "Our game is called **Residuum**. It's a first-person horror investigation game.
> Unity 6, single-player, about ten minutes a run."
>
> "Here's the premise in one line: **you don't know what it is. That is the entire game.**"

Don't explain the grid on the right yet — slide 4 pays it off. Advance.

---

### 2 · Premise — 40 sec

Read the sentence in the box. **Slowly.** One clause, one beat.

> "You walk into a house you shouldn't be in with three instruments." *[beat]*
> "Before your sanity runs out, you must identify what is haunting it — and get back
> out alive."

Beat. Then point at the four numbers and say them **one at a time**:

> "Eight to twelve minutes a run. Three ghosts. Three kinds of evidence.
> **Any two of them settle it for good.**"
>
> "We benchmarked against Phasmophobia — but we only copied its deductive structure,
> not its content volume. Here's why."

---

### 3 · Why we cut to 3 × 3 — 50 sec

> "Phasmophobia has twenty-four ghosts, seven kinds of evidence, forty-odd items.
> Its evidence sets overlap heavily, so players often finish collecting and are
> still guessing from behaviour."
>
> "We have three ghosts, three kinds of evidence, four items. And we didn't cut
> because we ran out of time — we cut because we worked out what mattered."

Point at the three cards along the bottom:

> "That game's appeal really comes down to three things. **Asymmetric information** —
> you know it's there, you don't know what or where. **Deduction under constraint** —
> elimination with a limited toolkit. And **risk pulling against reward** — one more
> second is one more clue, and one more chance to die."
>
> "All three of those survive completely at minimum scale. So we kept all three,
> and cut only the volume."

---

### 4 · The matrix — 60 sec ★ CORE

**Slow down. If there's a whiteboard, walk over and draw this instead of showing it —
three rows, three columns, one checkmark at a time.**

> "There are three ghosts: the **Spirit**, the **Wraith**, and the **Poltergeist**.
> And three kinds of evidence: an **EMF reading**, **ultraviolet fingerprints**,
> and **ghost writing**."
>
> "Here's the design —" *[beat]*
>
> "**Every ghost has exactly two. Not one, not three.**"
>
> "The Spirit is EMF plus fingerprints. The Wraith is EMF plus writing.
> The Poltergeist is fingerprints plus writing."

Point at each row as you say it, so their eyes follow your hand.
Beat after the last one, then advance.

---

### 5 · Uniqueness — 60 sec ★ PEAK OF THE TALK

> "Why is this airtight? Because if you choose two things out of three, there are
> **exactly three combinations**. Three combinations, three ghosts — one to one.
> Not one spare, not one missing."

Point at the three numbered points:

> "That gives you three consequences."
>
> "One. **A single clue is never enough.** Every kind of evidence is shared by two
> ghosts — find an EMF reading and you've only ruled out the Poltergeist. Two left."
>
> "Two. **Two clues are always decisive.** There is no second solution."
>
> "Three. **Elimination works just as well.** Proving a kind of evidence *absent*
> also advances the deduction."

Beat. Now the line at the bottom, word by word:

> "**No redundancy. No ambiguity. No luck.**"
>
> "When a player wins a run, they won it by deduction and nothing else."

**This is the peak.** Stop for two full seconds before you advance. Don't fill it.

---

### 6 · Sanity — 50 sec

> "Which raises the obvious question. If the answer is knowable, why not just take
> your time?"
>
> "Because your sanity is dropping."

Point at the left-hand table:

> "Standing in the dark costs you about a tenth of a percent every second. A lit room
> is half that. Holding a flashlight halves it again. Witnessing a ghost event takes
> fifteen percent in one hit. Being hunted takes half a percent a second. The only
> place you recover is the entrance."

Point at the formula:

> "Once sanity drops below fifty percent, the game rolls **once every twenty-five
> seconds** to start a hunt. The probability is just this: fifty minus your sanity,
> over fifty."
>
> "At fifty percent, that's zero. At twenty-five, it's a coin flip.
> **At zero, the hunt is guaranteed.**"

Beat.

> "So every extra clue costs you. The player is permanently doing arithmetic on how
> much longer they can stay. **That anxiety is the gameplay.**"

---

### 7 · The hunt — 50 sec

> "Once a hunt starts, most people's instinct is to run. We made sure running isn't
> the answer."

Point at the chart, right to left:

> "A player sprints at three and a half metres per second. The Spirit hunts at 3.3
> and the Wraith at 3.4 — you can just barely outrun those.
> **The Poltergeist moves at 3.6. It is faster than you.**"
>
> "And there's no adrenaline bonus during a hunt — a hunt sprint is exactly the same
> 3.5 as any other sprint. Your stamina lasts **4.2 seconds**, then takes three and
> a half to refill, and you're walking the whole time."

Beat.

> "So the only reliable move is to **break line of sight**. Lose it and the ghost only
> searches your last known position. Turn a corner, shut the door behind you,
> and you're gone."
>
> "We re-benchmarked all six of those numbers on day seven, specifically so that
> running would be a **costly choice** rather than a safe answer."

---

### 8 · The three instruments — 45 sec

> "About the tools. These three instruments are not three ways of getting the same
> thing. They are **three different fears**."

Take them one at a time:

> "The EMF reader makes you **track**. A higher reading means you're closer to where
> it just was — **it pulls you toward it.**"
>
> "The UV light makes you **search**. And while it's equipped, your flashlight is
> forced off — ultraviolet fluorescence is only visible in the dark, so that's physics,
> not a rule we invented. **It takes your light away.**"
>
> "The ghost writing book makes you **wait**. You set it down, you leave, and then you
> have to go back in to read it. **It sends you back in.**"

Point at the bottom bar:

> "And there are only three equipment slots, one of which the flashlight permanently
> occupies. So every run forces a trade. That pressure is deliberate."

---

### 9 · The three ghosts — 35 sec

> "On the matrix the three ghosts are symmetrical, but they don't play the same at all."
>
> "The Spirit is slow and stubborn — it lingers in its room, its footsteps are heavy
> and distinct. The Wraith drifts, blinks closer, and **leaves no footprints on the
> floor** — players can actually use that as a tell. The Poltergeist throws things,
> and drains your sanity half again as fast just by being around."

Point at the bar along the bottom — this is the engineering point:

> "But in the code, this is **not three classes**. It's **one GhostAI plus three
> ScriptableObject data assets.**"
>
> "So adding a fourth ghost means authoring one asset file. Not one line of code
> changes. The extension path was clear from day one."

---

### 10 · Level and lighting — 35 sec

> "The map is a single-floor apartment. Eight zones — a main room, a kitchen, a
> corridor, a washroom, three bedrooms, and a lobby. **Seven of them are ghost-room
> candidates, and one is drawn at random each run** — the player has to find it from
> temperature drop and EMF readings. The lobby is the safe zone."

Point at the right:

> "For lighting, the only ambient source is **cold blue moonlight** through the
> windows. The interior is almost black. The only warm light in the game is the
> flashlight in your hand — forty-two hundred Kelvin, twelve metre range."
>
> "When a hunt starts the whole house shifts red and flickers, your flashlight dies,
> and there's a chance the power cuts entirely."
>
> "And the corridor breaks up the sightlines — you rarely see the whole space at once,
> so **footsteps tend to arrive before vision.**"

If you're running short, cut this slide to its first and last sentence.

---

### 11 · Architecture — 45 sec

> "A word on the engineering."
>
> "The architecture has one rule at its centre: **zero direct references between
> modules.** Player, Ghost, Evidence, Items, World, UI — none of them know each other
> exists. Everything crosses through this static event bus in the middle,
> twenty-three events in total."

Point at the five rules:

> "On top of that: four interfaces fixed up front, seven contract files that only one
> person is allowed to edit, every tunable value exposed to the Inspector,
> and ghosts as data rather than classes."

Beat, then land why the rule is worth anything:

> "The payoff is direct: **any two modules can be written at the same time by two
> sessions that know nothing about each other, and it will still compile.**
> That's the precondition for writing sixteen thousand lines in a week."

---

### 12 · The pipeline — 60 sec ★ DIFFERENTIATOR

> "So — how did the code actually get written? We didn't just let an AI loose on it.
> We built it a pipeline. Six steps."

Left to right, one line each, **don't linger on any one**:

> "One: the task spec, **written by a human** — which files to touch, the default value
> of every number, what it is explicitly forbidden to do, and the acceptance criteria."
>
> "Two: the AI implements, behind the contract. It has no permission to edit the
> contract at all."
>
> "Three: static gates. Eight regular expressions. Any hit and it goes straight back."
>
> "Four: a headless Unity compile, for real compiler errors."
>
> "Five: the AI answers an eighteen-point audit checklist, line by line."
>
> "Six: **a human reads the diff** and either passes it or rejects it."

Point at the italic line:

> "A failure at step three or four returns automatically to step two, carrying the raw
> error text. Two rounds maximum."

Beat, then the bottom bar:

> "**Humans own design, contract and acceptance. The AI owns implementation. The author
> and the reviewer have to be two independent viewpoints.**"
>
> "An AI can write every script in this project in a day. But only a person can tell
> whether it *feels* right."

---

### 13 · Gates — 40 sec

> "These eight on the left are the gate rules. The first five are errors — a hit sends
> the work straight back. The last three are warnings."
>
> "Why bother? Because **an AI never gets tired, but it will make the same mistake a
> hundred times.** Unity 6 deprecated an entire family of lookup APIs. You can say so
> a hundred times in the prompt and it will still reach for them — but a regular
> expression catches it for free, every single time."

Point at the third card on the right — this one is worth its own beat:

> "One lesson worth stating on its own. **A gate must never accuse the innocent.**"
>
> "Our first rule set flagged two errors and fifteen warnings on a completely clean
> repository. **Every one was a false positive.** That isn't noise — it burns an entire
> round, because the AI then spends two attempts 'fixing' things that were never
> broken, some of which it isn't even allowed to touch. So we made it a rule:
> you validate a gate against a clean tree before you ship it."

---

### 14 · Numbers — 30 sec

**Don't read all six.** Pick two, let them read the rest.

> "Eight days, measured."
>
> "**Fifty-nine task specifications** — each one fixing the files, the values and the
> acceptance criteria before a line was written."
>
> "**Sixteen thousand four hundred and sixty-seven lines of C#**, across forty-three
> scripts, plus three thousand lines of design documentation."
>
> "All of it is in the repository and every figure is verifiable."

---

### 15 · Schedule and risk gates — 40 sec

> "The schedule was planned to the day from day zero."
>
> "Day one, walking and looking. Day two, instruments in hand. Day three, the evidence
> loop closes. Day four, the ghost arrives. Day five, the full loop runs — and note
> that at this point everything is still grey boxes. **Art doesn't land until day six.**"

Point at the two red rows:

> "These two are **risk gates**. If evidence hadn't closed by the end of day three,
> ghost writing was going to be cut and the matrix reduced to two-by-three. If the
> ghost AI hadn't worked by the end of day four, the art day moved and we'd have
> sacrificed the visuals."

Beat.

> "**Neither gate ever fired.** But both were written into the documents on day zero —
> not added afterwards. That's why we were willing to commit to this schedule."

---

### 16 · Non-goals — 35 sec

> "Finally, what we deliberately did **not** build."
>
> "No multiplayer — network sync costs three days minimum, and it would have eaten
> the entire slice."
>
> "No second map — one polished map beats three grey-boxed ones."
>
> "No fourth ghost — it would break the mathematical closure of the matrix."
>
> "We didn't even implement **jumping**. Because if you can jump you can climb onto
> furniture, and that breaks both level closure and navmesh pathfinding."

Beat, then the line at the bottom — the only reason this slide exists:

> "**A team that can tell you what it isn't building is ten times more credible.**"

---

### 17 · Demo hand-off — 25 sec

**Leave this slide up while you switch over to Unity.**

> "I'm going to play a run now — roughly eight to ten minutes. You'll see these six
> stages."
>
> "Enter. Find the ghost room — one of seven. Gather evidence. Then sanity crosses fifty percent,
> the lights start flickering and the heartbeat comes up. Then the hunt — the house
> goes red and I have to break line of sight to survive it. Then back to the entrance,
> tick two clues in the journal, and name the ghost."
>
> "It's graded on these four. I'll try for an A or better — but no promises.
> **This game is losable.**"

During the demo, **narrate what is happening, never the mechanics**: "temperature just
dropped," "reading's at four," "it's hunting." The mechanics are already explained, and
explaining them again kills the atmosphere.

---

### 18 · Close — 30 sec

Switch back after the demo.

> "Zero to a playable run, in eight days."
>
> "We didn't build a bigger game. **We built one that closes.**"

Point at the three cards:

> "Three things come out of it. A deduction core that closes mathematically.
> A production pipeline that transfers to the next project. And an architecture
> contract fixed in advance, so any two modules can be built in parallel.
> The last two we can carry straight into whatever we do next."
>
> "Next step is a thermometer as a fourth clue — that expands the matrix to four by
> four and supports six ghosts. The structure is already there. It's an asset file."

Stop. Then:

> "Thank you."

**Say nothing after that.** Let the last line hang and wait for questions.

---

## Cutting for time

| Time available | Slides |
|---|---|
| **3 minutes** | 4 → 5 → 6 → (one line on 12) |
| **6 minutes** | 2 → 4 → 5 → 6 → 7 → 12 → 14 → 18 |
| **12 minutes** | All 18 (this script) |
| **20 minutes** | All 18 + the 8-minute live demo + questions |

If time gets cut on the spot, drop **10 (level)**, **9 (ghosts)** and **16 (non-goals)**
first — lowest information density. **Never drop 4, 5 or 12.**

---

## Likely questions, and how to answer them

**Q: How is this different from Phasmophobia? Isn't it just a copy?**

> "Mechanically we did borrow its core loop, and we're not pretending otherwise.
> The difference is that we turned its deduction system into a structure that
> **closes mathematically**. In Phasmophobia the evidence sets across twenty-four
> ghosts overlap a lot, so players regularly end up guessing from behaviour.
> Ours is zero-ambiguity: two clues always identify the ghost. That's a
> **convergence** of the design, not a simplification of it."

**Q: Aren't three ghosts too few? Won't people get bored in two runs?**

> "For a vertical slice it's enough — ten minutes a run means you need at least three
> runs to see all of them. And the structure scales cleanly: four kinds of evidence
> supports six ghosts, five supports ten. Because ghosts are ScriptableObject data
> rather than classes, adding one is a single asset file and zero code. The extension
> path is clear. We just chose not to spend the week on it."

**Q: The AI wrote the code. Can you write it yourself? Is this really your work?**

> "The AI generated the code, but **a human wrote the architecture.** The event bus,
> the four interfaces, the seven contract files, the default value of every tunable —
> those were fixed as contracts first, and the AI could only fill in behind them.
> It didn't even have permission to open a contract file; the tooling rejects the run
> if it tries."
>
> "Every delivery went through eight static gates and an eighteen-point audit,
> and then I read the diff line by line. All fifty-nine task specifications are mine,
> and each one states what to build, **what explicitly not to build**, and how it
> would be accepted."
>
> "An AI can write every script in this project in a day. But only I can tell whether
> it feels right — how much faster than the player the Poltergeist has to be to feel
> threatening without feeling hopeless took three rounds of tuning.
> My job was design and review, not typing."

**Q: Why no multiplayer? That's where Phasmophobia's fun lives.**

> "Agreed, co-op is genuinely where most of its appeal comes from. But network sync
> costs three days minimum, and in a one-week project those three days would have
> crowded out everything else. We chose a single-player slice that runs the complete
> loop over a multiplayer build with nothing in it. Co-op is an explicit next step —
> just not this week."

**Q: Did you make the art yourselves?**

> "Not the assets. Everything is free-licensed — Poly Haven is CC0, Mixamo is free,
> and the Asset Store has free realistic interior kits. The source and licence of
> every single one is recorded in `Docs/ASSET_LICENSES.md`."
>
> "But **the lighting, the material consistency and the post-processing are ours**,
> and consistency of style matters far more than the fidelity of any one asset.
> The cheapest thing about a horror game is that **you don't have to build what nobody
> can see** — our ghost is invisible most of the time by design."

**Q: A one-week project — how buggy is it? Is it stable?**

> "Compilation was verified with headless Unity on every single round, so I can
> guarantee that. But **a clean compile only means the assembly built, not that the
> behaviour is right** — behaviour we verified by hand in the editor against a
> written, step-by-step acceptance walkthrough, which is in `Docs/09_验收路径.md` (the acceptance walkthrough)."
>
> "Known outstanding issues are documented too — for example an ordering conflict
> between the hiding system and the camera's rotation write. I'm not going to claim
> it's bug-free. I will claim every known bug is written down."

**Q: Could that pipeline be reused on another project?**

> "Yes, and that's its biggest value. The tool is a dependency-free Python script.
> Moving it to a new project means changing three things: the gate regular expressions,
> the list of contract files, and the compile command. The scheduling, the send-back
> loop, the audit and the review-package generation are all generic."

---

## Pre-flight checklist

- [ ] Deck copied to the presenting machine and **opened once in PowerPoint** — check
      the dark background renders and nothing overflows
- [ ] Unity project **already open and played once**, so shaders are compiled and it
      won't stall live
- [ ] Confirm the Apartment Kit assets are imported in `Blockout.unity` — without them
      the scene is nearly empty. Run `python tools/check_kit.py` if unsure
- [ ] Audio tested through the room speakers — **a horror demo with no sound is not
      a demo**
- [ ] This script open on your phone as backup
- [ ] Have a plan if the demo crashes: cut back to slide 17 and narrate the six stages

---

## One last reminder

**Slow down.** Take the full twelve minutes for twelve minutes of material. The pauses
are where the audience catches up — especially the one after
"No redundancy. No ambiguity. No luck."

---

*Companion documents: `Docs/decks/RESIDUUM_Final_EN.pptx` · `Docs/01_GDD_残响.md` ·
`Docs/08_Pitch_Script_EN.md` (day-zero pitch version)*
