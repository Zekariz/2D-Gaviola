using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace YourGame.Editor
{
    /// <summary>
    /// Builds (or rebuilds) all Player animation clips and the Animator Controller
    /// from the two sprite sheets.
    ///
    /// Menu: Tools -> 2D-Gaviola -> Animations -> Build All Player Animations and Controller
    ///
    /// Safe to re-run — existing clips are updated in place, existing states are
    /// removed and recreated cleanly so no duplicates appear.
    /// </summary>
    public static class PlayerAnimatorBuilder
    {
        // ── Paths ─────────────────────────────────────────────────────────────
        private const string ForwardSheetPath  = "Assets/Sprites/Characters/SpriteSheet2D.png";
        private const string BackwardSheetPath = "Assets/Sprites/Characters/SpriteSheet2D-Backwards.png";
        private const string OutputDir         = "Assets/Animations/Player";
        private const string ControllerPath    = "Assets/Animations/Player/Player.controller";

        // The Animator clips animate a SpriteRenderer that is on a CHILD
        // GameObject named "Sprite". This path must exactly match that child's
        // name in the hierarchy, or the curves will silently target nothing.
        // If your child is named differently (e.g. "Visual"), change this.
        private const string SpriteChildPath = "Sprite";

        // Fall clip reuses the last frame of the jump_right clip (frame index 43).
        private const int FallFrameIndex = 43;

        // ── Entry Point ───────────────────────────────────────────────────────
        [MenuItem("Tools/2D-Gaviola/Animations/Build All Player Animations and Controller")]
        public static void Build()
        {
            // 1. Load + numerically sort sprites
            List<Sprite> fwd = LoadSprites(ForwardSheetPath);
            List<Sprite> bwd = LoadSprites(BackwardSheetPath);

            if (fwd.Count == 0 || bwd.Count == 0)
            {
                Debug.LogError(
                    "[PlayerAnimatorBuilder] Could not load sprites. " +
                    "Verify both sheet paths and that they are sliced as Multiple.");
                return;
            }

            Debug.Log($"[PlayerAnimatorBuilder] Loaded {fwd.Count} forward frames, {bwd.Count} backward frames.");

            // 2. Ensure output folder
            EnsureFolder("Assets", "Animations");
            EnsureFolder("Assets/Animations", "Player");

            // 3. Build Animation Clips
            //    Arguments: (clip name, sprite list, inclusive start index, inclusive end index, loop)
            AnimationClip idle      = MakeClip("player_idle",       fwd,  4, 13, loop: true);
            AnimationClip walkRight = MakeClip("player_walk_right", fwd, 14, 23, loop: true);
            AnimationClip walkLeft  = MakeClip("player_walk_left",  bwd,  0,  9, loop: true);
            AnimationClip runRight  = MakeClip("player_run_right",  fwd, 24, 33, loop: true);
            AnimationClip runLeft   = MakeClip("player_run_left",   bwd, 10, 19, loop: true);
            AnimationClip jumpRight = MakeClip("player_jump_right", fwd, 34, 43, loop: false);
            AnimationClip jumpLeft  = MakeClip("player_jump_left",  bwd, 20, 29, loop: false);

            // Fall: single static frame — the last frame of player_jump_right
            AnimationClip fall = MakeSingleFrameClip("player_fall", fwd, FallFrameIndex);

            // 4. Build Animator Controller
            BuildController(idle, walkRight, walkLeft, runRight, runLeft, jumpRight, jumpLeft, fall);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[PlayerAnimatorBuilder] Done. 8 clips created/updated, " +
                "8 states and all transitions wired in Player.controller.");
        }

        // ── Controller Builder ────────────────────────────────────────────────
        private static void BuildController(
            AnimationClip idle,
            AnimationClip walkRight, AnimationClip walkLeft,
            AnimationClip runRight,  AnimationClip runLeft,
            AnimationClip jumpRight, AnimationClip jumpLeft,
            AnimationClip fall)
        {
            // Load or create the controller asset
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // ── Parameters (rebuild from scratch for idempotency) ──
            // Copy the current list before iterating to avoid mutation during iteration
            foreach (AnimatorControllerParameter p in ctrl.parameters.ToArray())
                ctrl.RemoveParameter(p);

            ctrl.AddParameter("speed",       AnimatorControllerParameterType.Float);
            ctrl.AddParameter("isGrounded",  AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("velocityY",   AnimatorControllerParameterType.Float);
            ctrl.AddParameter("isRunning",   AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("facingLeft",  AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("jumpTrigger", AnimatorControllerParameterType.Trigger);

            // ── States (clear all, then re-add) ──
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            foreach (ChildAnimatorState cs in sm.states.ToArray())
                sm.RemoveState(cs.state);

            // Spread states visually so the Animator window is readable
            AnimatorState sIdle      = AddState(sm, "Idle",      idle,      new Vector3(  0,   0));
            AnimatorState sWalkRight = AddState(sm, "WalkRight", walkRight, new Vector3(300, -80));
            AnimatorState sWalkLeft  = AddState(sm, "WalkLeft",  walkLeft,  new Vector3(300,  80));
            AnimatorState sRunRight  = AddState(sm, "RunRight",  runRight,  new Vector3(600, -80));
            AnimatorState sRunLeft   = AddState(sm, "RunLeft",   runLeft,   new Vector3(600,  80));
            AnimatorState sJumpRight = AddState(sm, "JumpRight", jumpRight, new Vector3(300,-200));
            AnimatorState sJumpLeft  = AddState(sm, "JumpLeft",  jumpLeft,  new Vector3(300, 200));
            AnimatorState sFall      = AddState(sm, "Fall",      fall,      new Vector3(600,-200));

            sm.defaultState = sIdle;

            // ── Transitions ───────────────────────────────────────────────────
            // All transitions: hasExitTime = false, duration = 0 (instant).
            // Conditions follow the spec table exactly.

            // Any -> JumpRight  (jumpTrigger AND facing right)
            // Two conditions together prevent the trigger from re-firing every frame
            AnyTransition(sm, sJumpRight)
                .Cond(AnimatorConditionMode.If,    0f,   "jumpTrigger")
                .Cond(AnimatorConditionMode.IfNot, 0f,   "facingLeft");

            // Any -> JumpLeft   (jumpTrigger AND facing left)
            AnyTransition(sm, sJumpLeft)
                .Cond(AnimatorConditionMode.If,    0f,   "jumpTrigger")
                .Cond(AnimatorConditionMode.If,    0f,   "facingLeft");

            // JumpRight/Left -> Fall  (when apex is passed, velocityY goes negative)
            Trans(sJumpRight, sFall).Cond(AnimatorConditionMode.Less, 0f, "velocityY");
            Trans(sJumpLeft,  sFall).Cond(AnimatorConditionMode.Less, 0f, "velocityY");

            // Fall -> Idle       (grounded and not moving)
            Trans(sFall, sIdle)
                .Cond(AnimatorConditionMode.If,      0f,   "isGrounded")
                .Cond(AnimatorConditionMode.Less,    0.1f, "speed");

            // Fall -> Walk/Run   (grounded and moving — land into motion)
            Trans(sFall, sWalkRight)
                .Cond(AnimatorConditionMode.If,      0f,   "isGrounded")
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "isRunning")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "facingLeft");

            Trans(sFall, sWalkLeft)
                .Cond(AnimatorConditionMode.If,      0f,   "isGrounded")
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "isRunning")
                .Cond(AnimatorConditionMode.If,      0f,   "facingLeft");

            Trans(sFall, sRunRight)
                .Cond(AnimatorConditionMode.If,      0f,   "isGrounded")
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.If,      0f,   "isRunning")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "facingLeft");

            Trans(sFall, sRunLeft)
                .Cond(AnimatorConditionMode.If,      0f,   "isGrounded")
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.If,      0f,   "isRunning")
                .Cond(AnimatorConditionMode.If,      0f,   "facingLeft");

            // Idle -> Walk/Run
            Trans(sIdle, sWalkRight)
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "facingLeft")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "isRunning");

            Trans(sIdle, sWalkLeft)
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.If,      0f,   "facingLeft")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "isRunning");

            Trans(sIdle, sRunRight)
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.IfNot,   0f,   "facingLeft")
                .Cond(AnimatorConditionMode.If,      0f,   "isRunning");

            Trans(sIdle, sRunLeft)
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed")
                .Cond(AnimatorConditionMode.If,      0f,   "facingLeft")
                .Cond(AnimatorConditionMode.If,      0f,   "isRunning");

            // Walk -> Idle
            Trans(sWalkRight, sIdle).Cond(AnimatorConditionMode.Less, 0.1f, "speed");
            Trans(sWalkLeft,  sIdle).Cond(AnimatorConditionMode.Less, 0.1f, "speed");

            // Walk <-> Run
            Trans(sWalkRight, sRunRight).Cond(AnimatorConditionMode.If,    0f, "isRunning");
            Trans(sWalkLeft,  sRunLeft ).Cond(AnimatorConditionMode.If,    0f, "isRunning");
            Trans(sRunRight,  sWalkRight)
                .Cond(AnimatorConditionMode.IfNot,   0f,   "isRunning")
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed");
            Trans(sRunLeft,   sWalkLeft)
                .Cond(AnimatorConditionMode.IfNot,   0f,   "isRunning")
                .Cond(AnimatorConditionMode.Greater, 0.1f, "speed");

            // Run -> Idle
            Trans(sRunRight, sIdle).Cond(AnimatorConditionMode.Less, 0.1f, "speed");
            Trans(sRunLeft,  sIdle).Cond(AnimatorConditionMode.Less, 0.1f, "speed");

            // Direction swaps mid-movement (no need to stop and change direction)
            Trans(sWalkRight, sWalkLeft).Cond(AnimatorConditionMode.If,    0f, "facingLeft");
            Trans(sWalkLeft,  sWalkRight).Cond(AnimatorConditionMode.IfNot, 0f, "facingLeft");
            Trans(sRunRight,  sRunLeft).Cond(AnimatorConditionMode.If,    0f, "facingLeft");
            Trans(sRunLeft,   sRunRight).Cond(AnimatorConditionMode.IfNot, 0f, "facingLeft");

            EditorUtility.SetDirty(ctrl);
        }

        // ── Clip Builders ─────────────────────────────────────────────────────
        private static AnimationClip MakeClip(
            string clipName, List<Sprite> sprites, int startIdx, int endIdx, bool loop)
        {
            string assetPath = $"{OutputDir}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            clip.frameRate = 12f;

            // Set loop flag
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Clamp to actual sprite count in case the sheet has fewer frames than expected
            startIdx = Mathf.Clamp(startIdx, 0, sprites.Count - 1);
            endIdx   = Mathf.Clamp(endIdx,   0, sprites.Count - 1);
            int frameCount = Mathf.Abs(endIdx - startIdx) + 1;

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    // time is in seconds; dividing by frameRate gives equal spacing
                    time  = i / clip.frameRate,
                    value = sprites[startIdx + i]
                };
            }

            // Bind to the SpriteRenderer on the child named SpriteChildPath.
            // If your Sprite child is named differently (e.g. "Visual" or "Art"),
            // change the SpriteChildPath constant at the top of this file.
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type         = typeof(SpriteRenderer),
                path         = SpriteChildPath,
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip MakeSingleFrameClip(string clipName, List<Sprite> sprites, int frameIdx)
        {
            // A single-frame non-looping clip effectively freezes on that frame forever
            string assetPath = $"{OutputDir}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            clip.frameRate = 12f;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            frameIdx = Mathf.Clamp(frameIdx, 0, sprites.Count - 1);

            ObjectReferenceKeyframe[] keys =
            {
                new ObjectReferenceKeyframe { time = 0f, value = sprites[frameIdx] }
            };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type         = typeof(SpriteRenderer),
                path         = SpriteChildPath,
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        // ── Sprite Loader (numeric sort) ──────────────────────────────────────
        private static List<Sprite> LoadSprites(string path)
        {
            // LoadAllAssetsAtPath returns both the Texture2D and each Sprite slice.
            // OfType<Sprite>() discards the Texture2D root object.
            // OrderBy uses numeric sort — NOT string sort — so frame 10 comes after 9.
            List<Sprite> sprites = AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => ExtractTrailingInt(s.name))
                .ToList();

            return sprites;
        }

        /// <summary>
        /// Parses the integer after the last '_' in a sprite name.
        /// e.g. "SpriteSheet2D_10" -> 10, "SpriteSheet2D-Backwards_3" -> 3.
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

        // ── Transition Builder DSL ────────────────────────────────────────────
        // Tiny fluent wrapper so the wiring code above stays readable.
        private struct TransitionBuilder
        {
            private AnimatorStateTransition _t;

            internal TransitionBuilder(AnimatorStateTransition t)
            {
                _t             = t;
                _t.hasExitTime = false;
                _t.duration    = 0f;
                _t.offset      = 0f;
            }

            internal TransitionBuilder Cond(AnimatorConditionMode mode, float threshold, string param)
            {
                _t.AddCondition(mode, threshold, param);
                return this;
            }
        }

        private static TransitionBuilder Trans(AnimatorState from, AnimatorState to)
        {
            return new TransitionBuilder(from.AddTransition(to));
        }

        private static TransitionBuilder AnyTransition(AnimatorStateMachine sm, AnimatorState to)
        {
            return new TransitionBuilder(sm.AddAnyStateTransition(to));
        }

        // ── Folder Helper ─────────────────────────────────────────────────────
        private static void EnsureFolder(string parent, string child)
        {
            string full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }

        // ── State Helper ──────────────────────────────────────────────────────
        private static AnimatorState AddState(
            AnimatorStateMachine sm, string stateName, AnimationClip clip, Vector3 position)
        {
            AnimatorState state = sm.AddState(stateName, position);
            state.motion = clip;
            return state;
        }
    }
}
