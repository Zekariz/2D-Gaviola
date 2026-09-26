using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YourGame.Editor
{
    /// <summary>
    /// Generates 10 new player animation clips from the new per-action sprite sheets.
    ///
    /// Menu: Tools -> 2D-Gaviola -> Animation -> Build New Animation Clips
    ///
    /// Safe to re-run (idempotent). Clips are overwritten in-place (GUID preserved).
    /// Nothing is deleted. Old clips are left untouched. The Animator Controller
    /// is not modified -- that is Milestone 3.
    /// </summary>
    public static class BuildNewAnimationClips
    {
        // ── Constants ─────────────────────────────────────────────────────────

        private const string OutputDir = "Assets/Animations/Player";

        // The binding path must match the name of the child GameObject that
        // holds the SpriteRenderer in the Player prefab hierarchy.
        // Confirmed from PlayerAnimatorBuilder.cs: the child is named "Sprite".
        private const string SpriteChildPath = "Sprite";

        // 4 FPS = 4 frames over 1 second, 0.25 s per frame.
        private const float FrameRate = 4f;

        // ── Sheet-to-clip mapping ─────────────────────────────────────────────

        private struct ClipSpec
        {
            public string SheetPath;   // asset path to the sprite sheet
            public string ClipName;    // output .anim name (without extension)
            public bool   Loop;        // true = loop, false = hold last frame
        }

        private static readonly ClipSpec[] _specs =
        {
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/idle1-yawn-l.png",   ClipName = "player_idle_yawn_left",    Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/idle1-yawn-r.png",   ClipName = "player_idle_yawn_right",   Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/idle2-guitar-l.png", ClipName = "player_idle_guitar_left",  Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/idle2-guitar-r.png", ClipName = "player_idle_guitar_right", Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/walk-l.png",         ClipName = "player_walk_left",         Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/walk-r.png",         ClipName = "player_walk_right",        Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/sprint-l.png",       ClipName = "player_sprint_left",       Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/sprint-r.png",       ClipName = "player_sprint_right",      Loop = true  },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/jump-l.png",         ClipName = "player_jump_left",         Loop = false },
            new ClipSpec { SheetPath = "Assets/Sprites/Characters/jump-r.png",         ClipName = "player_jump_right",        Loop = false },
        };

        // ── Entry Point ───────────────────────────────────────────────────────

        [MenuItem("Tools/2D-Gaviola/Animation/Build New Animation Clips")]
        public static void Build()
        {
            // Ensure the output folder exists
            EnsureFolder("Assets", "Animations");
            EnsureFolder("Assets/Animations", "Player");

            int created  = 0;
            int failed   = 0;
            var failures = new List<string>();

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (ClipSpec spec in _specs)
                {
                    bool ok = BuildClip(spec);
                    if (ok) { created++; }
                    else    { failed++; failures.Add(spec.ClipName); }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── Verification pass ─────────────────────────────────────────────
            Debug.Log("[BuildNewAnimationClips] -- Verification Pass --");
            int verified = 0;

            foreach (ClipSpec spec in _specs)
            {
                string assetPath = OutputDir + "/" + spec.ClipName + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

                if (clip == null)
                {
                    Debug.LogWarning("[BuildNewAnimationClips] " + spec.ClipName + ": FAIL -- asset not found at " + assetPath);
                    continue;
                }

                // Check frameRate
                bool rateOk = Mathf.Approximately(clip.frameRate, FrameRate);

                // Check loop setting
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                bool loopOk = (clipSettings.loopTime == spec.Loop);

                // Check m_Sprite curve keyframe count
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    type         = typeof(SpriteRenderer),
                    path         = SpriteChildPath,
                    propertyName = "m_Sprite"
                };
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                bool keysOk = (keys != null && keys.Length == 4);

                string loopLabel = spec.Loop ? "loop=yes" : "loop=no";
                string status    = (rateOk && loopOk && keysOk) ? "OK" : "FAIL";

                if (status == "OK")
                {
                    Debug.Log(
                        "[BuildNewAnimationClips] " + spec.ClipName +
                        ": 4 frames, 4 FPS, " + loopLabel + " OK");
                    verified++;
                }
                else
                {
                    string reason = "";
                    if (!rateOk)  reason += " frameRate=" + clip.frameRate + " (want 4)";
                    if (!loopOk)  reason += " loopTime=" + clipSettings.loopTime + " (want " + spec.Loop + ")";
                    if (!keysOk)  reason += " keyframes=" + (keys != null ? keys.Length.ToString() : "null") + " (want 4)";
                    Debug.LogWarning("[BuildNewAnimationClips] " + spec.ClipName + ": FAIL --" + reason);
                }
            }

            Debug.Log(
                "[BuildNewAnimationClips] Done. " +
                created  + "/" + _specs.Length + " clips created/updated, " +
                verified + "/" + _specs.Length + " verified. " +
                (failed > 0 ? "FAILURES: " + string.Join(", ", failures) : "No failures."));
        }

        // ── Clip Builder ──────────────────────────────────────────────────────

        private static bool BuildClip(ClipSpec spec)
        {
            // Load sprites from the sheet -- numeric sort on trailing _N index
            List<Sprite> sprites = AssetDatabase
                .LoadAllAssetsAtPath(spec.SheetPath)
                .OfType<Sprite>()
                .OrderBy(s => ExtractTrailingInt(s.name))
                .ToList();

            if (sprites.Count == 0)
            {
                Debug.LogError(
                    "[BuildNewAnimationClips] No sprites loaded from: " + spec.SheetPath + "\n" +
                    "Ensure Milestone 1 ran successfully (sheet sliced into Multiple sprites).");
                return false;
            }

            if (sprites.Count != 4)
            {
                Debug.LogWarning(
                    "[BuildNewAnimationClips] " + Path.GetFileName(spec.SheetPath) +
                    ": expected 4 sprites, found " + sprites.Count + ". Continuing with what is available.");
            }

            // Load existing clip to preserve GUID (idempotent), or create new
            string assetPath = OutputDir + "/" + spec.ClipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            bool isNew = (clip == null);
            if (isNew)
            {
                clip = new AnimationClip();
                // CreateAsset must be called outside StartAssetEditing for new assets.
                // We call StopAssetEditing / StartAssetEditing around this in Build(),
                // but individual clip creation needs the DB unlocked.
                // Workaround: we create the asset here; Unity 6 allows this mid-batch
                // because StartAssetEditing only defers reimport, not asset creation.
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            // Frame rate -- MUST be set before computing keyframe times
            clip.frameRate = FrameRate;

            // Loop flag
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = spec.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Build keyframes: 4 frames at t = 0.0, 0.25, 0.5, 0.75 seconds
            int frameCount = sprites.Count;
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    // 1 / FrameRate = 0.25 s per frame; exact rational avoids float drift
                    time  = i / FrameRate,
                    value = sprites[i]
                };
            }

            // Bind to the SpriteRenderer.m_Sprite property on the child "Sprite".
            // "Sprite" is confirmed as the SpriteRenderer child name in PlayerAnimatorBuilder.cs.
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type         = typeof(SpriteRenderer),
                path         = SpriteChildPath,
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            EditorUtility.SetDirty(clip);

            Debug.Log(
                "[BuildNewAnimationClips] [" + spec.ClipName + "] " +
                "source=" + Path.GetFileName(spec.SheetPath) + " " +
                "frames=" + frameCount + " " +
                "FPS=" + FrameRate + " " +
                "loop=" + spec.Loop + " " +
                (isNew ? "(created)" : "(overwritten)"));

            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Parses the integer after the last '_' in a sprite name.
        /// e.g. "walk-l_2" -> 2, "idle1-yawn-l_0" -> 0.
        /// Returns int.MaxValue on failure so malformed names sort to the end.
        /// </summary>
        private static int ExtractTrailingInt(string name)
        {
            int idx = name.LastIndexOf('_');
            if (idx >= 0 && idx < name.Length - 1)
            {
                if (int.TryParse(name.Substring(idx + 1), out int n))
                    return n;
            }

            return int.MaxValue;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}