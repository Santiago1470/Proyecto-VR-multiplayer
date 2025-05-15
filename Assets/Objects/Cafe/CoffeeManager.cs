using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using System;

public class OrderCompletionSystem : NetworkBehaviour
{
    [System.Serializable]
    public class OrderItem
    {
        public string itemName;
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
        public bool isCompleted = false;
        
        [Tooltip("Tipo de item: 0 = Taza, 1 = Donut")]
        public int itemType = 0; // 0 = Taza, 1 = Donut
    }

    [Header("Pedidos")]
    public OrderItem[] orderItems;

    [Header("Contadores")]
    public int totalCups = 0;  // Será establecido por el NetworkOrderManager
    public int totalDonuts = 0; // Será establecido por el NetworkOrderManager
    public int completedCups = 0;
    public int completedDonuts = 0;

    [Header("Verificación de Tazas")]
    [Tooltip("Tag del objeto que representa el líquido en la taza")]
    public string coffeeTag = "Coffee";
    [Tooltip("Escala Y mínima requerida para considerar una taza llena")]
    public float requiredFillLevel = 0.3f;

    [Header("Audio")]
    public AudioClip itemPlacedSound;
    public AudioClip orderCompletedSound;
    private AudioSource audioSource;

    [Header("Multijugador")]
    [Tooltip("Referencia al NetworkOrderManager")]
    public NetworkOrderManager networkOrderManager;
    
    [Header("Identificación del Jugador")]
    [Tooltip("Índice del jugador (0 o 1)")]
    public int playerIndex = 0;

    [Header("Eventos")]
    public UnityEvent onOrderCompleted;
    // Evento para notificar al NetworkOrderManager
    public Action onOrderCompletedEvent;

    // Variables de red para este jugador específico
    private NetworkVariable<int> networkPlayerCompletedCups = new NetworkVariable<int>(0);
    private NetworkVariable<int> networkPlayerCompletedDonuts = new NetworkVariable<int>(0);
    private NetworkVariable<bool> networkPlayerOrderCompleted = new NetworkVariable<bool>(false);

    private bool isOrderCompleted = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("AudioSource agregado automáticamente");
        }

        // Registrar eventos para cada socket
        foreach (var item in orderItems)
        {
            if (item.socket != null)
            {
                item.socket.selectEntered.AddListener((args) => OnItemPlaced(item, args.interactableObject));
                Debug.Log($"Registrado listener para socket: {item.itemName}");
            }
        }
        
        // Inicializar contadores
        completedCups = 0;
        completedDonuts = 0;
        isOrderCompleted = false;
        
        // Si estamos en la autoridad de red, inicializar los valores de red
        if (IsOwner)
        {
            networkPlayerCompletedCups.Value = 0;
            networkPlayerCompletedDonuts.Value = 0;
            networkPlayerOrderCompleted.Value = false;
        }
    }

    private void OnDestroy()
    {
        // Limpiar los listeners para evitar memory leaks
        foreach (var item in orderItems)
        {
            if (item.socket != null)
            {
                item.socket.selectEntered.RemoveAllListeners();
            }
        }
    }

    private void OnItemPlaced(OrderItem item, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        if (item.isCompleted || isOrderCompleted)
            return;

        GameObject placedObject = interactable.transform.gameObject;
        Debug.Log($"Objeto colocado en socket {item.itemName}: {placedObject.name}");

        // Verificar si es una taza
        if (item.itemType == 0)
        {
            // Buscar el objeto de café por tag dentro de la taza
            Transform[] allChildren = placedObject.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.CompareTag(coffeeTag))
                {
                    // Verificar si el café está lleno (escala Y >= requiredFillLevel)
                    if (child.localScale.y >= requiredFillLevel)
                    {
                        Debug.Log($"Taza llena detectada: {child.localScale.y} (Requerido: {requiredFillLevel})");
                        CompleteItemServerRpc(Array.IndexOf(orderItems, item), 0);
                        break;
                    }
                    else
                    {
                        Debug.Log($"Taza no lo suficientemente llena: {child.localScale.y}");
                    }
                }
            }
        }
        // Verificar si es un donut
        else if (item.itemType == 1)
        {
            CompleteItemServerRpc(Array.IndexOf(orderItems, item), 1); // Completar como donut
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CompleteItemServerRpc(int itemIndex, int itemType)
    {
        // Validar el índice
        if (itemIndex < 0 || itemIndex >= orderItems.Length)
            return;
            
        OrderItem item = orderItems[itemIndex];
        if (item.isCompleted)
            return;

        item.isCompleted = true;

        // Incrementar el contador apropiado
        if (itemType == 0)
        {
            completedCups++;
            networkPlayerCompletedCups.Value = completedCups;
            Debug.Log($"Taza completada. Total: {completedCups}/{totalCups}");
        }
        else if (itemType == 1)
        {
            completedDonuts++;
            networkPlayerCompletedDonuts.Value = completedDonuts;
            Debug.Log($"Donut completado. Total: {completedDonuts}/{totalDonuts}");
        }

        // Notificar a todos los clientes
        CompleteItemClientRpc(itemIndex, itemType);
        
        // Verificar si el pedido está completo
        CheckOrderCompletion();
    }

    [ClientRpc]
    private void CompleteItemClientRpc(int itemIndex, int itemType)
    {
        if (IsServer) return; // El servidor ya actualizó sus datos
        
        // Validar el índice
        if (itemIndex < 0 || itemIndex >= orderItems.Length)
            return;
            
        OrderItem item = orderItems[itemIndex];
        if (item.isCompleted)
            return;

        item.isCompleted = true;

        // Incrementar el contador apropiado
        if (itemType == 0)
        {
            completedCups++;
            Debug.Log($"(Cliente) Taza completada. Total: {completedCups}/{totalCups}");
        }
        else if (itemType == 1)
        {
            completedDonuts++;
            Debug.Log($"(Cliente) Donut completado. Total: {completedDonuts}/{totalDonuts}");
        }

        // Reproducir sonido de item colocado
        if (itemPlacedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(itemPlacedSound);
            Debug.Log("Reproduciendo sonido de item colocado");
        }
        
        // Verificar si el pedido está completo
        CheckOrderCompletion();
    }

    private void CheckOrderCompletion()
    {
        if (isOrderCompleted)
            return;

        // Verificar si se completaron todas las tazas y donuts
        if (completedCups >= totalCups && completedDonuts >= totalDonuts)
        {
            isOrderCompleted = true;
            
            if (IsServer)
            {
                networkPlayerOrderCompleted.Value = true;
            }
            
            Debug.Log("¡Pedido completo! Todas las tazas y donuts han sido entregados correctamente.");
            
            // Reproducir sonido de pedido completado
            if (orderCompletedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(orderCompletedSound);
                Debug.Log("Reproduciendo sonido de pedido completado");
            }
            
            // Invocar evento de pedido completado
            onOrderCompleted?.Invoke();
            onOrderCompletedEvent?.Invoke();
            
            // Notificar al NetworkOrderManager si estamos en el servidor
            if (IsServer && networkOrderManager != null)
            {
                networkOrderManager.OnPlayerOrderCompleted();
            }
        }
    }

    // Método público para resetear el sistema
    public void ResetOrderSystem()
    {
        completedCups = 0;
        completedDonuts = 0;
        isOrderCompleted = false;
        
        if (IsServer)
        {
            networkPlayerCompletedCups.Value = 0;
            networkPlayerCompletedDonuts.Value = 0;
            networkPlayerOrderCompleted.Value = false;
        }
        
        foreach (var item in orderItems)
        {
            item.isCompleted = false;
        }
        
        Debug.Log($"Sistema de pedidos del jugador {playerIndex} reiniciado");
    }
    
    // Método para obtener los valores sincronizados por red
    public int GetNetworkCompletedCups() => networkPlayerCompletedCups.Value;
    public int GetNetworkCompletedDonuts() => networkPlayerCompletedDonuts.Value;
    public bool IsNetworkOrderCompleted() => networkPlayerOrderCompleted.Value;
}