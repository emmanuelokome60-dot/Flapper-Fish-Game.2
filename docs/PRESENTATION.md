<!--
HOW TO TURN THIS INTO A CANVA DECK
  1. Canva -> Docs -> blank Doc.
  2. Copy everything BELOW the ===== line and paste it in.
  3. Top-left menu -> "Convert to presentation" (Docs to Decks).
  4. Pick a template. A dark or underwater one suits the art.
  5. The "> " lines are the spoken script, not slide content. Delete any Canva keeps.

Canva starts a new slide at every "#", so don't add or remove those headings.
Every number in here is measured from the project, not estimated.
-->

=====

# Abyssal Fish

- A one-tap swimmer set in a cave that closes in on you
- ABYSSAL TEAM
- EUAS — Mobile Game Development
- Unity 6000.3.8f1, 2D URP, built for phones

> We had ten weeks. This is what shipped, what broke, and what we learned fixing it.

# The game

- You are an anglerfish with one lamp on your head
- Dodge the rocks and the trash
- Collect only the coins
- Currents shove you when you can least afford it
- The deeper you go, the less you can see

> One rule the player never gets told: everything except coins kills you. That reads instantly,
> so we never needed a tutorial.

# The design problem

- One tap. That is the whole control scheme.
- Which puts us one step from being Flappy Bird
- "Not Flappy Bird" had to be a measurable target
- Otherwise it is just a reskin with a fish

> This is the slide the whole project turns on. We said "make it fluid and underwater" for weeks
> without defining it, and you cannot hit a target you have not written down.

# Making the fish feel underwater

- Flappy Bird: rise in hops, fall like a stone
- Our fish sank at **4.4** units/sec — nearly the same
- One missed tap cost **1.52** units of a 2.6 gap
- Fix: slow the fall to **3.2**, thicken the water
- Result: a missed tap now costs **1.00**. Still dives.

> We tuned the rise three times. The hop height never actually changed across any of them.
> The fall speed was the cause, and nobody had touched it.

# Why that took three attempts

- Complaint was "too floaty" AND "I hit spikes too easily"
- Those sound contradictory, so we guessed
- Simulating the jump arc showed both were true at once
- Floaty rise, stone-like fall — two different numbers

> Lesson we actually paid for: two complaints that contradict each other usually means two
> separate causes, not one confused player.

# Difficulty that builds

- Opening gap **3.6** units, narrowing to **2.6**
- Speed climbs from ×0.9 to ×1.6 over 150 seconds
- The gap narrows on the same number that dims the light
- So the player reads one signal, not two
- Gap stops shrinking while speed is still climbing

> Tying the walls to the darkness was the best decision in the project. It staggers the
> difficulty for free and it means there is only one dial to balance.

# Currents: the part that is not a bird game

- A surge every **11.5** seconds: 9 calm, then 2.5 of push
- It speeds the whole cave AND shoves the fish back
- Measured it: only 0.23 units of shove
- The fish is 1.09 wide — a fifth of a body
- Raised to a full body width. Now you feel it.

> The mechanic we were proudest of was almost switched off and we never noticed, because we
> never measured it. It looked fine in the Inspector.

# Darkness as a difficulty curve

- The cave light drops from 0.05 to 0.02
- The lamp on the fish is the only light you get
- It brightens as the cave darkens, and flickers
- Rocks are pure black — measured 0.000 luminance
- Background sits at 0.209, so rocks read as silhouette

> We measured the actual art before picking light values instead of eyeballing it. The rocks
> being literally black is why the silhouette works at all.

# How it is held together

- **18** C# scripts, **3,163** lines
- One shared cave speed, owned by one script
- Rocks, trash and coins all read that same number
- So nothing can drift out of step with anything else
- Spawners ask the rock wall where the gap is

> Every bug where two things disagreed came from two copies of one number. Fixing that pattern
> once removed a whole class of problem.

# Built for a phone, not a desktop

- 16:9 landscape, laid out as fractions of the screen
- Screen edges computed once, not per object per frame
- Music streams instead of loading into memory
- Score counter was eating taps — a huge dead zone
- Adaptive app icon, 57 sprites, 5 audio clips

> The tap bug is our favourite. Anything drawn in the UI absorbs touches by default, so a
> 1018x141 score counter across the top silently ate every tap that landed on it.

# Protecting an amateur release

- Terms and Privacy shown in-game, before you play
- **1,399** words, accept or exit
- States plainly: unfinished, unoptimised, AI-assisted
- No warranty, no liability for device, data or money
- Collects nothing — no internet, no accounts, no ads

> We are students, not a studio, and this is free. Saying so clearly up front is both honest and
> the only protection we have.

# Where it stands

- Playable end to end: menu, run, shop, game over
- Fish skins bought with collected coins
- Tuned on a real phone, not just in the Editor
- Next: a sixth fish, sprite atlas, wider device testing
- 35 commits, 20 September to 3 November

> Thanks for listening. Happy to take questions, or hand you a phone.
