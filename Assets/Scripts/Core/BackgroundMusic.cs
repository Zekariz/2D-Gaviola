using UnityEngine;

namespace YourGame.Core
{
    /// <summary>
    /// Plays a single background music track on loop at game start.
    /// Attach to a persistent GameObject in the scene (e.g. "GameManager").
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip _musicClip;
        [SerializeField] [Range(0f, 1f)] private float _volume = 0.5f;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource          = GetComponent<AudioSource>();
            _audioSource.clip     = _musicClip;
            _audioSource.loop     = true;
            _audioSource.volume   = _volume;
            _audioSource.playOnAwake = false;

            if (_musicClip == null)
            {
                Debug.LogWarning("[BackgroundMusic] No AudioClip assigned. Assign LittlerootTown in the Inspector.");
                return;
            }

            _audioSource.Play();
        }
    }
}
