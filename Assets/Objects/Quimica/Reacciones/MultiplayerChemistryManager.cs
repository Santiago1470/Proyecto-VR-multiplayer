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

        // Subscribe to events
        totalObjectivesCompleted.OnValueChanged += OnTotalObjectivesChanged;
        connectedPlayers.OnValueChanged += OnConnectedPlayersChanged;
        totalRequiredObjectives.OnValueChanged += OnRequiredObjectivesChanged;
        playerObjectives.OnListChanged += OnPlayerObjectivesChanged;

        // If we're the host, initialize the game
        if (IsHost || IsServer)
        {
            connectedPlayers.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
            totalRequiredObjectives.Value = connectedPlayers.Value * reactionsPerPlayer;
            InitializePlayerObjectives();
        }

        // Setup local player's objectives
        if (localReactionContainer != null)
        {
            // Override the ReactionContainer's ProcessCompletedReaction to notify this manager
            localReactionContainer.OnReactionCompleted += HandleLocalReactionCompleted;
        }

        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from events
        totalObjectivesCompleted.OnValueChanged -= OnTotalObjectivesChanged;
        connectedPlayers.OnValueChanged -= OnConnectedPlayersChanged;
        totalRequiredObjectives.OnValueChanged -= OnRequiredObjectivesChanged;
        playerObjectives.OnListChanged -= OnPlayerObjectivesChanged;

        if (localReactionContainer != null)
        {
            localReactionContainer.OnReactionCompleted -= HandleLocalReactionCompleted;
        }

        base.OnNetworkDespawn();
    }

    // Server-side: Initialize player objectives
    private void InitializePlayerObjectives()
    {
        if (!IsServer && !IsHost) return;

        playerObjectives.Clear();
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            playerObjectives.Add(new PlayerObjectiveData
            {
                PlayerId = clientId,
                ObjectivesCompleted = 0,
                RequiredObjectives = reactionsPerPlayer
            });
        }
    }

    // When a player completes a reaction locally
    private void HandleLocalReactionCompleted(string formula)
    {
        if (HasCompletedAllObjectives(localPlayerId)) return;

        NotifyReactionCompletedServerRpc(formula);
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyReactionCompletedServerRpc(string formula, ServerRpcParams serverRpcParams = default)
    {
        ulong senderId = serverRpcParams.Receive.SenderClientId;

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

                // Notify all clients about the update
                NotifyObjectiveCompletedClientRpc(senderId, formula);

                // Check if game is completed
                if (totalObjectivesCompleted.Value >= totalRequiredObjectives.Value)
                {
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
        UpdateUI();
    }

    private void OnConnectedPlayersChanged(int previous, int current)
    {
        if (IsHost || IsServer)
        {
            totalRequiredObjectives.Value = current * reactionsPerPlayer;
        }
        UpdateUI();
    }

    private void OnRequiredObjectivesChanged(int previous, int current)
    {
        UpdateUI();
    }

    private void OnPlayerObjectivesChanged(NetworkListEvent<PlayerObjectiveData> changeEvent)
    {
        UpdateUI();
    }

    // Update UI elements with the latest network state
    private void UpdateUI()
    {
        // Update global progress text
        if (globalProgressText != null)
        {
            globalProgressText.text = $"Progreso Global: {totalObjectivesCompleted.Value}/{totalRequiredObjectives.Value}";
        }

        // Update player objectives text
        if (playerObjectivesText != null)
        {
            string text = "<b>Objetivos por Jugador:</b>\n";
            foreach (var playerData in playerObjectives)
            {
                string playerLabel = playerData.PlayerId == localPlayerId ? "Tú" : $"Jugador {playerData.PlayerId}";
                string completionStatus = playerData.ObjectivesCompleted >= playerData.RequiredObjectives ? 
                    "<color=green>✓ Completado</color>" : 
                    $"{playerData.ObjectivesCompleted}/{playerData.RequiredObjectives}";
                
                text += $"{playerLabel}: {completionStatus}\n";
            }
            playerObjectivesText.text = text;
        }

        // Update game state text if game is completed
        if (gameStateText != null && gameCompleted)
        {
            gameStateText.text = "¡TODOS LOS OBJETIVOS COMPLETADOS!";
            gameStateText.color = Color.green;
        }
    }

    // Called when a player connects or disconnects
    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsServer || IsHost)
        {
            UpdateConnectedPlayersServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateConnectedPlayersServerRpc(ServerRpcParams serverRpcParams = default)
    {
        connectedPlayers.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
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
}