---
name: unity-hdrp
description: Use when working with Unity HDRP — high-fidelity rendering, ray tracing, volumetric effects, subsurface scattering, physical light units, and HDRP-specific shader authoring. Triggers on HDRP setup, high-end desktop/console rendering, or photorealistic visuals.
---

# unity-hdrp

HDRP is Unity's high-fidelity pipeline for PC and console. Steep requirements; overkill for mobile/VR.

## When HDRP makes sense

- Photorealistic or high-production-value 3D.
- Target hardware: RTX 3060+ or console equivalent.
- Team has rendering engineers.
- You need raytracing, advanced volumetrics, or high-end PBR.

**Not for:** mobile, WebGL, VR (generally), 2D games.

## Core setup

- **HDRP Asset** — pipeline config.
- **HDRP Default Settings** — per-scene defaults.
- **Volume** system — post-processing, fog, sky.
- **Physical light units** — lumens, lux, nits (not intensity 0–1).
- **Camera** — use `HDCamera`, not `Camera`.

## Lighting

- **Physically-based** by default.
- **Sun** for directional, **Lights** for local.
- **Light Layers** for advanced lighting control.
- **Reflection Probes** or **Planar Reflections**.
- **Screen Space Reflections** for cheaper reflections.
- **Ray Traced Reflections** on RTX/console (expensive but beautiful).
- **Volumetric Fog** — expensive; use Low/Medium quality on mid hardware.
- **Ambient Occlusion** — Screen Space AO or Ray Traced AO.

## Materials

- **Lit** shader for PBR.
- **Subsurface Scattering** for skin, wax, marble.
- **Clear Coat** for car paint, varnish.
- **Iridescence** for soap bubbles, oil slicks.
- **Anisotropy** for brushed metal.

## Post-processing

- **Bloom** — physically-based by default.
- **Motion Blur** — camera + per-object.
- **Depth of Field** — filmic or physical.
- **Chromatic Aberration**, **Lens Distortion**, **Vignette**.
- **Color Grading** — LUTs or Tonemapping.
- **Tonemapping** — ACES default.

## Custom HDRP shaders

```hlsl
Shader "Custom/HDRPLit"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Base", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="ForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDRenderPipeline.hlsl"
            // ...
            ENDHLSL
        }
    }
}
```

HDRP shaders are more complex than URP — study the sample shaders in `Packages/com.unity.render-pipelines.high-definition/Runtime/Material/`.

## Ray tracing

- Requires DirectX 12 / Vulkan + RTX-capable GPU.
- Enable in HDRP Asset → Ray Tracing.
- RT Reflections, RT AO, RT Shadows, RT GI.
- Expensive; use for hero scenes, cutscenes, or high-end modes.
- Fallback to screen-space when RT unavailable.

## Performance

- **Volumetric Fog** is the #1 cost — cap quality.
- **SSR** cheaper than RT; use SSR as fallback.
- **Decals** — limit count, use clustered.
- **LODs + Culling** still essential.
- **Ray Tracing** — profile heavily; it's not free.
- **MSAA** — expensive at high resolutions; use TAA instead.
- **DLSS/FSR** support for upscaling.

## Anti-patterns

- HDRP on mobile/VR/WebGL
- 2D games on HDRP (no 2D renderer)
- Physical units ignored (1.0 intensity looks broken)
- Volumetric fog everywhere on mid hardware
- Ray tracing always-on without fallback
- No LODs because "the GPU can handle it"
- Missing RTX fallback for non-RTX GPUs
- Custom shaders without SRP Batcher compatibility audit
