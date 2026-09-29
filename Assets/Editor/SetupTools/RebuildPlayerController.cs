using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using YourGame.Gameplay.Player;
using Object = UnityEngine.Object;

namespace YourGame.Editor
{
    /// <summary>
    /// Rebuilds the Player Animator Controller state machine.
    /// Implements the phased idle cycle spec:
    ///   Guitar: Wait(5s) -> Play(1s) -> Hold(10s, alternating _2/_3)
    ///   Yawn:   Wait(5s) -> Play(1s) -> Hold(10s, frozen on _3)
    ///   Alternation driven by the "nextIsYawn" Bool parameter.
    ///
    /// Menu: Tools -> 2D-Gaviola -> Animation -> Rebuild Player Controller
    /// </summary>
    public static class RebuildPlayerController
    {
        // ── Paths ─────────────────────────────────────────────────────────────

        private const string ControllerPath = "Assets/Animations/Player/Player.controller";
        private const string ClipDir        = "Assets/Animations/Player/";
        private const string BackupPath     = "Assets/Animations/Player/_backup_Player.controller.backup";
        private const string SpriteDir      = "Assets/Sprites/Characters/";

        // ── Default state ─────────────────────────────────────────────────────

        private const string DefaultStateName = "Guitar_Wait_Right";

        // ── Sprite binding helper ─────────────────────────────────────────────

        private static readonly EditorCurveBinding SpriteBinding = new EditorCurveBinding
        {
            type         = typeof(SpriteRenderer),
            path         = "Sprite",
            propertyName = "m_Sprite"
        };

        // ── Entry Point ───────────────────────────────────────────────────────

        // [MenuItem("Tools/2D-Gaviola/Animation/Rebuild Player Controller")]
        public static void Rebuild()
        {
            // ── 1. Load controller ────────────────────────────────────────────
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
            {
                Debug.LogError(
                    "[RebuildPlayerController] Could not load AnimatorController at: " +
                    ControllerPath + "\nAborting.");
                return;
            }

            // ── 2. Backup ─────────────────────────────────────────────────────
            try
            {
                File.Copy(
                    Path.GetFullPath(ControllerPath),
                    Path.GetFullPath(BackupPath),
                    overwrite: true);
                Debug.Log("[RebuildPlayerController] Backup written to: " + BackupPath);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[RebuildPlayerController] Failed to write backup: " + e.Message +
                    "\nAborting.");
                return;
            }

            // ── 3. Regenerate non-idle clips (jump, fall, walk) ──────────────
            RebuildJumpClip("player_jump_left",  "jump-l", reverse: true);
            RebuildJumpClip("player_jump_right", "jump-r", reverse: false);
            RebuildFallClip("player_fall_left",  "player_jump_left");
            RebuildFallClip("player_fall_right", "player_jump_right");
            RebuildWalkClips();

            // ── 4. Generate 12 idle clips (idempotent) ────────────────────────
            GenerateIdleClips();

            // ── 5. Load the 8 non-idle clips we need ──────────────────────────
            var nonIdleClipNames = new[]
            {
                "player_walk_left",   "player_walk_right",
                "player_sprint_left", "player_sprint_right",
                "player_jump_left",   "player_jump_right",
                "player_fall_left",   "player_fall_right",
            };

            var nonIdleClips = new Dictionary<string, AnimationClip>(nonIdleClipNames.Length);
            foreach (string cn in nonIdleClipNames)
            {
                string path = ClipDir + cn + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    Debug.LogError("[RebuildPlayerController] Missing non-idle clip: " + path + "\nAborting.");
                    return;
                }
                nonIdleClips[cn] = clip;
            }

            // ── 6. Load the 12 new idle clips ─────────────────────────────────
            string[] idleClipNames = BuildIdleClipNames();
            var idleClips = new Dictionary<string, AnimationClip>(idleClipNames.Length);
            foreach (string cn in idleClipNames)
            {
                string path = ClipDir + cn + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    Debug.LogError("[RebuildPlayerController] Missing idle clip after generation: " + path + "\nAborting.");
                    return;
                }
                idleClips[cn] = clip;
            }

            // ── 7. Get layer 0 state machine ──────────────────────────────────
            AnimatorControllerLayer layer0 = controller.layers[0];
            AnimatorStateMachine    sm     = layer0.stateMachine;

            // ── 8. Clear existing states and AnyState transitions ─────────────
            foreach (ChildAnimatorState s in sm.states)
            {
                sm.RemoveState(s.state);
            }
            sm.anyStateTransitions = new AnimatorStateTransition[0];
            Debug.Log("[RebuildPlayerController] Cleared existing states and AnyState transitions.");

            // ── 9. Ensure required parameters exist ───────────────────────────
            EnsureParam(controller, "speed",       AnimatorControllerParameterType.Float);
            EnsureParam(controller, "isGrounded",  AnimatorControllerParameterType.Bool);
            EnsureParam(controller, "velocityY",   AnimatorControllerParameterType.Float);
            EnsureParam(controller, "isRunning",   AnimatorControllerParameterType.Bool);
            EnsureParam(controller, "facingLeft",  AnimatorControllerParameterType.Bool);
            EnsureParam(controller, "jumpTrigger", AnimatorControllerParameterType.Trigger);
            EnsureParam(controller, "nextIsYawn",  AnimatorControllerParameterType.Bool);
            RemoveParamIfExists(controller, "idleIndex");  // replaced by nextIsYawn

            // ── 10. Create states ─────────────────────────────────────────────
            var st = new Dictionary<string, AnimatorState>(32);

            // Non-idle states
            st["Walk_Left"]    = AddState(sm, "Walk_Left",    nonIdleClips["player_walk_left"],    new Vector3(-200, 100));
            st["Walk_Right"]   = AddState(sm, "Walk_Right",   nonIdleClips["player_walk_right"],   new Vector3( 100, 100));
            st["Sprint_Left"]  = AddState(sm, "Sprint_Left",  nonIdleClips["player_sprint_left"],  new Vector3(-200, 170));
            st["Sprint_Right"] = AddState(sm, "Sprint_Right", nonIdleClips["player_sprint_right"], new Vector3( 100, 170));
            st["Jump_Left"]    = AddState(sm, "Jump_Left",    nonIdleClips["player_jump_left"],    new Vector3(-200, 240));
            st["Jump_Right"]   = AddState(sm, "Jump_Right",   nonIdleClips["player_jump_right"],   new Vector3( 100, 240));
            st["Fall_Left"]    = AddState(sm, "Fall_Left",    nonIdleClips["player_fall_left"],    new Vector3(-200, 310));
            st["Fall_Right"]   = AddState(sm, "Fall_Right",   nonIdleClips["player_fall_right"],   new Vector3( 100, 310));

            // Guitar idle states (left column x=-500, right column x=-200, starting y=-80)
            float gx1 = -550f, gx2 = -250f, gy = -80f, gh = 70f;
            st["Guitar_Wait_Left"]  = AddState(sm, "Guitar_Wait_Left",  idleClips["player_idle_guitar_wait_left"],  new Vector3(gx1, gy));
            st["Guitar_Play_Left"]  = AddState(sm, "Guitar_Play_Left",  idleClips["player_idle_guitar_play_left"],  new Vector3(gx1, gy + gh));
            st["Guitar_Hold_Left"]  = AddState(sm, "Guitar_Hold_Left",  idleClips["player_idle_guitar_hold_left"],  new Vector3(gx1, gy + gh * 2));
            st["Guitar_Wait_Right"] = AddState(sm, "Guitar_Wait_Right", idleClips["player_idle_guitar_wait_right"], new Vector3(gx2, gy));
            st["Guitar_Play_Right"] = AddState(sm, "Guitar_Play_Right", idleClips["player_idle_guitar_play_right"], new Vector3(gx2, gy + gh));
            st["Guitar_Hold_Right"] = AddState(sm, "Guitar_Hold_Right", idleClips["player_idle_guitar_hold_right"], new Vector3(gx2, gy + gh * 2));

            // Yawn idle states (left column x=-550, right column x=-250, continuing below guitar)
            float yx1 = -550f, yx2 = -250f, yy = gy + gh * 4;
            st["Yawn_Wait_Left"]  = AddState(sm, "Yawn_Wait_Left",  idleClips["player_idle_yawn_wait_left"],  new Vector3(yx1, yy));
            st["Yawn_Play_Left"]  = AddState(sm, "Yawn_Play_Left",  idleClips["player_idle_yawn_play_left"],  new Vector3(yx1, yy + gh));
            st["Yawn_Hold_Left"]  = AddState(sm, "Yawn_Hold_Left",  idleClips["player_idle_yawn_hold_left"],  new Vector3(yx1, yy + gh * 2));
            st["Yawn_Wait_Right"] = AddState(sm, "Yawn_Wait_Right", idleClips["player_idle_yawn_wait_right"], new Vector3(yx2, yy));
            st["Yawn_Play_Right"] = AddState(sm, "Yawn_Play_Right", idleClips["player_idle_yawn_play_right"], new Vector3(yx2, yy + gh));
            st["Yawn_Hold_Right"] = AddState(sm, "Yawn_Hold_Right", idleClips["player_idle_yawn_hold_right"], new Vector3(yx2, yy + gh * 2));

            // ── 11. Attach IdleAlternator to Hold states ──────────────────────
            //   Guitar_Hold exits  -> nextIsYawn = true  (Yawn is next)
            //   Yawn_Hold   exits  -> nextIsYawn = false (Guitar is next)
            AttachAlternator(st["Guitar_Hold_Left"],  isGuitarHold: true);
            AttachAlternator(st["Guitar_Hold_Right"], isGuitarHold: true);
            AttachAlternator(st["Yawn_Hold_Left"],    isGuitarHold: false);
            AttachAlternator(st["Yawn_Hold_Right"],   isGuitarHold: false);

            sm.defaultState = st[DefaultStateName];
            Debug.Log("[RebuildPlayerController] Default state: " + DefaultStateName);

            // ── 12. Wire transitions ──────────────────────────────────────────
            int transitionCount = 0;

            AnimatorStateTransition T(AnimatorState from, AnimatorState to)
            {
                AnimatorStateTransition t = from.AddTransition(to);
                t.hasExitTime        = false;
                t.exitTime           = 0f;
                t.duration           = 0f;
                t.offset             = 0f;
                t.canTransitionToSelf = false;
                transitionCount++;
                return t;
            }

            AnimatorStateTransition TExitTime(AnimatorState from, AnimatorState to)
            {
                AnimatorStateTransition t = from.AddTransition(to);
                t.hasExitTime        = true;
                t.exitTime           = 1.0f;
                t.duration           = 0f;
                t.offset             = 0f;
                t.canTransitionToSelf = false;
                transitionCount++;
                return t;
            }

            AnimatorStateTransition TAny(AnimatorState to)
            {
                AnimatorStateTransition t = sm.AddAnyStateTransition(to);
                t.hasExitTime        = false;
                t.exitTime           = 0f;
                t.duration           = 0f;
                t.canTransitionToSelf = false;
                transitionCount++;
                return t;
            }

            // ── AnyState -> Jump ──────────────────────────────────────────────
            {
                var t = TAny(st["Jump_Left"]);
                t.AddCondition(AnimatorConditionMode.If,    0, "jumpTrigger");
                t.AddCondition(AnimatorConditionMode.If,    0, "facingLeft");
            }
            {
                var t = TAny(st["Jump_Right"]);
                t.AddCondition(AnimatorConditionMode.If,    0, "jumpTrigger");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }

            // ── Idle cycle: Guitar (left) ─────────────────────────────────────
            // Guitar_Wait_Left -> Guitar_Play_Left (exit time)
            TExitTime(st["Guitar_Wait_Left"], st["Guitar_Play_Left"]);
            // Guitar_Play_Left -> Guitar_Hold_Left (exit time)
            TExitTime(st["Guitar_Play_Left"], st["Guitar_Hold_Left"]);
            // Guitar_Hold_Left -> Yawn_Wait_Left (exit time)
            TExitTime(st["Guitar_Hold_Left"], st["Yawn_Wait_Left"]);

            // ── Idle cycle: Guitar (right) ────────────────────────────────────
            TExitTime(st["Guitar_Wait_Right"], st["Guitar_Play_Right"]);
            TExitTime(st["Guitar_Play_Right"], st["Guitar_Hold_Right"]);
            TExitTime(st["Guitar_Hold_Right"], st["Yawn_Wait_Right"]);

            // ── Idle cycle: Yawn (left) ───────────────────────────────────────
            TExitTime(st["Yawn_Wait_Left"], st["Yawn_Play_Left"]);
            TExitTime(st["Yawn_Play_Left"], st["Yawn_Hold_Left"]);
            TExitTime(st["Yawn_Hold_Left"], st["Guitar_Wait_Left"]);

            // ── Idle cycle: Yawn (right) ──────────────────────────────────────
            TExitTime(st["Yawn_Wait_Right"], st["Yawn_Play_Right"]);
            TExitTime(st["Yawn_Play_Right"], st["Yawn_Hold_Right"]);
            TExitTime(st["Yawn_Hold_Right"], st["Guitar_Wait_Right"]);

            // ── Facing flips during idle (hasExitTime = false) ────────────────
            // These are checked BEFORE the ExitTime transitions — add them last so they
            // are at the top of each state's transition list and evaluated first.
            // Guitar
            {
                var t = T(st["Guitar_Wait_Left"], st["Guitar_Wait_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Guitar_Wait_Right"], st["Guitar_Wait_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                var t = T(st["Guitar_Play_Left"], st["Guitar_Play_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Guitar_Play_Right"], st["Guitar_Play_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                var t = T(st["Guitar_Hold_Left"], st["Guitar_Hold_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Guitar_Hold_Right"], st["Guitar_Hold_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            // Yawn
            {
                var t = T(st["Yawn_Wait_Left"], st["Yawn_Wait_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Yawn_Wait_Right"], st["Yawn_Wait_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                var t = T(st["Yawn_Play_Left"], st["Yawn_Play_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Yawn_Play_Right"], st["Yawn_Play_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                var t = T(st["Yawn_Hold_Left"], st["Yawn_Hold_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Yawn_Hold_Right"], st["Yawn_Hold_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // ── Movement interrupts from all 12 idle states ───────────────────
            // Same-facing interrupts — existing logic kept as-is.
            // Cross-facing interrupts — new, fixes "tap A while facing right swallows input"
            // by routing directly to the opposite movement state without routing through
            // the facing-flip Wait state first (which ate the input frame).
            string[] allIdleStates =
            {
                "Guitar_Wait_Left",  "Guitar_Play_Left",  "Guitar_Hold_Left",
                "Guitar_Wait_Right", "Guitar_Play_Right", "Guitar_Hold_Right",
                "Yawn_Wait_Left",    "Yawn_Play_Left",    "Yawn_Hold_Left",
                "Yawn_Wait_Right",   "Yawn_Play_Right",   "Yawn_Hold_Right",
            };

            foreach (string sn in allIdleStates)
            {
                bool isLeft = sn.EndsWith("Left");

                // ── Same-facing interrupts ────────────────────────────────────
                AnimatorState walkSame   = isLeft ? st["Walk_Left"]   : st["Walk_Right"];
                AnimatorState sprintSame = isLeft ? st["Sprint_Left"] : st["Sprint_Right"];
                AnimatorConditionMode sameFacing = isLeft
                    ? AnimatorConditionMode.If
                    : AnimatorConditionMode.IfNot;

                var twSame = T(st[sn], walkSame);
                twSame.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                twSame.AddCondition(AnimatorConditionMode.IfNot,   0,    "isRunning");
                twSame.AddCondition(sameFacing,                    0,    "facingLeft");

                var tsSame = T(st[sn], sprintSame);
                tsSame.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                tsSame.AddCondition(AnimatorConditionMode.If,      0,    "isRunning");
                tsSame.AddCondition(sameFacing,                    0,    "facingLeft");

                // ── Cross-facing interrupts ───────────────────────────────────
                // If the player taps the opposite direction while idle, the facing
                // parameter flips before speed can exceed 0.1. Without these, the
                // Animator routes through the facing-flip Wait transition, which
                // eats the input frame and drops the movement entirely.
                AnimatorState walkOpposite   = isLeft ? st["Walk_Right"]   : st["Walk_Left"];
                AnimatorState sprintOpposite = isLeft ? st["Sprint_Right"] : st["Sprint_Left"];
                AnimatorConditionMode oppositeFacing = isLeft
                    ? AnimatorConditionMode.IfNot
                    : AnimatorConditionMode.If;

                var twOpposite = T(st[sn], walkOpposite);
                twOpposite.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                twOpposite.AddCondition(AnimatorConditionMode.IfNot,   0,    "isRunning");
                twOpposite.AddCondition(oppositeFacing,                0,    "facingLeft");

                var tsOpposite = T(st[sn], sprintOpposite);
                tsOpposite.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                tsOpposite.AddCondition(AnimatorConditionMode.If,      0,    "isRunning");
                tsOpposite.AddCondition(oppositeFacing,                0,    "facingLeft");
            }

            // ── Movement -> Idle entry (checks nextIsYawn) ────────────────────
            // Walk/Sprint/Fall/Jump land -> Guitar_Wait or Yawn_Wait based on nextIsYawn
            string[] landingStates = { "Walk_Left", "Walk_Right", "Sprint_Left", "Sprint_Right",
                                       "Fall_Left",  "Fall_Right" };

            foreach (string sn in landingStates)
            {
                bool isLeft = sn.EndsWith("Left");
                AnimatorConditionMode facingCond = isLeft
                    ? AnimatorConditionMode.If
                    : AnimatorConditionMode.IfNot;

                // -> Guitar_Wait (nextIsYawn = false)
                AnimatorState guitarWait = isLeft ? st["Guitar_Wait_Left"] : st["Guitar_Wait_Right"];
                var tg = T(st[sn], guitarWait);
                tg.AddCondition(AnimatorConditionMode.Less,  0.1f, "speed");
                tg.AddCondition(AnimatorConditionMode.IfNot, 0,    "nextIsYawn");
                tg.AddCondition(facingCond,                  0,    "facingLeft");

                // -> Yawn_Wait (nextIsYawn = true)
                AnimatorState yawnWait = isLeft ? st["Yawn_Wait_Left"] : st["Yawn_Wait_Right"];
                var ty = T(st[sn], yawnWait);
                ty.AddCondition(AnimatorConditionMode.Less,  0.1f, "speed");
                ty.AddCondition(AnimatorConditionMode.If,    0,    "nextIsYawn");
                ty.AddCondition(facingCond,                  0,    "facingLeft");
            }

            // Walk/Sprint -> Walk/Sprint (direction flip)
            {
                var t = T(st["Walk_Left"], st["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Walk_Right"], st["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                var t = T(st["Sprint_Left"], st["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Sprint_Right"], st["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // Walk <-> Sprint (isRunning toggle)
            {
                var t = T(st["Walk_Left"], st["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
            }
            {
                var t = T(st["Walk_Right"], st["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
            }
            {
                var t = T(st["Sprint_Left"], st["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
            }
            {
                var t = T(st["Sprint_Right"], st["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
            }

            // Jump -> Fall (exit time 1.0 + velocityY < 0 + !isGrounded)
            {
                AnimatorStateTransition t = st["Jump_Left"].AddTransition(st["Fall_Left"]);
                t.hasExitTime  = true;
                t.exitTime     = 1.0f;
                t.duration     = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.Less,  0, "velocityY");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
                transitionCount++;
            }
            {
                AnimatorStateTransition t = st["Jump_Right"].AddTransition(st["Fall_Right"]);
                t.hasExitTime  = true;
                t.exitTime     = 1.0f;
                t.duration     = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.Less,  0, "velocityY");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
                transitionCount++;
            }

            // Jump -> landing (short jump, isGrounded while still in Jump state)
            // Handled by the landing logic above (Walk_Left/Right speed condition also
            // covers Jump states because they are in landingStates? No — Jump is excluded.
            // Handle Jump landings explicitly:
            foreach (string jumpState in new[] { "Jump_Left", "Jump_Right" })
            {
                bool isLeft = jumpState.EndsWith("Left");
                AnimatorConditionMode facingCond = isLeft
                    ? AnimatorConditionMode.If
                    : AnimatorConditionMode.IfNot;

                // -> Walk
                var tw = T(st[jumpState], isLeft ? st["Walk_Left"] : st["Walk_Right"]);
                tw.AddCondition(AnimatorConditionMode.If,      0,    "isGrounded");
                tw.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                tw.AddCondition(AnimatorConditionMode.IfNot,   0,    "isRunning");
                tw.AddCondition(facingCond,                    0,    "facingLeft");

                // -> Sprint
                var ts = T(st[jumpState], isLeft ? st["Sprint_Left"] : st["Sprint_Right"]);
                ts.AddCondition(AnimatorConditionMode.If,      0,    "isGrounded");
                ts.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                ts.AddCondition(AnimatorConditionMode.If,      0,    "isRunning");
                ts.AddCondition(facingCond,                    0,    "facingLeft");

                // -> Guitar_Wait (speed < 0.1, nextIsYawn false)
                AnimatorState guitarWait = isLeft ? st["Guitar_Wait_Left"] : st["Guitar_Wait_Right"];
                var tgw = T(st[jumpState], guitarWait);
                tgw.AddCondition(AnimatorConditionMode.If,    0,    "isGrounded");
                tgw.AddCondition(AnimatorConditionMode.Less,  0.1f, "speed");
                tgw.AddCondition(AnimatorConditionMode.IfNot, 0,    "nextIsYawn");
                tgw.AddCondition(facingCond,                  0,    "facingLeft");

                // -> Yawn_Wait (speed < 0.1, nextIsYawn true)
                AnimatorState yawnWait = isLeft ? st["Yawn_Wait_Left"] : st["Yawn_Wait_Right"];
                var tyw = T(st[jumpState], yawnWait);
                tyw.AddCondition(AnimatorConditionMode.If,   0,    "isGrounded");
                tyw.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
                tyw.AddCondition(AnimatorConditionMode.If,   0,    "nextIsYawn");
                tyw.AddCondition(facingCond,                 0,    "facingLeft");

                // Cross-facing landing (rare but handles mid-air turn)
                bool otherLeft = !isLeft;
                AnimatorConditionMode otherFacing = otherLeft
                    ? AnimatorConditionMode.If
                    : AnimatorConditionMode.IfNot;

                var tgwx = T(st[jumpState], otherLeft ? st["Guitar_Wait_Left"] : st["Guitar_Wait_Right"]);
                tgwx.AddCondition(AnimatorConditionMode.If,    0,    "isGrounded");
                tgwx.AddCondition(AnimatorConditionMode.Less,  0.1f, "speed");
                tgwx.AddCondition(AnimatorConditionMode.IfNot, 0,    "nextIsYawn");
                tgwx.AddCondition(otherFacing,                 0,    "facingLeft");

                var tywx = T(st[jumpState], otherLeft ? st["Yawn_Wait_Left"] : st["Yawn_Wait_Right"]);
                tywx.AddCondition(AnimatorConditionMode.If,   0,    "isGrounded");
                tywx.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
                tywx.AddCondition(AnimatorConditionMode.If,   0,    "nextIsYawn");
                tywx.AddCondition(otherFacing,                0,    "facingLeft");
            }

            // Fall -> Fall facing flip
            {
                var t = T(st["Fall_Left"], st["Fall_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                var t = T(st["Fall_Right"], st["Fall_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // Fall -> Walk/Sprint (with speed)
            {
                var t = T(st["Fall_Left"], st["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.If,      0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot,   0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.If,      0,    "facingLeft");
            }
            {
                var t = T(st["Fall_Right"], st["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.If,      0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot,   0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot,   0,    "facingLeft");
            }
            {
                var t = T(st["Fall_Left"], st["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If,      0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If,      0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.If,      0,    "facingLeft");
            }
            {
                var t = T(st["Fall_Right"], st["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.If,      0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If,      0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot,   0,    "facingLeft");
            }

            // ── 13. Commit ────────────────────────────────────────────────────
            AnimatorControllerLayer[] allLayers = controller.layers;
            allLayers[0] = layer0;
            controller.layers = allLayers;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 14. Summary ───────────────────────────────────────────────────
            Debug.Log(
                "[RebuildPlayerController] -- Verification Summary --\n" +
                "States created   : " + st.Count + " (8 non-idle + 12 idle = 20 total)\n" +
                "Transitions wired: " + transitionCount + "\n" +
                "Default state    : " + DefaultStateName + "\n" +
                "Backup saved to  : " + BackupPath + "\n" +
                "Idle alternation : nextIsYawn (Bool) — Guitar first by default.\n" +
                "IdleAlternator   : attached to Guitar_Hold_* (IsGuitarHold=true) and Yawn_Hold_* (IsGuitarHold=false).");

            Debug.Log("[RebuildPlayerController] Done. Open Window -> Animator to inspect the new state machine.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static AnimatorState AddState(
            AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 pos)
        {
            AnimatorState state = sm.AddState(name, pos);
            state.motion = clip;
            return state;
        }

        private static void AttachAlternator(AnimatorState state, bool isGuitarHold)
        {
            IdleAlternator b = state.AddStateMachineBehaviour<IdleAlternator>();
            if (b != null)
            {
                b.IsGuitarHold = isGuitarHold;
                Debug.Log($"[RebuildPlayerController] IdleAlternator attached to {state.name} (IsGuitarHold={isGuitarHold}).");
            }
        }

        private static void EnsureParam(
            AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
            Debug.Log($"[RebuildPlayerController] Added parameter: {name} ({type})");
        }

        private static void RemoveParamIfExists(AnimatorController controller, string name)
        {
            int idx = -1;
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name == name) { idx = i; break; }
            }
            if (idx >= 0)
            {
                controller.RemoveParameter(idx);
                Debug.Log($"[RebuildPlayerController] Removed obsolete parameter: {name}");
            }
        }

        // ── Idle clip name table ──────────────────────────────────────────────

        private static string[] BuildIdleClipNames()
        {
            return new[]
            {
                "player_idle_guitar_wait_left",  "player_idle_guitar_play_left",  "player_idle_guitar_hold_left",
                "player_idle_guitar_wait_right", "player_idle_guitar_play_right", "player_idle_guitar_hold_right",
                "player_idle_yawn_wait_left",    "player_idle_yawn_play_left",    "player_idle_yawn_hold_left",
                "player_idle_yawn_wait_right",   "player_idle_yawn_play_right",   "player_idle_yawn_hold_right",
            };
        }

        // ── Idle clip generation ──────────────────────────────────────────────

        private static void GenerateIdleClips()
        {
            // Guitar right — forward order (_0 = standing pose, _3 = peak of animation)
            GenerateWaitClip("player_idle_guitar_wait_right", "idle2-guitar-r", 0);
            GeneratePlayClip("player_idle_guitar_play_right", "idle2-guitar-r", reverse: false);
            GenerateGuitarHoldClip("player_idle_guitar_hold_right", "idle2-guitar-r", frame2: 2, frame3: 3);

            // Guitar left — REVERSED order (_3 = standing pose, _0 = peak of animation)
            // The left sheet is authored right-to-left, same as jump-l (Milestone 4.6).
            GenerateWaitClip("player_idle_guitar_wait_left", "idle2-guitar-l", 3);
            GeneratePlayClip("player_idle_guitar_play_left", "idle2-guitar-l", reverse: true);
            GenerateGuitarHoldClip("player_idle_guitar_hold_left", "idle2-guitar-l", frame2: 1, frame3: 0);

            // Yawn right — forward order (_0 = standing pose, _3 = peak of animation)
            GenerateWaitClip("player_idle_yawn_wait_right", "idle1-yawn-r", 0);
            GeneratePlayClip("player_idle_yawn_play_right", "idle1-yawn-r", reverse: false);
            GenerateYawnHoldClip("player_idle_yawn_hold_right", "idle1-yawn-r", holdFrame: 3);

            // Yawn left — REVERSED order (_3 = standing pose, _0 = peak of animation)
            GenerateWaitClip("player_idle_yawn_wait_left", "idle1-yawn-l", 3);
            GeneratePlayClip("player_idle_yawn_play_left", "idle1-yawn-l", reverse: true);
            GenerateYawnHoldClip("player_idle_yawn_hold_left", "idle1-yawn-l", holdFrame: 0);
        }

        /// <summary>Wait clip: 2 keyframes at t=0 and t=5, both on sprite[frameIndex].</summary>
        private static void GenerateWaitClip(string clipName, string sheetName, int frameIndex)
        {
            Sprite[] sprites = LoadSprites(sheetName);
            if (sprites == null) return;
            if (frameIndex >= sprites.Length)
            {
                Debug.LogError($"[RebuildPlayerController] {sheetName} has {sprites.Length} sprites, but frameIndex={frameIndex}.");
                return;
            }

            AnimationClip clip = EnsureClip(clipName);
            clip.frameRate = 4f;
            SetNonLooping(clip);

            ObjectReferenceKeyframe[] keys =
            {
                new ObjectReferenceKeyframe { time = 0f, value = sprites[frameIndex] },
                new ObjectReferenceKeyframe { time = 5f, value = sprites[frameIndex] },
            };

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            EditorUtility.SetDirty(clip);
            Debug.Log($"[RebuildPlayerController] Generated wait clip: {clipName} (5s freeze on _0).");
        }

        /// <summary>
        /// Play clip: 4 keyframes at t=0, 0.25, 0.5, 0.75.
        /// When <paramref name="reverse"/> is false, frames play _0 _1 _2 _3 (right-facing sheets).
        /// When <paramref name="reverse"/> is true, frames play _3 _2 _1 _0 (left-facing sheets
        /// authored in reverse order, same pattern as jump-l).
        /// </summary>
        private static void GeneratePlayClip(string clipName, string sheetName, bool reverse)
        {
            Sprite[] sprites = LoadSprites(sheetName);
            if (sprites == null || sprites.Length < 4) return;

            AnimationClip clip = EnsureClip(clipName);
            clip.frameRate = 4f;
            SetNonLooping(clip);

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[4];
            for (int i = 0; i < 4; i++)
            {
                int spriteIndex = reverse ? (3 - i) : i;
                keys[i] = new ObjectReferenceKeyframe { time = i * 0.25f, value = sprites[spriteIndex] };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            EditorUtility.SetDirty(clip);
            string order = reverse ? "_3 _2 _1 _0" : "_0 _1 _2 _3";
            Debug.Log($"[RebuildPlayerController] Generated play clip: {clipName} (1s, 4 FPS, order: {order}).");
        }

        /// <summary>
        /// Guitar hold clip: 40 keyframes alternating between sprites[frame2] and sprites[frame3]
        /// at 4 FPS for 10 seconds total.
        /// </summary>
        private static void GenerateGuitarHoldClip(
            string clipName, string sheetName, int frame2, int frame3)
        {
            Sprite[] sprites = LoadSprites(sheetName);
            if (sprites == null) return;
            if (frame2 >= sprites.Length || frame3 >= sprites.Length)
            {
                Debug.LogError($"[RebuildPlayerController] {sheetName}: frame2={frame2} or frame3={frame3} out of range (len={sprites.Length}).");
                return;
            }

            AnimationClip clip = EnsureClip(clipName);
            clip.frameRate = 4f;
            SetNonLooping(clip);

            // 40 keyframes at 0.25s intervals = 10 seconds
            const int keyCount = 40;
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time  = i * 0.25f,
                    value = (i % 2 == 0) ? sprites[frame2] : sprites[frame3],
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            EditorUtility.SetDirty(clip);
            Debug.Log($"[RebuildPlayerController] Generated guitar hold clip: {clipName} (10s, alternating _{frame2}/_{frame3}).");
        }

        /// <summary>Yawn hold clip: 2 keyframes at t=0 and t=10, both on sprites[holdFrame].</summary>
        private static void GenerateYawnHoldClip(string clipName, string sheetName, int holdFrame)
        {
            Sprite[] sprites = LoadSprites(sheetName);
            if (sprites == null) return;
            if (holdFrame >= sprites.Length)
            {
                Debug.LogError($"[RebuildPlayerController] {sheetName}: holdFrame={holdFrame} out of range.");
                return;
            }

            AnimationClip clip = EnsureClip(clipName);
            clip.frameRate = 4f;
            SetNonLooping(clip);

            ObjectReferenceKeyframe[] keys =
            {
                new ObjectReferenceKeyframe { time = 0f,  value = sprites[holdFrame] },
                new ObjectReferenceKeyframe { time = 10f, value = sprites[holdFrame] },
            };

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            EditorUtility.SetDirty(clip);
            Debug.Log($"[RebuildPlayerController] Generated yawn hold clip: {clipName} (10s freeze on _{holdFrame}).");
        }

        // ── Low-level clip/sprite utilities ───────────────────────────────────

        /// <summary>Load all sprites from a sheet, sorted by name (_0, _1, _2, _3).</summary>
        private static Sprite[] LoadSprites(string sheetName)
        {
            string path = SpriteDir + sheetName + ".png";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[RebuildPlayerController] Could not find sprites at: {path}");
                return null;
            }

            var list = new List<Sprite>();
            foreach (Object a in assets)
            {
                if (a is Sprite s) list.Add(s);
            }

            list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return list.ToArray();
        }

        /// <summary>Load existing clip, or create a new .anim asset if it doesn't exist yet.</summary>
        private static AnimationClip EnsureClip(string clipName)
        {
            string path = ClipDir + clipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
                Debug.Log($"[RebuildPlayerController] Created new clip asset: {path}");
            }
            return clip;
        }

        private static void SetNonLooping(AnimationClip clip)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        // ── Walk clip rebuilder ───────────────────────────────────────────────

        /// <summary>
        /// Rebuilds <c>player_walk_right.anim</c> and <c>player_walk_left.anim</c>,
        /// dropping the standing-pose frame from each sheet so the loop never
        /// flashes an idle frame mid-stride.
        ///
        /// <para>
        /// <c>walk-r.png</c> is forward: _0 = standing (dropped), _1/_2/_3 = strides.
        /// Clip plays indices [1, 2, 3] → 3-frame loop.
        /// </para>
        /// <para>
        /// <c>walk-l.png</c> is authored right-to-left (same convention as jump-l / idle-l):
        /// _3 = standing (dropped), _2/_1/_0 = strides (reversed order).
        /// Clip plays indices [2, 1, 0] → 3-frame loop.
        /// </para>
        /// </summary>
        private static void RebuildWalkClips()
        {
            // ── Right ─────────────────────────────────────────────────────────
            Sprite[] spritesR = LoadSprites("walk-r");
            if (spritesR != null && spritesR.Length >= 4)
            {
                // Stride frames: indices 1, 2, 3 (drop index 0 = standing pose)
                int[] indicesR = { 1, 2, 3 };
                RebuildWalkClip("player_walk_right", spritesR, indicesR);
            }
            else
            {
                Debug.LogError("[RebuildPlayerController] RebuildWalkClips: could not load walk-r sprites (need >= 4).");
            }

            // ── Left ──────────────────────────────────────────────────────────
            Sprite[] spritesL = LoadSprites("walk-l");
            if (spritesL != null && spritesL.Length >= 4)
            {
                // Sheet is authored right-to-left: _3 = standing (drop it).
                // Stride frames in playback order: indices 2, 1, 0.
                int[] indicesL = { 2, 1, 0 };
                RebuildWalkClip("player_walk_left", spritesL, indicesL);
            }
            else
            {
                Debug.LogError("[RebuildPlayerController] RebuildWalkClips: could not load walk-l sprites (need >= 4).");
            }
        }

        /// <summary>
        /// Writes a looping walk clip to disk using the specified sprite indices.
        /// If the .anim asset already exists it is overwritten in-place (GUID preserved).
        /// </summary>
        private static void RebuildWalkClip(string clipName, Sprite[] sprites, int[] frameIndices)
        {
            string path = ClipDir + clipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
                Debug.Log($"[RebuildPlayerController] Created new walk clip asset: {path}");
            }

            clip.frameRate = 4f;

            // Enable loop time
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Build keyframes at 0.25 s intervals (4 FPS)
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameIndices.Length];
            for (int i = 0; i < frameIndices.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time  = i * 0.25f,
                    value = sprites[frameIndices[i]],
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            EditorUtility.SetDirty(clip);

            string frameLog = string.Join(", ", System.Array.ConvertAll(frameIndices, idx => sprites[idx].name));
            Debug.Log($"[RebuildPlayerController] Rebuilt walk clip: {clipName} — {frameIndices.Length} frames at 4 FPS, loopTime=true. Sprites: [{frameLog}]");
        }

        // ── Jump / Fall clip rebuilders (preserved from previous version) ─────

        private static void RebuildJumpClip(string clipName, string sheetName, bool reverse)
        {
            string path = ClipDir + clipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            Sprite[] sprites = LoadSprites(sheetName);
            if (sprites == null) return;

            if (reverse)
            {
                Array.Reverse(sprites);
            }

            clip.frameRate = 8f;
            SetNonLooping(clip);

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprites[i] };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            EditorUtility.SetDirty(clip);
            Debug.Log($"[RebuildPlayerController] Regenerated {clipName} — {sprites.Length} frames at 8 FPS (reversed={reverse}).");
        }

        private static void RebuildFallClip(string fallClipName, string jumpClipName)
        {
            string fallPath = ClipDir + fallClipName + ".anim";
            AnimationClip fallClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fallPath);
            if (fallClip == null)
            {
                fallClip = new AnimationClip();
                AssetDatabase.CreateAsset(fallClip, fallPath);
            }

            AnimationClip sourceClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDir + jumpClipName + ".anim");
            if (sourceClip == null) return;

            Sprite lastSprite = null;
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
            {
                if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                {
                    ObjectReferenceKeyframe[] keys =
                        AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                    if (keys.Length > 0) lastSprite = keys[keys.Length - 1].value as Sprite;
                    break;
                }
            }

            if (lastSprite == null)
            {
                Debug.LogError($"[RebuildPlayerController] Could not find last sprite in {jumpClipName}.");
                return;
            }

            fallClip.frameRate = 4f;
            SetNonLooping(fallClip);

            ObjectReferenceKeyframe[] newKeys =
            {
                new ObjectReferenceKeyframe { time = 0f,    value = lastSprite },
                new ObjectReferenceKeyframe { time = 0.25f, value = lastSprite },
            };

            AnimationUtility.SetObjectReferenceCurve(fallClip, SpriteBinding, newKeys);
            EditorUtility.SetDirty(fallClip);
            Debug.Log($"[RebuildPlayerController] Regenerated fall clip {fallClipName} (sprite={lastSprite.name}).");
        }
    }
}
