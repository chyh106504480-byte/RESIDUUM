# RESIDUUM — Final Talk (8 minutes, no demo)

**Deck:** `Docs/decks/RESIDUUM_Final_v2.pptx` — 16 slides, 16:9
**Length:** about **10 to 11 minutes** at a calm pace. No live demo, no video.
(Measured: 1,180 spoken words plus the pauses that are marked in the script.)
**Rebuild the deck:** `npm install --no-save pptxgenjs && node tools/deck_final_v2.js`

This is a *shorter and different* talk from `Docs/11_Final_Presentation_Script_EN.md`,
which is the 12-minutes-plus-demo version. Use one or the other, not both.

Every line below is written to be said out loud by someone whose English is fine but
not perfect. Short sentences. Common words. If a line feels hard in your mouth, cut it.
The short version is always allowed.

---

## The four words to practise

| Word | Say it like |
|---|---|
| **Residuum** | ri-ZID-yoo-um |
| **Wraith** | RAYTH — one sound, rhymes with *faith* |
| **Poltergeist** | POLE-ter-guyst |
| **Sanity** | SAN-i-tee |

Those four are the only ones that come up more than once. Everything else in the deck
is a common word on purpose.

---

## Three things to hold on to

**One. Slides 4 and 5 are the talk.** Everything before them sets them up, everything
after them is proof. If someone cuts your time in half on the spot, keep 4 and 5.

**Two. This is not a pitch.** A pitch asks *is this a good idea*. A final asks *did you
do it*. That is why slides 12, 13 and 14 exist. Say those numbers like you mean them.

**Three. Saying we are beginners makes the rest more believable, not less.** It is also
the honest reason the process is so strict.

---

## Slide by slide

### 1 · Cover — 20 sec

> "Our game is called **Residuum**. First-person horror, Unity 6, single player,
> about ten minutes a round."
>
> "One line before I start. **You do not know what it is. That is the whole game.**"

Do not explain the light behind the title. Do not explain the bar along the bottom.
Move on.

---

### 2 · One sentence — 30 sec

Read the box in the middle **slowly**. Stop where the comma is.

> "You break into a house you should not be in, carrying three tools." *[stop]*
> "Before your sanity runs out, work out what is haunting it — and get back out alive."

Then point at the four numbers and say them one at a time:

> "Three ghosts. Three kinds of evidence. Four tools, three slots."
>
> "And the last one. **Two clues give you one answer.** That is the whole design."

---

### 3 · The problem — 30 sec

> "We started from *Phasmophobia*. Twenty-four ghosts, seven kinds of evidence,
> forty tools."
>
> "But a lot of its ghosts share the same clues. So you collect everything you can —
> **and then you still guess between two.**"

*[stop]*

> "We wanted a game where winning is never luck. So we made ours smaller, on purpose,
> until the guessing was gone."

---

### 4 · The table — 35 sec ★

**Slow down. If there is a whiteboard, draw this instead of showing it.**

> "Three ghosts: the **Spirit**, the **Wraith**, the **Poltergeist**. Three kinds of
> evidence: an **EMF reading**, **UV fingerprints**, **ghost writing**."
>
> "Here is the design." *[stop]*
>
> "**Every ghost has exactly two. Not one. Not three.**"
>
> "The Spirit leaves EMF and fingerprints. The Wraith leaves EMF and writing.
> The Poltergeist leaves fingerprints and writing."

Point at each row as you say it, so people's eyes follow your hand. Then stop for one
full second before you change slide.

---

### 5 · Why it always works — 55 sec ★ THE BEST MOMENT

> "Why does this always work? Because if you choose **two things out of three**, there
> are **only three ways to do it**. Three ways, three ghosts. One each."
>
> "So — one clue is never enough. Every clue is shared by two ghosts. Find EMF, and you
> have only ruled the Poltergeist out."
>
> "Two clues are always enough. There is never a second answer."
>
> "And proving a clue is **not** there counts just as much."

*[stop]* Then the big line, one word at a time:

> "**No guessing. No luck. Just logic.**"
>
> "If a player wins, it is because they worked it out."

**Stop for two full seconds. Do not fill the silence.** This is the best moment in the
talk, and the silence is what makes it land.

---

### 6 · The clock — 45 sec

> "So why not just take your time? Because the house charges you for every second."

Point at the left table:

> "Standing in the dark costs you. A lit room costs half that. Your flashlight halves it
> again. Seeing the ghost takes fifteen percent at once."

Point at the formula:

> "And under fifty percent, every twenty-five seconds, the game rolls for a hunt.
> Fifty, minus your sanity, divided by fifty. At twenty-five that is a coin flip.
> **At zero it is certain.**"

*[stop]*

> "So knowing more always costs you. **That trade is the game.**"

---

### 7 · Running — 35 sec

> "When it hunts, most people run. We made sure running does not work."

Point at the bars:

> "You sprint at three point five. The Spirit hunts at three point three, the Wraith
> three point four — you can just outrun those. **The Poltergeist moves at three point
> six. It is faster than you.**"

*[stop]*

> "So the only answer is to break its line of sight. Turn a corner, shut the door,
> and it loses you."

---

### 8 · Three tools — 40 sec

> "The three tools are not three ways to get the same thing. They are **three different
> fears.**"

One at a time:

> "The EMF reader makes you **follow** — it pulls you toward it."
>
> "The UV light makes you **search**, and it switches your flashlight off, because
> fingerprints only glow in the dark. It takes your light away."
>
> "The book makes you **wait**, and then it sends you back in."

Point at the bar at the bottom:

> "Three slots, and the flashlight always owns one. So every round you leave something
> behind."

---

### 9 · The look — 30 sec

> "The only light in the house is **cold blue moonlight** through the windows. The only
> warm light in the game is the **flashlight in your hand**."
>
> "Eight areas, any of seven can be the haunted one, picked at random. When a hunt
> starts the house goes red and your torch dies."

*[stop]*

> "**The cheapest thing in a horror game is everything nobody can see.**"

---

### 10 · Divider — 20 sec

Let this one breathe. Do not rush it.

> "That is the game. Now, how we built it."
>
> "**None of us had ever made a 3D game.** Not one of us."
>
> "So before we wrote a single line of game code, we wrote the rules the code would have
> to obey."

---

### 11 · The contract — 40 sec

> "The first rule is the important one. **Nothing in the game talks to anything else
> directly.**"
>
> "Six systems — Player, Ghost, Evidence, Items, World, UI. None of them knows the other
> five exist. Everything goes through one noticeboard. Twenty-three messages."
>
> "Plus four interfaces agreed before day one, and seven files only one person may edit."

*[stop]* Then say why it was worth it:

> "Why bother? Because **two people can build two systems at the same time, never read
> each other's code, and it still compiles.**"

---

### 12 · The production line — 80 sec ★ WHAT MAKES US DIFFERENT

> "So how did the code get written? We used an AI. But we did not let it loose — we
> built it a production line. **Six steps, every single task.**"

Left to right, one line each. **Do not slow down here** — it is a list, not an argument.

> "One. **A person** writes the task spec. Which files, which numbers, and what it may
> not do."
>
> "Two. **The AI** writes the code inside those walls."
>
> "Three. Eight text rules run over the change."
>
> "Four. Unity compiles it with no window open."
>
> "Five. The AI answers eighteen fixed questions about its own work."
>
> "Six. **A person** reads the change and says yes or no."

Point at the red line:

> "If three or four fails, it goes straight back to step two with the error pasted in.
> Two attempts, then a person looks at it."

*[stop]* Then the bar at the bottom, slowly:

> "**The one who writes the code and the one who checks it are never the same.**"
>
> "An AI can write every script in this project in a day. Only a person can tell you
> whether the game **feels** right."

---

### 13 · What the machine gets wrong — 50 sec

> "Two things we learned working this way."
>
> "A machine never gets tired — and it never learns either. Unity 6 deleted a whole
> family of functions. You can ban them ten times in the instructions and they still
> come back. **A text search catches every one, for free.**"

Point at the red card:

> "And this one hurt. **A check must never blame good code.** Our first version of these
> rules ran on clean code and reported two errors and fifteen warnings.
> **All seventeen were wrong.**"
>
> "A false alarm is worse than silence — it burns both retries making the AI *fix*
> something that was never broken."

---

### 14 · The numbers — 25 sec

**Do not read all eight.** Pick three and let people read the rest.

> "Eight days. **Seventy-two task specs**, every one written before the code.
> **Forty-six scripts. Seventeen thousand lines of C#.**"
>
> "It is all in one repository — pick any number here and check it."
>
> "And ten of those scripts are editor tools, so the level can be rebuilt from a menu
> instead of by hand."

---

### 15 · What we cut — 45 sec

> "Last, what we chose **not** to build."
>
> "No multiplayer — three days of network code, in a one-week project, *is* the project.
> No second map. No fourth ghost, because that breaks the table. And **no jumping** —
> you would climb the furniture and break both the level and the ghost's paths."
>
> "We also wrote two cut-off dates on day zero. **Neither was needed** — but both were
> written down before we started."

*[stop]*

> "**A team that can tell you what it is not building is easier to believe.**"

---

### 16 · The end — 40 sec

> "Eight days. From an empty project to a game that finishes."
>
> "Three things came out of it. A table that cannot lie. A way of working we can use
> again. And rules we agreed before the first line of code."
>
> "If there is a next step, it is a fourth clue. Two out of four is six ghosts — and a
> ghost here is a data file, not a class. That is one afternoon, not one week."

*[stop]*

> "Thank you."

**Say nothing after that.** Wait for questions.

---

## If they cut your time

| You get | Slides | Roughly |
|---|---|---|
| **3 minutes** | 4 → 5 → 12 | the design, and how it was built |
| **5 minutes** | 2 → 4 → 5 → 6 → 12 → 16 | + the pitch and the clock |
| **7 minutes** | 2 → 3 → 4 → 5 → 6 → 12 → 13 → 14 → 16 | + the proof |
| **10–11 minutes** | all 16 | this script, unabridged |

Drop **9 (the look)**, **7 (running)** and **15 (what we cut)** first — in that order.
**Never drop 4, 5 or 12.**

---

## Questions you will probably get

Keep the answers short. The first sentence of each is enough on its own.

**"Isn't this just a copy of Phasmophobia?"**

> "We borrowed the loop and we are not hiding that. The difference is the table.
> In Phasmophobia a lot of ghosts share clue sets, so you can finish collecting and
> still be guessing. Ours never does that. Two clues, one answer, every time.
> We made it smaller on purpose, not because it was easier."

**"Three ghosts is not very many."**

> "For a ten-minute round it is enough — you need at least three rounds to meet them
> all. And it grows cheaply: four kinds of clue would give six ghosts. Our ghosts are
> data files, not code, so a fourth one is one file and no programming. We just chose
> not to spend the week on it."

**"The AI wrote your code. Is this really your work?"**

> "The AI wrote the code. **We decided what the code is.** The noticeboard, the four
> interfaces, the seven protected files, the default value of every number — all of that
> was fixed before it started, and it is not even allowed to open those files. Our tool
> rejects the work if it tries."
>
> "Everything it handed in went through eight automatic checks and eighteen questions,
> and then one of us read the change line by line. All seventy-two task specs are ours.
> Each one says what to build, what **not** to build, and how we would test it."

**"Why no multiplayer? That is the fun part."**

> "We agree — co-op is where most of that game's fun comes from. But network code costs
> three days minimum, and in a one-week project those three days would take everything
> else with them. We chose a single-player game that finishes over a multiplayer game
> with nothing in it."

**"A one-week project — is it buggy?"**

> "Every round was compiled by Unity before we accepted it, so it builds. But building
> is not the same as working, so we also tested the behaviour by hand in the editor
> against a written list. That list is in our documents, and so are the bugs we know
> about and did not fix. I will not tell you it has no bugs. I will tell you every bug
> we know about is written down."

**"Did you make the art?"**

> "Not the models — everything is free to use, and every source is listed in
> `Docs/ASSET_LICENSES.md`. But the lighting and the look are ours, and one clear style
> matters more than one nice-looking chair."

---

## Before you go on

- [ ] Deck copied to the presenting machine and **opened once in PowerPoint**. Check the
      dark background looks right and no text is cut off.
- [ ] Georgia, Calibri and Courier New are all standard on Windows and Mac. If a
      slide looks wrong, it is the projector, not the file.
- [ ] The room is bright: this deck is very dark. Ask for the lights down if you can.
- [ ] This script open on your phone.
- [ ] Practise slides 4, 5 and 12 out loud. Those three carry the talk.

---

## One last thing

**Talk slowly and take the full eleven minutes.** The pauses are where the room catches
up — especially the one after *"No guessing. No luck. Just logic."*

If you forget a line, say the short version and move on. Nobody in the room knows what
you were going to say.

---

*See also: `Docs/01_GDD_残响.md` (every number on slides 4–8) ·
`Docs/02_技术架构.md` (slide 11) · `tools/codexctl/` (slides 12–13)*
