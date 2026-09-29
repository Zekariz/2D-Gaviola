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
            // Check if the object touching us has a PlayerController (or is a child HitBox of the player)
            if (collision.GetComponentInParent<YourGame.Gameplay.Player.PlayerController>() != null)
            {
                CoinManager.Instance?.AddCoin();
                gameObject.SetActive(false);
            }
        }
    }
}
