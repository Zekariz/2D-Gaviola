using UnityEngine;

namespace YourGame.Gameplay.Coins
{
    /// <summary>
    /// Attached to each Coin GameObject in the scene.
    /// When the Player touches it, notifies CoinManager and hides itself.
    /// Because it uses SetActive(false) instead of Destroy, reloading the scene
    /// naturally resets all coins.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                CoinManager.Instance?.AddCoin();
                gameObject.SetActive(false);
            }
        }
    }
}
