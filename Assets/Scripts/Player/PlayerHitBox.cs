using UnityEngine;

namespace YourGame.Gameplay.Player
{
    /// <summary>
    /// Attached to a child GameObject of the Player called "HitBox".
    /// This is a trigger-only collider that is smaller than the physics body collider.
    /// It represents the "vulnerable" area of the player (body + forehead) that hazards can hit.
    /// The feet are excluded so that the player can stand ON top of hazards without instantly dying.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerHitBox : MonoBehaviour
    {
        // Marker component — KillZone checks for this to confirm the hazard
        // only registers when it touches the player's body, not their feet.
    }
}
