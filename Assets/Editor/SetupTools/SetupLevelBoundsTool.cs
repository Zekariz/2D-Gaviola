using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YourGame.Gameplay.Level;
using YourGame.Gameplay.Core;

namespace YourGame.Editor
{
    /// <summary>
    /// Installs a <see cref="LevelBoundsManager"/> into the active scene and wires
    /// it to the main camera.
    ///
    /// <para>
    /// Menu: <c>Tools → 2D-Gaviola → Setup → Setup Static Level Bounds</c>
    /// </para>
    /// </summary>
    public static class SetupLevelBoundsTool
    {
        private const string ManagerName = "LevelBoundsManager";

        // [MenuItem("Tools/2D-Gaviola/Setup/Setup Static Level Bounds")]
        public static void Setup()
        {
            // 1. Create or Find Manager
            GameObject go = GameObject.Find(ManagerName);
            if (go == null)
            {
                go = new GameObject(ManagerName);
                Undo.RegisterCreatedObjectUndo(go, "Create LevelBoundsManager");
                Debug.Log($"[SetupLevelBoundsTool] Created GameObject: {ManagerName}");
            }
            else
            {
                Debug.Log($"[SetupLevelBoundsTool] Found existing GameObject: {ManagerName}");
            }

            // 2. Add Component and Enable
            LevelBoundsManager mgr = go.GetComponent<LevelBoundsManager>();
            if (mgr == null)
            {
                mgr = Undo.AddComponent<LevelBoundsManager>(go);
            }
            
            SerializedObject serializedMgr = new SerializedObject(mgr);
            SerializedProperty groundLayerProp = serializedMgr.FindProperty("_groundLayer");
            if (groundLayerProp != null)
            {
                groundLayerProp.intValue = 1 << LayerMask.NameToLayer("Ground");
                serializedMgr.ApplyModifiedProperties();
            }

            go.SetActive(true); // Ensure GameObject is enabled
            mgr.enabled = true; // Ensure Component is enabled

            // 3. Wire Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
                if (camFollow != null)
                {
                    Undo.RecordObject(camFollow, "Wire CameraFollow Bounds");
                    
                    SerializedObject serializedCam = new SerializedObject(camFollow);
                    SerializedProperty boundsMgrProp = serializedCam.FindProperty("_boundsManager");
                    SerializedProperty useDynamicProp = serializedCam.FindProperty("_useDynamicBounds");
                    
                    if (boundsMgrProp != null) boundsMgrProp.objectReferenceValue = mgr;
                    if (useDynamicProp != null) useDynamicProp.boolValue = true;
                    
                    serializedCam.ApplyModifiedProperties();
                    Debug.Log("[SetupLevelBoundsTool] Wired CameraFollow to LevelBoundsManager.");
                }
            }

            // 4. Cleanup old static walls if they exist
            if (GameObject.Find("LevelBound_Left") != null || GameObject.Find("LevelBound_Right") != null)
            {
                Debug.LogWarning("[SetupLevelBoundsTool] Found existing 'LevelBound_Left' or 'LevelBound_Right' GameObjects in the scene. Please delete them manually, as walls are created at runtime.");
            }

            // Save Scene
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveScene(go.scene);

            Debug.Log("[SetupLevelBoundsTool] Done. Setup complete.");
            Selection.activeGameObject = go;
        }
    }
}
