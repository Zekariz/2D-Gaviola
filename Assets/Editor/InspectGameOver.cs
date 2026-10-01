using UnityEditor;
using UnityEngine;

public static class InspectGameOver
{
    public static void Inspect()
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/game-over.png"))
        {
            if (asset is Sprite s)
            {
                Debug.Log($"Sprite: {s.name} | Size: {s.rect.width}x{s.rect.height}");
            }
        }
    }
}
