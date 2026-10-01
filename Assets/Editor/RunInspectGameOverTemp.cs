using UnityEditor;

[InitializeOnLoad]
public static class RunInspectGameOver
{
    static RunInspectGameOver()
    {
        EditorApplication.delayCall += () =>
        {
            InspectGameOver.Inspect();
            AssetDatabase.DeleteAsset("Assets/Editor/RunInspectGameOverTemp.cs");
        };
    }
}
