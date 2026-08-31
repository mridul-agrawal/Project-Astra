# Runtime flow: startup → Gurukul Hub → battle

How the game actually boots and runs today, after the Gurukul Hub work. Written against the code as
committed at `29554b9`, not against an intended design. Every claim below names the file that backs
it so you can follow it yourself.

Legend used throughout:
**[reused]** existing and untouched · **[modified]** existing, changed by this work ·
**[new]** introduced by this work · **[temp]** demo-only, must not ship as-is ·
**[gap]** authored or half-built but not wired

---

## 1. Launch

`BootScene` is build index 0 and is the only scene that starts on its own. It holds twelve objects,
all of which make themselves `DontDestroyOnLoad` and survive every later scene load:

`EventService` · `GameStateManager` · `SceneLoader` · `GameFlow` · `Input Manager` ·
`DialogueService` · `OverlayManager` · `AudioManager` · `EventSystem` · `UICanvas` ·
`Main Camera` · `Global Light 2D`

Only one of these has a forced order: `EventService` is `[DefaultExecutionOrder(-1000)]`, so its
channels exist before anything tries to publish. Everything else relies on Unity's rule that *all*
`Awake` calls run before *any* `Start`.

**Awake phase**

| Object | What it does |
|---|---|
| `EventService.Awake` | wires the five SO event channels, `DontDestroyOnLoad` **[reused]** |
| `GameStateManager.Awake` | singleton guard, `transitionTable.Initialize()`, `currentState = initialState`. **Raises nothing** — this is a silent assignment, not a transition **[reused]** |
| `SceneLoader.Awake` | singleton guard, `DontDestroyOnLoad` the `EventSystem`, instantiate the `ScreenFader` prefab **[reused]** |
| `GameFlow.Awake` | singleton guard only. `stepIndex` stays at its initialiser, `-1` **[reused]** |

**Start phase** — `SceneLoader.Start` (`Core/Scenes/SceneLoader.cs:60`) is the one that matters:

```csharp
EventService.Instance.SubscribeGameStateChanged(OnStateChanged);

var initialState = GameStateManager.Instance.CurrentState;
if (HasSceneFor(initialState))
{
    currentScene = initialState.ToString();
    LoadScene(currentScene, useFader: false);
}
```

So the first scene loaded is decided entirely by `GameStateManager.initialState`, serialised in
`BootScene.unity`.

---

## 2. Which state is first — and the temporary bit

**`BootScene.unity:836` currently reads `initialState: 16`, which is `GameState.Gurukul`.** **[temp]**

That means the game as committed boots *straight into the hub*. Splash, Title, Main Menu and the
opening cutscene are all skipped. This is `DevBootTarget`'s dev-boot mode
(`Core/Editor/DevBootTarget.cs`, menu `Project Astra/Dev/Boot Straight To Gurukul`), and it is
committed deliberately so hub play-testing is one keypress away.

It was already in dev mode before this work — the previous committed value was `4`
(`GameState.BattleMap`). Run **`Project Astra/Dev/Restore Normal Boot`** to set it back to `Splash`
before any build that ships.

**Normal boot, for reference** — `initialState: Splash (14)`:

```
Splash ──▶ TitleScreen ──▶ (player presses confirm)
                              │
                              └─ TitleScreenUI.cs:25 → GameFlow.Begin() → EnterStep(0)
```

`Splash → TitleScreen` and `TitleScreen → Cutscene` are rows in
`ScriptableObjects/Core/TransitionTable.asset`.

---

## 3. Every transition from boot to the hub

### Under the committed dev boot **[temp]**

There are **no state transitions at all**. `GameStateManager.Awake` assigns `Gurukul` directly and
`SceneLoader.Start` loads the matching scene. The transition table is never consulted, `GameFlow`
never runs `EnterStep`, and `stepIndex` is still `-1` when the hub scene wakes up.

### Under normal boot

```
Splash                                  initialState, no transition
  └─▶ TitleScreen        SplashUI            → RequestTransition
       └─▶ Cutscene      GameFlow.EnterStep(0), step 0 is Cutscene(Opening)
            └─▶ Gurukul  CutsceneUI.cs:36 → NotifyCutsceneFinished() → EnterStep(1)
```

Every one of those is validated by `GameStateTransitionTable.IsValid` against the SO asset. The seven
hub edges added by this work are `TitleScreen→Gurukul`, `MainMenu→Gurukul`, `Cutscene→Gurukul`,
`ChapterClear→Gurukul`, `Gurukul→BattleMap`, `Gurukul→Cutscene`, `Gurukul→TitleScreen`
(`Core/State/GameStateTransitionTable.cs`, `CreateDefaultTransitions`). **[modified]**

Two things worth knowing about `GameStateManager` **[reused]**:

- **One transition per frame.** A second `RequestTransition` in the same frame is discarded with a
  warning (`IsBlockedThisFrame`). This is why the hub's six sub-states are *not* `GameState`s.
- An illegal transition is rejected and logged as an error; state does not change.

---

## 4. Campaign and progression data

`GameFlow` **[modified]** holds four serialised references, all wired on the `GameFlow` object in
`BootScene`:

| Field | Asset | Status |
|---|---|---|
| `campaign` | `ScriptableObjects/Core/Campaign.asset` | **[reused]** |
| `mapCatalog` | `MapCatalog` | **[reused]** |
| `cutsceneCatalog` | `CutsceneCatalog` | **[reused]** |
| `visitDatabase` | `Assets/Gurukul/Data/GurukulVisitDatabase.asset` | **[new]** |

The whole of campaign progression is one field: `private int stepIndex = -1`. It is never
serialised, so it lives for the session only. **[gap — no save system, deferred by decision]**

`Campaign.asset` as committed, four steps:

```
0  Cutscene   CutsceneId.Opening
1  HubVisit   visitId "hub1"          ← added by this work [modified]
2  Battle     mapId "tooltesting map"
3  Cutscene   CutsceneId.Ch1Ending
```

`CampaignStepKind` gained `HubVisit = 2` (appended — the kind is stored as an int in the asset) and
`CampaignStep` gained a `visitId` string. `CampaignStepDrawer` **[modified]** grew a third branch, or
the field would be invisible in the inspector.

---

## 5. Selecting the campaign entry and the right visit

`GurukulBootstrapper.ResolveVisit` (`Core/Gurukul/GurukulBootstrapper.cs:49`) **[new]**:

```csharp
GameFlow flow = GameFlow.Instance;
GurukulVisitData campaignVisit = flow != null ? flow.EnsureHubStepStarted() : null;
return campaignVisit != null ? campaignVisit : fallbackVisit;
```

`GameFlow.EnsureHubStepStarted()` **[new]**, mirroring the existing `EnsureBattleStepStarted`:

- If `CurrentVisit` already resolves (the campaign walked here properly), return it untouched.
- Otherwise find `campaign.IndexOfFirst(CampaignStepKind.HubVisit)`, set `stepIndex` to it, and
  return that visit.

`CurrentVisit` resolves as: current step must be `HubVisit`, then `visitDatabase.Get(step.VisitId)`.

**Under the dev boot this second branch is what fires.** `stepIndex` is `-1`, so it snaps to index 1
and loads `hub1`. That is why the departure later finds `tooltesting map` as the next step.

**Pressing Play on `Gurukul.unity` directly is different again.** There is no `BootScene`, so no
`GameFlow`, so `ResolveVisit` falls through to the serialised `fallbackVisit` — currently the greybox
visit, not `hub1`. `InputManager` is also absent, so nothing responds to keys. Always boot from
`BootScene`. **[temp — a consequence of the fallback, which exists for editor convenience]**

---

## 6. Loading and initialising the hub scene

`SceneManager.LoadScene("Gurukul")` in **single** mode. `SceneLoader` has no additive path, which is
the reason interiors are *not* scenes.

**Awake phase in `Gurukul.unity`**

| Component | Order | What it does |
|---|---|---|
| `GurukulInputRouter` | −50 | constructs `GurukulStateMachine`, subscribes to its own `StateChanged` **[new]** |
| `GurukulActor` | — | `Position = transform.position` **[new]** |
| `GurukulHUDController` | — | news the prompt and objective controllers, finds the driver and router **[new]** |
| `GurukulCameraRig` | 100 | reads `PixelPerfectCamera` for viewport tiles **[new]** |
| `MapCamera` | — | adds `PixelPerfectCamera`, 480×270 @ 32 PPU **[reused, unmodified]** |

**Start phase**, strictly in this order:

1. **`GurukulBootstrapper.Start`** (order −100) — everything below.
2. **`GurukulHUDController.Start`** (default order) — `objectives.Bind(...)`. `Bind` calls `Refresh`
   immediately rather than waiting for an event, which is what stops the HUD missing the opening
   objective the bootstrapper already announced.

`GurukulBootstrapper.Start`:

```
GurukulWorld.Clear()                     drop any actors/interactables from a previous session
ResolveVisit()                           §5
GurukulProgressService.Load(visit)       builds runtime state + objective runner, applies baseline
OpenVisit(visit)
```

`GurukulProgressService.Load` **[new]** constructs `GurukulRuntimeState` and
`ObjectiveSequenceRunner`, then `ApplyBaselineGates()` writes the visit's `openGates` and
`interactableOverrides` into that state.

`OpenVisit`:

```
loader.CreatePlayer(protagonistUnitId, spawn, facing, playerRoot)   non-solid
  └─ + GurukulPlayerController
  └─ cameraRig.Follow(player.transform)
  └─ interactionDriver.Bind(router, player)
loader.Load(startLocationId, spawn, facing, houseIdentity: null)    §7
events.BindToVisit()                                               EventQueueGuard over this visit
Objectives.EventRequested += director.PlayEvent                    effect → event bridge
Objectives.Begin()                                                 §8
if (visit.OpeningEventId != "") events.TryPlay(...)                before control is given
```

---

## 7. Building the world

`GurukulLocationLoader.Load` (`Core/Gurukul/GurukulLocationLoader.cs:24`) **[new]** — the *single*
path used both by the bootstrapper and by every doorway, so arriving in a room always means the same
thing:

```
ClearCast()                              destroy previous room's NPCs
GurukulLocationService.Load(location)    builds the collision map
locationHost.Show(location)              base-art sprite + props prefab
State.EnterLocation(locationId, houseIdentity)
ApplyHouseIdentity()                     switch on only this house's objects
PlacePlayer(spawn, facing)               she is moved, never rebuilt
SpawnCast(locationId)
```

**Collision** — `GurukulLocationData.BuildCollisionMap()` makes a `GurukulCollisionMap` at **half-tile**
resolution from the painted `blockedCells` mask, then stamps every `propFootprint` marked
`startsSolid`. Half a tile, not a whole one, because a tall sprite blocks only its base. Prop rects
are kept rather than baked flat so a state change can un-stamp exactly what it stamped. **[new]**

**Environment art** — `GurukulLocationHost.Show` draws `baseArt` as one `SpriteRenderer` on the
`Ground` sorting layer and instantiates `propsPrefab`. This mirrors `MapRenderer.DrawBaseArt` /
`SpawnEnvironment`. **[new, modelled on reused code]**

**Environment *state*** — `GurukulVisitData.environmentSet` is authored and has **no consumer anywhere**.
Per-visit environmental change (flood debris, damp ground) is not implemented. **[gap]**

**NPCs** — `SpawnCast` walks `visit.CharacterPlacements`, resolves each through
`GurukulProgressService.TryGetPlacement` (so a relocation or conversation override applied by an
earlier objective wins over the authored baseline), skips anyone whose `locationId` isn't this room,
and calls `GurukulActorFactory.Create(..., solid: true)`.

`GurukulActorFactory` **[new]** builds actors procedurally — the project has no character prefabs, so
this mirrors `UnitSpawner.AttachSprite`. Identity comes from the existing `UnitDefinition`
(`unitName`, `mapSprite`, `mapAnimator`, `speaker`) **[reused]**. The sprite child gets
`SpriteRenderer` + `YSortRenderer` + optionally `Animator` + `UnitAnimator`.

Actors register themselves with `GurukulWorld` in `OnEnable` **[new]**, which is the list the
interaction check reads each frame instead of sweeping the scene.

**Interactables** are *not* spawned from visit data. They are components authored inside the
location's `propsPrefab` and register themselves the same way. The visit can only override their
*state* — `GurukulInteractableOverride.conversationId` is authored but
`ApplyBaselineGates` applies only `state`, so the conversation half is dropped. **[gap]**

**Conversations** are named by id on a placement or an interactable and resolved at interaction time
through `ConversationGraphDatabase`. Nothing about them is initialised at load.

---

## 8. Determining the active objective

`ObjectiveSequenceRunner` **[new]**, pure C#:

```csharp
public GurukulObjectiveData ActiveObjective =>
    state.ObjectiveIndex >= 0 && state.ObjectiveIndex < objectives.Length
        ? objectives[state.ObjectiveIndex] : null;
```

`Begin()` calls `SettleImmediateObjectives()` — clearing any leading stage whose condition kind is
`Immediate`, which exists so a stage can just run effects and hand on — then raises
`ObjectiveChanged` with whatever is now active.

The active objective is therefore only ever "the one at the current index". There is no activation
condition; `GurukulObjectiveData` has exactly five fields: `objectiveId`, `displayText`, `completion`,
`onComplete`, `markerTargetIds`.

---

## 9. Progression state within one visit

All of it lives in one object, `GurukulRuntimeState` **[new]** — session-scoped, but written as plain
serialisable data (`List<string>`, asset ids, no SO references) so a later save ticket is wiring
rather than a rewrite.

| Held | Purpose |
|---|---|
| `objectiveIndex` | which stage is active |
| `satisfiedTargets` | targets cleared **for the current stage only**; cleared on advance, so acting on a later stage's target early banks nothing |
| `completedObjectiveIds`, `completedConversationIds`, `completedEventIds` | one-time guards |
| `askedTopics` | keyed `conversationId:optionId`, so two characters can own a topic of the same name |
| `openGates`, `interactables`, `relocations`, `conversationOverrides` | what objective effects write |
| `currentLocationId`, `houseIdentity`, return ticket | where she is, and how six houses share one interior |
| `departed` | set only after departure commits |

**Sub-states within the visit** are a separate thing: `GurukulStateMachine` **[new]** holds
`FreeExploration / Conversation / ChoiceOrQuiz / ScriptedEvent / LocationTransition / Departure` with
a code-side legal-transition set, modelled on `CursorStateMachine`. `Departure` is terminal.

`GurukulInputRouter` **[new]** is the **only** `InputManager` subscriber while in the hub, and routes
each press by sub-state through a single confirm latch — so one press physically cannot be read as
both "talk to him" and "advance the line". It polls `IsActionHeld` rather than subscribing, which
also bypasses `DelayedAutoShift` without touching it. **`InputManager` itself was not modified.**

---

## 10. Completing an objective

The only path in is `ObjectiveSequenceRunner.Report(kind, targetId)`, called from
`GurukulVisitDirector` **[new]**:

- `OnConversationFinished` → `Report(ConversationCompleted, conversationId)`, plus
  `Report(ObjectInspected, interactableId)` when the conversation was opened from a non-character.
- `OnFlagRaised` (a `SetFlag` node, or an event's `RaiseFlag`) → `Report(QuizPassed, …)` and
  `Report(EventCompleted, …)`. Only the kind an objective actually named will match.

Progress is reported **after** the conversation finishes, which is what makes the completion order
hold. `Report`:

```
Accepts(kind, targetId)?     kind must match and the id must be in the target set
Satisfy(targetId)            deduped — a repeat earns nothing
ProgressChanged              HUD counter refresh
CurrentProgress >= Required? → Complete(active)
```

`Complete` runs in this exact order:

```csharp
state.CompleteObjective(objective.ObjectiveId);  // clears satisfiedTargets, ++objectiveIndex
ApplyEffects(objective);                         // gates, relocations, states, FireEvent
ObjectiveCompleted?.Invoke(objective);           // HUD cue
SettleImmediateObjectives();
ObjectiveChanged?.Invoke(ActiveObjective);       // HUD shows the next line
```

**The index advances before the effects run.** That is load-bearing: Hub 1's last objective fires an
event whose `Depart` action asks `CanDepart`, and `CanDepart` is `IsVisitComplete`, which is only true
once the index has passed the end.

Effects (`GurukulEffect`) write into runtime state rather than touching the scene, so the same list
replays identically whether she is standing outside or three rooms away: `SetGate`,
`SetInteractableState`, `RelocateCharacter`, `SetCharacterConversation`, and `FireEvent` — which is
raised as `EventRequested` because the runner cannot see the world.

**What unlocks as a result:** a gate opening changes `GurukulInteractable.IsGated` and
`GurukulDoor.requiredGate`, so a previously-refused door now opens and the prompt changes on the next
frame. `GurukulMarkerManager` recomputes every `LateUpdate`, so markers move to the new stage's
targets with no explicit refresh call.

---

## 11. Departure into the battle

Hub 1 uses the automatic mode. The chain:

```
last objective completes
  └─ effect FireEvent "hub1_training_ground"
      └─ ObjectiveSequenceRunner.EventRequested
          └─ GurukulVisitDirector.PlayEvent
              └─ GurukulEventRunner.TryPlay
                  ├─ EventQueueGuard: not already done, nothing else running
                  ├─ sub-state → ScriptedEvent  (input locked, prompts hidden)
                  ├─ action PlayConversation "hub1_training_scene"
                  └─ action Depart → DepartureRequested
                      └─ GurukulDepartureController.TryDepart()
```

`TryDepart` (`Core/Gurukul/GurukulDepartureController.cs`) **[new]**:

```csharp
DepartureGate.CanDepart(progress.CanDepart,          // every objective done
                        progress.DestinationMapId,   // authored on the visit
                        GameFlow.Instance.NextBattleMapId,
                        out string problem)
```

`DepartureGate` **[new]** is pure C# and refuses on any of: unfinished objectives, a visit with no
destination, no battle after this step, or the visit and the campaign naming *different* maps. The
last one matters because the spec forbids inferring the next battle — a disagreement is reported, not
silently resolved.

`Commit` then:

```csharp
router.States.TryTransition(GurukulSubState.Departure);   // lock first
GameFlow.Instance.NotifyHubVisitFinished();               // EnterStep(stepIndex + 1)

if (GameStateManager.Instance.CurrentState == GameState.Gurukul)  // it didn't take
{
    Debug.LogError(...);
    router.States.TryTransition(GurukulSubState.FreeExploration);
    return false;                                          // she stays in the hub, recoverable
}

progress.State.MarkDeparted();
HasDeparted = true;
```

`NotifyHubVisitFinished` → `EnterStep(2)` → step is `Battle` → `RequestState(GameState.BattleMap)` →
transition table validates `Gurukul → BattleMap` → `EventService.RaiseGameStateChanged` →
`SceneLoader.OnStateChanged` loads `BattleMap.unity` **with** the fader. From there
`MapBootstrapper` **[reused, untouched]** takes over and `GameFlow.CurrentMap` resolves
`tooltesting map`.

The event runner breaks out of its action loop after `Depart` and deliberately does **not** return to
`FreeExploration`, so there is no frame of playable hub between the decision and the battle.

**Return from battle** is the pre-existing path, unchanged: `BattleVictoryWatcher` → `WarLedger` or
`ChapterClear` → `ChapterClearUI.cs:100` → `NotifyBattleFinished()` → `EnterStep(+1)`. If that next
step is a `HubVisit`, `ChapterClear → Gurukul` is now a legal edge. Defeat routes to `GameOver`, which
never touches `GameFlow`, so losing cannot advance progression.

---

## What is not finished

| | |
|---|---|
| **Disk save/load** | None. All progression is session-only. Deferred by decision until ≥2 maps and ≥2 hubs are playable. |
| **`GurukulVisitData.environmentSet`** | Authored field, zero consumers. Per-visit environmental change is not implemented. |
| **`GurukulDeparture.mode` / `departureTargetId`** | Authored, zero consumers. Only the automatic path works; a deliberate "walk to a Depart target and confirm" is not wired, though the pieces exist (a `Depart` verb, a choice node, the `depart` flag). |
| **`GurukulInteractableOverride.conversationId`** | Only `state` is applied at load; the conversation half is silently dropped. |
| **`GurukulEventTrigger.VisitLoad`** | Never matched. Opening events are fired by id from `visit.OpeningEventId`; only `AreaEntered` is matched, by `GurukulAreaTriggerWatcher`. |
| **Cross-room wayfinding** | `GurukulMarkerManager.AnchorFor` resolves targets only through `GurukulWorld`, i.e. the current room. A target in another room gets no marker and no edge arrow — there is no "mark the door instead" behaviour, which the spec asks for. Hub 1 puts the Guru in a different room, so this is visible. |
| **Retry after defeat** | `GameFlow.stepIndex` is never persisted, so Game Over → Title → New Game rewinds the whole campaign. Pre-existing; the hub makes it visible. |
| **`DeathRegistry` / `CommitmentTracker`** | Still scene-scoped `MonoBehaviour`s with no `DontDestroyOnLoad`; their state dies on every scene load. Pre-existing, untouched. |
| **Latent: cast teardown on a room change** | `ClearCast()` uses `Destroy`, which defers `OnDisable` to end of frame. By then `GurukulLocationService.Instance` holds the **new** room's collision map, so the departing actors un-stamp against it. `AddToCells` clamps at zero so it is usually harmless, but an old footprint overlapping a new actor's cells would wrongly clear that actor's stamp. |

## Hub content status

Hub Interaction 1 runs end to end, but every line of its dialogue reads `PLACEHOLDER` and the whole
cast stands in a programmer-art courtyard rather than the authored Gurukul.
See `docs/hub/hub1-content-owed.md`.
