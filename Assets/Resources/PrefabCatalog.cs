using System.Collections.Generic;
using UnityEngine;

namespace LiminalSpace
{
    [CreateAssetMenu(fileName = "PrefabCatalog", menuName = "ScriptableObjects/PrefabCatalog")]
    public class PrefabCatalog : ScriptableObject
    {
        [SerializeField] private List<GameObject> prefabs;

        // ランダムにプレハブを1つ選ぶ関数
        public GameObject GetRandomPrefab()
        {
            if (prefabs == null || prefabs.Count == 0) return null;
            int randomIndex = Random.Range(0, prefabs.Count);
            return prefabs[randomIndex];
        }
    }
}
