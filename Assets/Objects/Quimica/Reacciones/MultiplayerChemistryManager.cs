using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using TMPro;

public class MultiplayerChemistryManager : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI globalProgressText;
    [SerializeField] private TextMeshProUGUI playerObjectivesText;
    [SerializeField] private TextMeshProUGUI gameStateText;
    [SerializeField] private int reactionsPerPlayer = 3;

    // Networked variables
    private NetworkVariable<int> totalObjectivesCompleted = new NetworkVariable<int>(0);
    private NetworkVariable<int> connectedPlayers = new NetworkVariable<int>(0);
    private NetworkVariable<int> totalRequiredObjectives = new NetworkVariable<int>(3);

    // List to track which player completed which objectives
    private NetworkList<PlayerObjectiveData> playerObjectives;

    // Local references
    private ChemistryManager localChemistryManager;
    private ReactionContainer localReactionContainer;
    private ulong localPlayerId;
    private bool gameCompleted = false;
    private bool isInitialized = false;

    private void Awake()
    {
        playerObjectives = new NetworkList<PlayerObjectiveData>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        localPlayerId = NetworkManager.Singleton.LocalClientId;
        
        // Find or add required components
        localChemistryManager = FindObjectOfType<ChemistryManager>();
        if (localChemistryManager == null)
        {
            localChemistryManager = gameObject.AddComponent<ChemistryManager>();
        }

        localReactionContainer = FindObjectOfType<ReactionContainer>();
        if (localReactionContainer == null)
        {
            Debug.LogError("No ReactionContainer found in the scene!");
        }

        // Subscribe to network variable changes
        totalObjectivesCompleted.OnValueChanged += OnTotalObjectivesChanged;
        connectedPlayers.OnValueChanged += OnConnectedPlayersChanged;
        totalRequiredObjectives.OnValueChanged += OnRequiredObjectivesChanged;
        playerObjectives.OnListChanged += OnPlayerObjectivesChanged;

        // Subscribe to NetworkManager events to track player connections
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Setup local player's objectives
        if (localReactionContainer != null)
        {
            localReactionContainer.OnReactionCompleted += HandleLocalReactionCompleted;
        }

        // Initialize the game state
        StartCoroutine(InitializeAfterFrame());
    }

    private System.Collections.IEnumerator InitializeAfterFrame()
    {
        // Wait one frame to ensure all network objects are spawned
        yield return null;
        
        if (IsServer || IsHost)
        {
            InitializeGameState();
        }
        
        // Request current state if we're a client
        if (!IsServer && !IsHost)
        {
            RequestGameStateServerRpc();
        }
        
        isInitialized = true;
        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from events
        totalObjectivesCompleted.OnValueChanged -= OnTotalObjectivesChanged;
        connectedPlayers.OnValueChanged -= OnConnectedPlayersChanged;
        totalRequiredObjectives.OnValueChanged -= OnRequiredObjectivesChanged;
        playerObjectives.OnListChanged -= OnPlayerObjectivesChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (localReactionContainer != null)
        {
            localReactionContainer.OnReactionCompleted -= HandleLocalReactionCompleted;
        }

        base.OnNetworkDespawn();
    }

    // Server-side: Initialize game state
    private void InitializeGameState()
    {
        if (!IsServer && !IsHost) return;

        int currentPlayerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        connectedPlayers.Value = currentPlayerCount;
        totalRequiredObjectives.Value = currentPlayerCount * reactionsPerPlayer;

        InitializePlayerObjectives();
        
        Debug.Log($"Game initialized: {currentPlayerCount} players, {totalRequiredObjectives.Value} total objectives required");
    }

    // Server-side: Initialize player objectives
    private void InitializePlayerObjectives()
    {
        if (!IsServer && !IsHost) return;

        // Clear existing data
        playerObjectives.Clear();

        // Add data for each connected client
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            playerObjectives.Add(new PlayerObjectiveData
            {
                PlayerId = clientId,
                ObjectivesCompleted = 0,
                RequiredObjectives = reactionsPerPlayer
            });
        }

        Debug.Log($"Initialized objectives for {playerObjectives.Count} players");
    }

    // Client requests current game state from server
    [ServerRpc(RequireOwnership = false)]
    private void RequestGameStateServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Send current state to the requesting client
        ulong requestingClient = serverRpcParams.Receive.SenderClientId;
        SendGameStateClientRpc(
            totalObjectivesCompleted.Value,
            connectedPlayers.Value,
            totalRequiredObjectives.Value,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { requestingClient }
                }
            }
        );
    }

    [ClientRpc]
    private void SendGameStateClientRpc(int objectives, int players, int totalRequired, ClientRpcParams clientRpcParams = default)
    {
        if (!isInitialized)
        {
            Debug.Log($"Received game state: {objectives}/{totalRequired} objectives, {players} players");
            UpdateUI();
        }
    }

    // Player connection callbacks
    private void OnClientConnected(ulong clientId)
    {
        if (IsServer || IsHost)
        {
            Debug.Log($"Client {clientId} connected");
            // Reinitialize game state with new player count
            InitializeGameState();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer || IsHost)
        {
            Debug.Log($"Client {clientId} disconnected");
            // Remove player from objectives and recalculate
            RemovePlayerAndRecalculate(clientId);
        }
    }

    private void RemovePlayerAndRecalculate(ulong disconnectedClientId)
    {
        if (!IsServer && !IsHost) return;

        // Remove the disconnected player from objectives
        for (int i = playerObjectives.Count - 1; i >= 0; i--)
        {
            if (playerObjectives[i].PlayerId == disconnectedClientId)
            {
                // Subtract their completed objectives from the total
                totalObjectivesCompleted.Value -= playerObjectives[i].ObjectivesCompleted;
                playerObjectives.RemoveAt(i);
                break;
            }
        }

        // Update player count and total required objectives
        int currentPlayerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        connectedPlayers.Value = currentPlayerCount;
        totalRequiredObjectives.Value = currentPlayerCount * reactionsPerPlayer;

        Debug.Log($"Player removed. New totals: {totalObjectivesCompleted.Value}/{totalRequiredObjectives.Value}");
    }

    // When a player completes a reaction locally
    private void HandleLocalReactionCompleted(string formula)
    {
        if (HasCompletedAllObjectives(localPlayerId)) return;

        Debug.Log($"Local player completed reaction: {formula}");
        NotifyReactionCompletedServerRpc(formula);
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyReactionCompletedServerRpc(string formula, ServerRpcParams serverRpcParams = default)
    {
        ulong senderId = serverRpcParams.Receive.SenderClientId;
        Debug.Log($"Server received reaction completion from player {senderId}: {formula}");

        // Update the player's objectives
        for (int i = 0; i < playerObjectives.Count; i++)
        {
            var playerData = playerObjectives[i];
            if (playerData.PlayerId == senderId && playerData.ObjectivesCompleted < playerData.RequiredObjectives)
            {
                playerData.ObjectivesCompleted++;
                playerObjectives[i] = playerData;

                // Update total objectives completed
                totalObjectivesCompleted.Value++;

                Debug.Log($"Player {senderId} now has {playerData.ObjectivesCompleted}/{playerData.RequiredObjectives} objectives. Total: {totalObjectivesCompleted.Value}/{totalRequiredObjectives.Value}");

                // Notify all clients about the update
                NotifyObjectiveCompletedClientRpc(senderId, formula);

                // Check if game is completed
                if (totalObjectivesCompleted.Value >= totalRequiredObjectives.Value && !gameCompleted)
                {
                    gameCompleted = true;
                    GameCompletedClientRpc();
                }
                break;
            }
        }
    }

    [ClientRpc]
    private void NotifyObjectiveCompletedClientRpc(ulong playerId, string formula)
    {
        // Show notification when another player completes an objective
        if (playerId != localPlayerId)
        {
            Debug.Log($"Player {playerId} completed objective with {formula}");
            // Could add a visual notification here
        }
        
        UpdateUI();
    }

    [ClientRpc]
    private void GameCompletedClientRpc()
    {
        gameCompleted = true;
        Debug.Log("All objectives completed!");
        UpdateUI();
        
        // Add any game completion celebration effects here
        if (gameStateText != null)
        {
            gameStateText.text = "¡TODOS LOS OBJETIVOS COMPLETADOS!";
            gameStateText.color = Color.green;
        }
    }

    // Check if this player has completed all their objectives
    private bool HasCompletedAllObjectives(ulong playerId)
    {
        foreach (var playerData in playerObjectives)
        {
            if (playerData.PlayerId == playerId)
            {
                return playerData.ObjectivesCompleted >= playerData.RequiredObjectives;
            }
        }
        return false;
    }

    // Network variable change handlers
    private void OnTotalObjectivesChanged(int previous, int current)
    {
        Debug.Log($"Total objectives changed: {previous} -> {current}");
        UpdateUI();
    }

    private void OnConnectedPlayersChanged(int previous, int current)
    {
        Debug.Log($"Connected players changed: {previous} -> {current}");
        UpdateUI();
    }

    private void OnRequiredObjectivesChanged(int previous, int current)
    {
        Debug.Log($"Required objectives changed: {previous} -> {current}");
        UpdateUI();
    }

    private void OnPlayerObjectivesChanged(NetworkListEvent<PlayerObjectiveData> changeEvent)
    {
        Debug.Log($"Player objectives list changed: {changeEvent.Type}");
        UpdateUI();
    }

    // Update UI elements with the latest network state
    private void UpdateUI()
    {
        if (!isInitialized) return;

        // Update global progress text
        if (globalProgressText != null)
        {
            globalProgressText.text = $"Progreso Global: {totalObjectivesCompleted.Value}/{totalRequiredObjectives.Value}";
        }

        // Update player objectives text
        if (playerObjectivesText != null)
        {
            string text = "<b>Objetivos por Jugador:</b>\n";
            
            if (playerObjectives.Count == 0)
            {
                text += "Cargando jugadores...";
            }
            else
            {
                foreach (var playerData in playerObjectives)
                {
                    string playerLabel = playerData.PlayerId == localPlayerId ? "Tú" : $"Jugador {playerData.PlayerId+1}";
                    string completionStatus = playerData.ObjectivesCompleted >= playerData.RequiredObjectives ? 
                        "<color=green>✓ Completado</color>" : 
                        $"{playerData.ObjectivesCompleted}/{playerData.RequiredObjectives}";
                    
                    text += $"{playerLabel}: {completionStatus}\n";
                }
            }
            
            playerObjectivesText.text = text;
        }

        // Update game state text if game is completed
        if (gameStateText != null)
        {
            if (gameCompleted)
            {
                gameStateText.text = "¡TODOS LOS OBJETIVOS COMPLETADOS!";
                gameStateText.color = Color.green;
            }
            else
            {
                gameStateText.text = $"Jugadores conectados: {connectedPlayers.Value}";
                gameStateText.color = Color.white;
            }
        }
    }

    // Debug method to check current state
    [ContextMenu("Debug Current State")]
    private void DebugCurrentState()
    {
        Debug.Log($"=== MULTIPLAYER CHEMISTRY MANAGER STATE ===");
        Debug.Log($"Is Host/Server: {IsHost || IsServer}");
        Debug.Log($"Local Player ID: {localPlayerId}");
        Debug.Log($"Connected Players: {connectedPlayers.Value}");
        Debug.Log($"Total Objectives Completed: {totalObjectivesCompleted.Value}");
        Debug.Log($"Total Required Objectives: {totalRequiredObjectives.Value}");
        Debug.Log($"Game Completed: {gameCompleted}");
        Debug.Log($"Player Objectives Count: {playerObjectives.Count}");
        
        for (int i = 0; i < playerObjectives.Count; i++)
        {
            var player = playerObjectives[i];
            Debug.Log($"Player {player.PlayerId}: {player.ObjectivesCompleted}/{player.RequiredObjectives}");
        }
    }
}

// Struct to hold player objective data for the NetworkList
public struct PlayerObjectiveData : INetworkSerializable, System.IEquatable<PlayerObjectiveData>
{
    public ulong PlayerId;
    public int ObjectivesCompleted;
    public int RequiredObjectives;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref ObjectivesCompleted);
        serializer.SerializeValue(ref RequiredObjectives);
    }

    public bool Equals(PlayerObjectiveData other)
    {
        return PlayerId == other.PlayerId &&
               ObjectivesCompleted == other.ObjectivesCompleted &&
               RequiredObjectives == other.RequiredObjectives;
    }

    public override string ToString()
    {
        return $"Player {PlayerId}: {ObjectivesCompleted}/{RequiredObjectives}";
    }
}