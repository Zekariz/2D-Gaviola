using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace YourGame.Gameplay.UI
{
    public class PlayerLifeUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image[] _hearts;
        
        [Header("Settings")]
        [SerializeField] private float _heartFlickerDuration = 0.1f;

        public void Initialize(int startingLives)
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                _hearts[i].enabled = (i < startingLives);
            }
        }

        public void LoseLife(int remainingLives)
        {
            if (remainingLives >= 0 && remainingLives < _hearts.Length)
            {
                StartCoroutine(FlickerAndHideHeart(_hearts[remainingLives]));
            }
        }

        private IEnumerator FlickerAndHideHeart(Image heart)
        {
            for (int i = 0; i < 3; i++)
            {
                heart.enabled = false;
                yield return new WaitForSeconds(_heartFlickerDuration);
                heart.enabled = true;
                yield return new WaitForSeconds(_heartFlickerDuration);
            }
            heart.enabled = false;
        }
    }
}
