using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyActionGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Spawning")]
        public GameObject enemyPrefab;
        public Transform[] spawnPoints;
        public float spawnInterval = 2.4f;
        public int maxEnemies = 8;

        public int Score { get; private set; }
        public int Kills { get; private set; }
        public bool IsGameOver { get; private set; }

        readonly HashSet<Enemy> aliveEnemies = new HashSet<Enemy>();

        void Awake()
        {
            Instance = this;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                List<Transform> points = new List<Transform>();
                Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
                foreach (Transform item in all)
                    if (item != transform && item.name.StartsWith("SpawnPoint"))
                        points.Add(item);
                spawnPoints = points.ToArray();
            }

            if (FindFirstObjectByType<GameHud>() == null)
                gameObject.AddComponent<GameHud>();

            StartCoroutine(SpawnEnemies());
        }

        void Update()
        {
            if (IsGameOver && GameInput.RestartPressedThisFrame)
                Restart();
            else if (!IsGameOver && Cursor.lockState != CursorLockMode.Locked && GameInput.FireHeld)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        IEnumerator SpawnEnemies()
        {
            while (true)
            {
                if (!IsGameOver)
                {
                    aliveEnemies.RemoveWhere(enemy => enemy == null);
                    if (aliveEnemies.Count < maxEnemies && spawnPoints != null && spawnPoints.Length > 0)
                    {
                        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                        if (spawnPoint != null)
                        {
                            GameObject instance = enemyPrefab != null
                                ? Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation)
                                : CreateFallbackEnemy(spawnPoint);
                            Enemy enemy = instance.GetComponent<Enemy>();
                            if (enemy != null) aliveEnemies.Add(enemy);
                        }
                    }
                }

                float interval = Mathf.Max(0.6f, spawnInterval - Kills * 0.04f);
                yield return new WaitForSeconds(interval);
            }
        }

        GameObject CreateFallbackEnemy(Transform spawnPoint)
        {
            GameObject enemy = new GameObject("Enemy");
            enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            CharacterController controller = enemy.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.up;
            enemy.AddComponent<Enemy>();
            return enemy;
        }

        public void EnemyDied(Enemy enemy)
        {
            aliveEnemies.Remove(enemy);
            if (IsGameOver) return;
            Kills++;
            Score += 100;
        }

        public void OnPlayerDied()
        {
            IsGameOver = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
