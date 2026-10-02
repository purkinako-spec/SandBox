using UnityEngine;

namespace LiminalSpace
{
    public class LiminalSpaceGenerator : MonoBehaviour
    {
        protected PrefabCatalog prefabCatalog;

        protected void Awake()
        {
            // Resourcesフォルダの「PrefabCatalog」という名前のファイルから取得
            prefabCatalog = Resources.Load<PrefabCatalog>("PrefabCatalog");

            // 見つからなかった場合のエラーチェック
            if (prefabCatalog == null)
            {
                Debug.LogError("【エラー】Resourcesフォルダの中に 'PrefabCatalog' アセットが見つかりません！名前と配置を確認してください。");
            }
        }

        [SerializeField] private GameObject targetObject;
        public virtual bool Generate()
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
            GameObject prefabToSpawn = prefabCatalog.GetRandomPrefab();
            if (prefabToSpawn == null)
            {
                Debug.LogError("PrefabCatalog returned null prefab.");
                return false;
            }
            Instantiate(prefabToSpawn, targetObject.transform.position, Quaternion.identity);
            return true;
        }

        [SerializeField] protected float checkRadius = 0.5f;
        [SerializeField] protected float checkDistance = 0.6f;
        protected virtual bool ObjectCheck()
        {
            return Physics.CheckSphere(targetObject.transform.position, checkRadius);
        }
    }
}
