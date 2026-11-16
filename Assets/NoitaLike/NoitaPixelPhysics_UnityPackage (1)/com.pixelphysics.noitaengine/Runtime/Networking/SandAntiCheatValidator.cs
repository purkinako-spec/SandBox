// Unity Noita-Style Pixel Physics Engine
// SandAntiCheatValidator.cs
using System;
using UnityEngine;

namespace PixelPhysics.Networking
{
    /// <summary>
    /// Server-authoritative anti-cheat validation component.
    /// Inspects player packets and flags malicious behaviors.
    /// </summary>
    public class SandAntiCheatValidator : MonoBehaviour
    {
        [Header("Movement Sanity Limits")]
        public float MaxSpeedUnitsPerSecond = 14f;

        [Header("Wand Constraints")]
        public float MinCastIntervalSeconds = 0.08f;

        public event Action<string> OnCheatDetected;

        private Vector2 _lastPosition;
        private float _lastCastTime;

        public bool ValidateMovement(Vector2 newPos, float deltaTime)
        {
            if (deltaTime <= 0.0001f) return true;

            float distance = Vector2.Distance(newPos, _lastPosition);
            float speed = distance / deltaTime;

            if (speed > MaxSpeedUnitsPerSecond)
            {
                string msg = $"[AntiCheat] SpeedHack Detected: {speed:F1} u/s exceeds {MaxSpeedUnitsPerSecond} u/s!";
                UnityEngine.Debug.LogWarning(msg);
                OnCheatDetected?.Invoke(msg);
                return false;
            }

            _lastPosition = newPos;
            return true;
        }

        public bool ValidateWandCast()
        {
            float now = Time.time;
            if (now - _lastCastTime < MinCastIntervalSeconds)
            {
                string msg = $"[AntiCheat] Wand Cooldown Spam detected (interval {(now - _lastCastTime)*1000:F0}ms)!";
                UnityEngine.Debug.LogWarning(msg);
                OnCheatDetected?.Invoke(msg);
                return false;
            }

            _lastCastTime = now;
            return true;
        }

        public bool ValidateMaterialSpawn(byte matId)
        {
            if (matId > 13)
            {
                string msg = $"[AntiCheat] Illegal material ID {matId} injected!";
                UnityEngine.Debug.LogError(msg);
                OnCheatDetected?.Invoke(msg);
                return false;
            }
            return true;
        }
    }
}
