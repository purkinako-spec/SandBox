using UnityEngine;
using System.Collections;

namespace MyActionGame
{
    public class GameManager : MonoBehaviour
    {
        [Header("Spawning")]
        public GameObject enemyPrefab; // Assign enemy prefab with Enemy script
        public Transform[] spawnPoints; // Assign empty GameObjects as spawn points
        public float spawnInterval = 5f;
        public int maxEnemies = 10;

        private int currentEnemyCount = 0;

        void Start()
        {
            StartCoroutine(SpawnEnemies());
        }

        IEnumerator SpawnEnemies()
        {
            while (true)
            {
                if (currentEnemyCount < maxEnemies && spawnPoints.Length > 0)
                {
                    int index = Random.Range(0, spawnPoints.Length);
                    Transform spawnPoint = spawnPoints[index];
                    Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                    currentEnemyCount++;
                }
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        // Called by Enemy when it dies
        public void EnemyDied()
        {
            currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
        }
    }
}
