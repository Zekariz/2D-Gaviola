using UnityEngine;

namespace YourGame.Gameplay.Obstacles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class SwingingHazard : MonoBehaviour
    {
        [SerializeField] private float _pushForce = 10f;
        
        private Rigidbody2D _rb;

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Push it to start swinging
            _rb.AddForce(Vector2.right * _pushForce, ForceMode2D.Impulse);
        }
    }
}
