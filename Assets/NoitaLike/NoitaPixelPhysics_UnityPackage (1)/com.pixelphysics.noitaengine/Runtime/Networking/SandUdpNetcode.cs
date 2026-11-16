// Unity Noita-Style Pixel Physics Engine
// SandUdpNetcode.cs
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using PixelPhysics.Core;

namespace PixelPhysics.Networking
{
    /// <summary>
    /// UDP low-latency network synchronization manager.
    /// Dispatches compressed dirty chunk deltas and synchronizes player actions.
    /// </summary>
    public class SandUdpNetcode : MonoBehaviour
    {
        [Header("UDP Socket Config")]
        public int Port = 7777;
        public string RemoteHost = "127.0.0.1";
        public bool IsServer = true;

        [Header("Network Tickrate")]
        [Range(10, 60)] public int TickRate = 30;

        [Header("Network Sync Toggle")]
        [Tooltip("Enable when participating in multiplayer rooms. Keep false for local singleplayer to avoid GPU readback stalls.")]
        public bool EnableNetworkSync = false;

        private UdpClient _udpClient;
        private IPEndPoint _remoteEndpoint;
        private float _tickTimer;

        [SerializeField] private FallingSandSimulationManager _simManager;
        [SerializeField] private SandAntiCheatValidator _antiCheat;

        private void Start()
        {
            if (!EnableNetworkSync) return;

            try
            {
                _udpClient = new UdpClient(IsServer ? Port : 0);
                _remoteEndpoint = new IPEndPoint(IPAddress.Parse(RemoteHost), Port);
                UnityEngine.Debug.Log($"[SandNet] UDP socket bound on port {Port}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SandNet] Failed to bind UDP socket: {ex.Message}");
            }
        }

        private void Update()
        {
            if (!EnableNetworkSync) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= (1f / TickRate))
            {
                _tickTimer = 0f;
                SendDeltaUpdate();
            }

            ReceivePackets();
        }

        private void SendDeltaUpdate()
        {
            if (!EnableNetworkSync) return;
            if (_simManager == null || _simManager.ActiveProvider == null) return;

            byte[] dirtyMask = _simManager.ActiveProvider.GetDirtyChunkMask();
            byte[] pixelData = _simManager.ActiveProvider.GetPixelArrayDirect();

            byte[] payload = SandChunkCompressor.CompressChunks(
                pixelData,
                dirtyMask,
                _simManager.SimulationWidth,
                _simManager.SimulationHeight
            );

            if (payload != null && payload.Length > 0)
            {
                _udpClient?.Send(payload, payload.Length, _remoteEndpoint);
                _simManager.ActiveProvider.ResetDirtyChunkMask();
            }
        }

        private void ReceivePackets()
        {
            if (_udpClient == null) return;

            while (_udpClient.Available > 0)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpClient.Receive(ref sender);

                if (SandChunkCompressor.DecompressChunks(data, _simManager.ActiveProvider, _simManager.SimulationWidth, _simManager.SimulationHeight))
                {
                    // Successfully applied remote state
                }
            }
        }

        private void OnDestroy()
        {
            _udpClient?.Close();
        }
    }
}
