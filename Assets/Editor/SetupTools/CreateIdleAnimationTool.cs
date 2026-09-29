using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

public class CreateIdleAnimationTool
{
    [MenuItem("Tools/Create Idle Animation")]
    public static void CreateIdleAnimation()
    {
        string spriteSheetPath = "Assets/Sprites/Characters/SpriteSheet2D.png";
        
        // Load all sprites from the spritesheet
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("Could not find SpriteSheet2D.png at " + spriteSheetPath + "! Make sure it's sliced and named correctly.");
            return;
        }

        // We want sprites 4 through 13
        List<Sprite> idleSprites = new List<Sprite>();
        for (int i = 4; i <= 13; i++)
        {
            string targetName = "SpriteSheet2D_" + i;
            Sprite s = assets.OfType<Sprite>().FirstOrDefault(x => x.name == targetName);
            if (s != null)
            {
                idleSprites.Add(s);
            }
        }

        if (idleSprites.Count == 0)
        {
            Debug.LogError("Could not find the sliced sprites (SpriteSheet2D_4 to SpriteSheet2D_13). Did you slice them in the Sprite Editor?");
            return;
        }

        // Ensure Animations folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        // Create Animation Clip
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 12f; // 12 FPS for retro feel

        // Set clip to loop
        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        // Create keyframes
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "Sprite"; // This targets the child object named "Sprite"
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[idleSprites.Count];
        for (int i = 0; i < idleSprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i / clip.frameRate;
            keyframes[i].value = idleSprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        AssetDatabase.CreateAsset(clip, "Assets/Animations/PlayerIdle.anim");

        // Create Animator Controller
        string controllerPath = "Assets/Animations/Player.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Add Idle State
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.states.FirstOrDefault(s => s.state.name == "Idle").state;
        if (idleState == null)
        {
            idleState = stateMachine.AddState("Idle");
            stateMachine.defaultState = idleState; // Make it the default
        }
        idleState.motion = clip;

        // Assign to Player
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = controller;
            }

            // Set the default sprite to the first frame
            Transform spriteChild = player.transform.Find("Sprite");
            if (spriteChild != null)
            {
                SpriteRenderer sr = spriteChild.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = idleSprites[0];
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Successfully created PlayerIdle animation and assigned it to the Player!");
    }
}
