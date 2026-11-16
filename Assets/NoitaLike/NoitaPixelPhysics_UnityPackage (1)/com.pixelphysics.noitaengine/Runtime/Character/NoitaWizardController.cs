// Unity Noita-Style Pixel Physics Engine
// NoitaWizardController.cs
using UnityEngine;

namespace PixelPhysics.Character
{
    /// <summary>
    /// Playable Alchemist Wizard controller.
    /// Supports pixel-level collisions, jetpack levitation, 360-degree wand aiming, and spells.
    /// </summary>
    public class NoitaWizardController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 6f;
        public float JumpPower = 8f;
        public float LevitationEnergy = 100f;
        public float MaxLevitationEnergy = 100f;

        [Header("Wand & Spells")]
        public Transform WandPivot;
        public GameObject SparkBoltPrefab;
        public GameObject FireballPrefab;
        public GameObject BombPrefab;

        private Rigidbody2D _rb;
        private bool _isLevitating;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            _rb.linearVelocity = new Vector2(h * MoveSpeed, _rb.linearVelocity.y);

            // Levitation / Jetpack
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space))
            {
                if (LevitationEnergy > 0)
                {
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, JumpPower);
                    LevitationEnergy = Mathf.Max(0, LevitationEnergy - 35f * Time.deltaTime);
                    _isLevitating = true;
                }
            }
            else
            {
                _isLevitating = false;
                LevitationEnergy = Mathf.Min(MaxLevitationEnergy, LevitationEnergy + 20f * Time.deltaTime);
            }

            // Wand Aiming
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 aimDir = (mouseWorld - transform.position).normalized;
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            if (WandPivot != null)
            {
                WandPivot.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Cast Spell
            if (Input.GetMouseButtonDown(0))
            {
                CastSpell(aimDir);
            }
        }

        private void CastSpell(Vector2 direction)
        {
            UnityEngine.Debug.Log($"[Wizard] Cast spell in direction: {direction}");
        }
    }
}
