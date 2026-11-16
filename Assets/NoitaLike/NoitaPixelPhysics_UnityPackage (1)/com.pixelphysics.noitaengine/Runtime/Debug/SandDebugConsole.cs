// Unity Noita-Style Pixel Physics Engine
// SandDebugConsole.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelPhysics.Debug
{
    /// <summary>
    /// Comprehensive In-Engine Debug Console.
    /// Provides log filtering, live diagnostics, and simulation command execution.
    /// </summary>
    public class SandDebugConsole : MonoBehaviour
    {
        public enum LogFilter { All, Info, Warning, Error, Net, Physics }

        public struct ConsoleEntry
        {
            public string Time;
            public LogFilter Category;
            public string Message;
            public string Details;
        }

        public bool IsOpen = false;
        public KeyCode ToggleKey = KeyCode.BackQuote;

        private readonly List<ConsoleEntry> _logs = new List<ConsoleEntry>();
        private LogFilter _currentFilter = LogFilter.All;
        private string _searchFilter = "";
        private string _inputCommand = "";
        private Vector2 _scrollPos;

        [SerializeField] private FallingSandSimulationManager _manager;

        private void Awake()
        {
            Application.logMessageReceived += OnUnityLog;
            AddLog(LogFilter.Info, "Debug Console Initialized", "System ready");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnUnityLog;
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                IsOpen = !IsOpen;
            }
        }

        private void OnUnityLog(string message, string stackTrace, LogType type)
        {
            LogFilter cat = LogFilter.Info;
            if (type == LogType.Warning) cat = LogFilter.Warning;
            else if (type == LogType.Error || type == LogType.Exception) cat = LogFilter.Error;

            if (message.Contains("[SandNet]")) cat = LogFilter.Net;
            else if (message.Contains("[PixelPhysics]")) cat = LogFilter.Physics;

            AddLog(cat, message, stackTrace);
        }

        public void AddLog(LogFilter cat, string msg, string details)
        {
            _logs.Add(new ConsoleEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss.ff"),
                Category = cat,
                Message = msg,
                Details = details
            });
            if (_logs.Count > 100) _logs.RemoveAt(0);
        }

        private void OnGUI()
        {
            if (!IsOpen) return;

            GUI.Box(new Rect(20, Screen.height - 340, Screen.width - 40, 320), "Pixel Physics Debug Console");
            GUILayout.BeginArea(new Rect(30, Screen.height - 315, Screen.width - 60, 290));

            // Filter bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(50));
            foreach (LogFilter filter in Enum.GetValues(typeof(LogFilter)))
            {
                if (GUILayout.Toggle(_currentFilter == filter, filter.ToString(), "Button", GUILayout.Width(70)))
                {
                    _currentFilter = filter;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchFilter = GUILayout.TextField(_searchFilter, GUILayout.Width(140));
            GUILayout.EndHorizontal();

            // Logs list
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(190));
            foreach (var log in _logs)
            {
                if (_currentFilter != LogFilter.All && log.Category != _currentFilter) continue;
                if (!string.IsNullOrEmpty(_searchFilter) && !log.Message.ToLower().Contains(_searchFilter.ToLower())) continue;

                Color oldCol = GUI.color;
                if (log.Category == LogFilter.Error) GUI.color = Color.red;
                else if (log.Category == LogFilter.Warning) GUI.color = Color.yellow;
                else if (log.Category == LogFilter.Net) GUI.color = Color.cyan;

                GUILayout.Label($"[{log.Time}] [{log.Category}] {log.Message}");
                GUI.color = oldCol;
            }
            GUILayout.EndScrollView();

            // Command CLI input
            GUILayout.BeginHorizontal();
            GUILayout.Label(">", GUILayout.Width(20));
            _inputCommand = GUILayout.TextField(_inputCommand);
            if (GUILayout.Button("Execute", GUILayout.Width(90)) || (Event.current.isKey && Event.current.keyCode == KeyCode.Return))
            {
                ExecuteCommand(_inputCommand);
                _inputCommand = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void ExecuteCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            string[] parts = cmd.Trim().Split(' ');

            switch (parts[0].ToLower())
            {
                case "/clear":
                    _manager?.ActiveProvider?.Clear();
                    AddLog(LogFilter.Physics, "Simulation cleared.", "");
                    break;
                case "/provider":
                    if (parts.Length > 1 && parts[1].ToLower() == "cpu")
                        _manager?.SwitchProvider(ProviderMode.ForceCPUMultiThreaded);
                    else
                        _manager?.SwitchProvider(ProviderMode.ForceGPUComputeShader);
                    break;
                case "/gravity":
                    if (parts.Length > 1 && float.TryParse(parts[1], out float g))
                        _manager.Settings.Gravity = g;
                    break;
                default:
                    AddLog(LogFilter.Warning, $"Unknown command: {cmd}", "");
                    break;
            }
        }
    }
}
