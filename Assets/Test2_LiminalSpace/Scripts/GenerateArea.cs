using UnityEngine;

namespace LiminalSpace
{
    public class GenerateArea : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var areaManager = FindObjectOfType<AreaManager>();
            if (areaManager != null)
            {
                areaManager.Generate();
            }
            else
            {
                Debug.LogError("AreaManager not found in scene. Ensure there is an AreaManager component active.");
            }
        }
    }
}
