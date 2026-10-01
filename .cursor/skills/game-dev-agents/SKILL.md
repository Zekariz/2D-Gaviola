---
name: game-dev-agents
description: Use for Unity game development tasks — routes to specialized agents for gameplay, performance, rendering, netcode, DOTS, 2D, save systems, tools, QA, and platform work. Triggers on any Unity / game-dev task.
---

# game-dev-agents

See `agents/game-dev/AGENTS.md` for the full roster, routing table, and guardrails.

## Quick routing

| Task | Agent |
|---|---|
| Gameplay code | gameplay-engineer |
| Profiling / GC | performance-engineer |
| Shaders / URP | rendering-engineer |
| Multiplayer | netcode-engineer |
| Editor tools / CI | tools-engineer |
| Tests | qa-engineer |
| Platform builds | platform-engineer |
| Architecture | architect |
| Save systems | save-systems-engineer |
| DOTS / ECS / Jobs / Burst | dots-engineer |
| 2D / sprites / tilemap / 2D physics | 2d-engineer |

## Defaults

- Unity LTS, URP, New Input System, UniTask, Addressables, C#.
- Allocation-free hot paths.
- Profile on target hardware.
- Never trust the client.
- Version save formats.

Ask before executing: platform, frame budget, scale, netcode, stage.
