using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyActionGame
{
    public static class SetupPlayableArena
    {
        const string Root = "Assets/MyActionGame";
        const string ArenaScenePath = Root + "/Scenes/ActionArena.unity";

        [MenuItem("My Action Game/Setup Playable Arena")]
        public static void Setup()
        {
            string prefabFolder = Root + "/Prefabs";
            string sceneFolder = Root + "/Scenes";
            string materialFolder = Root + "/Materials";
            string enemyPrefabPath = prefabFolder + "/Enemy.prefab";
            string bulletPrefabPath = prefabFolder + "/Bullet.prefab";

            if (!AssetDatabase.IsValidFolder(materialFolder))
                AssetDatabase.CreateFolder(Root, "Materials");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material groundMaterial = GetMaterial(materialFolder + "/ArenaGround.mat", shader, new Color(0.22f, 0.26f, 0.31f));
            Material playerMaterial = GetMaterial(materialFolder + "/ArenaPlayer.mat", shader, new Color(0.1f, 0.45f, 0.95f));
            Material enemyMaterial = GetMaterial(materialFolder + "/ArenaEnemy.mat", shader, new Color(0.9f, 0.12f, 0.08f));
            Material bulletMaterial = GetMaterial(materialFolder + "/ArenaBullet.mat", shader, new Color(1f, 0.78f, 0.05f));

            GameObject enemyRoot = new GameObject("Enemy");
            CharacterController enemyController = enemyRoot.AddComponent<CharacterController>();
            enemyController.height = 2f;
            enemyController.radius = 0.45f;
            enemyController.center = Vector3.up;
            Enemy enemyScript = enemyRoot.AddComponent<Enemy>();
            enemyScript.health = 30;
            enemyScript.damageOnTouch = 15;
            enemyScript.moveSpeed = 3.4f;
            enemyScript.detectionRadius = 24f;
            GameObject enemyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyVisual.name = "EnemyMesh";
            Object.DestroyImmediate(enemyVisual.GetComponent<Collider>());
            enemyVisual.transform.SetParent(enemyRoot.transform, false);
            enemyVisual.transform.localPosition = Vector3.up;
            enemyVisual.GetComponent<MeshRenderer>().sharedMaterial = enemyMaterial;
            PrefabUtility.SaveAsPrefabAsset(enemyRoot, enemyPrefabPath);
            Object.DestroyImmediate(enemyRoot);

            GameObject bulletRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletRoot.name = "Bullet";
            bulletRoot.transform.localScale = Vector3.one * 0.22f;
            bulletRoot.GetComponent<MeshRenderer>().sharedMaterial = bulletMaterial;
            bulletRoot.GetComponent<Collider>().isTrigger = true;
            Rigidbody bulletBody = bulletRoot.AddComponent<Rigidbody>();
            bulletBody.useGravity = false;
            bulletBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bulletRoot.AddComponent<BulletProjectile>();
            PrefabUtility.SaveAsPrefabAsset(bulletRoot, bulletPrefabPath);
            Object.DestroyImmediate(bulletRoot);
            AssetDatabase.SaveAssets();

            Scene arena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(32f, 1f, 32f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

            GameObject playerObject = new GameObject("Player");
            playerObject.tag = "Player";
            CharacterController playerController = playerObject.AddComponent<CharacterController>();
            playerController.height = 2f;
            playerController.radius = 0.45f;
            playerController.center = Vector3.up;
            PlayerController playerScript = playerObject.AddComponent<PlayerController>();
            playerScript.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPrefabPath);
            GameObject playerVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerVisual.name = "PlayerMesh";
            Object.DestroyImmediate(playerVisual.GetComponent<Collider>());
            playerVisual.transform.SetParent(playerObject.transform, false);
            playerVisual.transform.localPosition = Vector3.up;
            playerVisual.GetComponent<MeshRenderer>().sharedMaterial = playerMaterial;
            GameObject muzzle = new GameObject("FirePoint");
            muzzle.transform.SetParent(playerObject.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 1.1f, 0.75f);
            playerScript.firePoint = muzzle.transform;
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 7.5f, -12f);
            cameraObject.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            GameObject managerObject = new GameObject("GameManager");
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
            manager.spawnInterval = 2.4f;
            manager.maxEnemies = 8;
            managerObject.AddComponent<GameHud>();
            managerObject.AddComponent<ArenaBootstrap>();

            Vector3[] spawnPositions =
            {
                new Vector3(-12f, 0f, 10f), new Vector3(12f, 0f, 10f),
                new Vector3(-12f, 0f, -10f), new Vector3(12f, 0f, -10f)
            };
            List<Transform> spawnPoints = new List<Transform>();
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject spawn = new GameObject("SpawnPoint" + (i + 1));
                spawn.transform.position = spawnPositions[i];
                spawnPoints.Add(spawn.transform);
            }
            manager.spawnPoints = spawnPoints.ToArray();

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light sceneLight = lightObject.AddComponent<Light>();
            sceneLight.type = LightType.Directional;
            sceneLight.intensity = 1.2f;

            EditorSceneManager.MarkSceneDirty(arena);
            EditorSceneManager.SaveScene(arena, ArenaScenePath);
            EditorSceneManager.SaveScene(arena, sceneFolder + "/NewScene.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ArenaScenePath, true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
            Debug.Log("Playable action arena is ready. Open ActionArena and press Play.");
        }

        static Material GetMaterial(string assetPath, Shader shader, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
    }
}
