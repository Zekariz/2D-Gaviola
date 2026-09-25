using UnityEngine;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.GetComponent<PlayerController>() != null)
            {
                RespawnManager.Instance.Respawn();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<PlayerController>() != null)
            {
                RespawnManager.Instance.Respawn();
            }
        }
    }
}
