using UnityEngine;

namespace MyActionGame
{
    public class GameHud : MonoBehaviour
    {
        GUIStyle titleStyle;
        GUIStyle bodyStyle;
        GUIStyle shadowStyle;

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = Color.white;

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.UpperLeft
            };
            bodyStyle.normal.textColor = Color.white;

            shadowStyle = new GUIStyle(bodyStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.65f);
        }

        void OnGUI()
        {
            EnsureStyles();
            GameManager manager = GameManager.Instance;
            PlayerController player = FindFirstObjectByType<PlayerController>();
            int health = player != null ? player.CurrentHealth : 0;
            int score = manager != null ? manager.Score : 0;
            int kills = manager != null ? manager.Kills : 0;

            string hud = $"HP  {health}\nSCORE  {score}\nKILLS  {kills}\n\nWASD 移動   マウス 旋回\n左クリック 射撃   Space ジャンプ\nEsc カーソル";
            DrawShadowed(new Rect(24f, 18f, 520f, 240f), hud, bodyStyle);

            if (manager != null && manager.IsGameOver)
            {
                DrawShadowed(new Rect(0f, Screen.height * 0.38f, Screen.width, 80f), "GAME OVER", titleStyle);
                DrawShadowed(new Rect(0f, Screen.height * 0.38f + 70f, Screen.width, 40f), "R でリスタート", bodyStyleCentered());
            }
        }

        GUIStyle bodyStyleCentered()
        {
            GUIStyle centered = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };
            return centered;
        }

        void DrawShadowed(Rect rect, string text, GUIStyle style)
        {
            Rect shadow = rect;
            shadow.x += 2f;
            shadow.y += 2f;
            GUI.Label(shadow, text, shadowStyle);
            GUI.Label(rect, text, style);
        }
    }
}
