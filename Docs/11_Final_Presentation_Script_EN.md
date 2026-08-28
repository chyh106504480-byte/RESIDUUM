# RESIDUUM — Final Presentation Script (English)

**Deck**: `Docs/decks/RESIDUUM_Final_EN.pptx` — 18 slides, 16:9
**Plan**: about 12 minutes of talking + 8 minutes of live demo + 5 minutes of questions

Every line below is written to be **said out loud by someone whose English is OK but
not perfect**. Short sentences. Common words. If a line feels hard in your mouth,
cut it — the short version is always allowed.

---

## Say these words out loud a few times first

| Word | Say it like | Not |
|---|---|---|
| **Residuum** | ri-ZID-yoo-um | "resi-doom" |
| **Wraith** | RAYTH — one sound, rhymes with *faith* | "wray-ith" |
| **Poltergeist** | POLE-ter-guyst | "polter-gee-st" |
| **Phasmophobia** | faz-mo-FOH-bee-uh | — |
| **Sanity** | SAN-i-tee | — |
| **Evidence** | EV-i-dence | — |

Only four of these come up more than once: **Wraith**, **Poltergeist**, **sanity**,
**evidence**. Practise those four and you are fine.

---

## Words to keep out of your mouth

You do not need any of these, and none of them make the talk better. The plain
version on the right is what the deck already says.

| Don't say | Say |
|---|---|
| deduction / deduce | work it out · figure it out |
| ambiguous / redundant | not clear · repeated |
| architecture | how the code fits together |
| implement | write · build |
| vertical slice | a playable demo |
| asymmetric information | you don't know what it is |
| credible | easy to trust |
| mitigate the risk | keep the risk small |
| iterate | try it again |
| leverage | use |
| in terms of | for · about |

---

## Three things to remember

**One. The 3 × 3 table is your best weapon.** Slides 4 and 5 are the good part.
Everything else is there to set them up. If you only get three minutes, do 4, 5 and 6.

**Two. A final show is not a pitch.** A pitch asks *is this a good idea*. A final show
asks *did you do it*. That is why slides 14 and 15 matter — the numbers and the plan
are your proof. Say them like you mean them.

**Three. It is fine to say we are new at this.** We are. Saying it makes the rest more
believable, not less. It also explains why we made so many rules before we started.

---

## Slide by slide

### 1 · Cover — 15 sec

> "Our game is called **Residuum**. It is a first-person horror game. Unity 6,
> single player, about ten minutes per game."
>
> "Here is the idea in one line: **you don't know what it is. That is the whole game.**"

Do not explain the squares on the right yet. Slide 4 explains them. Move on.

---

### 2 · The game in one sentence — 40 sec

Read the sentence in the box. **Slowly.** Stop where the commas are.

> "You go into a house you should not be in, carrying three tools." *[stop]*
> "Before your sanity runs out, you have to work out what is haunting it —
> and get back out alive."

Stop. Then point at the four numbers and say them **one at a time**:

> "Eight to twelve minutes per game. Three ghosts. Three kinds of evidence.
> **Two clues give you the answer.**"
>
> "We looked at Phasmophobia. We copied the way it makes you think — not the amount
> of stuff in it. I'll show you why."

---

### 3 · Keeping it small — 50 sec

> "Phasmophobia has twenty-four ghosts, seven kinds of evidence, and more than forty
> tools. But a lot of its ghosts share the same clues. So players collect everything
> and then still guess."
>
> "We have three ghosts, three kinds of evidence, four tools. And we did not make it
> small because we ran out of time. **We made it small on purpose.**"

Point at the three boxes along the bottom:

> "We think that game is fun for three reasons."
>
> "**You don't know what it is.** You know something is in the house. You just don't
> know what, or where."
>
> "**You have to work it out.** You use clues to rule ghosts out."
>
> "**Staying is dangerous.** One more second is one more clue — and one more chance
> to die."
>
> "All three of those still work in a small game. So we kept all three, and cut
> everything else."

---

### 4 · The table — 60 sec ★ THE IMPORTANT ONE

**Slow down here. If there is a whiteboard, go and draw this instead of showing it —
three rows, three columns, one tick at a time.**

> "There are three ghosts: the **Spirit**, the **Wraith**, and the **Poltergeist**.
> And three kinds of evidence: an **EMF reading**, **UV fingerprints**,
> and **ghost writing**."
>
> "Here is the design." *[stop]*
>
> "**Every ghost has exactly two. Not one. Not three.**"
>
> "The Spirit has EMF and fingerprints. The Wraith has EMF and writing.
> The Poltergeist has fingerprints and writing."

Point at each row while you say it, so people's eyes follow your hand.
Stop for one second, then move on.

---

### 5 · Why it works — 60 sec ★ THE BEST MOMENT

> "Why does this always work? Because if you pick two things out of three, there are
> **only three ways to do it.** Three ways, three ghosts. One each. Nothing left over,
> nothing missing."

Point at the three numbered points:

> "That gives us three things."
>
> "One. **One clue is never enough.** Two ghosts share every clue. So if you find an
> EMF reading, you have only ruled out the Poltergeist. Two ghosts left."
>
> "Two. **Two clues always give the answer.** There is no second answer."
>
> "Three. **Ruling things out works too.** Showing that a clue is *not* there also
> helps you."

Stop. Now the big line at the bottom, one word at a time:

> "**No guessing. No luck. Just logic.**"
>
> "If a player wins, it is because they worked it out."

**This is the best moment in the talk.** Stop for two full seconds. Do not fill the
silence. Then move on.

---

### 6 · Sanity — 50 sec

> "So the obvious question: if the answer can be worked out, why not just take your time?"
>
> "Because your sanity is going down."

Point at the table on the left:

> "Standing in the dark costs you a small amount every second. A room with the light on
> is half that. Holding a flashlight halves it again. Seeing the ghost do something takes
> fifteen percent at once. Being hunted takes half a percent every second.
> The only place you get sanity back is the lobby."

Point at the formula:

> "Once your sanity is under fifty percent, the game checks **every twenty-five seconds**
> whether a hunt starts. The chance is simple: fifty minus your sanity, divided by fifty."
>
> "At fifty percent that is zero. At twenty-five it is a coin flip.
> **At zero, the hunt always happens.**"

Stop.

> "So every extra clue costs you something. The player is always asking the same
> question: how much longer can I stay? **That worry is the game.**"

---

### 7 · Running — 50 sec

> "When a hunt starts, most people try to run. We made sure running does not work."

Point at the chart, right to left:

> "You sprint at three point five metres per second. The Spirit hunts at three point
> three, the Wraith at three point four — you can just about outrun those.
> **The Poltergeist moves at three point six. It is faster than you.**"
>
> "And there is no speed boost during a hunt. Sprinting is three point five, the same
> as normal. Your sprint only lasts **four point two seconds**, then it takes three and
> a half seconds to come back, and you are walking the whole time."

Stop.

> "So the only thing that works is **getting out of its sight.** If it cannot see you,
> it only goes to where you were. Turn a corner, close the door behind you, and you
> are gone."
>
> "We set all six of these speeds by hand on day seven, so that running would be a
> **choice with a cost**, not a safe answer."

---

### 8 · Three tools — 45 sec

> "About the tools. These three are not just three ways to get the same thing.
> They are **three different kinds of fear**."

One at a time:

> "The EMF reader makes you **follow**. A higher number means it was just there —
> **it pulls you toward it.**"
>
> "The UV light makes you **search**. And while you hold it, your flashlight turns off.
> UV only shows up in the dark, so that is real physics, not a rule we made up.
> **It takes your light away.**"
>
> "The ghost writing book makes you **wait**. You put it down, you leave, and then you
> have to go back in to read it. **It sends you back in.**"

Point at the bar at the bottom:

> "And you only get three tool slots. The flashlight always takes one. So you always
> have to choose. We did that on purpose."

---

### 9 · Three ghosts — 35 sec

> "On the table the three ghosts look the same. In the game they do not feel the same
> at all."
>
> "The Spirit is slow and stubborn. It stays in its room, and its footsteps are loud
> and clear. The Wraith is quiet — it **leaves no footprints**, which players can
> actually use as a hint. The Poltergeist throws things around, and just being near it
> drains your sanity one and a half times faster."

Point at the bar at the bottom. This is the code part:

> "But in the code this is **not three sets of scripts**. It is **one GhostAI script
> plus three data files.**"
>
> "So to add a fourth ghost, we make one more data file. We do not change any code."

---

### 10 · The map — 35 sec

> "The map is one floor of an apartment. Eight areas: a main room, a kitchen, a corridor,
> a washroom, three bedrooms, and a lobby. **Seven of them can be the ghost room, and
> the game picks one at random each time.** The player has to find it using cold air
> and the EMF reader. The lobby is the safe area."

Point at the right side:

> "For light, the only light in the rooms is **cold blue moonlight** through the windows.
> Inside it is almost black. The only warm light in the game is the flashlight in your
> hand — twelve metres, a forty-five degree cone."
>
> "When a hunt starts the whole house turns red and flickers, your flashlight stops
> working, and sometimes the power goes out completely."

If you are short on time, say only the first sentence and the last one.

---

### 11 · How the code fits together — 45 sec

> "A bit about the code."
>
> "There is one rule at the centre of it: **no part of the code talks to another part
> directly.** Player, Ghost, Evidence, Items, World, UI — none of them know the others
> exist. Everything goes through this message board in the middle. Twenty-three messages
> in total."

Point at the five rules:

> "On top of that: four interfaces agreed before we started, seven files only one person
> is allowed to edit, all the numbers set in the Unity Inspector instead of in code,
> and ghosts stored as data instead of code."

Stop, then say why it was worth doing:

> "Why bother? Because it means **two parts of the game can be written at the same time
> by people who never talk to each other, and it still builds.** That is how we got
> sixteen thousand lines done in a week."

---

### 12 · How we wrote the code — 60 sec ★ WHAT MAKES US DIFFERENT

> "So how did the code actually get written? We did not just let an AI loose on it.
> We built it a process. Six steps."

Left to right, one line each. **Do not slow down here** — it is a list, not an argument.

> "One: the task spec. **A person writes it** — which files to touch, what each number
> should be, and what it is not allowed to do."
>
> "Two: the AI writes the code, inside those rules. It cannot change the rules at all."
>
> "Three: an automatic check. Eight text rules. One hit and it goes straight back."
>
> "Four: Unity builds it with no window open, so we get real errors."
>
> "Five: the AI answers eighteen questions about its own work."
>
> "Six: **a person reads the changes** and says yes or no."

Point at the line in the middle:

> "If step three or four fails, it goes back to step two with the error text.
> Two tries, then it stops."

Stop, then the bar at the bottom:

> "**People decide what to build and whether it is good. The AI writes it.
> The person who writes it and the person who checks it cannot be the same person.**"
>
> "We are new to 3D games. That is exactly why we made the rules so strict before we
> started. An AI can write every script in this project in one day. But only a person
> can tell whether the game **feels** right."

---

### 13 · The eight rules — 40 sec

> "These eight on the left are the automatic checks. The first five are errors — one hit
> and the work goes back. The last three are warnings."
>
> "Why do this? Because **an AI never gets tired, but it makes the same mistake again
> and again.** Unity 6 removed a whole group of old functions. You can say that ten
> times in the instructions and it will still use them. But a text search catches it
> every single time, for free."

Point at the third box on the right. This one deserves its own moment:

> "One thing we learned the hard way. **A check must not blame good code.**"
>
> "Our first version of these rules found two errors and fifteen warnings in code that
> was completely fine. **Every single one was wrong.** That is not just annoying — it
> wastes a whole round, because the AI then spends two tries fixing things that were
> never broken. So now we test a new rule on clean code first."

---

### 14 · The numbers — 30 sec

**Do not read all six.** Pick two. Let people read the rest.

> "Eight days. Here is what that came to."
>
> "**Fifty-nine task specs** — each one saying which files, which numbers, and how we
> would test it, before any code was written."
>
> "**Sixteen thousand four hundred and sixty-seven lines of C#**, across forty-three
> scripts. Plus three thousand lines of documents."
>
> "All of it is in our repository. You can check any of these."

---

### 15 · The plan — 40 sec

> "We planned the whole week day by day, before we started."
>
> "Day one, walking and looking. Day two, tools in hand. Day three, the clues work.
> Day four, the ghost. Day five, the whole game runs — and note that everything is
> still grey boxes at this point. **Art does not go in until day six.**"

Point at the two red rows:

> "These two are **cut-off days**. If the clues did not work by the end of day three,
> we were going to drop ghost writing and use a two-by-three table instead. If the
> ghost AI did not work by the end of day four, the art day would move and we would
> accept a worse-looking game."

Stop.

> "**We did not have to do either one.** But both were written down on day zero,
> not made up afterwards. That is why we were willing to promise this plan."

---

### 16 · What we did not build — 35 sec

> "Last, the things we chose **not** to build."
>
> "No online multiplayer — network code costs three days at least, and it would have
> eaten the whole project."
>
> "No second map — one good map beats three empty ones."
>
> "No fourth ghost — the three-by-three table would stop working."
>
> "We did not even add **jumping**. Because if you can jump, you can climb on the
> furniture, and that breaks both the level and the paths the ghost walks on."

Stop, then the line at the bottom. This is the only reason the slide exists:

> "**A team that can tell you what it is NOT making is easier to trust.**"

---

### 17 · Before the demo — 25 sec

**Leave this slide up while you switch to Unity.**

> "I am going to play one game now. About eight to ten minutes. You will see these
> six stages."
>
> "Go in. Find the ghost room. Get the clues. Then sanity drops under fifty percent,
> the lights start flickering, and a heartbeat starts. Then the hunt — the house turns
> red and I have to get out of its sight. Then back to the lobby, tick two clues,
> and pick a ghost."
>
> "It is scored on these four. I will try for an A or better. No promises —
> **you can lose this game.**"

While you play, **only say what is happening**: "the air just got cold," "the reading is
at four," "it's hunting." Do not explain the rules again — you already did, and
explaining kills the mood.

---

### 18 · The end — 30 sec

Come back to the slides after the demo.

> "Eight days, from nothing to a playable game."
>
> "We did not make a bigger game. **We made one that finishes.**"

Point at the three boxes:

> "Three things came out of it. A clue table that always works. A way of working we can
> use again. And a set of rules we agreed before we started, so we could all work at the
> same time."
>
> "This was our first 3D game. Honestly, most of the eight days went into deciding what
> **not** to build."
>
> "Next step is a thermometer as a fourth clue. That gives us a four-by-four table and
> six ghosts. The structure is already there — it is one more data file."

Stop. Then:

> "Thank you."

**Say nothing after that.** Wait for questions.

---

## If they cut your time

| Time | Slides |
|---|---|
| **3 minutes** | 4 → 5 → 6 (and one line about 12) |
| **6 minutes** | 2 → 4 → 5 → 6 → 7 → 12 → 14 → 18 |
| **12 minutes** | All 18 — this script |
| **20 minutes** | All 18 + the demo + questions |

If someone cuts your time on the spot, drop **10 (the map)**, **9 (the ghosts)** and
**16 (what we did not build)** first. **Never drop 4, 5 or 12.**

---

## Questions people will probably ask

Keep these answers short. If you get stuck, the first sentence of each answer is enough.

**Q: How is this different from Phasmophobia? Isn't it a copy?**

> "We did borrow the main loop, and we are not hiding that. The difference is our clue
> table. Phasmophobia has twenty-four ghosts and a lot of them share clues, so players
> often finish collecting and still guess. Ours never does that — two clues always give
> one answer. We made it smaller on purpose, not because it was easier."

**Q: Are three ghosts too few? Won't people get bored after two games?**

> "For a demo it is enough — ten minutes a game means you need at least three games to
> see all of them. And it grows easily: four kinds of clue would give six ghosts.
> Because our ghosts are data files and not code, adding one is one file and no code
> at all. We just chose not to spend the week on it."

**Q: The AI wrote your code. Can you write code yourselves? Is this really your work?**

> "The AI wrote the code, but **we decided how the code fits together.** The message
> board, the four interfaces, the seven rule files, the default value of every number —
> all of that was fixed first, and the AI could only fill in behind it. It is not even
> allowed to open those files; our tool rejects the work if it tries."
>
> "Everything it handed in went through eight automatic checks and eighteen questions,
> and then one of us read the changes line by line. All fifty-nine task specs are ours.
> Each one says what to build, **what not to build**, and how we would test it."
>
> "We are new to 3D games, so honestly this process is how we stayed in control.
> An AI can write every script here in a day. But only a person can tell whether the
> game feels right — how much faster than the player the Poltergeist should be took us
> three tries to get right."

**Q: Why no multiplayer? That is the fun part of Phasmophobia.**

> "We agree, co-op is where most of its fun comes from. But network code costs three
> days at least, and in a one-week project those three days would take everything else
> away. We chose a single-player game that finishes over a multiplayer game with nothing
> in it. Multiplayer is the obvious next step, just not this week."

**Q: Did you make the art yourselves?**

> "Not the models. Everything is free to use — Poly Haven, Mixamo, and free interior
> packs from the Asset Store. We wrote down where every single one came from in
> `Docs/ASSET_LICENSES.md`."
>
> "But **the lighting and the look are ours**, and one clear style matters much more
> than one nice-looking chair. The cheapest part of a horror game is that **you don't
> have to build what nobody can see** — our ghost is invisible most of the time anyway."

**Q: A one-week project — is it buggy?**

> "Every round was built with Unity before we accepted it, so it builds. But **building
> is not the same as working** — we tested the actual behaviour by hand in the editor,
> following a written step-by-step list. That list is in our documents."
>
> "We also wrote down the bugs we know about and did not fix. I am not going to say it
> has no bugs. I will say every bug we know about is written down."

**Q: Could you use that process on another project?**

> "Yes, and that is the part we are most happy with. The tool is one Python file with no
> extra libraries. To move it to a new project you change three things: the eight text
> rules, the list of protected files, and the build command. Everything else works as is."

---

## Before you go on stage

- [ ] Deck copied to the presenting computer and **opened once in PowerPoint** —
      check the dark background looks right and no text is cut off
- [ ] Unity project **already open, and played once**, so the shaders are ready and it
      does not freeze in front of everyone
- [ ] Check the Apartment Kit models are imported in `Blockout.unity`, or the scene will
      look almost empty. Run `python tools/check_kit.py` if you are not sure
- [ ] Sound tested through the room speakers — **a horror demo with no sound is not
      a demo**
- [ ] This script open on your phone
- [ ] Have a backup plan if the demo crashes: go back to slide 17 and just say the
      six stages out loud

---

## One last thing

**Talk slowly.** Take the full twelve minutes. The pauses are when people catch up —
especially the one after "No guessing. No luck. Just logic."

If you forget a line, say the short version and move on. Nobody in the room knows what
you were going to say.

---

*See also: `Docs/decks/RESIDUUM_Final_EN.pptx` · `Docs/01_GDD_残响.md` ·
`Docs/08_Pitch_Script_EN.md` (the day-zero pitch)*
