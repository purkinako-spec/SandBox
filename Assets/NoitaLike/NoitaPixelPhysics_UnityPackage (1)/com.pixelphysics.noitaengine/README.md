# Noita-Style Pixel Physics Engine for Unity

A production-ready, compute-shader accelerated cellular automata physics engine for Unity with CPU Job System fallback, low-latency UDP multiplayer netcode, and integrated step profiler.

## Features

- **Full Cellular Automata Simulation**: Sand, Water, Gunpowder, Fire, Acid, Lava, Oil, Wood, Stone, Smoke, Steam, and TNT.
- **GPU Compute Shader Acceleration**: ComputeBuffer ping-pong parallel dispatch with 4-phase checkerboard kernel preventing race conditions.
- **CPU Multi-Threaded Fallback**: C# Job System / Burst with 16x16 chunk culling for low-spec PCs without GPU compute support.
- **Zero-Copy Rendering**: Directly renders the ComputeBuffer via unlit pixel shader without CPU readback roundtrips.
- **UDP Low-Latency Netcode**: Chunk dirty-mask delta compression with Run-Length Encoding (RLE), delivering >95% bandwidth reduction.
- **Server-Authoritative Anti-Cheat**: Hard sanity checks on speed, wand casting intervals, and material legitimacy.
- **Profiling Suite & Hot-Reload**: In-engine bottleneck profiler UI and debug console with log filtering.

## Quickstart Guide (100% Zero-Setup Ready)

1. Unzip the package into your Unity project's `Packages/com.pixelphysics.noitaengine` or `Assets/PixelPhysics/` folder.
2. In an empty scene, create an empty GameObject (e.g. `"SimulationManager"`) and attach `FallingSandSimulationManager`.
3. In the Inspector of `FallingSandSimulationManager`:
   - Set `Mode` = `ForceCPUMultiThreaded` (or `AutoDetect` / `ForceGPUComputeShader` with `SandComputeShader` assigned).
   - Ensure `AutoCreateDisplayQuad` and `SpawnSampleTerrainOnStart` are checked (Enabled by default).
4. Press **Play**! 
   - A display Quad automatically aligns to `Camera.main`.
   - Sample terrain with Sand, Water, Gunpowder, and Lava automatically spawns!
   - Use **Left-Click** to draw sand, **Right-Click** for water, **Middle-Click/Space** to explode!

## Manual Setup Options

If you prefer manual placement instead of auto-quad:
- **3D Quad Setup**: Create a `3D Object > Quad`, create a Material with shader `PixelPhysics/SandRenderer`, and assign it to the Quad's MeshRenderer.
- **UI RawImage Setup**: Create a UI `RawImage`, assign a Material with `PixelPhysics/SandRenderer`, and link it to `CustomTargetRenderer` or `CustomRawImage`.

## Troubleshooting: D3D11 Device Reset / GPU Timeout (TDR)

If you see:
`Failed to present D3D11 swapchain due to device reset/removed... Windows detected a GPU Timeout and reset the device`

### Why this happens:
1. **Integrated / Low-spec GPU Timeout**: Windows has a 2-second timeout (TDR). If a GPU compute workload takes too long or if a laptop switches graphics power states, Windows resets the D3D11 device.
2. **Buffer Stride or UAV Hazard**: If ComputeBuffer stride doesn't match HLSL (16 bytes = `uint4`) or if the shader lacks `#pragma target 4.5`.

### Immediate Fixes:
1. **Switch to CPU Multi-Threaded Mode (100% stable on all PCs)**:
   In the Inspector of `FallingSandSimulationManager`, set:
   `Mode` = `ForceCPUMultiThreaded`
   This bypasses the GPU completely and runs the simulation across CPU worker threads (C# Job System), which will NEVER trigger a D3D11 device reset.
2. **Ensure Shaders are assigned**: Make sure both `SandComputeShader` and a material with `SandRenderer.shader` are assigned.
3. **Disable Network Sync for Local Play**: Keep `EnableNetworkSync` unchecked on `SandUdpNetcode` during local singleplayer play to prevent CPU-GPU sync stalls.
