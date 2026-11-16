using UnityEngine;

namespace MyActionGame
{
    public class BulletProjectile : MonoBehaviour
    {
        [SerializeField] int damage = 10;
        [SerializeField] float lifetime = 2.5f;
        bool hasHit;

        public void Initialize(GameObject owner, int damageAmount)
        {
            damage = Mathf.Max(1, damageAmount);
            Collider bulletCollider = GetComponent<Collider>();
            if (bulletCollider != null)
                bulletCollider.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
                body.useGravity = false;

            if (owner == null) return;
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] bulletColliders = GetComponentsInChildren<Collider>();
            foreach (Collider ownerCollider in ownerColliders)
            foreach (Collider bulletColliderItem in bulletColliders)
                if (ownerCollider != null && bulletColliderItem != null)
                    Physics.IgnoreCollision(ownerCollider, bulletColliderItem);
        }

        void Awake()
        {
            Renderer bulletRenderer = GetComponent<Renderer>();
            if (bulletRenderer != null)
                bulletRenderer.material.color = new Color(1f, 0.82f, 0.12f);
            Destroy(gameObject, Mathf.Max(0.1f, lifetime));
        }

        void OnTriggerEnter(Collider other) => Hit(other);

        void OnCollisionEnter(Collision collision) => Hit(collision.collider);

        void Hit(Collider other)
        {
            if (hasHit || other == null) return;
            if (other.GetComponentInParent<PlayerController>() != null) return;

            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(damage);

            hasHit = true;
            Destroy(gameObject);
        }
    }
}
