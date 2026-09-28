using System.Collections;
using UnityEngine;

namespace YourGame.Gameplay.Victory
{
    /// <summary>
    /// Static helper that fades the scene's BackgroundMusic AudioSource to silence.
    /// Works independently of the BackgroundMusic component so that component
    /// remains unmodified.
    /// </summary>
    public class BackgroundMusicFader : MonoBehaviour
    {
        private static BackgroundMusicFader _runner;

        /// <summary>
        /// Fades the first active AudioSource that is looping (background music)
        /// to zero volume over <paramref name="duration"/> real-time seconds.
        /// Uses <see cref="Time.unscaledDeltaTime"/> so it works at timeScale 0.
        /// </summary>
        public static void FadeOut(float duration = 0.5f)
        {
            // Find or create a persistent coroutine runner
            if (_runner == null)
            {
                var go = new GameObject("__BackgroundMusicFader");
                DontDestroyOnLoad(go);
                _runner = go.AddComponent<BackgroundMusicFader>();
            }

            _runner.StartCoroutine(_runner.DoFade(duration));
        }

        private IEnumerator DoFade(float duration)
        {
            // Locate the looping audio source that is playing (background music)
            AudioSource music = null;
            foreach (var src in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
            {
                if (src.loop && src.isPlaying)
                {
                    music = src;
                    break;
                }
            }

            if (music == null) yield break;

            float startVolume = music.volume;
            float elapsed     = 0f;

            while (elapsed < duration)
            {
                elapsed     += Time.unscaledDeltaTime;
                music.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            music.volume = 0f;
            music.Pause();
        }
    }
}
