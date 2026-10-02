using UnityEngine;

namespace LiminalSpace
{
    public class GenerateArea : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            GetComponent<AreaManager>().Generate();
        }
    }
}
