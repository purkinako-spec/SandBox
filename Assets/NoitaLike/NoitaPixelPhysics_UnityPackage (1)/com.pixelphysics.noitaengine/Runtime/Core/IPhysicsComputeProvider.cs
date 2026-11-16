// Unity Noita-Style Pixel Physics Engine
// IPhysicsComputeProvider.cs
using System;
using UnityEngine;

namespace PixelPhysics.Core
{
    /// <summary>
    /// Unified computation provider interface.
    /// Enables transparent runtime switching between GPU Compute Shader and CPU Job System fallbacks.
    /// </summary>
    public interface IPhysicsComputeProvider : IDisposable
    {
        string Name { get; }
        bool IsGpuAccelerated { get; }

        void Initialize(int width, int height, SimulationSettings settings);
        void Step(SimulationSettings settings);

        void SetPixel(int x, int y, byte materialId);
        byte GetPixel(int x, int y);
        void DrawCircle(int cx, int cy, int radius, byte materialId);
        void Explode(int cx, int cy, int radius, float power);
        void Clear();

        ComputeBuffer GetPixelComputeBuffer();
        byte[] GetPixelArrayDirect();
        byte[] GetDirtyChunkMask();
        void ResetDirtyChunkMask();
    }
}
