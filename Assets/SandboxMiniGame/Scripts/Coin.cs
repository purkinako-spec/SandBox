using UnityEngine;

namespace SandBox.MiniGame
{
    /// <summary>
    /// A collectible coin: spins, bobs, and reports to the GameManager
    /// when the player touches it.
    /// </summary>
    public sealed class Coin : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 120f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private float _baseY;
        private float _phase;

        private void Awake()
        {
            _baseY = transform.position.y;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            Vector3 position = transform.position;
            position.y = _baseY + Mathf.Sin(Time.time * bobSpeed + _phase) * bobAmplitude;
            transform.position = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            GameManager.Instance?.CollectCoin();
            Destroy(gameObject);
        }
    }
}
