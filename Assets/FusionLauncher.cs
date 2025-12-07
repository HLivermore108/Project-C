// FusionLauncher.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal Fusion launcher for Shared mode with room/session spawn of a car prefab.
/// - Assign a NetworkRunner prefab optionally (runnerPrefab) or let the script create a runtime runner.
/// - Assign a NetworkObject carPrefab (the prefab asset must have NetworkObject on root).
/// - Assign spawnPoint in the scene (optional).
/// 
/// This class implements callbacks via an internal proxy to avoid Unity message warnings.
/// </summary>
public class FusionLauncher : MonoBehaviour
{
    [Tooltip("Optional: assign a NetworkRunner prefab (with NetworkSceneManagerDefault if you want scene management).")]
    public NetworkRunner runnerPrefab;

    [Tooltip("Assign the Car prefab asset (must contain NetworkObject component)")]
    public NetworkObject carPrefab;

    [Tooltip("Optional spawn point transform in the scene")]
    public Transform spawnPoint;

    NetworkRunner runner;

    async void Start()
    {
        // Create/instantiate runner
        if (runnerPrefab != null)
        {
            NetworkRunner spawnedRunner = Instantiate(runnerPrefab);
            runner = spawnedRunner;
        }
        else
        {
            GameObject go = new GameObject("NetworkRunner");
            runner = go.AddComponent<NetworkRunner>();
        }

        runner.ProvideInput = true;

        // Register callbacks via proxy
        runner.AddCallbacks(new RunnerCallbacksProxy(this));

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "TestSession",
            Scene = SceneRef.None
        };

        Debug.Log("[FusionLauncher] Starting Runner (Shared mode)...");
        await runner.StartGame(startArgs);
    }

    internal void HandleOnPlayerJoined(NetworkRunner runnerInstance, PlayerRef player)
    {
        Debug.Log("[FusionLauncher] Player joined: " + player);

        Vector3 pos = spawnPoint != null ? spawnPoint.position :
                      new Vector3(UnityEngine.Random.Range(-3f, 3f), 1f, UnityEngine.Random.Range(-3f, 3f));
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        if (carPrefab == null)
        {
            Debug.LogError("[FusionLauncher] carPrefab is null. Assign it in the inspector.");
            return;
        }

        NetworkObject spawned = runnerInstance.Spawn(carPrefab, pos, rot, player);
        if (spawned == null)
        {
            Debug.LogError("[FusionLauncher] Failed to spawn car prefab.");
        }
        else
        {
            Debug.Log("[FusionLauncher] Spawned car: " + spawned.name + " for player " + player);
            if (spawned.HasInputAuthority)
            {
                CameraFollow cam = Camera.main?.GetComponent<CameraFollow>();
                if (cam != null) cam.SetTarget(spawned.transform);
            }
        }
    }

    // Proxy implementing INetworkRunnerCallbacks to avoid Unity message collisions.
    class RunnerCallbacksProxy : INetworkRunnerCallbacks
    {
        FusionLauncher owner;
        public RunnerCallbacksProxy(FusionLauncher owner) { this.owner = owner; }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            owner.HandleOnPlayerJoined(runner, player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { Debug.Log("[RunnerCallbacks] OnPlayerLeft: " + player); }
        public void OnInput(NetworkRunner runner, NetworkInput input) { /* Not used here */ }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { Debug.Log("[RunnerCallbacks] OnShutdown: " + shutdownReason); }
        public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("[RunnerCallbacks] OnConnectedToServer"); }

        // Implement the signature that some Fusion versions expect:
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log("[RunnerCallbacks] OnDisconnectedFromServer: " + reason);
        }

        // Older/alternate signature (some versions use OnDisconnectedFromServer with no NetDisconnectReason).
        // If your Fusion version requires a different signature, let me know the exact compiler error and I'll adjust.

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.LogError("[RunnerCallbacks] ConnectFailed: " + reason); }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { Debug.Log("[RunnerCallbacks] OnHostMigration"); }

        // Reliable data received - common signature without DeliveryIntent
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            // If you use reliable streams, handle them here
            Debug.Log("[RunnerCallbacks] OnReliableDataReceived from " + player + " size=" + data.Count);
        }

        // Some Fusion versions define the method with a DeliveryIntent parameter.
        // If your Fusion version *requires* the DeliveryIntent variant, tell me the compiler error text
        // and I will provide an alternative version tailored for your Fusion version.

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnConnectedToSharedMode(NetworkRunner runner) { Debug.Log("[RunnerCallbacks] OnConnectedToSharedMode"); }
        public void OnDisconnectedFromSharedMode(NetworkRunner runner) { Debug.Log("[RunnerCallbacks] OnDisconnectedFromSharedMode"); }

        // AOI callbacks
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        // The interface may evolve between Fusion versions. If the compiler complains about any missing or mismatched method,
        // copy the exact error message here and I'll update this proxy to match your Fusion version.
    }
}
