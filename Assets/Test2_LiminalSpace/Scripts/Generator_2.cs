using UnityEngine;

namespace LiminalSpace
{
    public class Generator_2 : LiminalSpaceGenerator
    {
        [SerializeField] protected GameObject targetObject1;
        [SerializeField] protected GameObject targetObject2;
        public override bool Generate()
        {
            GameObject prefabToSpawn1;
            GameObject prefabToSpawn2;

            prefabToSpawn1 = prefabCatalog.GetRandomPrefab();
            prefabToSpawn2 = prefabCatalog.GetRandomPrefab();

            Instantiate(prefabToSpawn1, targetObject1.transform.position, Quaternion.identity);
            Instantiate(prefabToSpawn2, targetObject2.transform.position, Quaternion.identity);

            return true;
        }

        protected override bool ObjectCheck()
        {
            return (
                Physics.CheckSphere(
                targetObject1.transform.position, checkRadius)
                ||
                Physics.CheckSphere(
                targetObject2.transform.position, checkRadius));
        }
    }
}
