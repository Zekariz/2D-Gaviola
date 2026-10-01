---
name: unity-mobile-optimization
description: Use when optimizing or shipping Unity to mobile (iOS/Android) or VR (Quest, PSVR2, PCVR) — thermal budgets, resolution scaling, texture compression, IL2CPP, single-pass stereo, foveated rendering, and store requirements.
---

# unity-mobile-optimization

Mobile and VR are the tightest platforms. Every recommendation below assumes you've profiled on **target hardware**, not editor.

## Thermal reality

Modern phones throttle within **2–5 minutes** of sustained load. Benchmarks that look great for 30 seconds will collapse.

- Design for **sustained** performance, not peak.
- Budget **~4–6 W** sustained on flagship, ~2–3 W on mid-tier.
- Reduce quality when `SystemInfo.batteryLevel` is low or `Device.graphicsMultiThreaded` is false.
- Provide **Low/Medium/High** presets; auto-detect at first launch.

## Frame budgets

| Platform | Target | Budget |
|---|---|---|
| Mobile 30 FPS | Casual | 33 ms |
| Mobile 60 FPS | Action | 16.6 ms, aim ≤ 8 ms CPU |
| Quest 72 Hz | Base | 13.9 ms |
| Quest 90 Hz | Recommended | 11.1 ms |
| Quest 120 Hz | High | 8.3 ms |
| PSVR2 90/120 | — | 11.1 / 8.3 ms |

**Missed VR frames = nausea.** Fix drops before shipping.

## Rendering (URP mobile)

- **URP** with the **Mobile** or **Universal** renderer.
- **Forward** or **Forward+** (Forward+ for many lights, at cost).
- **Disable** real-time GI, SSAO, DoF, motion blur.
- **Bake** lighting; use light probes for dynamic objects.
- **Shadows:** hard only, short distance (20–30 m), or disable on low tier.
- **Post-processing:** bloom (cheap), color grading (cheap), vignette (cheap). Skip the rest.
- **Resolution scale** (URP Render Scale): 0.7–0.9 on mid, 0.6 on low.
- **MSAA** off or 2× on mobile; not free.
- **Occlusion culling** baked; verify in Frame Debugger.
- **LOD Groups** with per-platform bias.
- **SRP Batcher** on; audit for compatibility.
- **GPU Instancing** for repeated props.

## Textures

- **ASTC** for mobile (6×6 or 8×8 block size depending on budget).
- **ETC2** only for very old devices (pre-2017).
- **Never** uncompressed RGBA32 for large textures.
- **Mipmaps on** for anything seen at distance.
- **Streaming** for large textures (> 1K).
- **Read/Write off** unless CPU access needed.
- **Max size** 1024 or 2048; 4096+ only for hero assets.
- **Atlas** UI and sprites; ≤ 2048 per atlas on mobile.

## Meshes

- **Read/Write off.**
- **Vertex count** keep < 20k per mesh on low tier; < 50k on mid.
- **LOD chain** for anything > 2k triangles.
- **Combine** static meshes.
- **Skinning**: limit bone influences (2 on low, 4 on high).
- **Blend shapes** expensive on mobile — cap.

## Shaders

- **SRP Batcher compatible** (single `UnityPerMaterial` CBUFFER).
- **`half` precision** everywhere color/UV/lighting allows.
- **Strip variants** aggressively; use `shader_feature_local` over `multi_compile` when possible.
- **Avoid `discard`** on mobile (kills early-Z).
- **Avoid complex math** in fragment; precompute in vertex.
- **Mobile-specific shader** variants where the visuals allow.

## Audio

- **Vorbis** for music (streaming).
- **ADPCM** for short frequent SFX.
- **PCM** for tiny one-shots.
- **Compressed in memory** for medium SFX.
- **Streaming** for music, ambient.
- **Force to mono** for 3D positional SFX.
- **Sample rate** 22–44 kHz; 48 kHz only if needed.

## Code

- **IL2CPP** + **ARM64** (mandatory for stores).
- **Managed stripping level**: Medium or High; verify with `link.xml`.
- **Avoid `System.Threading`** on low-end; Job System is fine.
- **Job System + Burst** for CPU-heavy work.
- **Avoid `string` allocations** in hot paths.
- **Avoid reflection** at runtime; cache `MethodInfo`.
- **Async loading** — never block the main thread.
- **GC mode**: incremental; disable during intense gameplay with caution.

## Input

- **New Input System** with touch bindings.
- **On-screen controls** (virtual joystick, buttons) — UI Toolkit or uGUI.
- **Multi-touch** support if applicable.
- **Handedness** and reachability — don't put buttons under the thumb.
- **Safe area** handling for notched devices.

## VR specifics

- **Single-pass instanced rendering** (not multi-pass).
- **Fixed Foveated Rendering** on Quest (3 levels).
- **Application SpaceWarp / ASW** on supported devices.
- **Camera**: 72/90/120 Hz configurable; use `XRDisplaySubsystem`.
- **Comfort**: snap turn, teleport, vignette during motion.
- **Controller tracking**: XR Interaction Toolkit.
- **Hand tracking**: Quest 2/3 with controller fallback.
- **Passthrough / MR**: AR Foundation.
- **Memory**: Quest 2/3 share ~4–6 GB with OS; be aggressive.

## Platform store requirements

**iOS:**
- 64-bit only (A11+ required for some features).
- ATS (HTTPS) for network.
- Permission strings for camera/mic/photos.
- Launch screen + app icons per size.
- Privacy manifest (since 2024).

**Android:**
- 64-bit required.
- Target API level (current Play requirement).
- Privacy policy link.
- App bundle (`.aab`) for Play Store.
- Data safety form.

**Quest:**
- Meta SDK compliance.
- Store assets + trailer.
- Comfort ratings (motion sickness).
- Performance tier validation.

## Anti-patterns

- Assuming editor ≈ device
- Peak benchmarks without sustained thermal testing
- HDRP on mobile
- Real-time shadows/GI on mobile
- Multi-pass stereo in VR
- Missing IL2CPP/ARM64 (store rejection)
- No resolution scaling
- Missing safe area handling (notched phones)
- Uncompressed textures or no mipmaps
- Heavy shader variants shipping
- Ignoring battery/power state
- Relying on reflection at runtime
- Missing privacy strings (iOS rejection)
