# Cursor variants — handoff

Branch `cursor-variants`, off `dev.master`. Nine commits, one per to-do.

## Getting started

1. Open `Assets/Scenes/CursorLab.unity` and press Play. It is self-contained — it carries its
   own EventService, so you do not need to run from BootScene.
2. You start on a ready ally, with three spent allies to the left and two enemies north.
3. Press **1–4** to switch variant. **F1** hides the debug panel for screenshots.

The panel shows the live state, hover kind, legacy mode, tile, and the derived visual state,
plus force-state buttons, a variant picker, and a reset.

## Where the knobs are

`Assets/ScriptableObjects/UI/CursorVariants/` — one asset per variant. Everything is here;
no cursor look or timing is hardcoded anywhere else.

| Section | What to reach for |
|---|---|
| Modules | Which of brackets / arrows / morph / directional hints the variant assembles |
| Art | Sprite per element. Empty falls back to the procedural placeholder |
| Visual states | Per state: tint, **inset**, piece scale, **breath amplitude**, breath period, arrows-inward |
| Motion | Step slide, state blend, morph duration, morph rotation direction, error shake |
| Feel | Held-repeat delay and step, range flood duration |
| Audio | A SoundId per event |

**Inset and breath amplitude are the two that matter most.** Inset is the distance from tile
centre to each piece in world units, where one tile is 1.0 — it is what makes Selectable read
as *tightening* rather than merely recolouring. Breath amplitude at 0 stops the motion dead,
which is what makes Acted read as inert. Both work without colour, which is the colourblind
safety net.

All of it is live in play mode. Held repeat is re-read every frame, so you can drag the
slider while holding a direction and feel it change.

**A fifth variant is a duplicate.** Copy an asset, rename it, change numbers, and add it to
the `profiles` list on `GridCursor/CursorVisuals`. No code.

## To put a sound on anything

Drop a clip into the matching asset in `Assets/ScriptableObjects/Audio/` — `CursorStepped`,
`CursorHoverSelectable`, `CursorError`, and six more. The profiles already point at them, so
that is the only step. Everything ships clipless, so the default is silence.

## What I checked

- 605 EditMode tests pass, including the 14 pre-existing `GridCursorTests`.
- Clean compile, no console errors, at every step.
- CursorLab runs with no exceptions; the roster is right (verified 7 units — 5 allies, 2
  enemies) and the 3-of-5 read is unambiguous on screen: three dark, frozen allies against two
  bright ones, with the cursor's gold brackets tightened around a ready one.
- Corner brackets render as corner frames at the right scale.

## What I did not check, and you should

- **Held-repeat scrubbing across the unit line**, and whether the Morphing Compass stays clean
  doing it. The design makes queueing structurally impossible — a morph always restarts from
  the current interpolated pose, and the slide duration is clamped to the repeat interval — but
  I could not drive input from here, so it is unproven in the hand.
- **Flipping variants mid-selection.** Same reason. The path is a profile swap plus a re-blend
  and touches no FSM state, but you should confirm it feels right rather than takes your eye.
- **Gamepad.** Bindings already exist for every action; nothing in this work changed them.
- **GC allocations.** The update loop is written for zero — preallocated pose and direction
  arrays, cached renderers, no LINQ, no per-frame `new` — but I have not put a profiler on it.

## Things I'd flag

**The morph duration is the one number I'd expect you to change first.** 105 ms is the middle
of the range you gave. Below about 60 ms the sweep stops reading as motion and starts reading
as a pop, which throws away the reason to use that variant at all.

**Bracket Compass may still be too busy.** I cut the bracket arms and shrank the arrows to
about 0.8 scale, but eight elements on one 32-pixel tile is a lot, and it is the variant most
likely to fight the unit sprite underneath. If it reads as cluttered, pull `inset` out slightly
before you shrink the pieces further — pushing them apart helps more than making them smaller.

**Compass Petals loses its cursor entirely on a spent ally.** The arrows retract to nothing,
which is a strong, clear statement, but it means there is a moment where the only thing telling
you where the cursor is, is the unit's own grey. Worth a look in motion — it may need a
residual stub rather than a full retract.

**The placeholder art is deliberately plain.** Flat white shapes with a dark keyline, tinted
per state. They are there so the behaviour can be judged without the art arguing with it. Drop
your own sprites into the profile's Art slots when you want to see it properly.

## Two fixes that came out of this

**A live bug.** Returning to the battle map from combat, dialogue or the unit-info screen
force-set the cursor to Free, silently dropping a unit the player had already picked up. The
cursor now suspends and restores, and a surviving selection comes back.

**A smaller one.** Confirming on an unreachable tile with a unit selected still played the move
sound and raised the MoveConfirmed dialogue beat, even though the move was refused. Those now
fire only on a real commit, and the refusal raises `ErrorFeedback` instead.

## One thing I left alone

Gamepad d-pad left/right is bound to both `CursorLeft`/`CursorRight` **and**
`PrevUnit`/`NextUnit`, so a single d-pad press fires a cursor step and a unit jump together.
It predates this work and is out of scope, but it will look like dropped or doubled input while
you are evaluating on a pad. Say the word and I'll fix it.
