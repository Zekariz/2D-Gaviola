using UnityEngine;
using YourGame.Gameplay.Player;

namespace YourGame.Gameplay.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class FinishLine : MonoBehaviour
    {
        private bool _hasFinished = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_hasFinished) return;

            if (collision.GetComponent<PlayerController>() != null)
            {
                _hasFinished = true;
                float time = RespawnManager.Instance.GetElapsedTime();
                
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                
                Debug.Log($"<color=green>[FinishLine] Course Completed! Time: {minutes:00}:{seconds:00}</color>");
            }
        }
    }
}
