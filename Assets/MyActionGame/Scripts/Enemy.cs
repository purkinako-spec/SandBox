using UnityEngine;

namespace MyActionGame
{
    [RequireComponent(typeof(CharacterController))]
    public class Enemy : MonoBehaviour
    {
        [Header("Stats")]
        public int health = 30;
        public int damageOnTouch = 20;
        public float moveSpeed = 3f;
        public float detectionRadius = 10f;

        [Header("References")]
        public Transform player; // Assign player transform in inspector or find at runtime

        private CharacterController controller;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        void Update()
        {
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= detectionRadius)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0; // Keep movement on XZ plane
                controller.Move(direction * moveSpeed * Time.deltaTime);
                // Optional: look at player
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
            }

            // Simple collision damage via trigger
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.gameObject.CompareTag("Player"))
            {
                var playerHealth = hit.gameObject.GetComponent<MyActionGame.PlayerController>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(damageOnTouch);
            }
        }

        public void TakeDamage(int amount)
        {
            health -= amount;
            Debug.Log($"Enemy took {amount} damage. Health: {health}");
            if (health <= 0)
                Die();
        }

        void Die()
        {
            Debug.Log("Enemy died!");
            Destroy(gameObject);
        }
    }
}
