# Hub State Integration

Decided and executed 2026-08-31, on `hub.dev.master`. Supersedes the earlier draft, which
over-reached.

**Status: all six phases done.** 838 EditMode tests green, hub verified in play mode end to end
(boot, conversation, scripted sequence, refused departure, doorway). Two soft-locks found and
fixed on the way — see §7. Uncommitted.

**Goal:** the Gurukul hub stops running a private state machine. Its six sub-states become
either high-level `GameState` values or busy locks, and `GurukulStateMachine` is deleted.
Nothing else in the game changes.

---

## 1. Scope

**In:** `GurukulSubState` and the seven hub scripts that read it, plus the two
`GameStateManager` capabilities that make the merge possible.

**Out, and staying out:**

| Machine | Why it stays |
|---|---|
| `CursorStateMachine` / `CursorMode` | The cursor is an entity that exists only on the battle map. Free / Selected / Targeting describe what a cursor is doing, not what the game is. An entity owning its own state machine is correct design, not duplication. |
| `BattlePhaseManager` / `BattlePhase` | A different axis — whose turn it is, not what the player is doing. Both are true at once, so merging produces a cross-product. |
| `InventoryMenuUI`, `TradeScreenUI`, `ConvoyUI` | Battle-map screens, working today. Out of this budget. |

The test for what belongs in `GameState`: **does the player see and do something
categorically different?** Free roaming, dialogue, a cutscene — yes. A cursor changing what
it highlights — no.

*Separate, unrelated note:* `CursorMode` (5 values, input rules) and `CursorState` (6 values,
visuals) overlap inside `GridCursor` and are not identical. That is a cursor-internal cleanup
for some other day. It is not state-architecture work and is not in this plan.

---

## 2. The six sub-states, mapped

| `GurukulSubState` | Becomes | Kind |
|---|---|---|
| `FreeExploration` | `GameState.HubExploration` | rename of the state it already duplicates |
| `Conversation` | `GameState.Dialogue` | existing state, widened |
| `ChoiceOrQuiz` | *(nothing)* | a node kind inside a dialogue, not a mode |
| `ScriptedEvent` | `GameState.ScriptedSequence` | one new state |
| `LocationTransition` | busy lock | door fade — already duplicated as `IsTransitioning` |
| `Departure` | busy lock | hub-to-battle handover |

Six sub-states, one new global state.

---

## 3. Enum changes

`GameState` ints are serialized in `TransitionTable.asset`, `InputContextTable` and
`SceneStateCatalog`. **Rename freely — the int is unchanged. Append only. Never reorder.**

| # | Change |
|---|---|
| 16 | `Gurukul` → `HubExploration` *(rename, int unchanged, all existing rows keep working)* |
| 17 | `ScriptedSequence` *(new, appended)* |

17 states become 18.

`Cutscene` (2) cannot serve as the scripted-sequence state: it is scene-backed, so entering it
would load `Cutscene.unity` and tear the hub down mid-event. `ScriptedSequence` is an overlay
state — it must **not** be added to `SceneStateCatalog`.

### The scene rename

`SceneLoader` resolves scenes by `state.ToString()`, so renaming state 16 means renaming
`Assets/Scenes/Gurukul.unity` to `HubExploration.unity` and confirming Build Settings follows
the move. That is smaller than replacing the name lookup with an explicit state-to-scene map.
Build the explicit map later, when a second explorable location actually needs it.

---

## 4. Rows to append to `TransitionTable.asset`

Derived from the sub-state graph. Seven rows; **append only, never regenerate.**

| From | To | Replaces |
|---|---|---|
| 16 `HubExploration` | 7 `Dialogue` | `FreeExploration → Conversation` |
| 7 `Dialogue` | 16 `HubExploration` | `Conversation → FreeExploration` |
| 16 `HubExploration` | 17 `ScriptedSequence` | `FreeExploration → ScriptedEvent` |
| 17 `ScriptedSequence` | 16 `HubExploration` | `ScriptedEvent → FreeExploration` |
| 7 `Dialogue` | 17 `ScriptedSequence` | `Conversation → ScriptedEvent` |
| 17 `ScriptedSequence` | 7 `Dialogue` | `ScriptedEvent → Conversation` |
| 17 `ScriptedSequence` | 4 `BattleMap` | `ScriptedEvent → Departure`, without flickering through exploration |

`16 → 4` (hub to battle) already exists, so a normal departure needs no new row.
`InputContextTable` needs one new row for `ScriptedSequence` — skip-dialogue only, matching
what a locked-out player can do during an event.

---

## 5. What GameStateManager must gain

Two things. Both are prerequisites, not nice-to-haves.

1. **A transition queue**, replacing the one-per-frame gate. The hub chains transitions inside
   a single frame today — a choice resolving into the next choice, a conversation ending and
   the event that owns it taking control back (`GurukulEventRunner.PlayConversation`). Under
   the current gate the second one is discarded with a warning.
2. **A return-to-caller state**, generalising `MenuReturnState`. `DialogueService` hardcodes
   returning to `BattleMap` (`DialogueService.cs:134-143`). Once hub conversations run in
   `GameState.Dialogue`, dialogue must return to whichever state opened it.

**Not needed for this scope:** the full single-owner-input refactor. A narrower rule is enough
— `GurukulInputRouter` acts only while the state is `HubExploration`, and `DialogueService`
owns confirm while the state is `Dialogue`. The router already re-arms its latches on every
sub-state change; that hook moves to `GameStateChanged`.

---

## 6. Migration order

Each phase leaves the game runnable and is independently committable.

**Phase 0 — Manager capabilities. DONE.** The one-per-frame gate is gone, replaced by a queue
that holds a transition asked for from inside a state-change listener and judges it against the
table when its turn comes, with a runaway-loop guard. `MenuReturnState` became a caller stack:
`CallerState` / `CanReturnToCaller()` / `ReturnToCaller()`, popped however a state is left so a
dialogue ending in a game over can't leave its caller behind. *848 tests green.*

**Phase 1 — `Gurukul` → `HubExploration`. DONE.** Enum value renamed at int 16, so every
existing table row kept working untouched. `Gurukul.unity` renamed to `HubExploration.unity`;
Build Settings followed the move with its GUID intact. *848 tests green; scene confirmed loading
under the new name in play mode.*

**Phase 2 — Dialogue absorbs `Conversation` and `ChoiceOrQuiz`. DONE.** The whole conversation
stack moved to `Core.Dialogue.Conversation` (`ConversationPlayer`, `ConversationRunner`, the
graph data and the choice menu UI), with GUIDs preserved so scene wiring survived. The runner's
last hub dependency became `IConversationMemory`, which `GurukulRuntimeState` implements — the
conversation stack now contains zero references to the hub, which is what makes it usable from a
battle map. `ConversationPlayer` owns the confirm button and the Dialogue state for a whole
exchange; `DialogueTriggeringContext.Gurukul` became `.Conversation`. `DialogueService` returns
to its caller instead of hardcoding `BattleMap`. *841 tests green.*

**Phase 3 — `ScriptedSequence`. DONE.** Appended at int 17 with its five table rows and an
input row allowing skip only. `GurukulEventRunner` moves the global state; the line that used to
re-take control after an inner conversation is gone, because the caller stack already returns
there. The objective panel now hides on `GameStateChanged` rather than the sub-state.
*842 tests green.*

**Phase 4 — Locks. DONE.** Both handovers became one refusable lock
(`TryBeginHandover` / `EndHandover` / `HandoverEnded`). The door fade's duplicate
`IsTransitioning` flag now just reads the lock, and the router re-arms the confirm button when a
handover ends. *838 tests green.*

**Phase 5 — Delete `GurukulStateMachine`. DONE.** `GurukulSubState` is gone. What is left is
`GurukulControlGate`, which answers one question — may anything in the hub act right now? — from
the game state and the handover lock. `router.States` became `router.Gate`. *838 tests green.*

---

## 7. Bugs fixed on the way

**Departure soft-lock (found by reading, fixed in Phase 4).** `GurukulSubState.Departure` was
terminal — no outgoing edges. When `GurukulDepartureController.Commit` found the state change
had not taken, it attempted `Departure → FreeExploration`, which was illegal, was rejected with
a warning, and left the hub with movement and interaction both dead. A lock can be released;
a terminal state could not.

**Refused-departure soft-lock in the event runner (found in play mode, fixed).** A sequence
ending in a `Depart` action set `departed = true` on the *intent* and skipped the hand-back, so
a departure the gate refused — an objective still unfinished, which is the normal case mid-visit
— ended the sequence with the game stuck in `ScriptedSequence` and every input dead. This one
was live before this refactor too, in the old sub-state. It now checks whether the game
actually left before skipping the hand-back. Reproduced and confirmed fixed by playing
`hub1_training_ground`, whose last action is exactly that.

---

## 8. Constraints on the work

- **`TransitionTable.asset` is append-only.** Never regenerate it from
  `CreateDefaultTransitions()` — that silently drops designer-added rows. Edit additively and
  confirm `git diff` shows only additions.
- **`GameState` ints are serialized** in three shared assets. Rename freely, append only,
  never reorder or insert.
- **Verify in the editor, not from serialized data.** Play-test or screenshot each phase.
- **Seven non-test scripts read `GurukulSubState`:** `GurukulInputRouter`,
  `GurukulStateMachine`, `GurukulConversationPlayer`, `GurukulEventRunner`,
  `GurukulLocationTransition`, `GurukulDepartureController`, `GurukulObjectiveController`.
  Plus `GurukulStateMachineTests`.
