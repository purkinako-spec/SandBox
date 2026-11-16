using UnityEngine;

namespace MyActionGame
{
    [RequireComponent(typeof(CharacterController))]
    public class Enemy : MonoBehaviour
    {
        [Header("Stats")]
        public int health = 30;
        public int damageOnTouch = 15;
        public float moveSpeed = 3.4f;
        public float detectionRadius = 24f;
        public float contactRange = 1.35f;
        public float gravity = -24f;

        [Header("References")]
        public Transform player;

        CharacterController controller;
        Vector3 velocity;
        float nextDamageTime;
        bool isDead;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            EnsureHitVolume();
            EnsureVisual();
            if (player == null)
            {
                PlayerController found = FindFirstObjectByType<PlayerController>();
                if (found != null) player = found.transform;
            }
        }

        void Update()
        {
            if (isDead || controller == null) return;
            if (player == null)
            {
                PlayerController found = FindFirstObjectByType<PlayerController>();
                if (found != null) player = found.transform;
                else return;
            }

            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance <= detectionRadius && distance > 0.05f)
            {
                Vector3 direction = toPlayer / distance;
                controller.Move(direction * moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(direction);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            if (distance <= contactRange && Time.time >= nextDamageTime)
            {
                PlayerController target = player.GetComponent<PlayerController>();
                if (target != null && !target.IsDead)
                {
                    target.TakeDamage(damageOnTouch);
                    nextDamageTime = Time.time + 0.8f;
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || isDead) return;
            health -= amount;
            if (health <= 0)
                Die();
        }

        void Die()
        {
            if (isDead) return;
            isDead = true;
            GameManager.Instance?.EnemyDied(this);
            Destroy(gameObject);
        }

        void EnsureHitVolume()
        {
            if (GetComponent<CapsuleCollider>() != null) return;
            CapsuleCollider hit = gameObject.AddComponent<CapsuleCollider>();
            hit.isTrigger = true;
            hit.height = 2f;
            hit.radius = 0.5f;
            hit.center = Vector3.up;
        }

        void EnsureVisual()
        {
            if (transform.Find("EnemyMesh") != null) return;
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.name = "EnemyMesh";
            Destroy(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(transform, false);
            mesh.transform.localPosition = Vector3.up;
            Renderer renderer = mesh.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.9f, 0.12f, 0.08f);
        }
    }
}
