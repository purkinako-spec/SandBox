using UnityEngine;

namespace MyActionGame
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 7f;
        public float jumpHeight = 1.6f;
        public float gravity = -24f;
        public float mouseSensitivity = 0.12f;

        [Header("Shooting")]
        public GameObject bulletPrefab;
        public Transform firePoint;
        public float bulletSpeed = 28f;
        public float fireRate = 0.18f;
        public int bulletDamage = 10;

        [Header("Health")]
        public int maxHealth = 100;

        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        CharacterController controller;
        Vector3 velocity;
        float nextFireTime;
        float yaw;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            CurrentHealth = maxHealth;
            yaw = transform.eulerAngles.y;
            EnsureVisual();
            EnsureFirePoint();
            EnsureCamera();
        }

        void Update()
        {
            if (IsDead) return;

            if (GameInput.UnlockCursorPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            HandleLook();
            HandleMovement();
            HandleShooting();
        }

        void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            yaw += GameInput.MouseDelta.x * mouseSensitivity;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void HandleMovement()
        {
            bool grounded = controller.isGrounded;
            if (grounded && velocity.y < 0f)
                velocity.y = -2f;

            Vector2 input = GameInput.MoveAxis;
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (GameInput.JumpPressedThisFrame && grounded)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        void HandleShooting()
        {
            if (!GameInput.FireHeld) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (Time.time < nextFireTime) return;

            Shoot();
            nextFireTime = Time.time + Mathf.Max(0.05f, fireRate);
        }

        void Shoot()
        {
            Vector3 direction = transform.forward;
            Vector3 spawnPosition = firePoint != null
                ? firePoint.position
                : transform.position + Vector3.up * 1.1f + direction * 0.85f;

            GameObject bullet = bulletPrefab != null
                ? Instantiate(bulletPrefab, spawnPosition, Quaternion.LookRotation(direction))
                : CreateFallbackBullet(spawnPosition, direction);

            Rigidbody body = bullet.GetComponent<Rigidbody>();
            if (body == null) body = bullet.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = direction * bulletSpeed;

            BulletProjectile projectile = bullet.GetComponent<BulletProjectile>();
            if (projectile == null) projectile = bullet.AddComponent<BulletProjectile>();
            projectile.Initialize(gameObject, bulletDamage);
        }

        static GameObject CreateFallbackBullet(Vector3 position, Vector3 direction)
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "Bullet";
            bullet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            bullet.transform.localScale = Vector3.one * 0.22f;
            Collider collider = bullet.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            return bullet;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth <= 0)
                Die();
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            enabled = false;
            GameManager.Instance?.OnPlayerDied();
        }

        public void ResetForRestart()
        {
            IsDead = false;
            enabled = true;
            CurrentHealth = maxHealth;
            velocity = Vector3.zero;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            yaw = 0f;
        }

        void EnsureVisual()
        {
            if (transform.Find("PlayerMesh") != null) return;
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.name = "PlayerMesh";
            Object.Destroy(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(transform, false);
            mesh.transform.localPosition = Vector3.up;
            Renderer renderer = mesh.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.12f, 0.55f, 0.95f);
        }

        void EnsureFirePoint()
        {
            if (firePoint != null) return;
            Transform existing = transform.Find("FirePoint");
            if (existing != null)
            {
                firePoint = existing;
                return;
            }

            GameObject muzzle = new GameObject("FirePoint");
            muzzle.transform.SetParent(transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 1.1f, 0.75f);
            firePoint = muzzle.transform;
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 7.5f, -12f);
            cameraObject.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            cameraObject.AddComponent<AudioListener>();
        }
    }
}
