// Unity Noita-Style Pixel Physics Engine
// FallingSandSimulationManager.cs
using System;
using UnityEngine;
using PixelPhysics.Core;
using PixelPhysics.Debug;

namespace PixelPhysics
{
    public enum ProviderMode
    {
        AutoDetect,
        ForceGPUComputeShader,
        ForceCPUMultiThreaded
    }

    [System.Serializable]
    public class SimulationSettings
    {
        [Range(0.1f, 5f)] public float Gravity = 1.0f;
        [Range(0.1f, 5f)] public float BurnSpeed = 1.0f;
        [Range(1, 10)] public int FluidDispersion = 4;
        [Range(0.5f, 3f)] public float ExplosionRadiusScale = 1.0f;
    }

    /// <summary>
    /// Central manager for the Pixel Physics simulation in Unity.
    /// Manages IPhysicsComputeProvider lifecycle and coordinates between GPU/CPU backends.
    /// </summary>
    public class FallingSandSimulationManager : MonoBehaviour
    {
        [Header("Simulation Resolution")]
        public int SimulationWidth = 320;
        public int SimulationHeight = 240;

        [Header("Provider Architecture")]
        [Tooltip("ForceCPUMultiThreaded executes 100% on CPU Job System. Stable, full cellular reactions, and 0% GPU TDR risk.")]
        public ProviderMode Mode = ProviderMode.ForceCPUMultiThreaded;
        public ComputeShader SandComputeShader;
        public Material SandRendererMaterial;

        [Header("Auto Display & Camera Setup")]
        [Tooltip("Automatically spawns a Quad facing the camera to render the simulation immediately with zero setup.")]
        public bool AutoCreateDisplayQuad = true;

        [Tooltip("Spawns sample terrain (ground, sand, water, gunpowder, lava) on start so the simulation is active immediately.")]
        public bool SpawnSampleTerrainOnStart = true;

        [Tooltip("Allows left-click to paint and right-click to spawn water in the Game view.")]
        public bool EnableMouseInteraction = true;

        [Header("Display Presentation")]
        [Tooltip("Direct screen presentation via GUI.DrawTexture. 100% immune to camera clipping, shader pipeline incompatibility, and lighting settings.")]
        public bool DirectScreenDisplay = true;

        [Header("Manual Display Target (Optional)")]
        public Renderer CustomTargetRenderer;

        [Header("Physics Tuning")]
        public SimulationSettings Settings = new SimulationSettings();

        public IPhysicsComputeProvider ActiveProvider { get; private set; }

        private GameObject _displayQuad;
        private byte _selectedBrushMaterial = 1; // 1 = Sand
        private int _brushRadius = 4;
        private Texture2D _simulationTexture;
        private Color32[] _colorBuffer;

        private void Awake()
        {
            InitializeEngine();
        }

        public void InitializeEngine()
        {
            ActiveProvider?.Dispose();

            bool useGpu = false;
            switch (Mode)
            {
                case ProviderMode.AutoDetect:
                    useGpu = SystemInfo.supportsComputeShaders && SandComputeShader != null;
                    break;
                case ProviderMode.ForceGPUComputeShader:
                    useGpu = true;
                    break;
                case ProviderMode.ForceCPUMultiThreaded:
                    useGpu = false;
                    break;
            }

            if (useGpu && SandComputeShader != null)
            {
                ActiveProvider = new GpuComputeShaderProvider(SandComputeShader);
                UnityEngine.Debug.Log($"[PixelPhysics] Initialized with GPU: {ActiveProvider.Name}");
            }
            else
            {
                ActiveProvider = new CpuFallbackProvider();
                UnityEngine.Debug.Log($"[PixelPhysics] Running on CPU Job System: {ActiveProvider.Name}");
            }

            ActiveProvider.Initialize(SimulationWidth, SimulationHeight, Settings);

            // Auto-detect and fix material if missing or assigned a standard non-pixel material (e.g. ArenaPlane)
            if (SandRendererMaterial == null || SandRendererMaterial.shader == null || SandRendererMaterial.shader.name != "PixelPhysics/SandRenderer")
            {
                Shader shader = Shader.Find("PixelPhysics/SandRenderer");
                if (shader != null)
                {
                    SandRendererMaterial = new Material(shader);
                    UnityEngine.Debug.Log("[PixelPhysics] Automatically created SandRendererMaterial using 'PixelPhysics/SandRenderer' shader.");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[PixelPhysics] 'PixelPhysics/SandRenderer' shader not found. Using Texture2D fallback on existing material.");
                }
            }

            // Connect compute buffer to render material
            if (SandRendererMaterial != null && SandRendererMaterial.HasProperty("_PixelBuffer"))
            {
                SandRendererMaterial.SetBuffer("_PixelBuffer", ActiveProvider.GetPixelComputeBuffer());
                SandRendererMaterial.SetInt("_Width", SimulationWidth);
                SandRendererMaterial.SetInt("_Height", SimulationHeight);
            }

            // Auto-create Screen Display Quad if no custom renderer is assigned
            if (AutoCreateDisplayQuad && CustomTargetRenderer == null)
            {
                SetupAutoDisplayQuad();
            }
            else if (CustomTargetRenderer != null && SandRendererMaterial != null)
            {
                CustomTargetRenderer.sharedMaterial = SandRendererMaterial;
            }

            // Spawn initial playground terrain
            if (SpawnSampleTerrainOnStart)
            {
                SpawnSampleTerrain();
            }
        }

        private void SetupAutoDisplayQuad()
        {
            if (_displayQuad != null) return;

            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();

            _displayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _displayQuad.name = "SandSimulation_AutoDisplayQuad";
            _displayQuad.transform.SetParent(this.transform);

            // Remove mesh collider to avoid physics raycast blocking
            Collider col = _displayQuad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = _displayQuad.GetComponent<MeshRenderer>();
            if (SandRendererMaterial != null)
            {
                mr.sharedMaterial = SandRendererMaterial;
            }

            if (cam != null)
            {
                float targetAspect = (float)SimulationWidth / SimulationHeight;
                float dist = 3.5f;

                // Position quad in front of camera and orient it to face the camera
                _displayQuad.transform.position = cam.transform.position + cam.transform.forward * dist;
                _displayQuad.transform.rotation = Quaternion.LookRotation(-cam.transform.forward, cam.transform.up);

                if (cam.orthographic)
                {
                    float orthoHeight = cam.orthographicSize * 1.8f;
                    float orthoWidth = orthoHeight * targetAspect;
                    _displayQuad.transform.localScale = new Vector3(orthoWidth, orthoHeight, 1f);
                }
                else
                {
                    float frustumHeight = 2.0f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.9f;
                    float frustumWidth = frustumHeight * targetAspect;
                    _displayQuad.transform.localScale = new Vector3(frustumWidth, frustumHeight, 1f);
                }
                UnityEngine.Debug.Log($"[PixelPhysics] Auto-aligned Display Quad facing Camera: {cam.name}");
            }
            else
            {
                _displayQuad.transform.position = new Vector3(0, 0, 5);
                _displayQuad.transform.localScale = new Vector3(16f, 12f, 1f);
            }
        }

        public void SpawnSampleTerrain()
        {
            if (ActiveProvider == null) return;
            int w = SimulationWidth;
            int h = SimulationHeight;

            // Stone floor
            for (int x = 0; x < w; x++)
            {
                ActiveProvider.SetPixel(x, h - 1, 9);
                ActiveProvider.SetPixel(x, h - 2, 9);
                ActiveProvider.SetPixel(x, h - 3, 9);
            }
            // Stone side walls
            for (int y = 0; y < h; y++)
            {
                ActiveProvider.SetPixel(0, y, 9);
                ActiveProvider.SetPixel(1, y, 9);
                ActiveProvider.SetPixel(w - 1, y, 9);
                ActiveProvider.SetPixel(w - 2, y, 9);
            }

            // Central mountain of sand
            ActiveProvider.DrawCircle(w / 2, h / 2 + 10, 26, 1); // Sand
            // Water pool on left
            ActiveProvider.DrawCircle(w / 4, h / 2 + 20, 18, 2); // Water
            // Gunpowder & wood on right
            ActiveProvider.DrawCircle(w * 3 / 4, h / 2 + 20, 20, 3); // Gunpowder
            // Wood scaffold
            for (int x = w / 4; x < w * 3 / 4; x++)
            {
                ActiveProvider.SetPixel(x, h / 2 + 42, 8); // Wood
                ActiveProvider.SetPixel(x, h / 2 + 43, 8);
            }
            // Molten lava suspended above
            ActiveProvider.DrawCircle(w / 2, h / 3, 12, 6); // Lava

            UnityEngine.Debug.Log("[PixelPhysics] Initial sample terrain spawned successfully!");
        }

        private void Update()
        {
            if (ActiveProvider == null) return;
            ActiveProvider.Step(Settings);

            // Path A: ComputeBuffer direct render (GPU mode with SandRenderer.shader)
            if (SandRendererMaterial != null && SandRendererMaterial.HasProperty("_PixelBuffer"))
            {
                SandRendererMaterial.SetBuffer("_PixelBuffer", ActiveProvider.GetPixelComputeBuffer());
                SandRendererMaterial.SetInt("_Width", SimulationWidth);
                SandRendererMaterial.SetInt("_Height", SimulationHeight);
            }

            // Path B: Universal Texture2D output (Guaranteed to work with ANY shader, URP Lit, Unlit, Standard)
            UpdateTextureOutput();

            // Mouse Painting Interaction
            if (EnableMouseInteraction)
            {
                HandleMouseInput();
            }
        }

        private void UpdateTextureOutput()
        {
            if (_simulationTexture == null || _simulationTexture.width != SimulationWidth || _simulationTexture.height != SimulationHeight)
            {
                _simulationTexture = new Texture2D(SimulationWidth, SimulationHeight, TextureFormat.RGBA32, false);
                _simulationTexture.filterMode = FilterMode.Point;
                _colorBuffer = new Color32[SimulationWidth * SimulationHeight];
            }

            byte[] pixels = ActiveProvider.GetPixelArrayDirect();
            // Invert Y mapping: Simulation row y=0 (Sky/Top) maps to Texture2D top row (Height - 1).
            // Simulation row y=Height-1 (Ground/Floor) maps to Texture2D bottom row 0.
            for (int y = 0; y < SimulationHeight; y++)
            {
                int simRow = y * SimulationWidth;
                int texRow = (SimulationHeight - 1 - y) * SimulationWidth;
                for (int x = 0; x < SimulationWidth; x++)
                {
                    _colorBuffer[texRow + x] = GetMaterialColor32(pixels[simRow + x]);
                }
            }
            _simulationTexture.SetPixels32(_colorBuffer);
            _simulationTexture.Apply(false, false);

            if (SandRendererMaterial != null)
            {
                SandRendererMaterial.mainTexture = _simulationTexture;
                if (SandRendererMaterial.HasProperty("_BaseMap")) SandRendererMaterial.SetTexture("_BaseMap", _simulationTexture);
                if (SandRendererMaterial.HasProperty("_MainTex")) SandRendererMaterial.SetTexture("_MainTex", _simulationTexture);
            }
        }

        private static Color32 GetMaterialColor32(byte mat)
        {
            switch (mat)
            {
                case 0: return new Color32(11, 15, 23, 255);    // Air (dark background)
                case 1: return new Color32(227, 185, 108, 255); // Sand (gold)
                case 2: return new Color32(59, 130, 246, 255);  // Water (blue)
                case 3: return new Color32(100, 105, 115, 255); // Gunpowder (grey)
                case 4: return new Color32(249, 115, 22, 255);  // Fire (orange)
                case 5: return new Color32(74, 222, 128, 255);  // Acid (green)
                case 6: return new Color32(239, 68, 68, 255);   // Lava (red)
                case 7: return new Color32(161, 98, 7, 255);    // Oil (brown)
                case 8: return new Color32(120, 75, 45, 255);   // Wood (dark wood)
                case 9: return new Color32(148, 163, 184, 255); // Stone (slate)
                case 10: return new Color32(100, 116, 139, 255);// Smoke
                case 11: return new Color32(203, 213, 225, 255);// Steam
                case 12: return new Color32(220, 38, 38, 255);  // TNT
                default: return new Color32(11, 15, 23, 255);
            }
        }

        private void HandleMouseInput()
        {
            Vector2 mouseUv = GetMouseSimulationUV();
            if (mouseUv.x < 0f || mouseUv.x > 1f || mouseUv.y < 0f || mouseUv.y > 1f) return;

            int sx = Mathf.Clamp((int)(mouseUv.x * SimulationWidth), 0, SimulationWidth - 1);
            int sy = Mathf.Clamp((int)(mouseUv.y * SimulationHeight), 0, SimulationHeight - 1);

            if (Input.GetMouseButton(0)) // Left Click = Draw Selected Material
            {
                ActiveProvider.DrawCircle(sx, sy, _brushRadius, _selectedBrushMaterial);
            }
            else if (Input.GetMouseButton(1)) // Right Click = Water
            {
                ActiveProvider.DrawCircle(sx, sy, _brushRadius, 2);
            }
            else if (Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Space)) // Middle Click / Space = Explode
            {
                ActiveProvider.Explode(sx, sy, 22, 25);
            }

            // Keyboard quick select
            if (Input.GetKeyDown(KeyCode.Alpha1)) _selectedBrushMaterial = 1; // Sand
            if (Input.GetKeyDown(KeyCode.Alpha2)) _selectedBrushMaterial = 2; // Water
            if (Input.GetKeyDown(KeyCode.Alpha3)) _selectedBrushMaterial = 3; // Gunpowder
            if (Input.GetKeyDown(KeyCode.Alpha4)) _selectedBrushMaterial = 4; // Fire
            if (Input.GetKeyDown(KeyCode.Alpha5)) _selectedBrushMaterial = 5; // Acid
            if (Input.GetKeyDown(KeyCode.Alpha6)) _selectedBrushMaterial = 6; // Lava
            if (Input.GetKeyDown(KeyCode.Alpha7)) _selectedBrushMaterial = 8; // Wood
            if (Input.GetKeyDown(KeyCode.Alpha8)) _selectedBrushMaterial = 9; // Stone
            if (Input.GetKeyDown(KeyCode.Alpha0)) _selectedBrushMaterial = 0; // Air / Eraser
        }

        private Vector2 GetMouseSimulationUV()
        {
            if (DirectScreenDisplay && _simulationTexture != null)
            {
                float simAspect = (float)SimulationWidth / SimulationHeight;
                float pad = 10f;
                float availW = Screen.width - pad * 2f;
                float availH = Screen.height - pad * 2f;
                float w = availW;
                float h = w / simAspect;
                if (h > availH)
                {
                    h = availH;
                    w = h * simAspect;
                }
                float x = (Screen.width - w) * 0.5f;
                float y = (Screen.height - h) * 0.5f;

                Vector2 mouse = Input.mousePosition;
                float guiY = Screen.height - mouse.y;
                float u = (mouse.x - x) / w;
                float v = (guiY - y) / h;
                return new Vector2(u, v);
            }

            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(Input.mousePosition) : new Ray();
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                return hit.textureCoord;
            }
            // Screen viewport fallback
            Vector2 mouseScreen = Input.mousePosition;
            return new Vector2(mouseScreen.x / Screen.width, 1.0f - (mouseScreen.y / Screen.height));
        }

        private void OnGUI()
        {
            // 1. Direct Screen Overlay: Always renders on any pipeline, camera, or shader!
            if (DirectScreenDisplay && _simulationTexture != null)
            {
                float simAspect = (float)SimulationWidth / SimulationHeight;
                float pad = 10f;
                float availW = Screen.width - pad * 2f;
                float availH = Screen.height - pad * 2f;
                float w = availW;
                float h = w / simAspect;
                if (h > availH)
                {
                    h = availH;
                    w = h * simAspect;
                }
                float x = (Screen.width - w) * 0.5f;
                float y = (Screen.height - h) * 0.5f;

                GUI.DrawTexture(new Rect(x, y, w, h), _simulationTexture, ScaleMode.StretchToFill, false);
            }

            // 2. On-screen Game View HUD
            GUI.Box(new Rect(10, 10, 420, 115), $"<b>Pixel Physics Engine ({ActiveProvider?.Name})</b>");
            GUILayout.BeginArea(new Rect(18, 30, 400, 90));
            GUILayout.Label("Left Click: Paint | Right Click: Water | Middle/Space: Explode | Keys 1-8: Material", GUI.skin.label);

            GUILayout.BeginHorizontal();
            string[] mats = { "Sand", "Water", "Powder", "Fire", "Acid", "Lava", "Wood", "Stone" };
            byte[] matIds = { 1, 2, 3, 4, 5, 6, 8, 9 };
            for (int i = 0; i < mats.Length; i++)
            {
                if (GUILayout.Toggle(_selectedBrushMaterial == matIds[i], mats[i], "Button", GUILayout.Width(44)))
                {
                    _selectedBrushMaterial = matIds[i];
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Respawn Sample Terrain & Reset", GUILayout.Height(22)))
            {
                ActiveProvider?.Clear();
                SpawnSampleTerrain();
            }
            DirectScreenDisplay = GUILayout.Toggle(DirectScreenDisplay, "Direct Screen Overlay", "Button", GUILayout.Width(150), GUILayout.Height(22));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public void SwitchProvider(ProviderMode newMode)
        {
            Mode = newMode;
            InitializeEngine();
        }

        private void OnDestroy()
        {
            ActiveProvider?.Dispose();
        }
    }
}
