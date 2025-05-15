using UnityEngine;
using TMPro;
using Unity.Netcode;

public class OrderStatusDisplay : NetworkBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Referencia al TextMeshProUGUI para mostrar el estado del pedido del jugador local")]
    public TextMeshProUGUI localPlayerStatusText;
    
    [Tooltip("Referencia al TextMeshProUGUI para mostrar el estado del pedido del otro jugador")]
    public TextMeshProUGUI otherPlayerStatusText;
    
    [Tooltip("Referencia al TextMeshProUGUI para mostrar el estado global del pedido")]
    public TextMeshProUGUI globalStatusText;

    [Header("Formato")]
    [Tooltip("Formato del texto del jugador. {0}=tazas completadas, {1}=total tazas, {2}=donuts completados, {3}=total donuts, {4}=nombre del jugador")]
    public string playerTextFormat = "<color=#C87B30>Tazas: {0}/{1}</color>\n<color=#FF69B4>Donuts: {2}/{3}</color>";
    
    [Tooltip("Formato del texto global. {0}=tazas completadas, {1}=total tazas, {2}=donuts completados, {3}=total donuts")]
    public string globalTextFormat = "Pedido Café VR:\n<color=#C87B30>Tazas: {0}/{1}</color>\n<color=#FF69B4>Donuts: {2}/{3}</color>";
    
    [Header("Multijugador")]
    [Tooltip("Referencia al NetworkOrderManager")]
    public NetworkOrderManager networkOrderManager;
    
    [Tooltip("Referencias a los OrderCompletionSystem de cada jugador")]
    public OrderCompletionSystem[] playerOrderSystems;
    
    [Tooltip("Índice del jugador local (0 o 1)")]
    public int localPlayerIndex = 0;

    private void Start()
    {
        // Verificar las referencias necesarias
        if (networkOrderManager == null)
        {
            Debug.LogError("No se ha asignado NetworkOrderManager");
            enabled = false;
            return;
        }

        if (playerOrderSystems == null || playerOrderSystems.Length < 2)
        {
            Debug.LogError("Se necesitan referencias a los OrderCompletionSystem de ambos jugadores");
            enabled = false;
            return;
        }

        if (localPlayerStatusText == null || otherPlayerStatusText == null || globalStatusText == null)
        {
            Debug.LogError("Faltan referencias a TextMeshProUGUI para mostrar el estado");
            enabled = false;
            return;
        }

        // Actualizar la UI inicial
        UpdateStatusDisplay();
    }

    private void Update()
    {
        // Actualizar la UI en cada frame para reflejar cambios
        UpdateStatusDisplay();
    }

    private void UpdateStatusDisplay()
    {
        // Verificar que tengamos referencias válidas
        if (playerOrderSystems == null || playerOrderSystems.Length < 2 || 
            localPlayerStatusText == null || otherPlayerStatusText == null || 
            globalStatusText == null || networkOrderManager == null)
            return;
        
        int otherPlayerIndex = (localPlayerIndex == 0) ? 1 : 0;
        
        // Obtener los datos de ambos jugadores
        OrderCompletionSystem localSystem = playerOrderSystems[localPlayerIndex];
        OrderCompletionSystem otherSystem = playerOrderSystems[otherPlayerIndex];

        // Actualizar texto del jugador local
        localPlayerStatusText.text = "Tú:\n" + string.Format(playerTextFormat, 
            localSystem.completedCups, 
            localSystem.totalCups, 
            localSystem.completedDonuts, 
            localSystem.totalDonuts);
        
        // Actualizar texto del otro jugador
        otherPlayerStatusText.text = "Compañero:\n" + string.Format(playerTextFormat, 
            otherSystem.completedCups, 
            otherSystem.totalCups, 
            otherSystem.completedDonuts, 
            otherSystem.totalDonuts);
        
        // Actualizar texto global
        int globalCompletedCups = networkOrderManager.GetGlobalCompletedCups();
        int globalCompletedDonuts = networkOrderManager.GetGlobalCompletedDonuts();
        int totalCups = networkOrderManager.GetTotalCups();
        int totalDonuts = networkOrderManager.GetTotalDonuts();
        
        globalStatusText.text = string.Format(globalTextFormat, 
            globalCompletedCups, 
            totalCups, 
            globalCompletedDonuts, 
            totalDonuts);
        
        // Comprobar si el pedido del jugador local está completo
        if (localSystem.completedCups >= localSystem.totalCups && 
            localSystem.completedDonuts >= localSystem.totalDonuts)
        {
            localPlayerStatusText.text += "\n<color=#00FF00>¡COMPLETADO!</color>";
        }
        
        // Comprobar si el pedido del otro jugador está completo
        if (otherSystem.completedCups >= otherSystem.totalCups && 
            otherSystem.completedDonuts >= otherSystem.totalDonuts)
        {
            otherPlayerStatusText.text += "\n<color=#00FF00>¡COMPLETADO!</color>";
        }
        
        // Comprobar si el pedido global está completo
        if (networkOrderManager.IsGlobalOrderCompleted())
        {
            globalStatusText.text += "\n\n<color=#FFFF00>¡PEDIDO COMPLETO!</color>";
        }
    }

    // Método opcional para inicializar las referencias si se agrega el script en tiempo de ejecución
    public void Initialize(TextMeshProUGUI localText, TextMeshProUGUI otherText, TextMeshProUGUI globalText, 
                          NetworkOrderManager orderManager, OrderCompletionSystem[] systems, int playerIndex)
    {
        localPlayerStatusText = localText;
        otherPlayerStatusText = otherText;
        globalStatusText = globalText;
        networkOrderManager = orderManager;
        playerOrderSystems = systems;
        localPlayerIndex = playerIndex;
        UpdateStatusDisplay();
    }
}