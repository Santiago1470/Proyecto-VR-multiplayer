using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class OrderCompletionSystem : NetworkBehaviour
{
    [System.Serializable]
    public class OrderItem
    {
        public string itemName;
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
        public NetworkVariable<bool> isCompleted = new(false);
        
        [Tooltip("Tipo de item: 0 = Taza, 1 = Donut")]
        public int itemType = 0; // 0 = Taza, 1 = Donut
    }

    [Header("Pedidos")]
    public OrderItem[] orderItems;

    [Header("Contadores")]
    public NetworkVariable<int> totalCups = new(5);
    public NetworkVariable<int> totalDonuts = new(3);
    public NetworkVariable<int> completedCups = new(0);
    public NetworkVariable<int> completedDonuts = new(0);

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

    public NetworkVariable<bool> isOrderCompleted = new(false);

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("AudioSource agregado automáticamente");
        }

        // Registrar eventos para cada socket
        
            foreach (var socketItem in orderItems)
            {
                var itemCopy = socketItem;
                if (itemCopy.socket != null)
                {
                    itemCopy.socket.selectEntered.AddListener((args) =>
                    {
                        if (IsServer) OnItemPlaced(itemCopy, args.interactableObject);
                    });
                }
            }
        
        // Inicializar contadores
        completedCups.Value = 0;
        completedDonuts.Value = 0;
        isOrderCompleted.Value = false;
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
        if (!IsServer || item.isCompleted.Value || isOrderCompleted.Value) return;

        GameObject placedObject = interactable.transform.gameObject;

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
                        CompleteItem(item, 0);
                        break;
                    }
                }
            }
        }
        // Verificar si es un donut
        else if (item.itemType == 1)
        {
            CompleteItem(item, 1); // Completar como donut
        }

        CheckOrderCompletion();
    }

    private void CompleteItem(OrderItem item, int type)
    {
        if (!IsServer) return;

        item.isCompleted.Value = true;

        if (type == 0)
        {
            completedCups.Value++;
            PlaySoundClientRpc("itemPlaced"); // Envía un string identificador
        }
        else if (type == 1)
        {
            completedDonuts.Value++;
            PlaySoundClientRpc("itemPlaced");
        }
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("UpdateUI", new FastBufferWriter());
    }

    [ClientRpc]
    private void PlaySoundClientRpc(string soundType)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        switch (soundType)
        {
            case "itemPlaced":
                if (itemPlacedSound != null) audioSource.PlayOneShot(itemPlacedSound);
                else Debug.LogWarning("No se asignó itemPlacedSound");
                break;
            case "orderCompleted":
                if (orderCompletedSound != null) audioSource.PlayOneShot(orderCompletedSound);
                else Debug.LogWarning("No se asignó orderCompletedSound");
                break;
        }
    }

    // Método público para resetear el sistema
    public void ResetOrderSystem()
    {
        if (!IsServer) return;

        completedCups.Value = 0;
        completedDonuts.Value = 0;
        isOrderCompleted.Value = false;

        foreach (var item in orderItems)
        {
            item.isCompleted.Value = false;
        }
    }
    private void CheckOrderCompletion()
    {
        if (!IsServer || isOrderCompleted.Value) return;

        if (completedCups.Value == totalCups.Value && completedDonuts.Value == totalDonuts.Value)
        {
            isOrderCompleted.Value = true;
            PlaySoundClientRpc("orderCompleted"); // Envía el identificador
            onOrderCompleted?.Invoke();
        }
    }
}