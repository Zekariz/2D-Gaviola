using UnityEngine;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Only kill if the HITBOX touches the hazard — not the feet/body physics collider.
            if (collision.GetComponent<PlayerHitBox>() != null)
            {
                RespawnManager.Instance.Respawn();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Fallback: If a solid collision somehow registers with the player
            if (collision.gameObject.GetComponentInChildren<PlayerHitBox>() != null)
            {
                // Verify the HitBox itself is what's touching us
                foreach (var contact in collision.contacts)
                {
                    if (contact.collider.GetComponent<PlayerHitBox>() != null)
                    {
                        RespawnManager.Instance.Respawn();
                        break;
                    }
                }
            }
        }
    }
}
