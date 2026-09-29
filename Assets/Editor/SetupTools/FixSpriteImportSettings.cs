using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YourGame.Editor
{
    /// <summary>
    /// Fixes import settings on the 10 new player animation sprite sheets and
    /// re-slices each one into exactly 4 equal horizontal grid cells.
    ///
    /// Menu: Tools -> 2D-Gaviola -> Animation -> Fix Sprite Import Settings
    ///
    /// Safe to re-run (idempotent). Only the 10 target sheets are touched.
    /// The old sheets (SpriteSheet2D.png / SpriteSheet2D-Backwards.png) are
    /// never accessed by this tool.
    ///
    /// Pivot: BottomCenter (0.5, 0) -- required because each sheet has a slightly
    /// different canvas height (236-247 px). A Center pivot would shift the
    /// character's feet up or down by up to 5 px on animation transitions.
    /// </summary>
    public static class FixSpriteImportSettings
    {
        private const int FramesPerSheet = 4;

        private static readonly string[] _targetPaths =
        {
            "Assets/Sprites/Characters/idle1-yawn-l.png",
            "Assets/Sprites/Characters/idle1-yawn-r.png",
            "Assets/Sprites/Characters/idle2-guitar-l.png",
            "Assets/Sprites/Characters/idle2-guitar-r.png",
            "Assets/Sprites/Characters/walk-l.png",
            "Assets/Sprites/Characters/walk-r.png",
            "Assets/Sprites/Characters/sprint-l.png",
            "Assets/Sprites/Characters/sprint-r.png",
            "Assets/Sprites/Characters/jump-l.png",
            "Assets/Sprites/Characters/jump-r.png"
        };

        [MenuItem("Tools/2D-Gaviola/Animation/Fix Sprite Import Settings")]
        public static void FixSettings()
        {
            // -- Step 1: Verify all files exist and importers are accessible ------
            var importers = new List<(string path, TextureImporter importer)>();

            foreach (string path in _targetPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError(
                        "[FixSpriteImportSettings] MISSING FILE: " + path + "\n" +
                        "Aborting. Ensure all 10 sprite sheets are present before running.");
                    return;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError(
                        "[FixSpriteImportSettings] Could not get TextureImporter for: " + path + "\n" +
                        "Aborting.");
                    return;
                }

                importers.Add((path, importer));
            }

            // -- Step 2: Read raw PNG dimensions for every sheet -------------------
            // GetSourceTextureWidthAndHeight reads the raw PNG header without
            // loading the texture into memory and without requiring isReadable.
            var sheetDimensions = new List<(string path, TextureImporter importer, int w, int h)>();

            foreach ((string path, TextureImporter importer) in importers)
            {
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                sheetDimensions.Add((path, importer, width, height));
            }

            // -- Step 3: Note on varying heights ----------------------------------
            // Measured heights: 236-247 px across the 10 sheets.
            // No uniform-height guard is applied -- varying heights are expected.
            // BottomCenter pivot locks the feet to y=0 regardless of canvas height.

            // -- Step 4: Apply settings and re-slice each sheet -------------------
            // Batch all reimports so Unity triggers a single domain reload at the
            // end rather than one reload per sheet.
            int processedCount = 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach ((string path, TextureImporter importer, int texWidth, int texHeight) in sheetDimensions)
                {
                    string fileName = Path.GetFileName(path);
                    string baseName = Path.GetFileNameWithoutExtension(path);

                    // Warn if the width is not cleanly divisible.
                    // sprint-l.png is 893 px -- 893 / 4 = 223 with 1 px remainder.
                    if (texWidth % FramesPerSheet != 0)
                    {
                        Debug.LogWarning(
                            "[FixSpriteImportSettings] " + fileName + ": width " + texWidth +
                            " px is not evenly divisible by " + FramesPerSheet + ". " +
                            "Cell width truncated to " + (texWidth / FramesPerSheet) + " px. " +
                            "Verify the source art dimensions.");
                    }

                    int cellWidth  = texWidth / FramesPerSheet;
                    int cellHeight = texHeight;

                    // -- Texture import settings --
                    importer.textureType         = TextureImporterType.Sprite;
                    importer.spriteImportMode    = SpriteImportMode.Multiple;
                    importer.spritePixelsPerUnit = 158f;
                    importer.filterMode          = FilterMode.Point;
                    importer.textureCompression  = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled       = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode            = TextureWrapMode.Clamp;

                    // -- Build the 4-cell SpriteMetaData array --
                    // Unity spritesheet rects use bottom-left origin (y=0 = bottom of texture).
                    // Each frame is a full-height column at x = i * cellWidth, y = 0.
                    // BottomCenter pivot: feet are always at the sprite's y=0 edge,
                    // so the player stands on the ground consistently across all states.
                    var spritesheet = new SpriteMetaData[FramesPerSheet];

                    for (int i = 0; i < FramesPerSheet; i++)
                    {
                        spritesheet[i] = new SpriteMetaData
                        {
                            name      = baseName + "_" + i,
                            rect      = new Rect(i * cellWidth, 0, cellWidth, cellHeight),
                            alignment = (int) SpriteAlignment.BottomCenter,
                            pivot     = new Vector2(0.5f, 0f)
                        };
                    }

                    importer.spritesheet = spritesheet;

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();

                    processedCount++;

                    Debug.Log(
                        "[FixSpriteImportSettings] [" + fileName + "] " +
                        texWidth + "x" + texHeight + " px" +
                        " -> cells " + cellWidth + "x" + cellHeight +
                        " -> " + FramesPerSheet + " sprites OK");
                }
            }
            finally
            {
                // Always stop editing even if an exception occurs.
                // Leaving AssetEditing open would lock the asset pipeline until restart.
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // -- Step 5: Verification pass ----------------------------------------
            Debug.Log("[FixSpriteImportSettings] -- Verification Pass --");

            foreach (string path in _targetPaths)
            {
                string fileName    = Path.GetFileName(path);
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);

                int spriteCount = 0;
                foreach (Object asset in allAssets)
                {
                    if (asset is Sprite)
                    {
                        spriteCount++;
                    }
                }

                if (spriteCount == FramesPerSheet)
                {
                    Debug.Log("[FixSpriteImportSettings] " + fileName + ": " + spriteCount + " sprites OK");
                }
                else
                {
                    Debug.LogWarning(
                        "[FixSpriteImportSettings] " + fileName +
                        ": expected " + FramesPerSheet + " sprites, found " + spriteCount +
                        ". FAIL -- inspect this sheet manually.");
                }
            }

            Debug.Log(
                "[FixSpriteImportSettings] Done. " +
                processedCount + "/" + _targetPaths.Length + " sheets processed. " +
                "Point filter, No compression, 158 PPU, BottomCenter pivot, 4 equal horizontal slices applied.");
        }
    }
}