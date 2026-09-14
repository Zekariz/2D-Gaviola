using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

namespace Gaviola.Editor
{
    public class PlayerAnimatorBuilder
    {
        private const string ForwardSheetPath = "Assets/Sprites/Characters/SpriteSheet2D.png";
        private const string BackwardSheetPath = "Assets/Sprites/Characters/SpriteSheet2D-Backwards.png";
        private const string OutputDir = "Assets/Animations/Player";
        private const string ControllerPath = "Assets/Animations/Player/Player.controller";

        [MenuItem("Tools/2D-Gaviola/Animations/Build Player Animator")]
        public static void BuildAnimator()
        {
            // 1. Load Sprite Sheets
            var forwardSprites = LoadSprites(ForwardSheetPath);
            var backwardSprites = LoadSprites(BackwardSheetPath);

            if (forwardSprites.Count == 0 || backwardSprites.Count == 0)
            {
                Debug.LogError("Failed to load sprites. Check paths and ensure they are sliced as Multiple.");
                return;
            }

            // Ensure output directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Animations")) AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/Animations/Player")) AssetDatabase.CreateFolder("Assets/Animations", "Player");

            // 2. Build Clips
            AnimationClip idle = CreateClip("player_idle", forwardSprites, 4, 13, true);
            AnimationClip walkRight = CreateClip("player_walk_right", forwardSprites, 14, 23, true);
            AnimationClip walkLeft = CreateClip("player_walk_left", backwardSprites, 0, 9, true);
            AnimationClip runRight = CreateClip("player_run_right", forwardSprites, 24, 33, true);
            AnimationClip runLeft = CreateClip("player_run_left", backwardSprites, 10, 19, true);
            AnimationClip jumpRight = CreateClip("player_jump_right", forwardSprites, 34, 43, false);
            AnimationClip jumpLeft = CreateClip("player_jump_left", backwardSprites, 20, 29, false);
            
            // Placeholder Fall/Land (reuse idle first frame for now)
            AnimationClip fallRight = CreateClip("player_fall_right", forwardSprites, 4, 4, false);
            AnimationClip fallLeft = CreateClip("player_fall_left", backwardSprites, 4, 4, false); // Using index 4 as fallback

            // 3. Setup Controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            
            // Clear existing parameters to rebuild cleanly
            foreach (var p in controller.parameters) controller.RemoveParameter(p);
            
            controller.AddParameter("speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("velocityY", AnimatorControllerParameterType.Float);
            controller.AddParameter("isRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("facingLeft", AnimatorControllerParameterType.Bool);
            controller.AddParameter("jumpTrigger", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // Clear existing states to rebuild cleanly
            var existingStates = sm.states.ToList();
            foreach (var state in existingStates) sm.RemoveState(state.state);

            // Create States
            var sIdle = sm.AddState("Idle");
            sIdle.motion = idle;
            sm.defaultState = sIdle;

            var sWalkR = sm.AddState("WalkRight"); sWalkR.motion = walkRight;
            var sWalkL = sm.AddState("WalkLeft"); sWalkL.motion = walkLeft;
            var sRunR = sm.AddState("RunRight"); sRunR.motion = runRight;
            var sRunL = sm.AddState("RunLeft"); sRunL.motion = runLeft;
            var sJumpR = sm.AddState("JumpRight"); sJumpR.motion = jumpRight;
            var sJumpL = sm.AddState("JumpLeft"); sJumpL.motion = jumpLeft;
            var sFallR = sm.AddState("FallRight"); sFallR.motion = fallRight;
            var sFallL = sm.AddState("FallLeft"); sFallL.motion = fallLeft;

            // Helper for Transitions
            AnimatorStateTransition MakeTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string param, float threshold = 0f)
            {
                var t = from.AddTransition(to);
                t.hasExitTime = false;
                t.duration = 0f;
                t.AddCondition(mode, threshold, param);
                return t;
            }

            // Idle -> Walk
            MakeTransition(sIdle, sWalkR, AnimatorConditionMode.Greater, "speed", 0.1f).AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");
            MakeTransition(sIdle, sWalkL, AnimatorConditionMode.Greater, "speed", 0.1f).AddCondition(AnimatorConditionMode.If, 0, "facingLeft");
            
            // Walk -> Idle
            MakeTransition(sWalkR, sIdle, AnimatorConditionMode.Less, "speed", 0.1f);
            MakeTransition(sWalkL, sIdle, AnimatorConditionMode.Less, "speed", 0.1f);

            // Walk <-> Run
            MakeTransition(sWalkR, sRunR, AnimatorConditionMode.If, "isRunning");
            MakeTransition(sWalkL, sRunL, AnimatorConditionMode.If, "isRunning");
            MakeTransition(sRunR, sWalkR, AnimatorConditionMode.IfNot, "isRunning").AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");
            MakeTransition(sRunL, sWalkL, AnimatorConditionMode.IfNot, "isRunning").AddCondition(AnimatorConditionMode.Greater, 0.1f, "speed");

            // Run -> Idle
            MakeTransition(sRunR, sIdle, AnimatorConditionMode.Less, "speed", 0.1f);
            MakeTransition(sRunL, sIdle, AnimatorConditionMode.Less, "speed", 0.1f);

            // Direction Swaps during movement
            MakeTransition(sWalkR, sWalkL, AnimatorConditionMode.If, "facingLeft");
            MakeTransition(sWalkL, sWalkR, AnimatorConditionMode.IfNot, "facingLeft");
            MakeTransition(sRunR, sRunL, AnimatorConditionMode.If, "facingLeft");
            MakeTransition(sRunL, sRunR, AnimatorConditionMode.IfNot, "facingLeft");

            // Any -> Jump
            var jR = sm.AddAnyStateTransition(sJumpR);
            jR.hasExitTime = false; jR.duration = 0f;
            jR.AddCondition(AnimatorConditionMode.If, 0, "jumpTrigger");
            jR.AddCondition(AnimatorConditionMode.IfNot, 0, "facingLeft");

            var jL = sm.AddAnyStateTransition(sJumpL);
            jL.hasExitTime = false; jL.duration = 0f;
            jL.AddCondition(AnimatorConditionMode.If, 0, "jumpTrigger");
            jL.AddCondition(AnimatorConditionMode.If, 0, "facingLeft");

            // Jump -> Fall
            MakeTransition(sJumpR, sFallR, AnimatorConditionMode.Less, "velocityY", 0f);
            MakeTransition(sJumpL, sFallL, AnimatorConditionMode.Less, "velocityY", 0f);

            // Fall -> Land (Idle)
            MakeTransition(sFallR, sIdle, AnimatorConditionMode.If, "isGrounded");
            MakeTransition(sFallL, sIdle, AnimatorConditionMode.If, "isGrounded");

            AssetDatabase.SaveAssets();
            Debug.Log("✅ 9 clips created/updated, transitions wired successfully to Player.controller");
        }

        private static List<Sprite> LoadSprites(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = assets.OfType<Sprite>().ToList();
            
            // Sort by numerical suffix to ensure correct frame order, regardless of prefix formatting
            sprites.Sort((a, b) => 
            {
                int numA = ExtractNumber(a.name);
                int numB = ExtractNumber(b.name);
                return numA.CompareTo(numB);
            });
            return sprites;
        }

        private static int ExtractNumber(string name)
        {
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore < name.Length - 1)
            {
                if (int.TryParse(name.Substring(lastUnderscore + 1), out int num))
                    return num;
            }
            return 0;
        }

        private static AnimationClip CreateClip(string clipName, List<Sprite> sprites, int startIdx, int endIdx, bool loop)
        {
            string path = $"{OutputDir}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            else
            {
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Sprite"), null); // Clear existing
            }

            clip.frameRate = 12f;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorCurveBinding curveBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "Sprite",
                propertyName = "m_Sprite"
            };

            // Clamp indices just in case the sheet is shorter than expected
            startIdx = Mathf.Clamp(startIdx, 0, sprites.Count - 1);
            endIdx = Mathf.Clamp(endIdx, 0, sprites.Count - 1);
            
            int frameCount = Mathf.Abs(endIdx - startIdx) + 1;
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frameCount];
            
            int step = startIdx <= endIdx ? 1 : -1;
            int currentIdx = startIdx;

            for (int i = 0; i < frameCount; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[currentIdx]
                };
                currentIdx += step;
            }

            AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);
            return clip;
        }
    }
}
