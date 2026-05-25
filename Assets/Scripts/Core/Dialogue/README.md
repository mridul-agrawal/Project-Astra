# Dialogue system — how to read this folder

Three layers, all in `ProjectAstra.Core.Dialogue`. The Unity UI (`DialogueView`)
lives **elsewhere**, in `Core/UI/Dialogue/`.

- **Data** — what you author as `.asset` files. Inert.
- **Engine** — loads a data asset and plays it. `DialogueRunner` is the brain;
  `DialogueService` is the front door.
- **Triggers/** — a battle-map add-on that calls the front door when something
  happens on the map.

Rule of thumb: **public = contract or data you touch from outside; `internal` =
engine guts.** If a type is `internal`, you never call it directly — it's wiring.

## Reading path (follow the flow, not alphabetical)

1. **`DialogueScript.cs` → `DialogueNode.cs`** — the shape of authored content.
   A script is an id + an ordered list of nodes. Everything else exists to play
   this. Start here.
2. **`DialogueService.cs`** — the only entry point: `Play(script, context,
   onComplete)`. Read `Play` → `StartNext` → `HandleRunnerComplete`. Owns the
   queue, the view, and the BattleMap↔Dialogue state flip.
3. **`DialogueRunner.cs`** — the playback loop. Top-down: `Start` →
   `PresentCurrentNode` → `Tick` (the typewriter) → `Confirm`/`Skip`. Time comes
   in via `Tick(dt)`, which is why it tests without Unity.
4. **`IDialogueView.cs`** — the contract between runner and screen. The runner
   only knows this interface; the real UI (`DialogueView.cs`) is in
   `Core/UI/Dialogue/`.
5. **`DialogueSpeaker.cs` / `DialogueSpeakerRegistry.cs`** — how a node's
   `speakerId` becomes a name + portrait. `"NARRATOR"` is the reserved
   no-portrait id.
6. **`Triggers/DialogueTriggerDriver.cs`** — last. `OnPhaseStarted` /
   `OnBattleEvent` → `Fire`. Then `DialogueTrigger.cs` (`Matches` / `Resolve`)
   for the rules.

The enums (`DialogueExpression`, `PortraitPosition`, `DialogueTriggeringContext`,
`BattleDialogueEventType`), `DialogueSettings`, and `BattleDialogueEventChannel`
are one-glance files — read them when a reference sends you there.

## The two flows that matter

**Opening cutscene:** `CutsceneUI.OnEnable` → `DialogueService.Play(OPENING_CH01,
Cutscene, →BattleMap)` → runner walks the nodes → `InputManager.OnConfirm`
advances → on end, the `onComplete` callback transitions to BattleMap.

**Battle tutorial:** `TurnManager` raises `TurnEventChannel.PhaseStarted` →
`DialogueTriggerDriver` matches a trigger → `DialogueService.Play(script,
BattleMap)` → service flips game state to `Dialogue` (suppresses map input) →
plays → flips back. The cursor also raises `BattleDialogueEventChannel`
(UnitSelected / MoveConfirmed / PreCombat) from `GridCursor.HandleConfirm` for
the same driver.

## Where to look for…

- **Letter-by-letter text** → `DialogueRunner.AdvanceCrawl` + `CompleteCrawl`.
- **Two dialogues at once** → the `Queue<Pending>` in `DialogueService`.
- **A missing speaker/unit** → `DialogueRunner.TryBuildLine` /
  `SkipMissingSpeaker` (skips + warns).
- **Adding a tutorial line** → author a `DialogueScript` asset + add a
  `DialogueTrigger` entry on the BattleMap `DialogueTriggerDriver`. No code.
- **The box rendering** → not here. `DialogueView.cs` (`Core/UI/Dialogue/`) is
  the only UI piece; the engine deliberately doesn't depend on the concrete UI.

## Deliberately not built yet

Branching, choice nodes, conditions (flags/stats/bonds/Shapath), audio, screen
effects, and localisation keys are out of scope for the prototype. The data
model leaves room for them, but they're unbuilt on purpose.
