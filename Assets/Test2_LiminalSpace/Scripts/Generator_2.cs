using UnityEngine;

namespace LiminalSpace
{
    public class Generator_2 : LiminalSpaceGenerator
    {
        [SerializeField] protected GameObject targetObject1;
        [SerializeField] protected GameObject targetObject2;
        public override bool Generate()
        {
            if (prefabCatalog == null)
            {
                Debug.LogError("PrefabCatalog is not loaded. Cannot generate.");
                return false;
            }
            if (ObjectCheck())
            {
                return false;
            }
            GameObject prefabToSpawn1 = prefabCatalog.GetRandomPrefab();
            GameObject prefabToSpawn2 = prefabCatalog.GetRandomPrefab();
            if (prefabToSpawn1 == null || prefabToSpawn2 == null)
            {
                Debug.LogError("PrefabCatalog returned null prefab.");
                return false;
            }
            Instantiate(prefabToSpawn1, targetObject1.transform.position, Quaternion.identity);
            Instantiate(prefabToSpawn2, targetObject2.transform.position, Quaternion.identity);
            return true;
        }

        protected override bool ObjectCheck()
        {
            if (targetObject1 == null || targetObject2 == null)
            {
                Debug.LogError("targetObject1 or targetObject2 is not assigned in Generator_2.");
                return true; // treat as occupied
            }
            return (
                Physics.CheckSphere(targetObject1.transform.position, checkRadius)
                ||
                Physics.CheckSphere(targetObject2.transform.position, checkRadius));
        }
    }
}
