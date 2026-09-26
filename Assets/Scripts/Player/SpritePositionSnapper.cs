using UnityEngine;

namespace YourGame.Gameplay.Player
{
    /// <summary>
    /// Attach to the Player''s "Sprite" child (the GameObject holding the
    /// SpriteRenderer). In LateUpdate, snaps the sprite''s world position to
    /// the nearest pixel boundary, eliminating sub-pixel blur without requiring
    /// the Pixel Perfect Camera component (which forces a fixed resolution).
    ///
    /// Only the visual (this transform) is snapped. The parent Player''s
    /// Rigidbody2D and physics run at full floating-point precision — this
    /// component only moves the rendered sprite, not the collider or physics body.
    ///
    /// A 1-pixel jitter during motion is expected and is the same trade-off
    /// made by Pixel Perfect Camera. It is only noticeable in static screenshots,
    /// not during normal gameplay.
    ///
    /// Set Pixels Per Unit to match the sprite import setting (158 for this project).
    /// </summary>
    [DefaultExecutionOrder(100)] // Run after all movement and physics has been applied.
    public class SpritePositionSnapper : MonoBehaviour
    {
        [SerializeField] private float _pixelsPerUnit = 158f;
        private Vector3 _originalLocalPos;

        private void Awake()
        {
            _originalLocalPos = transform.localPosition;
        }

        private void LateUpdate()
        {
            // Reset to the exact local offset to discard the previous frame's rounding error
            transform.localPosition = _originalLocalPos;

            Vector3 worldPos = transform.position;
            worldPos.x = Mathf.Round(worldPos.x * _pixelsPerUnit) / _pixelsPerUnit;
            worldPos.y = Mathf.Round(worldPos.y * _pixelsPerUnit) / _pixelsPerUnit;
            // z is intentionally not snapped
            transform.position = worldPos;
        }
    }
}
