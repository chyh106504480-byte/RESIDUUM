================================================================
  RESIDUUM  —  a first-person horror investigation game
  Unity 6 (URP) · Windows build · 7-day vertical slice
================================================================

HOW TO RUN
----------
1. Unzip the whole folder. Keep every file together.
   The .exe will not start if you move it out of the folder.
2. Double-click  RESIDUUM.exe
3. Windows may show a blue "Windows protected your PC" box,
   because the build is not code-signed. Click "More info",
   then "Run anyway". This is normal for student builds.
4. To quit: use the Quit button on the title screen, or press
   Alt+F4 at any time. There is no in-game pause menu in this
   slice, so Alt+F4 is the way out once a round has started.

REQUIREMENTS
------------
Windows 10 or 11, 64-bit.
A dedicated GPU is recommended but not required.
About 2 GB of free disk space.


CONTROLS
--------
  Move ................ W A S D
  Look ................ Mouse
  Sprint .............. Left Shift   (stamina: ~4 seconds)
  Crouch .............. C
  Interact ............ E            (doors, light switches, items)
  Flashlight .......... F
  Switch tool ......... 1 / 2 / 3
  Journal ............. Tab           (evidence notes + ghost table)
  Quit ................ Alt+F4        (no pause menu in this slice)


WHAT YOU ARE SUPPOSED TO DO
---------------------------
You enter a house with three tools. One ghost is inside.
Your job is to find out WHICH ghost it is, then leave alive.

There are 3 ghost types and 3 kinds of evidence.
Each ghost leaves EXACTLY TWO of the three:

                      EMF-5    UV Fingerprints   Ghost Writing
  Spirit               yes           yes              no
  Wraith               yes           no               yes
  Poltergeist          no            yes              yes

So any two pieces of evidence identify the ghost with certainty.
There is no guessing and no luck involved.

Use the three tools to collect evidence:
  - EMF Reader ....... beeps and lights up near ghost activity
  - UV Light ......... reveals fingerprints on doors and objects
  - Ghost Writing Book . leave it in a room; the ghost may write in it

Your SANITY drops while you are inside, faster in the dark.
When sanity gets low, the ghost can start a HUNT.
During a hunt you cannot outrun it forever - two of the three
ghosts are slightly slower than your sprint, one is faster.
The reliable way to survive is to BREAK LINE OF SIGHT:
turn a corner, close a door behind you, and stay still.

When you know the answer, go back to the exit door, record your
identification, and leave. You are scored on whether you named
the right ghost and whether you got out.


A SHORT SESSION LOOKS LIKE THIS
-------------------------------
Roughly 8-12 minutes. Walk in, sweep rooms with the EMF reader,
UV the doors that look used, drop the writing book somewhere the
ghost is active, collect two pieces of evidence, get out.


NOTES ON SCOPE
--------------
This is a vertical slice built in 7 days, not a finished game.
It contains one house, one round, three ghosts, three tools.
The design deliberately reduces Phasmophobia's 24 ghosts and
7 evidence types down to a 3x3 table that is mathematically
closed - that reduction is the central design decision of the
project, not a shortcut.
