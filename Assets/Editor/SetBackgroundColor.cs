using UnityEngine;
using UnityEditor;

public class SetBackgroundColor
{
    [MenuItem("Tools/Set Pink Background")]
    public static void SetColor()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color32(247, 131, 180, 255);
            
            // Mark the scene as dirty so Unity knows to save it
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(Camera.main.gameObject.scene);
            Debug.Log("Background color successfully updated to RGB(247, 131, 180)!");
        }
        else
        {
            Debug.LogError("Could not find the Main Camera!");
        }
    }
}
