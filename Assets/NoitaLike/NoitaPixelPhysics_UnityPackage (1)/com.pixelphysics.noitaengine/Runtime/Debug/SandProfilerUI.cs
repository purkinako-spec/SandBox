// Unity Noita-Style Pixel Physics Engine
// SandProfilerUI.cs
using System.Diagnostics;
using UnityEngine;

namespace PixelPhysics.Debug
{
    /// <summary>
    /// In-game Profiling UI.
    /// Visualizes step-by-step latency bottlenecks (Compute, VRAM transfer, dirty culling, network, blit).
    /// </summary>
    public class SandProfilerUI : MonoBehaviour
    {
        public bool ShowUI = true;

        public float ComputeDispatchMs { get; set; }
        public float VramTransferMs { get; set; }
        public float DirtyChunkScanMs { get; set; }
        public float NetSerializeMs { get; set; }
        public float RenderBlitMs { get; set; }

        private float _fps;
        private float _fpsTimer;
        private int _fpsCount;

        private void Update()
        {
            _fpsCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _fps = _fpsCount / _fpsTimer;
                _fpsCount = 0;
                _fpsTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (!ShowUI) return;

            GUI.Box(new Rect(10, 10, 280, 210), "Pixel Physics Step Profiler");
            GUILayout.BeginArea(new Rect(20, 35, 260, 180));

            GUILayout.Label($"FPS: {_fps:F1} ({1000f/_fps:F1} ms/frame)");
            GUILayout.Space(4);

            DrawTimingBar("1. Compute Dispatch", ComputeDispatchMs, Color.cyan);
            DrawTimingBar("2. VRAM Dirty Sync", VramTransferMs, Color.yellow);
            DrawTimingBar("3. Chunk Dirty Cull", DirtyChunkScanMs, Color.green);
            DrawTimingBar("4. UDP Net Pack", NetSerializeMs, Color.magenta);
            DrawTimingBar("5. Render Blit", RenderBlitMs, Color.blue);

            GUILayout.EndArea();
        }

        private void DrawTimingBar(string label, float ms, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(130));
            GUILayout.Label($"{ms:F2} ms", GUILayout.Width(60));
            GUILayout.EndHorizontal();
        }
    }
}
