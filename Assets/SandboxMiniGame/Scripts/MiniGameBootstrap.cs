using UnityEngine;

namespace SandBox.MiniGame
{
    /// <summary>
    /// Shared settings of the mini game arena.
    /// </summary>
    public static class ArenaSettings
    {
        public const float HalfSize = 9f;
        public const float TimeLimitSeconds = 60f;
    }

    /// <summary>
    /// Builds the whole stage at runtime (ground, walls, player, coins,
    /// camera follow) so the scene file itself stays minimal:
    /// camera + light + this bootstrap + GameManager.
    /// </summary>
    public sealed class MiniGameBootstrap : MonoBehaviour
    {
        private static readonly Vector3[] CoinPositions =
        {
            new Vector3(4f, 0.6f, 4f),
            new Vector3(-4f, 0.6f, 4f),
            new Vector3(4f, 0.6f, -4f),
            new Vector3(-4f, 0.6f, -4f),
            new Vector3(7f, 0.6f, 0f),
            new Vector3(-7f, 0.6f, 0f),
            new Vector3(0f, 0.6f, 7f),
            new Vector3(0f, 0.6f, -7f),
            new Vector3(6f, 0.6f, 6f),
            new Vector3(-6f, 0.6f, -6f),
        };

        private void Start()
        {
            BuildGround();
            BuildWalls();
            Transform player = BuildPlayer();
            int coinCount = BuildCoins();
            AttachCameraFollow(player);

            GameManager.Instance?.StartGame(coinCount);
        }

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.35f, 0.6f, 0.35f));
        }

        private static void BuildWalls()
        {
            Material wallMaterial = CreateMaterial(new Color(0.55f, 0.55f, 0.6f));
            CreateWall("WallNorth", new Vector3(0f, 0.5f, 9.5f), new Vector3(20f, 1f, 1f), wallMaterial);
            CreateWall("WallSouth", new Vector3(0f, 0.5f, -9.5f), new Vector3(20f, 1f, 1f), wallMaterial);
            CreateWall("WallEast", new Vector3(9.5f, 0.5f, 0f), new Vector3(1f, 1f, 20f), wallMaterial);
            CreateWall("WallWest", new Vector3(-9.5f, 0.5f, 0f), new Vector3(1f, 1f, 20f), wallMaterial);
        }

        private static void CreateWall(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Transform BuildPlayer()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            player.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.95f, 0.45f, 0.15f));

            Rigidbody rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            player.AddComponent<PlayerController>();
            return player.transform;
        }

        private static int BuildCoins()
        {
            Material coinMaterial = CreateMaterial(new Color(1f, 0.8f, 0.1f));

            foreach (Vector3 position in CoinPositions)
            {
                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin";
                coin.transform.position = position;
                coin.transform.localScale = new Vector3(0.9f, 0.12f, 0.9f);
                coin.GetComponent<Renderer>().sharedMaterial = coinMaterial;
                coin.GetComponent<Collider>().isTrigger = true;
                coin.AddComponent<Coin>();
            }

            return CoinPositions.Length;
        }

        private static void AttachCameraFollow(Transform player)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }

            follow.Target = player;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }
    }
}
