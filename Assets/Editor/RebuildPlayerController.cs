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
    /// Implements Milestone 4.8 specs: 
    /// - Fixes 0-length Fall clips causing the Animator to freeze
    ///
    /// Menu: Tools -> 2D-Gaviola -> Animation -> Rebuild Player Controller
    /// </summary>
    public static class RebuildPlayerController
    {
        // ── Paths ─────────────────────────────────────────────────────────────

        private const string ControllerPath = "Assets/Animations/Player/Player.controller";
        private const string ClipDir        = "Assets/Animations/Player/";
        private const string BackupPath     = "Assets/Animations/Player/_backup_Player.controller.backup";

        // ── Clip name -> state name mapping ──────────────────────────────────

        private struct StateSpec
        {
            public string ClipName;   // without extension
            public string StateName;  // name shown in Animator window
            public bool   IsWait;     // true = attach IdleWaitRandomizer
        }

        private static readonly StateSpec[] _states =
        {
            new StateSpec { ClipName = "player_idle_wait_left",    StateName = "IdleWait_Left",    IsWait = true  },
            new StateSpec { ClipName = "player_idle_wait_right",   StateName = "IdleWait_Right",   IsWait = true  },
            new StateSpec { ClipName = "player_idle_yawn_left",    StateName = "IdleYawn_Left",    IsWait = false },
            new StateSpec { ClipName = "player_idle_yawn_right",   StateName = "IdleYawn_Right",   IsWait = false },
            new StateSpec { ClipName = "player_idle_guitar_left",  StateName = "IdleGuitar_Left",  IsWait = false },
            new StateSpec { ClipName = "player_idle_guitar_right", StateName = "IdleGuitar_Right", IsWait = false },
            new StateSpec { ClipName = "player_walk_left",         StateName = "Walk_Left",        IsWait = false },
            new StateSpec { ClipName = "player_walk_right",        StateName = "Walk_Right",       IsWait = false },
            new StateSpec { ClipName = "player_sprint_left",       StateName = "Sprint_Left",      IsWait = false },
            new StateSpec { ClipName = "player_sprint_right",      StateName = "Sprint_Right",     IsWait = false },
            new StateSpec { ClipName = "player_jump_left",         StateName = "Jump_Left",        IsWait = false },
            new StateSpec { ClipName = "player_jump_right",        StateName = "Jump_Right",       IsWait = false },
            new StateSpec { ClipName = "player_fall_left",         StateName = "Fall_Left",        IsWait = false },
            new StateSpec { ClipName = "player_fall_right",        StateName = "Fall_Right",       IsWait = false },
        };

        private const string DefaultStateName = "IdleWait_Right";

        // ── Entry Point ───────────────────────────────────────────────────────

        [MenuItem("Tools/2D-Gaviola/Animation/Rebuild Player Controller")]
        public static void Rebuild()
        {
            // ── 1. Load Controller ────────────────────────────────────────────
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
            string absoluteController = Path.GetFullPath(ControllerPath);
            string absoluteBackup     = Path.GetFullPath(BackupPath);

            try
            {
                File.Copy(absoluteController, absoluteBackup, overwrite: true);
                Debug.Log("[RebuildPlayerController] Backup written to: " + BackupPath);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[RebuildPlayerController] Failed to write backup: " + e.Message + "\nAborting.");
                return;
            }

            // ── 3. Regenerate Jump, Wait & Fall Clips ─────────────────────────
            
            // Rebuild jump clips at 8 FPS with correct order (left reversed, right forward)
            RebuildJumpClip("player_jump_left", "jump-l", reverse: true);
            RebuildJumpClip("player_jump_right", "jump-r", reverse: false);
            
            // Ensure wait clips exist
            CreateWaitClipIfMissing("player_idle_wait_left", "player_idle_yawn_left");
            CreateWaitClipIfMissing("player_idle_wait_right", "player_idle_yawn_right");
            
            // Regenerate fall clips to strictly use the last sprite of the new jump clips
            // AND ensure they have a non-zero duration so the Animator doesn't freeze.
            RebuildFallClip("player_fall_left", "player_jump_left");
            RebuildFallClip("player_fall_right", "player_jump_right");

            // ── 4. Load all 14 clips & enforce loop flags ─────────────────────
            var clips = new Dictionary<string, AnimationClip>(_states.Length);
            bool clipSettingsChanged = false;

            foreach (StateSpec spec in _states)
            {
                string clipPath = ClipDir + spec.ClipName + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

                if (clip == null)
                {
                    Debug.LogError(
                        "[RebuildPlayerController] Could not load clip: " + clipPath + "\n" +
                        "Aborting.");
                    return;
                }

                // Enforce loopTime = true on yawn and guitar clips
                if (spec.ClipName.Contains("yawn") || spec.ClipName.Contains("guitar"))
                {
                    AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                    if (!settings.loopTime)
                    {
                        settings.loopTime = true;
                        AnimationUtility.SetAnimationClipSettings(clip, settings);
                        EditorUtility.SetDirty(clip);
                        clipSettingsChanged = true;
                        Debug.Log("[RebuildPlayerController] Forced loopTime = true on " + spec.ClipName);
                    }
                }

                clips[spec.ClipName] = clip;
            }

            if (clipSettingsChanged)
            {
                AssetDatabase.SaveAssets();
            }

            // ── 5. Get layer 0 state machine ──────────────────────────────────
            AnimatorControllerLayer layer0 = controller.layers[0];
            AnimatorStateMachine    sm     = layer0.stateMachine;

            // ── 6. Clear existing states ──────────────────────────────────────
            foreach (ChildAnimatorState s in sm.states)
            {
                sm.RemoveState(s.state);
            }

            sm.anyStateTransitions = new AnimatorStateTransition[0];
            Debug.Log("[RebuildPlayerController] Cleared existing states and AnyState transitions.");

            // ── 7. Preserve existing parameters, add idleIndex ────────────────
            string[] requiredParams = { "speed", "isGrounded", "velocityY", "isRunning", "facingLeft", "jumpTrigger" };

            foreach (string name in requiredParams)
            {
                bool found = false;
                foreach (AnimatorControllerParameter p in controller.parameters)
                {
                    if (p.name == name) { found = true; break; }
                }

                if (!found)
                {
                    Debug.LogWarning(
                        "[RebuildPlayerController] Expected parameter missing: " + name +
                        " — PlayerAnimatorDriver.cs may not function correctly.");
                }
            }

            bool idleIndexExists = false;
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name == "idleIndex") { idleIndexExists = true; break; }
            }

            if (!idleIndexExists)
            {
                controller.AddParameter("idleIndex", AnimatorControllerParameterType.Int);
                Debug.Log("[RebuildPlayerController] Added parameter: idleIndex (int)");
            }

            // ── 8. Create states and attach clips ─────────────────────────────
            var stateMap = new Dictionary<string, AnimatorState>(_states.Length);

            float colX1 = 250f;  // left column
            float colX2 = 550f;  // right column
            float rowY   = -250f;
            float rowH   = 70f;

            string[] leftStates  = { "IdleWait_Left",  "IdleYawn_Left",   "IdleGuitar_Left",  "Walk_Left",  "Sprint_Left",  "Jump_Left",  "Fall_Left" };
            string[] rightStates = { "IdleWait_Right", "IdleYawn_Right",  "IdleGuitar_Right", "Walk_Right", "Sprint_Right", "Jump_Right", "Fall_Right" };

            var positions = new Dictionary<string, Vector3>();
            for (int i = 0; i < leftStates.Length; i++)
            {
                positions[leftStates[i]]  = new Vector3(colX1, rowY + rowH * i);
                positions[rightStates[i]] = new Vector3(colX2, rowY + rowH * i);
            }

            int idleWaitRandCount = 0;

            foreach (StateSpec spec in _states)
            {
                Vector3 pos = positions.ContainsKey(spec.StateName)
                    ? positions[spec.StateName]
                    : new Vector3(400f, rowY);

                AnimatorState state = sm.AddState(spec.StateName, pos);
                state.motion        = clips[spec.ClipName];
                stateMap[spec.StateName] = state;

                if (spec.IsWait)
                {
                    IdleWaitRandomizer behaviour = state.AddStateMachineBehaviour<IdleWaitRandomizer>();
                    if (behaviour != null)
                    {
                        idleWaitRandCount++;
                        Debug.Log("[RebuildPlayerController] " + spec.StateName + ": IdleWaitRandomizer attached.");
                    }
                }
            }

            sm.defaultState = stateMap[DefaultStateName];
            Debug.Log("[RebuildPlayerController] Default state: " + DefaultStateName);

            // ── 9. Wire transitions ───────────────────────────────────────────
            int transitionCount = 0;

            AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to)
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

            AnimatorStateTransition AddAnyTransition(AnimatorState to)
            {
                AnimatorStateTransition t = sm.AddAnyStateTransition(to);
                t.hasExitTime        = false;
                t.exitTime           = 0f;
                t.duration           = 0f;
                t.canTransitionToSelf = false;
                transitionCount++;
                return t;
            }

            AnimatorStateTransition AddTimedExitTransition(AnimatorState from, AnimatorState to, float exitTimeValue)
            {
                AnimatorStateTransition t = from.AddTransition(to);
                t.hasExitTime        = true;
                t.exitTime           = exitTimeValue; 
                t.duration           = 0f;
                t.offset             = 0f;
                t.canTransitionToSelf = false;
                transitionCount++;
                return t;
            }

            // -- AnyState -> Jump (jumpTrigger + facingLeft) -------------------
            {
                AnimatorStateTransition t = AddAnyTransition(stateMap["Jump_Left"]);
                t.AddCondition(AnimatorConditionMode.If,         0, "jumpTrigger");
                t.AddCondition(AnimatorConditionMode.If,         0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddAnyTransition(stateMap["Jump_Right"]);
                t.AddCondition(AnimatorConditionMode.If,         0, "jumpTrigger");
                t.AddCondition(AnimatorConditionMode.IfNot,      0, "facingLeft");
            }

            // -- Movement -> IdleWait (speed < 0.1) ----------------------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Walk_Left"], stateMap["IdleWait_Left"]);
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Walk_Right"], stateMap["IdleWait_Right"]);
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Sprint_Left"], stateMap["IdleWait_Left"]);
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Sprint_Right"], stateMap["IdleWait_Right"]);
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
            }

            // -- Jump -> Fall (Wait for Jump clip to finish fully AND velocityY < 0 AND !isGrounded) ---
            {
                AnimatorStateTransition t = AddTimedExitTransition(stateMap["Jump_Left"], stateMap["Fall_Left"], 1.0f);
                t.AddCondition(AnimatorConditionMode.Less, 0, "velocityY");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
            }
            {
                AnimatorStateTransition t = AddTimedExitTransition(stateMap["Jump_Right"], stateMap["Fall_Right"], 1.0f);
                t.AddCondition(AnimatorConditionMode.Less, 0, "velocityY");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
            }

            // -- Jump -> Landing Fallbacks (short jump interrupts) -------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Left"], stateMap["IdleWait_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Left"], stateMap["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Left"], stateMap["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Left"], stateMap["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Left"], stateMap["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Left"], stateMap["IdleWait_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Right"], stateMap["IdleWait_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Right"], stateMap["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Right"], stateMap["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Right"], stateMap["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Right"], stateMap["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Jump_Right"], stateMap["IdleWait_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // -- Fall -> Landing to IdleWait (grounded, speed < 0.1) ------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Left"], stateMap["IdleWait_Left"]);
                t.AddCondition(AnimatorConditionMode.If,         0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Less,       0.1f, "speed");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Right"], stateMap["IdleWait_Right"]);
                t.AddCondition(AnimatorConditionMode.If,         0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Less,       0.1f, "speed");
            }

            // -- Fall -> Walk ---------------------------------------------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Left"], stateMap["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.If,         0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater,    0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot,      0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.If,         0,    "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Right"], stateMap["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.If,         0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater,    0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.IfNot,      0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot,      0,    "facingLeft");
            }

            // -- Fall -> Sprint -------------------------------------------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Left"], stateMap["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If,         0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater,    0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If,         0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.If,         0,    "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Right"], stateMap["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.If,         0,    "isGrounded");
                t.AddCondition(AnimatorConditionMode.Greater,    0.1f, "speed");
                t.AddCondition(AnimatorConditionMode.If,         0,    "isRunning");
                t.AddCondition(AnimatorConditionMode.IfNot,      0,    "facingLeft");
            }

            // -- Fall facing flips ----------------------------------------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Left"], stateMap["Fall_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Fall_Right"], stateMap["Fall_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // -- IdleWait -> Play Idle (hasExitTime = true, exitTime = 1.0, isGrounded) ----
            {
                AnimatorStateTransition t = AddTimedExitTransition(stateMap["IdleWait_Left"], stateMap["IdleYawn_Left"], 1.0f);
                t.AddCondition(AnimatorConditionMode.Equals, 0, "idleIndex");
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
            }
            {
                AnimatorStateTransition t = AddTimedExitTransition(stateMap["IdleWait_Left"], stateMap["IdleGuitar_Left"], 1.0f);
                t.AddCondition(AnimatorConditionMode.Equals, 1, "idleIndex");
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
            }
            {
                AnimatorStateTransition t = AddTimedExitTransition(stateMap["IdleWait_Right"], stateMap["IdleYawn_Right"], 1.0f);
                t.AddCondition(AnimatorConditionMode.Equals, 0, "idleIndex");
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
            }
            {
                AnimatorStateTransition t = AddTimedExitTransition(stateMap["IdleWait_Right"], stateMap["IdleGuitar_Right"], 1.0f);
                t.AddCondition(AnimatorConditionMode.Equals, 1, "idleIndex");
                t.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
            }

            // -- IdleWait / Play Idle -> Movement (speed > 0.1) ----------------
            foreach (string stateName in new[] { "IdleWait_Left", "IdleYawn_Left", "IdleGuitar_Left" })
            {
                AnimatorStateTransition t1 = AddTransition(stateMap[stateName], stateMap["Walk_Left"]);
                t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t1.AddCondition(AnimatorConditionMode.IfNot,   0,    "isRunning");
                t1.AddCondition(AnimatorConditionMode.If,      0,    "facingLeft");
                
                AnimatorStateTransition t2 = AddTransition(stateMap[stateName], stateMap["Sprint_Left"]);
                t2.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t2.AddCondition(AnimatorConditionMode.If,      0,    "isRunning");
                t2.AddCondition(AnimatorConditionMode.If,      0,    "facingLeft");
            }
            foreach (string stateName in new[] { "IdleWait_Right", "IdleYawn_Right", "IdleGuitar_Right" })
            {
                AnimatorStateTransition t1 = AddTransition(stateMap[stateName], stateMap["Walk_Right"]);
                t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t1.AddCondition(AnimatorConditionMode.IfNot,   0,    "isRunning");
                t1.AddCondition(AnimatorConditionMode.IfNot,   0,    "facingLeft");
                
                AnimatorStateTransition t2 = AddTransition(stateMap[stateName], stateMap["Sprint_Right"]);
                t2.AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
                t2.AddCondition(AnimatorConditionMode.If,      0,    "isRunning");
                t2.AddCondition(AnimatorConditionMode.IfNot,   0,    "facingLeft");
            }

            // -- Facing flips during idle (hasExitTime = false, duration = 0) --
            {
                AnimatorStateTransition t = AddTransition(stateMap["IdleWait_Left"], stateMap["IdleWait_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["IdleWait_Right"], stateMap["IdleWait_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["IdleYawn_Left"], stateMap["IdleYawn_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["IdleYawn_Right"], stateMap["IdleYawn_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["IdleGuitar_Left"], stateMap["IdleGuitar_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["IdleGuitar_Right"], stateMap["IdleGuitar_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // -- Walk <-> Sprint (isRunning toggle) ----------------------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Walk_Left"], stateMap["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Walk_Right"], stateMap["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Sprint_Left"], stateMap["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Sprint_Right"], stateMap["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
            }

            // -- Walk Left <-> Walk Right (facingLeft flip) --------------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Walk_Left"], stateMap["Walk_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Walk_Right"], stateMap["Walk_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // -- Sprint Left <-> Sprint Right (facingLeft flip) ----------------
            {
                AnimatorStateTransition t = AddTransition(stateMap["Sprint_Left"], stateMap["Sprint_Right"]);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            }
            {
                AnimatorStateTransition t = AddTransition(stateMap["Sprint_Right"], stateMap["Sprint_Left"]);
                t.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            }

            // ── 10. Commit changes ─────────────────────────────────────────────
            AnimatorControllerLayer[] allLayers = controller.layers;
            allLayers[0] = layer0;
            controller.layers = allLayers;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 11. Verification log ──────────────────────────────────────────
            Debug.Log(
                "[RebuildPlayerController] -- Verification Summary --\n" +
                "States created   : " + stateMap.Count + " / 14\n" +
                "Transitions wired: " + transitionCount + "\n" +
                "Default state    : " + DefaultStateName + "\n" +
                "Backup saved to  : " + BackupPath + "\n" +
                "Note: Fall clips updated to have >0s duration to fix Animator freezing.");

            Debug.Log("[RebuildPlayerController] Done. Open Window -> Animator to inspect the new state machine.");
        }

        private static void RebuildJumpClip(string clipName, string sheetName, bool reverse)
        {
            string clipPath = ClipDir + clipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            
            string sheetPath = "Assets/Sprites/Characters/" + sheetName + ".png";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[RebuildPlayerController] Could not find sprites at {sheetPath}");
                return;
            }

            List<Sprite> sprites = new List<Sprite>();
            foreach (Object asset in assets)
            {
                if (asset is Sprite s) sprites.Add(s);
            }
            
            sprites.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            if (reverse) sprites.Reverse();
            
            clip.frameRate = 8;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            
            EditorCurveBinding spriteBinding = new EditorCurveBinding {
                type = typeof(SpriteRenderer),
                path = "Sprite",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }
            
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
            EditorUtility.SetDirty(clip);
            Debug.Log($"[RebuildPlayerController] Regenerated {clipName} with {sprites.Count} frames at 8 FPS (Reversed: {reverse}).");
        }

        private static void CreateWaitClipIfMissing(string waitClipName, string sourceClipName)
        {
            string waitPath = ClipDir + waitClipName + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(waitPath) != null) return;

            string sourcePath = ClipDir + sourceClipName + ".anim";
            AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath);
            if (sourceClip == null) return; 

            Sprite frame1Sprite = null;
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
            foreach (var binding in bindings)
            {
                if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                {
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                    if (keys.Length > 0)
                    {
                        frame1Sprite = keys[0].value as Sprite;
                        break;
                    }
                }
            }

            if (frame1Sprite == null)
            {
                Debug.LogError($"Could not find frame 1 sprite in {sourceClipName}.");
                return;
            }

            AnimationClip waitClip = new AnimationClip();
            waitClip.frameRate = 4;
            
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(waitClip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(waitClip, settings);

            EditorCurveBinding spriteBinding = new EditorCurveBinding {
                type = typeof(SpriteRenderer),
                path = "Sprite",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] newKeys = new ObjectReferenceKeyframe[2];
            newKeys[0] = new ObjectReferenceKeyframe { time = 0f, value = frame1Sprite };
            newKeys[1] = new ObjectReferenceKeyframe { time = 5f, value = frame1Sprite };

            AnimationUtility.SetObjectReferenceCurve(waitClip, spriteBinding, newKeys);
            AssetDatabase.CreateAsset(waitClip, waitPath);
            Debug.Log($"[RebuildPlayerController] Created new wait clip: {waitPath}");
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

            string sourcePath = ClipDir + jumpClipName + ".anim";
            AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath);
            if (sourceClip == null) return;

            Sprite lastSprite = null;
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
            foreach (var binding in bindings)
            {
                if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                {
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                    if (keys.Length > 0)
                    {
                        lastSprite = keys[keys.Length - 1].value as Sprite;
                        break;
                    }
                }
            }

            if (lastSprite == null)
            {
                Debug.LogError($"Could not find last sprite in {jumpClipName}.");
                return;
            }

            fallClip.frameRate = 4;
            
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(fallClip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(fallClip, settings);

            EditorCurveBinding spriteBinding = new EditorCurveBinding {
                type = typeof(SpriteRenderer),
                path = "Sprite",
                propertyName = "m_Sprite"
            };

            // CRITICAL FIX FOR 4.8:
            // A 1-keyframe clip has a duration of 0.0 seconds. The Unity Animator state machine 
            // has a known bug where transitions (even condition-based ones) fail to evaluate 
            // when exiting a state with a 0-length clip.
            // We fix this by giving the clip a 2nd keyframe (same sprite) so it has an actual duration.
            ObjectReferenceKeyframe[] newKeys = new ObjectReferenceKeyframe[2];
            newKeys[0] = new ObjectReferenceKeyframe { time = 0f,    value = lastSprite };
            newKeys[1] = new ObjectReferenceKeyframe { time = 0.25f, value = lastSprite };

            AnimationUtility.SetObjectReferenceCurve(fallClip, spriteBinding, newKeys);
            EditorUtility.SetDirty(fallClip);
            Debug.Log($"[RebuildPlayerController] Regenerated fall clip {fallClipName} with sprite {lastSprite.name} (Duration fixed).");
        }
    }
}