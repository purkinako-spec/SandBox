using UnityEngine;

namespace LiminalSpace
{
    public class AreaManager : MonoBehaviour
    {
        public void Generate()
        {
            GetComponent<LiminalSpaceGenerator>().Generate();
        }
    }
}
