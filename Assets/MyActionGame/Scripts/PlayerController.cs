using UnityEngine;

namespace MyActionGame
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpHeight = 2f;
        public float gravity = -9.81f;

        [Header("Shooting")]
        public GameObject bulletPrefab; // Assign a simple sphere prefab in inspector
        public Transform firePoint;
        public float bulletSpeed = 20f;
        public float fireRate = 0.2f;
        private float nextFireTime = 0f;

        [Header("Health")]
        public int maxHealth = 100;
        private int currentHealth;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentHealth = maxHealth;
        }

        void Update()
        {
            HandleMovement();
            HandleShooting();
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
                velocity.y = -2f;

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (Input.GetButtonDown("Jump") && isGrounded)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        void HandleShooting()
        {
            if (Time.time >= nextFireTime && Input.GetButton("Fire1"))
            {
                nextFireTime = Time.time + fireRate;
                Shoot();
            }
        }

        void Shoot()
        {
            if (bulletPrefab == null || firePoint == null) return;
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
                rb.velocity = firePoint.forward * bulletSpeed;
            // Optional: destroy bullet after 2 seconds
            Destroy(bullet, 2f);
        }

        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            Debug.Log($"Player took {amount} damage. Health: {currentHealth}");
            if (currentHealth <= 0)
                Die();
        }

        void Die()
        {
            Debug.Log("Player died!");
            // Simple respawn or game over logic can be added here
            enabled = false;
        }
    }
}
