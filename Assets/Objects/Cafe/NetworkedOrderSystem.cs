using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkOrderManager : NetworkBehaviour
{
    [Header("Configuración de Pedidos")]
    [Tooltip("Número total de tazas para repartir entre jugadores")]
    public int totalCups = 5;
    
    [Tooltip("Número total de donuts para repartir entre jugadores")]
    public int totalDonuts = 3;
    
    [Header("Referencias")]
    [Tooltip("Referencias a los OrderCompletionSystem de cada jugador")]
    public OrderCompletionSystem[] playerOrderSystems;
    
    // Variables de red para el seguimiento global
    private NetworkVariable<int> networkCompletedCups = new NetworkVariable<int>(0);
    private NetworkVariable<int> networkCompletedDonuts = new NetworkVariable<int>(0);
    private NetworkVariable<bool> networkOrderCompleted = new NetworkVariable<bool>(false);

    // Evento para cuando se completa un pedido global
    public System.Action OnGlobalOrderCompleted;

    private void Start()
    {
        if (!IsServer) return;
        
        // Esperar hasta tener la cantidad necesaria de jugadores conectados
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2)
        {
            Debug.Log("Esperando a que se conecte otro jugador...");
            NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
                if (NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
                {
                    DistributeOrders();
                }
            };
        }
        else
        {
            DistributeOrders();
        }
    }

    // Este método se ejecuta solo en el servidor
    private void DistributeOrders()
    {
        if (!IsServer) return;
        
        Debug.Log("Distribuyendo pedidos entre jugadores...");

        // Asegurarse de que tenemos referencias válidas a los sistemas de pedidos
        if (playerOrderSystems.Length < 2)
        {
            Debug.LogError("Se necesitan al menos 2 referencias a OrderCompletionSystem");
            return;
        }

        // Crear listas para distribución aleatoria
        List<int> cupAssignments = new List<int>();
        List<int> donutAssignments = new List<int>();

        // Inicializar listas con todas las tazas y donuts
        for (int i = 0; i < totalCups; i++)
            cupAssignments.Add(Random.Range(0, 2)); // 0 o 1 (jugador 1 o 2)
            
        for (int i = 0; i < totalDonuts; i++)
            donutAssignments.Add(Random.Range(0, 2)); // 0 o 1 (jugador 1 o 2)

        // Contar asignaciones por jugador
        int player1Cups = 0, player2Cups = 0;
        int player1Donuts = 0, player2Donuts = 0;

        foreach (int assignment in cupAssignments)
            if (assignment == 0) player1Cups++; else player2Cups++;
            
        foreach (int assignment in donutAssignments)
            if (assignment == 0) player1Donuts++; else player2Donuts++;
        
        // Configurar los sistemas de pedidos de cada jugador
        ConfigurePlayerOrderSystemClientRpc(0, player1Cups, player1Donuts);
        ConfigurePlayerOrderSystemClientRpc(1, player2Cups, player2Donuts);
        
        Debug.Log($"Jugador 1: {player1Cups} tazas, {player1Donuts} donuts");
        Debug.Log($"Jugador 2: {player2Cups} tazas, {player2Donuts} donuts");
    }

    [ClientRpc]
    private void ConfigurePlayerOrderSystemClientRpc(int playerIndex, int assignedCups, int assignedDonuts)
    {
        if (playerIndex < 0 || playerIndex >= playerOrderSystems.Length)
        {
            Debug.LogError($"Índice de jugador inválido: {playerIndex}");
            return;
        }
        
        OrderCompletionSystem orderSystem = playerOrderSystems[playerIndex];
        if (orderSystem != null)
        {
            orderSystem.totalCups = assignedCups;
            orderSystem.totalDonuts = assignedDonuts;
            orderSystem.ResetOrderSystem();
            
            // Suscribirse al evento de finalización para actualizar los contadores globales
            orderSystem.onOrderCompletedEvent += OnPlayerOrderCompleted;
            
            Debug.Log($"Jugador {playerIndex}: Configurado con {assignedCups} tazas y {assignedDonuts} donuts");
        }
        else
        {
            Debug.LogError($"No se encontró OrderCompletionSystem para el jugador {playerIndex}");
        }
    }

    // Método para ser llamado por los OrderCompletionSystems cuando completan items
    public void UpdateGlobalCounters(int cups, int donuts)
    {
        if (!IsServer) return;
        
        // Actualizar contadores globales
        networkCompletedCups.Value = cups;
        networkCompletedDonuts.Value = donuts;
        
        // Comprobar si el pedido global está completo
        if (networkCompletedCups.Value == totalCups && networkCompletedDonuts.Value == totalDonuts && !networkOrderCompleted.Value)
        {
            networkOrderCompleted.Value = true;
            NotifyGlobalCompletionClientRpc();
        }
    }

    // Este método es llamado cuando un jugador individual completa su orden
    public void OnPlayerOrderCompleted()
    {
        if (IsServer)
        {
            // Sumar todos los completados de todos los jugadores
            int totalCompletedCups = 0;
            int totalCompletedDonuts = 0;
            
            foreach (var orderSystem in playerOrderSystems)
            {
                if (orderSystem != null)
                {
                    totalCompletedCups += orderSystem.completedCups;
                    totalCompletedDonuts += orderSystem.completedDonuts;
                }
            }
            
            UpdateGlobalCounters(totalCompletedCups, totalCompletedDonuts);
        }
    }

    [ClientRpc]
    private void NotifyGlobalCompletionClientRpc()
    {
        Debug.Log("¡Pedido global completado! Ambos jugadores han cumplido su parte.");
        OnGlobalOrderCompleted?.Invoke();
    }

    // Métodos públicos para obtener el estado global
    public int GetGlobalCompletedCups() => networkCompletedCups.Value;
    public int GetGlobalCompletedDonuts() => networkCompletedDonuts.Value;
    public int GetTotalCups() => totalCups;
    public int GetTotalDonuts() => totalDonuts;
    public bool IsGlobalOrderCompleted() => networkOrderCompleted.Value;
    
    // Método para reiniciar todo el sistema (solo desde el servidor)
    public void ResetGlobalOrderSystem()
    {
        if (!IsServer) return;
        
        networkCompletedCups.Value = 0;
        networkCompletedDonuts.Value = 0;
        networkOrderCompleted.Value = false;
        
        // Redistribuir pedidos
        DistributeOrders();
    }
}