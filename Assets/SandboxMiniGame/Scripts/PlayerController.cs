using UnityEngine;

namespace SandBox.MiniGame
{
    /// <summary>
    /// Moves the player ball with WASD / arrow keys inside the arena.
    /// Uses a kinematic Rigidbody so trigger coins are detected.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;

        private Rigidbody _rigidbody;
        private Vector2 _input;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (_input.sqrMagnitude > 1f)
            {
                _input.Normalize();
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            Vector3 delta = new Vector3(_input.x, 0f, _input.y) * (moveSpeed * Time.fixedDeltaTime);
            if (delta.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector3 next = _rigidbody.position + delta;
            next.x = Mathf.Clamp(next.x, -ArenaSettings.HalfSize, ArenaSettings.HalfSize);
            next.z = Mathf.Clamp(next.z, -ArenaSettings.HalfSize, ArenaSettings.HalfSize);
            _rigidbody.MovePosition(next);

            // Roll the ball in the moving direction.
            Vector3 axis = Vector3.Cross(Vector3.up, delta.normalized);
            float angle = delta.magnitude / 0.5f * Mathf.Rad2Deg;
            transform.Rotate(axis, angle, Space.World);
        }
    }
}
