using UnityEngine;

namespace MyActionGame
{
    [DefaultExecutionOrder(-100)]
    public class ArenaBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureArena()
        {
            if (FindFirstObjectByType<PlayerController>() == null &&
                FindFirstObjectByType<GameManager>() == null)
                return;

            BuildWallsIfMissing();
            if (FindFirstObjectByType<GameManager>() == null)
            {
                GameObject managerObject = new GameObject("GameManager");
                managerObject.AddComponent<GameManager>();
            }

            if (FindFirstObjectByType<GameHud>() == null)
                FindFirstObjectByType<GameManager>().gameObject.AddComponent<GameHud>();
        }

        static void BuildWallsIfMissing()
        {
            if (GameObject.Find("ArenaWalls") != null) return;

            GameObject root = new GameObject("ArenaWalls");
            Color wallColor = new Color(0.16f, 0.18f, 0.22f);
            CreateWall(root.transform, "North", new Vector3(0f, 1.5f, 16.5f), new Vector3(34f, 3f, 1f), wallColor);
            CreateWall(root.transform, "South", new Vector3(0f, 1.5f, -16.5f), new Vector3(34f, 3f, 1f), wallColor);
            CreateWall(root.transform, "East", new Vector3(16.5f, 1.5f, 0f), new Vector3(1f, 3f, 34f), wallColor);
            CreateWall(root.transform, "West", new Vector3(-16.5f, 1.5f, 0f), new Vector3(1f, 3f, 34f), wallColor);
        }

        static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }
    }
}
