# Cursor variants — handoff

Branch `cursor-variants`, off `dev.master`.

Everything runs **in the real game, on the real map, through the normal boot flow**. There is
no sandbox scene.

## The tuning loop

1. **Project Astra → Dev → Boot Straight To Battle Map.** Skips the splash, the title and the
   opening cutscene, which otherwise cost you a minute per iteration.
2. Press Play from BootScene. You land on the campaign's battle map with the real art and the
   real units.
3. **1–4** switches variant live, in any state, including mid-selection. **F1** hides the panel.
4. Select the profile asset in `Assets/ScriptableObjects/UI/CursorVariants/` and tune it in the
   Inspector **while the game is running**. Everything redraws immediately, including the
   shapes themselves.
5. **Project Astra → Dev → Restore Normal Boot** before you commit BootScene. The menu item
   warns you, and only one of the two entries is ever clickable, so it's hard to lose track.

## Where the knobs are

One asset per variant. Nothing about the cursor's look or feel is hardcoded anywhere else.

| Section | What to reach for |
|---|---|
| Modules | Which of brackets / arrows / morph / directional hints this variant assembles |
| Visual states ×5 | Each carries its own **shape**, tint, inset, scale, breath, and optional art |
| Motion | Step slide, state blend, morph duration, morph rotation, error shake |
| Feel | Held-repeat delay and step, range flood duration |
| Audio | A SoundId per event |

**The shape settings are the ones that matter for design work.** Each of the five states has its
own silhouette — arm length, thickness, outline weight, corner radius, arrow width, arrow
sharpness — and they redraw live as you drag. That is what makes "brackets clamp shut when
selected" and "arrows retract when spent" actual geometry rather than just a colour change.

Each state also has optional `bracketSprite` / `arrowSprite` slots. Fill one and it replaces the
generated shape for that state only. Leave them empty and the parametric shape is used.

**Inset and breath amplitude do the heavy lifting for readability.** Inset is distance from tile
centre in world units, where one tile is 1.0 — it makes Selectable read as *tightening*. Breath
amplitude at 0 stops motion dead, which is what makes Acted read as inert. Both work without
colour.

**A fifth variant is a duplicate.** Copy an asset, rename it, then run
**Project Astra → UI → Set Up Cursor Variants In Open Scene** to pick it up. No code.

## To put a sound on anything

Drop a clip into the matching asset in `Assets/ScriptableObjects/Audio/` — `CursorStepped`,
`CursorHoverSelectable`, `CursorError`, and six more. The profiles already point at them, so
that is the only step. Everything ships clipless, so the default is silence.

## Verified in the real game

Booted through BootScene onto the campaign map: no exceptions, cursor renders over the real
art, and hovering a ready ally correctly shows the gold tightened Selectable state. 605 EditMode
tests pass, including the 14 pre-existing `GridCursorTests`.

## Still needs your hands on the controls

I can't drive input from here, so these are unproven in the hand: held-repeat scrubbing and
whether Morphing Compass stays clean through it; flipping variants mid-selection; gamepad; and
GC allocations under the profiler. The design makes queueing structurally impossible — a morph
always restarts from the current interpolated pose, and the slide clamps to the repeat interval
— but structurally impossible is not the same as feels right.

## Things I'd flag

**The cursor is smooth-edged HD art sitting on pixel-art terrain.** You asked for HD and simple,
and at tile size the geometric shapes hold up. But it is a different rendering language from
everything else on the map, and it is the first thing I'd want your eye on. If it reads as
foreign, the fix is cheap: drop `outlineWeight` and `cornerRadius` to 0 and raise `thickness`,
which pushes the shapes toward hard-edged blocks that sit closer to the terrain's idiom.

**Morph duration (105 ms) is the number I'd expect you to change first.** Below about 60 ms the
sweep pops rather than reads as motion, which throws away the reason to use that variant.

**Bracket Compass may still be busy.** Eight elements on one tile over detailed terrain is a
lot. If it clutters, push `inset` out before shrinking the pieces — separating them helps more
than making them smaller.

**Compass Petals loses its cursor entirely on a spent ally.** The arrows retract to nothing.
Strong statement, but there's a moment where the only thing locating the cursor is the unit's
own grey. May want a residual stub.

## Bugs fixed along the way

**The map bootstrapper loaded the wrong map when the campaign hadn't been walked to the battle.**
Pressing Play on the battle map, or any dev shortcut into it, silently fell back to the
serialized editor fallback (`Map1_BridgeAtSuvarnapur`) instead of the map the campaign actually
points at (`tooltestingmap`). It now starts the campaign at its first battle, so what you see is
what the real flow gives you. This is why the cursor looked like it was on a placeholder map.

**The opening hover read as an empty tile.** The cursor asks the unit registry who's under it,
but the registry is empty until TurnManager starts the battle — and the cursor is already
sitting on a unit before that. Worse, an unregistered unit reported as *already acted*, so every
unit on the map would have greyed out pre-battle. Both now fall through to the unit's own state.

**Returning to the battle map dropped your selection.** Coming back from combat, dialogue or the
unit-info screen force-set the cursor to Free, silently deselecting a unit you'd picked up.

**A refused move still played the confirm.** Confirming on an unreachable tile played the move
sound and fired the MoveConfirmed dialogue beat despite refusing the move.

## One thing I left alone

Gamepad d-pad left/right is bound to both `CursorLeft`/`CursorRight` **and**
`PrevUnit`/`NextUnit`, so one press fires a cursor step *and* a unit jump. It predates this work
and is out of scope, but it will look like dropped or doubled input on a pad. Say the word.
