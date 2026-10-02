using UnityEngine;

namespace LiminalSpace
{
    public class AreaManager : MonoBehaviour
    {
        private bool IsGenerated = false;

        public void Generate()
        {
            if (!IsGenerated) IsGenerated = GetComponent<LiminalSpaceGenerator>().Generate();
        }
    }
}
