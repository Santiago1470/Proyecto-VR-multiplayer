using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

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
    public int totalCups = 5;
    public int totalDonuts = 3;
    
    // Variables sincronizadas en red
    private NetworkVariable<int> _completedCups = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _completedDonuts = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Propiedades para acceder a los valores sincronizados
    public int completedCups => _completedCups.Value;
    public int completedDonuts => _completedDonuts.Value;

    [Header("Verificación de Tazas")]
    [Tooltip("Tag del objeto que representa el líquido en la taza")]
    public string coffeeTag = "Coffee";
    [Tooltip("Escala Y mínima requerida para considerar una taza llena")]
    public float requiredFillLevel = 0.3f;

    [Header("Audio")]
    public AudioClip itemPlacedSound;
    public AudioClip orderCompletedSound;
    private AudioSource audioSource;

    [Header("Eventos")]
    public UnityEvent onOrderCompleted;

    private NetworkVariable<bool> _isOrderCompleted = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Cache para evitar llamadas RPC innecesarias
    private bool wasOrderCompleted = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse a cambios en las variables de red
        _isOrderCompleted.OnValueChanged += OnOrderCompletedChanged;
        
        if (IsServer)
        {
            // Inicializar contadores solo en el servidor
            _completedCups.Value = 0;
            _completedDonuts.Value = 0;
            _isOrderCompleted.Value = false;
        }
    }

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
        
        // Desuscribirse de eventos de red
        if (_isOrderCompleted != null)
        {
            _isOrderCompleted.OnValueChanged -= OnOrderCompletedChanged;
        }
    }

    private void OnItemPlaced(OrderItem item, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        if (item.isCompleted || _isOrderCompleted.Value)
            return;

        GameObject placedObject = interactable.transform.gameObject;
        Debug.Log($"Objeto colocado en socket {item.itemName}: {placedObject.name}");

        // Solicitar al servidor que verifique el item
        if (IsLocalPlayer || IsServer)
        {
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            CheckItemServerRpc(item.itemType, placedObject.name, clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CheckItemServerRpc(int itemType, string objectName, ulong clientId)
    {
        // Aquí verificamos el objeto en el servidor
        Debug.Log($"[Servidor] Verificando item tipo {itemType} por el cliente {clientId}");
        
        // Procesamos la información que llegó del cliente
        OrderItem relevantItem = FindNextUncompletedItem(itemType);
        
        if (relevantItem == null)
        {
            Debug.Log($"[Servidor] No hay más items de tipo {itemType} para completar");
            return;
        }
        
        // Verificamos si el ítem se puede completar
        bool isValid = false;
        
        // Para tazas, necesitamos una validación adicional
        if (itemType == 0) // Taza
        {
            isValid = true; // En una implementación real, verificaríamos el nivel del café
            
            // Actualizamos en red los contadores
            if (isValid && _completedCups.Value < totalCups)
            {
                _completedCups.Value++;
                relevantItem.isCompleted = true;
                
                // Notificamos a todos los clientes
                ItemCompletedClientRpc(itemType, _completedCups.Value);
                Debug.Log($"[Servidor] Taza completada. Total: {_completedCups.Value}/{totalCups}");
            }
        }
        // Para donuts, la validación es más simple
        else if (itemType == 1) // Donut
        {
            isValid = true;
            
            if (isValid && _completedDonuts.Value < totalDonuts)
            {
                _completedDonuts.Value++;
                relevantItem.isCompleted = true;
                
                // Notificamos a todos los clientes
                ItemCompletedClientRpc(itemType, _completedDonuts.Value);
                Debug.Log($"[Servidor] Donut completado. Total: {_completedDonuts.Value}/{totalDonuts}");
            }
        }
        
        // Verificar si el pedido está completo
        CheckOrderCompletion();
    }

    // Encuentra el próximo item de un tipo específico que no esté completado
    private OrderItem FindNextUncompletedItem(int itemType)
    {
        foreach (var item in orderItems)
        {
            if (item.itemType == itemType && !item.isCompleted)
            {
                return item;
            }
        }
        return null;
    }

    [ClientRpc]
    private void ItemCompletedClientRpc(int itemType, int newCount)
    {
        // Reproducir sonido de item colocado en todos los clientes
        if (itemPlacedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(itemPlacedSound);
        }
        
        Debug.Log($"[Cliente] Item tipo {itemType} completado. Nuevo conteo: {newCount}");
    }

    private void CheckOrderCompletion()
    {
        if (!IsServer || _isOrderCompleted.Value)
            return;

        // Solo el servidor verifica y actualiza el estado de completado
        if (_completedCups.Value >= totalCups && _completedDonuts.Value >= totalDonuts)
        {
            _isOrderCompleted.Value = true;
            Debug.Log("[Servidor] ¡Pedido completo! Notificando a los clientes.");
        }
    }
    
    private void OnOrderCompletedChanged(bool previousValue, bool newValue)
    {
        if (newValue && !wasOrderCompleted)
        {
            wasOrderCompleted = true;
            
            Debug.Log("¡Pedido completo! Todas las tazas y donuts han sido entregados correctamente.");
            
            // Reproducir sonido de pedido completado
            if (orderCompletedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(orderCompletedSound);
            }
            
            // Invocar evento de pedido completado
            onOrderCompleted?.Invoke();
        }
    }

    // Método público para resetear el sistema (solo el servidor debería llamar a esto)
    [ServerRpc(RequireOwnership = false)]
    public void ResetOrderSystemServerRpc(ulong clientId)
    {
        // Verificar si el cliente tiene permiso para resetear (podríamos implementar roles)
        Debug.Log($"[Servidor] Reseteando sistema por petición del cliente {clientId}");
        
        _completedCups.Value = 0;
        _completedDonuts.Value = 0;
        _isOrderCompleted.Value = false;
        wasOrderCompleted = false;
        
        foreach (var item in orderItems)
        {
            item.isCompleted = false;
        }
        
        // Notificar a todos los clientes
        ResetOrderSystemClientRpc();
    }
    
    [ClientRpc]
    private void ResetOrderSystemClientRpc()
    {
        // Resetear el estado local en todos los clientes
        foreach (var item in orderItems)
        {
            item.isCompleted = false;
        }
        
        wasOrderCompleted = false;
        Debug.Log("[Cliente] Sistema de pedidos reiniciado");
    }
    
    // Wrapper para el método legacy, para mantener compatibilidad
    public void ResetOrderSystem()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            ResetOrderSystemServerRpc(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            // Modo local/no-red
            foreach (var item in orderItems)
            {
                item.isCompleted = false;
            }
            wasOrderCompleted = false;
        }
    }
}