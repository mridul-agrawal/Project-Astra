# CRT filter — handoff

Branch `crt.dev.master`. Seven commits, one per phase.

The filter covers the game world and everything sitting in it — map art, units, the cursor,
range overlays, HP bars, damage numbers. The HUD, menus and dialogue composite afterwards and
stay pin-sharp. That split needed no work: Screen Space Overlay canvases are drawn after URP
finishes, so a camera pass cannot reach them.

## Turning it on

**Settings → CRT Filter → Off / Subtle / Full.** Persisted to PlayerPrefs, exactly like the
combat-speed setting. Ships **Off** — the filter is a taste, not an improvement everyone agrees
on, and you said you'd pick the default later.

The dropdown field on `SettingsMenuOverlayUI` is wired in code but **has no widget in the
prefab yet**. Until one is added the setting is reachable only from the F2 panel. Adding it is
a copy of the combat-speed dropdown row.

## Tuning it

**F2** in the battle map opens a live tuning panel (editor and dev builds only, hidden by
default). It edits whichever profile the current setting selects, so what you drag is always
what you can see, and it writes through `SerializedObject` — **edits survive leaving play mode**
rather than evaporating.

Or edit the assets directly in `Assets/ScriptableObjects/Rendering/`:
`CrtProfile_Subtle`, `CrtProfile_Full`. Values push to the material every frame, so the
Inspector works live too.

### The knobs, and what they actually are

| Knob | What it models |
|---|---|
| **Horizontal bleed** | The analog sweep. The beam's light spreading sideways — what lets two dithered colours merge into a third the palette never held |
| **Scanline / beam width** | The lit part of each line and the gap between. Horizontal is soft and continuous, vertical is hard and discrete; that asymmetry is what makes it read as *structured* rather than blurry |
| **Bloom** | Beam widening with brightness. **The main depth cue** — highlights physically spread and fill the scanline gaps while shadows stay tight. A flat panel cannot do this at all |
| **Halation** | Light scattering inside the glass. Warm by default, because the glass and red phosphor scatter most |
| **Gamma / gain** | Power-law response, and putting back the ~15% of light the scanlines eat |
| **Mask** | The phosphor lattice, computed in *output* pixels because it was a physical grid unrelated to the signal |
| **Curvature / vignette** | Off by default |

**The three worth touching first** are horizontal bleed (crisp vs. blended), beam width (how
present the lines are), and gain (bright vs. murky).

**Keep mask strength low.** Photographs of CRTs badly overstate the mask — a camera resolves
individual phosphor dots your eye would blend at viewing distance. Tuning against reference
photos is the classic way to end up with a screen-door nobody ever actually saw.

## Verified

- All three settings captured on the real map through the normal boot flow: Off is pixel-clean,
  Subtle reads as a warm screen, Full is unmistakably a CRT. HUD sharp in every one.
- 605/605 EditMode tests green throughout.
- Clean compile at every phase.

## Not verified

- **Frame cost.** The shader is 14 texture samples per pixel (5 sweep, 8 halation, 1 base) at
  1080p, which is cheap, but I did not put a profiler on it. Worth a look before Switch.
- **Camera panning.** I could not drive input, so I have not seen the scanline grid while the
  camera moves. It is derived analytically from the reference resolution rather than from pixel
  detection, so it should stay locked to the world — but that wants your eyes.
- **Non-integer window sizes.** At 1600×900 the source pixel blocks become uneven. The scanlines
  will still land correctly; the art underneath will not be evenly blocky.

## Things I'd flag

**The art was not authored for this**, and that ceiling is real. The era's magic came from
artists dithering *into* the blend — Sonic's waterfalls only read as transparent because the
checkerboard dissolved. Applied to art drawn on a sharp LCD, the filter softens but has no
dither to resolve. What you have is "warmer and rounder", not "suddenly 16-bit magic". Getting
the rest would mean authoring new art with the filter switched on.

**Unit sprites are 256×256 at 256 PPU** — one world unit, so 32 screen pixels at reference
resolution. That is an 8× point-filtered downscale of art that isn't on the pixel grid. It
already aliases; the scanlines make it obvious. Worth fixing at source independently.

**`CRT.mat` will show up as modified after you play.** The binder writes profile values onto the
material, and in the editor those writes persist to the asset. It is harmless — the binder
rewrites everything on the next run — but it is git noise, so discard that file rather than
puzzling over it.

**`renderPostProcessing` is now on** for the battle camera. I turned it on while debugging and
left it: it costs little and it is required if you ever want URP's Bloom for a wider halation
than the 8-tap ring gives.

## Two traps, recorded so nobody re-walks them

**A render-graph pass that blits back into the active colour texture gets culled.** Nothing
downstream reads that handle, so the write is dead and the whole pass is eliminated — it records
happily and draws nothing. Swapping `resources.cameraColor` fixes the culling and then blanks
the screen if the draw itself fails. Both have the same root cause and neither points at it.
Use URP's shipped `FullScreenPassRendererFeature`; it does this correctly for free.

**A full-screen shader must match URP's `CoreBlit.shader` boilerplate.** URP's `Core.hlsl`
before the core `Blit.hlsl`, a target pragma, explicit pass state, and — most likely the one
that actually mattered — `#pragma editor_sync_compilation`. Without it Unity compiles the shader
asynchronously and draws nothing meanwhile, which is indistinguishable from a broken effect.

**And the one that cost the most time:** `manage_camera screenshot` re-renders through
`Camera.Render()`, which **skips renderer features entirely**. It will show you a clean world and
tell you your effect is broken when it is merely invisible. Capture the real backbuffer with
`ScreenCapture` instead.
