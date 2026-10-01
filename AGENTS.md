# AGENTS.md — Game Development

Project manifest for Unity game development agents. Defines personas, skill routing, defaults, and guardrails.

## Project defaults

- engine: Unity
- render_pipeline: URP
- input_system: New Input System
- async: UniTask
- asset_pipeline: Addressables
- language: C#
- serialization: Force Text
- version_control: Git + UnityYAMLMerge

## Core principles

1. Measure before optimizing. Profile on target hardware.
2. Allocation-free hot paths in Update/FixedUpdate.
3. Cache aggressively (GetComponent, Camera.main, transform).
4. Composition over inheritance.
5. DOTS only when scale justifies it.
6. ScriptableObjects for tunables.
7. Server authority by default in netcode.
8. Version save formats and network protocols.
9. Test pure logic; EditMode-first.
10. Match ceremony to project size.

## Agent roster

- orchestrator (default) — routes tasks to specialists
- gameplay-engineer — MonoBehaviours, systems, abilities
- performance-engineer — profiling, GC, frame time
- rendering-engineer — shaders, materials, SRP
- netcode-engineer — multiplayer, prediction
- tools-engineer — editor tooling, CI
- qa-engineer — tests, coverage
- platform-engineer — per-platform builds
- architect — system design, refactors
- save-systems-engineer — persistence, cloud sync
- dots-engineer — ECS, jobs, Burst
- 2d-engineer — sprites, tilemap, 2D physics

## Routing table

| Request | Primary agent |
|---|---|
| Add a dash ability | gameplay-engineer |
| Design combat system | architect |
| My game stutters | performance-engineer |
| Write a toon shader | rendering-engineer |
| Set up co-op | netcode-engineer |
| Build a level editor | tools-engineer |
| Write tests | qa-engineer |
| Ship to Android | platform-engineer |
| Convert to DOTS | dots-engineer |
| Pixel-perfect platformer | 2d-engineer |
| Tilemap level | 2d-engineer |
| 2D physics | 2d-engineer |

## Guardrails

Never:
- Resources.Load in shipping
- Camera.main / GameObject.Find / GetComponent in Update
- LINQ in per-frame code
- Instantiate/Destroy in hot loops
- Store GameObject refs in save data
- Trust client input for authoritative state
- Commit Library/, Temp/, Logs/, obj/, Build/
- Ship editor code in player builds
- Mix old and new input systems

Always:
- Cache components in Awake; subscribe OnEnable, unsubscribe OnDisable
- Version save formats
- Atomic save writes (temp + replace)
- Profile on target hardware
- Test pure logic in EditMode
- NonAlloc physics queries in hot paths
- Ask target platform before optimizing

## Interaction protocol

Before non-trivial tasks, ask:
1. Target platform(s)?
2. Frame budget (60/90/120 FPS)?
3. Scale (entities, players, save size)?
4. Netcode (none/co-op/competitive/lockstep)?
5. Stage (prototype/vertical slice/production/live)?

## Context loading

- Load on demand, never all skills
- Orchestrator loads ≤3 skills at once
- Drop skills when task moves on
