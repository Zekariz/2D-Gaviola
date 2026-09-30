using UnityEditor;
using UnityEngine;

namespace YourGame.Editor
{
    public class SetupTagsAndLayers
    {
        [MenuItem("Tools/2D-Gaviola/Setup/1. Setup Tags and Layers")]
        public static void Setup()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            // Add Hazard Tag
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            bool foundTag = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Hazard")
                {
                    foundTag = true;
                    break;
                }
            }
            if (!foundTag)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Hazard";
                Debug.Log("[SetupTagsAndLayers] Added 'Hazard' tag.");
            }

            // Set Layers 6 and 7
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            EnsureLayer(layersProp, 6, "Ground");
            EnsureLayer(layersProp, 7, "Hazard");

            tagManager.ApplyModifiedProperties();
            Debug.Log("[SetupTagsAndLayers] Done verifying tags and layers.");
        }

        private static void EnsureLayer(SerializedProperty layersProp, int index, string name)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(index);
            if (sp.stringValue != name)
            {
                sp.stringValue = name;
                Debug.Log($"[SetupTagsAndLayers] Set Layer {index} to '{name}'.");
            }
        }
    }
}
