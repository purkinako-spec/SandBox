// Unity Noita-Style Pixel Physics Engine
// GpuComputeShaderProvider.cs
using System;
using UnityEngine;

namespace PixelPhysics.Core
{
    /// <summary>
    /// GPU Compute Shader Provider.
    /// Uses ping-pong StructuredBuffer<PixelData> and 4-phase checkerboard parallel dispatch.
    /// Minimizes CPU-GPU transfers by rendering directly via SandRenderer.shader.
    /// </summary>
    public class GpuComputeShaderProvider : IPhysicsComputeProvider
    {
        public string Name => "GPU Compute Shader (HLSL Parallel Dispatch)";
        public bool IsGpuAccelerated => true;

        private readonly ComputeShader _computeShader;
        private int _kernelMain;
        private int _width;
        private int _height;

        private ComputeBuffer _bufferA;
        private ComputeBuffer _bufferB;
        private ComputeBuffer _dirtyChunkBuffer;
        private ComputeBuffer _materialLookupBuffer;

        private bool _isPingPongPhase;
        private int _frameCount;
        private byte[] _cachedDirtyChunkMask;
        private int _chunkCount;

        public struct PixelData
        {
            public uint material;
            public uint age;
            public uint flags;
            public uint padding;
        }

        public GpuComputeShaderProvider(ComputeShader computeShader)
        {
            _computeShader = computeShader;
        }

        public void Initialize(int width, int height, SimulationSettings settings)
        {
            _width = width;
            _height = height;
            int totalPixels = width * height;

            _kernelMain = _computeShader.FindKernel("CSMain");

            // Allocate Ping-Pong StructuredBuffers with matching 16-byte stride (sizeof(uint) * 4)
            int stride = sizeof(uint) * 4; // Exactly 16 bytes, matching HLSL PixelData struct!
            _bufferA = new ComputeBuffer(totalPixels, stride, ComputeBufferType.Structured);
            _bufferB = new ComputeBuffer(totalPixels, stride, ComputeBufferType.Structured);

            // Clear buffers to 0 (Air)
            PixelData[] initialData = new PixelData[totalPixels];
            _bufferA.SetData(initialData);
            _bufferB.SetData(initialData);

            int chunksX = Mathf.CeilToInt(width / 16f);
            int chunksY = Mathf.CeilToInt(height / 16f);
            _chunkCount = chunksX * chunksY;
            _cachedDirtyChunkMask = new byte[_chunkCount];
            _dirtyChunkBuffer = new ComputeBuffer(_chunkCount, sizeof(uint), ComputeBufferType.Structured);

            _computeShader.SetInt("_GridWidth", _width);
            _computeShader.SetInt("_GridHeight", _height);
        }

        public void Step(SimulationSettings settings)
        {
            _frameCount++;
            _computeShader.SetInt("_FrameCount", _frameCount);
            _computeShader.SetFloat("_Gravity", settings.Gravity);
            _computeShader.SetFloat("_BurnSpeed", settings.BurnSpeed);

            // Bind current ping-pong buffers
            ComputeBuffer readBuf = _isPingPongPhase ? _bufferB : _bufferA;
            ComputeBuffer writeBuf = _isPingPongPhase ? _bufferA : _bufferB;

            // 1. First copy read buffer into write buffer so non-moving pixels and air remain intact
            int copyKernel = _computeShader.FindKernel("CSCopy");
            _computeShader.SetBuffer(copyKernel, "_ReadBuffer", readBuf);
            _computeShader.SetBuffer(copyKernel, "_WriteBuffer", writeBuf);
            _computeShader.Dispatch(copyKernel, Mathf.CeilToInt(_width / 8f), Mathf.CeilToInt(_height / 8f), 1);

            // 2. Dispatch cellular simulation
            _computeShader.SetBuffer(_kernelMain, "_ReadBuffer", readBuf);
            _computeShader.SetBuffer(_kernelMain, "_WriteBuffer", writeBuf);
            _computeShader.SetBuffer(_kernelMain, "_DirtyChunks", _dirtyChunkBuffer);

            // Dispatch 4 checkerboard sub-phases to avoid race conditions in cellular neighborhood
            for (int phase = 0; phase < 4; phase++)
            {
                _computeShader.SetInt("_CheckerboardPhase", phase);
                int groupsX = Mathf.CeilToInt(_width / 16f);
                int groupsY = Mathf.CeilToInt(_height / 16f);
                _computeShader.Dispatch(_kernelMain, groupsX, groupsY, 1);
            }

            _isPingPongPhase = !_isPingPongPhase;
        }

        public void SetPixel(int x, int y, byte materialId)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            // Immediate single-element update with matching uint struct
            PixelData[] single = new PixelData[1] { new PixelData { material = (uint)materialId, age = 0, flags = 0, padding = 0 } };
            int index = y * _width + x;
            GetCurrentWriteBuffer().SetData(single, 0, index, 1);
        }

        public byte GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return 9; // Stone
            PixelData[] res = new PixelData[1];
            GetCurrentReadBuffer().GetData(res, 0, y * _width + x, 1);
            return (byte)res[0].material;
        }

        public void DrawCircle(int cx, int cy, int radius, byte materialId)
        {
            int kernel = _computeShader.FindKernel("CSDrawCircle");
            _computeShader.SetInt("_BrushCX", cx);
            _computeShader.SetInt("_BrushCY", cy);
            _computeShader.SetInt("_BrushRadius", radius);
            _computeShader.SetInt("_BrushMaterial", materialId);
            _computeShader.SetBuffer(kernel, "_WriteBuffer", GetCurrentWriteBuffer());
            _computeShader.SetBuffer(kernel, "_DirtyChunks", _dirtyChunkBuffer);

            int boundsSize = radius * 2;
            int groupsX = Mathf.CeilToInt(boundsSize / 8f);
            int groupsY = Mathf.CeilToInt(boundsSize / 8f);
            _computeShader.Dispatch(kernel, Mathf.Max(1, groupsX), Mathf.Max(1, groupsY), 1);
        }

        public void Explode(int cx, int cy, int radius, float power)
        {
            int kernel = _computeShader.FindKernel("CSExplode");
            _computeShader.SetInt("_ExplodeCX", cx);
            _computeShader.SetInt("_ExplodeCY", cy);
            _computeShader.SetInt("_ExplodeRadius", radius);
            _computeShader.SetFloat("_ExplodePower", power);
            _computeShader.SetBuffer(kernel, "_WriteBuffer", GetCurrentWriteBuffer());
            _computeShader.SetBuffer(kernel, "_DirtyChunks", _dirtyChunkBuffer);

            int boundsSize = radius * 2;
            _computeShader.Dispatch(kernel, Mathf.CeilToInt(boundsSize / 8f), Mathf.CeilToInt(boundsSize / 8f), 1);
        }

        public void Clear()
        {
            PixelData[] empty = new PixelData[_width * _height];
            _bufferA.SetData(empty);
            _bufferB.SetData(empty);
        }

        public ComputeBuffer GetPixelComputeBuffer() => GetCurrentReadBuffer();

        public byte[] GetPixelArrayDirect()
        {
            // Only invoked when CPU explicitly requests whole buffer readback
            PixelData[] data = new PixelData[_width * _height];
            GetCurrentReadBuffer().GetData(data);
            byte[] bytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++) bytes[i] = (byte)data[i].material;
            return bytes;
        }

        public byte[] GetDirtyChunkMask()
        {
            _dirtyChunkBuffer.GetData(_cachedDirtyChunkMask);
            return _cachedDirtyChunkMask;
        }

        public void ResetDirtyChunkMask()
        {
            Array.Clear(_cachedDirtyChunkMask, 0, _cachedDirtyChunkMask.Length);
            _dirtyChunkBuffer.SetData(_cachedDirtyChunkMask);
        }

        private ComputeBuffer GetCurrentReadBuffer() => _isPingPongPhase ? _bufferB : _bufferA;
        private ComputeBuffer GetCurrentWriteBuffer() => _isPingPongPhase ? _bufferA : _bufferB;

        public void Dispose()
        {
            _bufferA?.Release();
            _bufferB?.Release();
            _dirtyChunkBuffer?.Release();
            _materialLookupBuffer?.Release();
        }
    }
}
