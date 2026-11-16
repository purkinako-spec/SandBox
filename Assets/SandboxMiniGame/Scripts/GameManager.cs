using UnityEngine;
using UnityEngine.SceneManagement;

namespace SandBox.MiniGame
{
    /// <summary>
    /// Mini game state: score, timer, win / lose and the HUD.
    /// Everything of this game lives in the SandBox.MiniGame namespace
    /// and in its own assembly, isolated from the other projects.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsPlaying { get; private set; }

        private int _score;
        private int _totalCoins;
        private float _remainingTime;
        private bool _isGameOver;
        private bool _isClear;

        private GUIStyle _hudStyle;
        private GUIStyle _titleStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartGame(int totalCoins)
        {
            _totalCoins = totalCoins;
            _score = 0;
            _remainingTime = ArenaSettings.TimeLimitSeconds;
            _isGameOver = false;
            _isClear = false;
            IsPlaying = true;
        }

        public void CollectCoin()
        {
            if (!IsPlaying)
            {
                return;
            }

            _score++;
            if (_score >= _totalCoins)
            {
                Finish(true);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Restart();
                return;
            }

            if (!IsPlaying)
            {
                return;
            }

            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                Finish(false);
            }
        }

        private void Finish(bool clear)
        {
            _isClear = clear;
            _isGameOver = true;
            IsPlaying = false;
        }

        private void Restart()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.buildIndex >= 0)
            {
                SceneManager.LoadScene(active.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(active.name);
            }
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                };
                _hudStyle.normal.textColor = Color.white;

                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 42,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                _titleStyle.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(16f, 12f, 400f, 30f), $"Coins: {_score} / {_totalCoins}", _hudStyle);
            GUI.Label(new Rect(16f, 44f, 400f, 30f), $"Time: {Mathf.CeilToInt(_remainingTime)}", _hudStyle);
            GUI.Label(new Rect(16f, Screen.height - 36f, 600f, 30f), "Move: WASD / Arrow Keys    Restart: R", _hudStyle);

            if (_isGameOver)
            {
                string message = _isClear ? "GAME CLEAR!" : "TIME UP";
                GUI.Label(new Rect(0f, Screen.height * 0.5f - 60f, Screen.width, 60f), message, _titleStyle);
                GUI.Label(new Rect(0f, Screen.height * 0.5f, Screen.width, 40f), "Press R to restart", _titleStyle);
            }
        }
    }
}
