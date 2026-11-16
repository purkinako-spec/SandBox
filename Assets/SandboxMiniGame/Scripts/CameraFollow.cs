using UnityEngine;

namespace SandBox.MiniGame
{
    /// <summary>
    /// Keeps the camera at a fixed offset from the player, looking at it.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target { get; set; }

        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -10f);

        private void LateUpdate()
        {
            if (Target == null)
            {
                return;
            }

            transform.position = Target.position + offset;
            transform.LookAt(Target.position);
        }
    }
}
