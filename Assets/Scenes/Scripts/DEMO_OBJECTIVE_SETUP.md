# Docking Bay Demo Objective Setup

## 1) Add manager

1. Create an empty GameObject in the scene named `DemoObjectiveManager`.
2. Add the `DemoObjectiveManager` component.
3. Leave **Create Hud At Runtime** enabled.
4. Make sure your player has `CharacterInputController` and `PeekSystem`.

The default objective order is already configured:
1. crouch
2. peek
3. takedown
4. disguise
5. idcard
6. exit

## 2) Crouch objective trigger

1. Add a trigger collider behind your crates.
2. Add `ObjectiveTriggerZone`.
3. Set:
   - **Objective Id** = `crouch`
   - **Require Crouching** = `true`
   - **Require Peeking** = `false`

## 3) Peek objective trigger

1. Add a trigger collider at the corner where player should peek.
2. Add `ObjectiveTriggerZone`.
3. Set:
   - **Objective Id** = `peek`
   - **Require Crouching** = `false` (optional true if desired)
   - **Require Peeking** = `true`

## 4) Takedown / disguise / ID card interactions

For each step, place a small trigger collider where interaction should happen and add `ObjectiveInteractable`.

- Takedown target trigger:
  - **Objective Id** = `takedown`
- Disguise swap trigger:
  - **Objective Id** = `disguise`
- ID card pickup trigger:
  - **Objective Id** = `idcard`

Default interact key is `E`.

## 5) Exit door objective gate

On your existing exit door trigger object (`DoorProximityTrigger`):

- Enable **Require Objective**
- Set **Required Objective Id** = `exit`
- Leave **Complete Objective On Open** enabled

This causes the door to open only when `exit` is the current objective, and marks the final objective complete when the player reaches the door.

## 6) Player tag

Ensure the player root/collider uses tag `Player`, or update `playerTag` in trigger scripts.

## Optional quick simplification

If you want no explicit trigger for crouch/peek, keep defaults in `DemoObjectiveManager`:
- `crouch` auto-completes when player crouches.
- `peek` auto-completes when player peeks.

You can still keep the zone triggers to constrain where those mechanics count.
