// Unity Noita-Style Pixel Physics Engine
// SandHotReloadTweaker.cs
using UnityEngine;

namespace PixelPhysics.Debug
{
    /// <summary>
    /// Hot-Reload Variable Tweaker.
    /// Immediately propagates changes to compute shaders and Job parameters without engine restart.
    /// </summary>
    [ExecuteAlways]
    public class SandHotReloadTweaker : MonoBehaviour
    {
        [SerializeField] private FallingSandSimulationManager _manager;

        public void ApplyParametersInstantly(float gravity, float burnSpeed, int dispersion)
        {
            if (_manager != null)
            {
                _manager.Settings.Gravity = gravity;
                _manager.Settings.BurnSpeed = burnSpeed;
                _manager.Settings.FluidDispersion = dispersion;
                UnityEngine.Debug.Log($"[HotReload] Applied: Gravity={gravity}, BurnSpeed={burnSpeed}, Dispersion={dispersion}");
            }
        }
    }
}
