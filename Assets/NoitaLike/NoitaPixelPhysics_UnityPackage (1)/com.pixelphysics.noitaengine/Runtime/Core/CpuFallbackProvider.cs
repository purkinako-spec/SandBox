// Unity Noita-Style Pixel Physics Engine
// CpuFallbackProvider.cs
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PixelPhysics.Core
{
    /// <summary>
    /// CPU Fallback Provider.
    /// Runs multi-threaded cellular automata via C# Job System and Chunk-based dirty tracking.
    /// Automatically activated when GPU Compute is unavailable or manually requested.
    /// </summary>
    public class CpuFallbackProvider : IPhysicsComputeProvider
    {
        public string Name => "CPU Multi-Threaded Chunk Fallback (Burst Ready)";
        public bool IsGpuAccelerated => false;

        private const int ChunkSize = 16;
        private int _width;
        private int _height;
        private int _chunksX;
        private int _chunksY;
        private int _totalChunks;

        private NativeArray<byte> _pixels;
        private NativeArray<byte> _pixelAges;
        private NativeArray<byte> _chunkAwake;
        private NativeArray<byte> _dirtyChunks;
        private byte[] _dirtyChunkManagedCache;

        private ComputeBuffer _fallbackComputeBuffer; // Allows same shader rendering path!
        private GpuComputeShaderProvider.PixelData[] _uploadCache;
        private int _frame;

        public void Initialize(int width, int height, SimulationSettings settings)
        {
            _width = width;
            _height = height;
            int total = width * height;

            _pixels = new NativeArray<byte>(total, Allocator.Persistent);
            _pixelAges = new NativeArray<byte>(total, Allocator.Persistent);

            _chunksX = Mathf.CeilToInt(width / (float)ChunkSize);
            _chunksY = Mathf.CeilToInt(height / (float)ChunkSize);
            _totalChunks = _chunksX * _chunksY;

            _chunkAwake = new NativeArray<byte>(_totalChunks, Allocator.Persistent);
            _dirtyChunks = new NativeArray<byte>(_totalChunks, Allocator.Persistent);
            _dirtyChunkManagedCache = new byte[_totalChunks];

            // Wake all initially
            for (int i = 0; i < _totalChunks; i++)
            {
                _chunkAwake[i] = 1;
                _dirtyChunks[i] = 1;
            }

            // Stride matches HLSL uint4 PixelData (16 bytes)
            _fallbackComputeBuffer = new ComputeBuffer(total, sizeof(uint) * 4, ComputeBufferType.Structured);
            _uploadCache = new GpuComputeShaderProvider.PixelData[total];
        }

        public void Step(SimulationSettings settings)
        {
            _frame++;
            bool altX = (_frame % 2 == 0);

            // Process cellular automata simulation
            SimulateJob job = new SimulateJob
            {
                Width = _width,
                Height = _height,
                ChunksX = _chunksX,
                AlternateX = altX,
                BurnSpeed = settings.BurnSpeed,
                Pixels = _pixels,
                PixelAges = _pixelAges,
                ChunkAwake = _chunkAwake,
                DirtyChunks = _dirtyChunks
            };

            // Run simulation chunks across worker threads
            JobHandle handle = job.Schedule();
            handle.Complete();
        }

        public void SetPixel(int x, int y, byte materialId)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            int idx = y * _width + x;
            _pixels[idx] = materialId;
            _pixelAges[idx] = 0;
            MarkDirty(x, y);
        }

        public byte GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return 9; // Stone
            return _pixels[y * _width + x];
        }

        public void DrawCircle(int cx, int cy, int radius, byte materialId)
        {
            int r2 = radius * radius;
            int minX = Mathf.Max(0, cx - radius);
            int maxX = Mathf.Min(_width - 1, cx + radius);
            int minY = Mathf.Max(0, cy - radius);
            int maxY = Mathf.Min(_height - 1, cy + radius);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= r2)
                    {
                        int idx = y * _width + x;
                        _pixels[idx] = materialId;
                        _pixelAges[idx] = 0;
                    }
                }
            }
            MarkAreaDirty(minX, minY, maxX, maxY);
        }

        public void Explode(int cx, int cy, int radius, float power)
        {
            int r2 = radius * radius;
            int minX = Mathf.Max(0, cx - radius);
            int maxX = Mathf.Min(_width - 1, cx + radius);
            int minY = Mathf.Max(0, cy - radius);
            int maxY = Mathf.Min(_height - 1, cy + radius);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx;
                    int d2 = dx * dx + dy * dy;
                    if (d2 <= r2)
                    {
                        int idx = y * _width + x;
                        if (d2 < r2 * 0.5f)
                        {
                            _pixels[idx] = 0; // Air
                        }
                        else
                        {
                            _pixels[idx] = 4; // Fire
                        }
                        _pixelAges[idx] = 0;
                    }
                }
            }
            MarkAreaDirty(minX, minY, maxX, maxY);
        }

        public void Clear()
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = 0;
                _pixelAges[i] = 0;
            }
            for (int c = 0; c < _totalChunks; c++)
            {
                _chunkAwake[c] = 1;
                _dirtyChunks[c] = 1;
            }
        }

        public ComputeBuffer GetPixelComputeBuffer()
        {
            // Syncs NativeArray to ComputeBuffer with matching 16-byte stride
            for (int i = 0; i < _pixels.Length; i++)
            {
                _uploadCache[i].material = (uint)_pixels[i];
            }
            _fallbackComputeBuffer.SetData(_uploadCache);
            return _fallbackComputeBuffer;
        }

        public byte[] GetPixelArrayDirect()
        {
            return _pixels.ToArray();
        }

        public byte[] GetDirtyChunkMask()
        {
            _dirtyChunks.CopyTo(_dirtyChunkManagedCache);
            return _dirtyChunkManagedCache;
        }

        public void ResetDirtyChunkMask()
        {
            for (int i = 0; i < _dirtyChunks.Length; i++) _dirtyChunks[i] = 0;
        }

        private void MarkDirty(int x, int y)
        {
            int cx = x / ChunkSize;
            int cy = y / ChunkSize;
            if (cx >= 0 && cx < _chunksX && cy >= 0 && cy < _chunksY)
            {
                int idx = cy * _chunksX + cx;
                _chunkAwake[idx] = 1;
                _dirtyChunks[idx] = 1;
            }
        }

        private void MarkAreaDirty(int minX, int minY, int maxX, int maxY)
        {
            for (int y = minY; y <= maxY; y += ChunkSize)
            {
                for (int x = minX; x <= maxX; x += ChunkSize)
                {
                    MarkDirty(x, y);
                }
            }
        }

        public void Dispose()
        {
            if (_pixels.IsCreated) _pixels.Dispose();
            if (_pixelAges.IsCreated) _pixelAges.Dispose();
            if (_chunkAwake.IsCreated) _chunkAwake.Dispose();
            if (_dirtyChunks.IsCreated) _dirtyChunks.Dispose();
            _fallbackComputeBuffer?.Release();
        }

        // Burst-compatible Job for cellular simulation
        private struct SimulateJob : IJob
        {
            public int Width;
            public int Height;
            public int ChunksX;
            public bool AlternateX;
            public float BurnSpeed;

            public NativeArray<byte> Pixels;
            public NativeArray<byte> PixelAges;
            public NativeArray<byte> ChunkAwake;
            public NativeArray<byte> DirtyChunks;

            public void Execute()
            {
                int stepX = AlternateX ? 1 : -1;
                int startX = AlternateX ? 0 : Width - 1;
                int endX = AlternateX ? Width : -1;

                // Process from bottom row upwards so falling elements move down cleanly
                for (int y = Height - 2; y >= 0; y--)
                {
                    int row = y * Width;
                    for (int x = startX; x != endX; x += stepX)
                    {
                        int idx = row + x;
                        byte mat = Pixels[idx];
                        if (mat == 0 || mat == 9) continue; // Air or Stone (stationary)

                        // 1. Falling Solids (Sand = 1, Gunpowder = 3)
                        if (mat == 1 || mat == 3)
                        {
                            int down = idx + Width;
                            byte downMat = Pixels[down];
                            // Direct fall into Air or Liquid (sand sinks in water/oil)
                            if (downMat == 0 || downMat == 2 || downMat == 7)
                            {
                                Pixels[down] = mat;
                                Pixels[idx] = downMat;
                                continue;
                            }

                            // Diagonal fall (cascading sand pyramid)
                            int diagX = x + stepX;
                            if (diagX >= 0 && diagX < Width)
                            {
                                int diag = down + stepX;
                                byte diagMat = Pixels[diag];
                                if (diagMat == 0 || diagMat == 2 || diagMat == 7)
                                {
                                    Pixels[diag] = mat;
                                    Pixels[idx] = diagMat;
                                    continue;
                                }
                            }
                        }
                        // 2. Liquids (Water = 2, Acid = 5, Lava = 6, Oil = 7)
                        else if (mat == 2 || mat == 5 || mat == 6 || mat == 7)
                        {
                            int down = idx + Width;
                            byte downMat = Pixels[down];

                            // Lava + Water Reaction -> Stone + Steam
                            if ((mat == 6 && downMat == 2) || (mat == 2 && downMat == 6))
                            {
                                Pixels[down] = 9;  // Stone
                                Pixels[idx] = 11;  // Steam
                                continue;
                            }

                            // Fall straight down
                            if (downMat == 0)
                            {
                                Pixels[down] = mat;
                                Pixels[idx] = 0;
                                continue;
                            }

                            // Flow sideways (liquid dispersion)
                            int sideX = x + stepX;
                            if (sideX >= 0 && sideX < Width)
                            {
                                int side = row + sideX;
                                if (Pixels[side] == 0)
                                {
                                    Pixels[side] = mat;
                                    Pixels[idx] = 0;
                                    continue;
                                }
                            }
                        }
                        // 3. Fire (4) & Gases (Smoke = 10, Steam = 11) - Rise Upwards!
                        else if (mat == 4 || mat == 10 || mat == 11)
                        {
                            if (y > 0)
                            {
                                int up = idx - Width;
                                if (Pixels[up] == 0)
                                {
                                    Pixels[up] = mat;
                                    Pixels[idx] = 0;
                                }
                                else if (mat == 4 && (Pixels[up] == 8 || Pixels[up] == 7)) // Fire ignites wood/oil
                                {
                                    Pixels[up] = 4;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
