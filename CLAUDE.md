# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity 6 (6000.0.65f1) third-person stealth game using the Universal Render Pipeline (URP). The player must navigate a sci-fi environment, avoid or neutralize guards, use disguises to gain clearance, and complete sequential objectives to win.

## Engine & Build

- **Unity version:** 6000.0.65f1 — open the project in this exact version via Unity Hub.
- **Rendering:** URP (`com.unity.render-pipelines.universal` 17.0.4).
- **Input:** New Unity Input System (`ENABLE_INPUT_SYSTEM` define is active). All input polling uses `UnityEngine.InputSystem.Keyboard.current`, with legacy `Input` fallbacks guarded by `#else`.
- There is no CLI build command. Build and Play are done from within the Unity Editor.

## Editor Utility Scripts

Custom editor tools live in `Assets/Editor/` and appear in the Unity menu bar:

| Script | Purpose |
|---|---|
| `BakeNavMesh.cs` | Batch-bake NavMeshes for all scenes |
| `SetupGuardDisguiseBoxes.cs` | Auto-configure `DisguiseBox` components on guard prefabs |
| `FixGuardDisguiseBoxes.cs` | Repair malformed disguise box colliders |
| `AddDisguiseSystemToPlayer.cs` | Attach and wire `DisguiseSystem` onto the player prefab |
| `DiagnoseTriggers.cs` | Audit trigger colliders in the active scene |

## Key Controls (Runtime)

| Key | Action |
|---|---|
| WASD | Move |
| Left Shift | Sprint (drains stamina) |
| Left Ctrl / C | Crouch |
| Space | Jump |
| F | Interact / Takedown / Equip disguise |
| Q / E | Peek left / right |

## Architecture

### Scenes

- `Assets/Scenes/SampleScene.unity` — main gameplay scene
- `Assets/Scenes/TitleScreen.unity` — title/main menu
- `Assets/Alpha.unity` — alpha build scene

### Player GameObject (component stack)

All of these components live on the same player GameObject:

| Component | Role |
|---|---|
| `CharacterInputController` | Reads keyboard input; manages stamina; writes `speed`, `MoveX`, `MoveY`, `isCrouching`, `isSprinting` animator params |
| `BasicControlScript` | Rigidbody physics movement, jumping, ground detection, camera-relative steering |
| `DisguiseSystem` | Owns the suspicion meter (0–100), security clearance, and disguise material swapping |
| `TakedownSystem` | F-key takedown — finds nearest guard, snaps player behind guard, plays synchronized animations |
| `PlayerInventory` (singleton) | Tracks keycard, mission keys, and blueprints |
| `PeekSystem` / `WallPeek` | Q/E wall-peek with raycasting |
| `PlayerNoiseEmitter` | Fires `NoiseEmittedEvent` on the `EventManager` when the player moves |

### EventManager

`Assets/Scripts/EventManager.cs` is a static, type-safe event bus. Event types are plain structs used as generic type parameters — there are no instances of these structs passed around.

Defined events:

| Event struct | Signature | When fired |
|---|---|---|
| `NoiseEmittedEvent` | `(Vector3 position, float radius)` | Player footsteps / landing |
| `SuspicionChangedEvent` | `(float delta, string reason)` | Any suspicion increase (zones, loitering) |
| `GlobalChaseCascadeEvent` | `()` | First guard to enter Chase state; alerts all other guards |
| `DisguiseChangedEvent` | `(SecurityClearance newClearance, SecurityClearance oldClearance)` | After disguise coroutine completes |
| `MissionFailEvent` | `(string reason, string detail)` | Suspicion hits 100 |
| `ZoneViolationEvent` | `(SecurityClearance required, string zoneName)` | Player enters zone with wrong clearance |
| `PlayerLandsEvent` | `(Vector3 contactPoint, float impulse)` | Player lands from a jump |

Usage pattern:
```csharp
// Subscribe (in OnEnable)
EventManager.AddListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
// Unsubscribe (in OnDisable)
EventManager.RemoveListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
// Fire
EventManager.TriggerEvent<NoiseEmittedEvent, Vector3, float>(position, radius);
```

### Guard AI

`Assets/Scripts/GuardAI.cs` — each guard is a state machine with three states:

- **Patrol**: ping-pong between assigned `patrolPoints[]`
- **Investigate**: move to last known player position, wait `investigateWaitTime`, then return to patrol
- **Chase**: pursue player directly; on reaching `catchDistance` starts `CatchPlayerRoutine` (takedown animation → `GameManager.TriggerLose()`)

Vision uses two tiers checked via `RaycastAll`:
- Full FOV (`fovDegrees`, default 90°): raises suspicion at `fullSightSuspicionRate` per second
- Peripheral (`peripheralFovDegrees`, default 150°): raises suspicion at `glimpseSuspicionRate` per second

Hearing: `OnNoiseHeard` checks distance against `baseHearingRadius`. Noise within `strongHearingFraction` of that radius triggers Investigate immediately; outside it only adds `faintNoiseSuspicion`.

When one guard begins chasing, it fires `GlobalChaseCascadeEvent`, putting every other guard into Chase state.

Guards call `_disguiseSystem.AddObserver()` while they have line-of-sight to the player (not disguised), which blocks suspicion decay in `DisguiseSystem`.

### Disguise & Suspicion System

`SecurityClearance` enum (`Civilian=0` through `Guard04=4`) controls zone access.

**Flow:**
1. Player approaches a `DisguiseBox` → `DisguiseUIPrompt` appears
2. Press F → `DisguiseBox.UseDisguise()` → `DisguiseSystem.ApplyDisguise(clearance, outfit)` coroutine: disables controls, plays VFX, swaps torso materials on `upper_cloth` child renderers, resets suspicion to 0
3. `SecurityZone` trigger colliders fire `SuspicionChangedEvent` continuously if player's `CurrentClearance < requiredClearance`
4. Suspicion decays when `_observerCount == 0` and the decay delay has elapsed
5. At 100 suspicion: `MissionFailEvent` → `GameManager.TriggerLose()`

`DisguiseOutfit` is a `ScriptableObject` (`Assets/Scripts/DisguiseOutfits/`) with per-slot material references (`shirtMaterial`, `tshirtMaterial`) matched against `SkinnedMeshRenderer.sharedMesh.name`.

### Singletons

| Class | Access |
|---|---|
| `GameManager` | `GameManager.Instance` / `GameManager.TriggerLose()` (static) |
| `PlayerInventory` | `PlayerInventory.Instance` |
| `DemoObjectiveManager` | `DemoObjectiveManager.Instance` |

### Objective System

`DemoObjectiveManager` drives a linear sequence of objectives (by string ID): `crouch` → `peek` → `takedown` → `disguise` → `idcard` → `exit`. Call `DemoObjectiveManager.Instance.CompleteObjective("id")` from interactable scripts to advance the sequence.

### Animator Parameter Contract

Both the player and guard animators must expose these parameters (names are case-sensitive as written in code):

| Parameter | Type | Owner |
|---|---|---|
| `speed` | Float | Player & Guard |
| `Speed` | Float | Guard (alternate casing) |
| `MoveX`, `MoveY` | Float | Player |
| `isCrouching`, `isSprinting` | Bool | Player |
| `IsPeeking` | Bool | Player |
| `PeekDirection` | Int | Player |
| `IsGrounded` | Bool | Player |
| `Jump` | Trigger | Player |
| `Takedown`       | Trigger | Player & Guard — attacker role (initiating the takedown) |
| `TakedownVictim` | Trigger | Player & Guard — victim role (being taken down / caught). Uses the "Being Strangled" Mixamo clip. |
