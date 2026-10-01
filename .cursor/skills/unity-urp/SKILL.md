---
name: unity-urp
description: Use when working with Unity URP — renderer features, ScriptableRendererFeature, custom passes, forward vs forward+, deferred, render scale, shader library, and URP-specific optimizations. Triggers on URP setup, custom passes, or URP rendering issues.
---

# unity-urp

URP is the default pipeline for mobile, VR, and most 2D/3D games. HDRP is for high-fidelity desktop/console.

## When URP vs HDRP

| Use URP | Use HDRP |
|---|---|
| Mobile, WebGL, VR | High-fidelity desktop/console |
| Stylized, toon, low-poly | Photorealistic, raytracing |
| Broad hardware target | Beefy GPU requirement OK |
| 2D games | 3D only (HDRP has no 2D) |

Never mix URP shaders with HDRP scenes — different lighting model.

## Renderer setup

- **URP Asset** — pipeline-wide settings.
- **Renderer Data** (Universal Renderer / 2D Renderer) — per-quality-level pass list.
- **Multiple Renderers** for different quality tiers or scene types.
- Assign via **Project Settings → Graphics** + **Quality**.

## Forward vs Forward+ vs Deferred

- **Forward** — default, low-end friendly, limited per-object lights.
- **Forward+** — many lights without per-object limit, higher base cost. Good for URP on mid/high.
- **Deferred** — many lights cheap, expensive on mobile/low-end. Good for desktop.

Pick per platform.

## ScriptableRendererFeature

Custom passes:

```csharp
public sealed class MyFeature : ScriptableRendererFeature
{
    private MyPass _pass;

    public override void Create()
    {
        _pass = new MyPass(RenderPassEvent.AfterRenderingOpaques);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
    {
        renderer.EnqueuePass(_pass);
    }

    private sealed class MyPass : ScriptableRenderPass
    {
        public MyPass(RenderPassEvent evt) { renderPassEvent = evt; }

        public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
        {
            // CommandBuffer work here
        }
    }
}
```

**RenderPassEvent** options: `BeforeRendering`, `AfterRenderingOpaques`, `AfterRenderingTransparents`, `AfterRenderingPostProcessing`, etc.

**Use for:** outlines, custom blits, depth-based effects, custom fog, water.

## Custom blit

```csharp
var cmd = CommandBufferPool.Get("MyBlit");
Blitter.BlitCameraTexture(cmd, src, dst, _material, 0);
ctx.ExecuteCommandBuffer(cmd);
CommandBufferPool.Release(cmd);
```

Avoid `cmd.Blit` (older API). Use `Blitter` (faster, cleaner).

## Render scale

`UniversalRenderPipelineAsset.renderScale` — 0.5–1.0. Reduces internal resolution before upscale. Cheap way to save fillrate on mobile.

Set per quality tier:
- Low: 0.6
- Medium: 0.8
- High: 1.0

## SRP Batcher

**Compatible if:**
- All per-material props in a single `CBUFFER_START(UnityPerMaterial)`.
- Same CBUFFER layout across all passes.
- No `MaterialPropertyBlock` on the renderer.

**Check:** Frame Debugger shows "SRP Batch" count; breaks are flagged.

## Shader library

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
```

Key functions:
- `TransformObjectToHClip(float3 positionOS)` → clip-space position.
- `TransformObjectToWorld(float3 positionOS)` → world position.
- `GetMainLight()` → main directional light.
- `GetAdditionalLight(i, worldPos)` → additional lights.

## 2D Renderer

- **URP 2D Renderer** for 2D games with 2D lighting.
- Uses **Light 2D** components, **Shadow Caster 2D**.
- Not compatible with the Universal (3D) renderer.
- Pick one — cannot have both in the same renderer data.

## Anti-patterns

- URP shaders in HDRP project (or vice versa)
- `MaterialPropertyBlock` on SRP Batcher-enabled shaders
- Per-material props outside `UnityPerMaterial` CBUFFER
- Deferred on mobile
- Forward+ without profiling
- Missing render scale on mobile
- Custom passes without Frame Debugger validation
- Blitting per-frame without pooling
- Too many `#pragma multi_compile` variants
