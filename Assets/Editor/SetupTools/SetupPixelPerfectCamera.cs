using UnityEditor;
using UnityEngine;

// In Unity 6 + URP 17, PixelPerfectCamera lives inside the URP package.
// No separate com.unity.2d.pixel-perfect package is required.
using UnityEngine.Rendering.Universal;

namespace YourGame.Editor
{
    /// <summary>
    /// Adds and configures a PixelPerfectCamera component on the Main Camera.
    ///
    /// Menu: Tools -> 2D-Gaviola -> Setup -> Add Pixel Perfect Camera
    ///
    /// Safe to re-run: skips component creation if already present, and
    /// re-applies the settings in case values need resetting.
    ///
    /// Settings applied:
    ///   Assets PPU              = 158  (matches the sprite import PPU from Milestone 1)
    ///   Reference Resolution    = 320 x 180  (standard low-res pixel-art target)
    ///   Upscale Render Texture  = true  (ensures pixel-perfect scaling at any window size)
    ///   Pixel Snapping          = true  (snaps sprites to pixel grid, eliminates sub-pixel blur)
    ///   Crop Frame              = None  (letterbox/pillarbox if aspect differs — adjustable)
    /// </summary>
    public static class SetupPixelPerfectCamera
    {
        private const int   AssetsPixelsPerUnit     = 158;
        private const int   ReferenceResolutionX    = 320;
        private const int   ReferenceResolutionY    = 180;

        [MenuItem("Tools/2D-Gaviola/Setup/Add Pixel Perfect Camera")]
        public static void Setup()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[SetupPixelPerfectCamera] No Camera tagged 'MainCamera' found in the scene. " +
                               "Ensure the Main Camera is present and tagged correctly.");
                return;
            }

            // Add component only if not already present.
            PixelPerfectCamera ppc = mainCam.GetComponent<PixelPerfectCamera>();
            bool wasAdded = ppc == null;
            if (wasAdded)
                ppc = Undo.AddComponent<PixelPerfectCamera>(mainCam.gameObject);

            // Apply settings (also re-applies on subsequent runs).
            ppc.assetsPPU           = AssetsPixelsPerUnit;
            ppc.refResolutionX      = ReferenceResolutionX;
            ppc.refResolutionY      = ReferenceResolutionY;
            ppc.upscaleRT           = true;   // renders at reference res, then scales up — prevents blur
            ppc.pixelSnapping       = true;   // snaps all sprite positions to pixel boundaries
            ppc.cropFrameX          = false;  // set to true to pillarbox if 16:9 doesn''t match
            ppc.cropFrameY          = false;  // set to true to letterbox

            EditorUtility.SetDirty(mainCam.gameObject);

            string action = wasAdded ? "Added and configured" : "Re-configured";
            Debug.Log("[SetupPixelPerfectCamera] " + action + " PixelPerfectCamera on '" +
                      mainCam.gameObject.name + "'.\n" +
                      "  Assets PPU            : " + AssetsPixelsPerUnit + "\n" +
                      "  Reference Resolution  : " + ReferenceResolutionX + " x " + ReferenceResolutionY + "\n" +
                      "  Upscale Render Texture: true\n" +
                      "  Pixel Snapping        : true\n" +
                      "Save the scene (Ctrl+S) to persist the change.");
        }
    }
}
