---
name: unity-vfx-graph
description: Use when authoring GPU-driven particle effects with Unity VFX Graph — spawn, update, output contexts, GPU events, attributes, and performance. Triggers on VFX Graph, GPU particles, or large-scale effects that Particle System can't handle.
---

# unity-vfx-graph

VFX Graph runs on the GPU via compute shaders. Handles **millions** of particles where Particle System caps at ~10k.

## When to use VFX Graph vs Particle System

**VFX Graph:**
- 10k–10M+ particles.
- GPU-driven simulation.
- Complex behaviors, GPU events between systems.
- Advanced attributes (custom per-particle data).

**Particle System:**
- < 10k particles.
- Simple effects (muzzle flashes, small sparks).
- CPU-side logic (collisions with scripts, per-particle events).
- Mobile (VFX Graph has higher base cost).

## Node graph structure

- **Spawn** — controls when/how many particles spawn.
- **Initialize** — sets initial per-particle attributes.
- **Update** — per-frame simulation (velocity, life, color over time).
- **Output** — renders (particle, mesh, line, terrain, lit).

Connect **Blocks** (operations) to **Contexts** (Spawn, Initialize, Update, Output).

## Attributes

Per-particle data written in Initialize / Update, read in Output.

Common:
- `position`, `velocity`, `size`, `color`, `lifetime`, `age`.
- `alpha`, `rotation`, `scale`, `direction`.
- Custom: define in **Blackboard** (float, vector3, etc.).

## Blackboard properties

- **Exposed** to Inspector and C# (`VisualEffect.SetFloat`).
- Types: `float`, `int`, `Vector2/3/4`, `Gradient`, `Texture2D`, `Mesh`, `AnimationCurve`, `bool`.
- Use **Property Blocks** for sub-graph reuse.

## GPU Events

Trigger one system from another:

```
System A: Trigger Event "OnDeath" when particle dies
System B: OnEvent "OnDeath" → spawn particles at event position
```

Enables chains (fireworks → sparks → smoke).

## Spawn contexts

- **Constant Spawn Rate** — N per second.
- **Single Burst** — N once.
- **Periodic Burst** — N every X seconds.
- **Spawn Over Distance** — for motion trails.
- **Custom Spawn** — write your own Spawn block via HLSL or nodes.

## Output contexts

- **Output Particle** — quad, mesh, point.
- **Output Particle Lit** — with lighting.
- **Output Particle Mesh** — arbitrary mesh.
- **Output Particle Line** — line renderer style.
- **Output Particle Distortion** — screen-space distortion.

## Performance

- **Particle count** is the primary cost. Target: 100k–1M on desktop GPU, 10k–100k on mobile.
- **GPU cost** scales with count × complexity.
- **Overdraw** kills fillrate — small particles are cheap, big are not.
- **Soft particles** (depth fade) are expensive.
- **Collision** — CPU colliders are slow; GPU collision via SDF is faster.
- **Sorting** — depth sorting is expensive; disable if not needed.
- **Bounds** — set them; wrong bounds cause culling pop.
- **Batching** — multiple VFX Graph instances batch on the same asset.

## Mobile caveats

- VFX Graph on mobile is **supported but limited**.
- Compute shaders required; not on all devices (Vulkan/Metal recommended).
- Use **lower particle counts**, simpler shaders.
- **Particle System** is often the better choice on mobile.
- Test on target device before committing.

## Common patterns

**Fire:**
- Spawn over distance + constant rate.
- Initialize: velocity upward, lifetime 1–2s.
- Update: turbulence, color over age (yellow → orange → red → black).
- Output: additive lit mesh.

**Explosion:**
- Single burst, 500–5000.
- Initialize: velocity outward (sphere), random size.
- Update: drag, gravity.
- GPU Event on death → secondary smoke.
- Output: additive quad.

**Magic trail:**
- Spawn over distance.
- Inherit velocity from parent.
- Update: swirl noise, fade over lifetime.
- Output: additive quad with texture.

## Anti-patterns

- VFX Graph for < 1k particles (Particle System is cheaper)
- Wrong bounds → culling pop
- Overdraw explosion (huge soft particles)
- Real-time CPU collision on 100k particles
- Mobile without profiling (may not run at all)
- Not using GPU Events for chained systems
- Ignoring `exposedProperties` for C# control
- Firing every frame from code (use Trigger)
- No LOD or distance culling for far-away effects
