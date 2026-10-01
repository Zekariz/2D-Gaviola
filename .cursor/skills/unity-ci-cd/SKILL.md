---
name: unity-ci-cd
description: Use when setting up Unity build automation — GitHub Actions with GameCI, GitLab CI, Azure Pipelines, Addressables build pipeline, per-platform matrix builds, caching, license activation, and artifact management.
---

# unity-ci-cd

Unity CI is slow without caching and license handling done right. Set these up first, then build pipelines.

## Fundamentals

- **Batchmode always**: `-batchmode -nographics -quit -logFile -`.
- **License**: Personal (file) or Pro (server, floating).
- **Cache**: `Library/`, `~/.cache/unity3d`, package manager cache.
- **Symbols**: upload for IL2CPP builds so crashes are readable.
- **Artifacts**: builds, test reports (JUnit XML), player logs on failure.
- **Matrix**: build platforms in parallel.

## GitHub Actions + GameCI

**`.github/workflows/build.yml`:**

```yaml
name: Build

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

jobs:
  test-editmode:
    name: EditMode Tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true

      - uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-

      - uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          testMode: EditMode
          artifactsPath: editmode-results
          githubToken: ${{ secrets.GITHUB_TOKEN }}

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: editmode-results
          path: editmode-results

  build:
    name: Build for ${{ matrix.targetPlatform }}
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        targetPlatform:
          - StandaloneWindows64
          - StandaloneLinux64
          - WebGL

    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true

      - uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ matrix.targetPlatform }}-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-${{ matrix.targetPlatform }}-
            Library-

      - uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          targetPlatform: ${{ matrix.targetPlatform }}
          buildMethod: BuildScript.BuildCI

      - uses: actions/upload-artifact@v4
        with:
          name: Build-${{ matrix.targetPlatform }}
          path: build
```

## License activation (Personal)

1. Get your Unity license `.ulf` file (from Unity Hub → Preferences → Licenses).
2. `base64 -w 0 Unity_v20XX.x.ulf > license.txt`
3. Add `UNITY_LICENSE` = base64 content of `license.txt`.
4. Add `UNITY_EMAIL`, `UNITY_PASSWORD`.

**Pro/Enterprise**: use Unity License Server URL instead.

## Build script

```csharp
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildCI()
    {
        var args = Environment.GetCommandLineArgs();
        string output = GetArg(args, "-customBuildPath") ?? "build";
        string targetStr = GetArg(args, "-buildTarget") ?? "StandaloneWindows64";
        var target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr);

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = GetOutputPath(output, target),
            target = target,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    private static string GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    private static string GetOutputPath(string dir, BuildTarget target) => target switch
    {
        BuildTarget.StandaloneWindows64 => $"{dir}/Game.exe",
        BuildTarget.StandaloneLinux64   => $"{dir}/Game.x86_64",
        BuildTarget.StandaloneOSX       => $"{dir}/Game.app",
        BuildTarget.Android             => $"{dir}/Game.apk",
        BuildTarget.WebGL               => dir,
        _                               => $"{dir}/Game",
    };
}
```

## Addressables content build

```csharp
// Editor/CIContentBuild.cs
public static class CIContentBuild
{
    public static void BuildContent()
    {
        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error))
            throw new Exception(result.Error);
    }
}
```

Run **before** player build in CI. Then player build picks up built content.

## Caching strategy

**Cache:**
- `Library/` — Unity's import cache. **Massive win** (10–20×).
- `~/.cache/unity3d` — Unity's download cache.

**Key by:** project hash + Unity version + platform.

**Restore-keys** for partial cache hits.

**Never cache:** `Temp/`, `Logs/`, `obj/`, `Build/`.

## Secrets reference

| Secret | Purpose |
|---|---|
| `UNITY_LICENSE` | Base64 of `.ulf` file |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `ANDROID_KEYSTORE_*` | Android signing |
| `APPLE_*` | iOS provisioning |
| `SENTRY_AUTH_TOKEN` | Symbol upload |

## Build time expectations

| Project size | Cold build | Cached build |
|---|---|---|
| Small (jam) | 5–10 min | 1–2 min |
| Medium | 15–30 min | 3–8 min |
| Large | 45–90 min | 10–20 min |

Cache cuts 60–80%.

## Anti-patterns

- No cache (5–10× slower)
- Building without `-batchmode` / `-nographics`
- Committing `Library/` to git
- No license secret (activation fail)
- Serial builds when matrix works
- No test job before build
- Missing symbol upload (unreadable crashes)
- iOS build on Linux runner (impossible)
- Storing secrets in repo
- `fail-fast: true` for multi-platform jobs
- No Addressables content build before player build
