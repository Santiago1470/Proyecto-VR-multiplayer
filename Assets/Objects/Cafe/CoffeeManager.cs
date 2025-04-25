using UnityEngine;
using UnityEngine.Events;

public class OrderCompletionSystem : MonoBehaviour
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
    public int totalCups = 7;
    public int totalDonuts = 5;
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

    [Header("Eventos")]
    public UnityEvent onOrderCompleted;

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
                        CompleteItem(item, 0);
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
            CompleteItem(item, 1); // Completar como donut
        }

        CheckOrderCompletion();
    }

    private void CompleteItem(OrderItem item, int type)
    {
        if (item.isCompleted)
            return;

        item.isCompleted = true;

        // Incrementar el contador apropiado
        if (type == 0)
        {
            completedCups++;
            Debug.Log($"Taza completada. Total: {completedCups}/{totalCups}");
        }
        else if (type == 1)
        {
            completedDonuts++;
            Debug.Log($"Donut completado. Total: {completedDonuts}/{totalDonuts}");
        }

        // Reproducir sonido de item colocado
        if (itemPlacedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(itemPlacedSound);
            Debug.Log("Reproduciendo sonido de item colocado");
        }
    }

    private void CheckOrderCompletion()
    {
        if (isOrderCompleted)
            return;

        // Verificar si se completaron todas las tazas y donuts
        if (completedCups == totalCups && completedDonuts == totalDonuts)
        {
            isOrderCompleted = true;
            
            Debug.Log("¡Pedido completo! Todas las tazas y donuts han sido entregados correctamente.");
            
            // Reproducir sonido de pedido completado
            if (orderCompletedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(orderCompletedSound);
                Debug.Log("Reproduciendo sonido de pedido completado");
            }
            
            // Invocar evento de pedido completado
            onOrderCompleted?.Invoke();
        }
    }

    // Método público para resetear el sistema
    public void ResetOrderSystem()
    {
        completedCups = 0;
        completedDonuts = 0;
        isOrderCompleted = false;
        
        foreach (var item in orderItems)
        {
            item.isCompleted = false;
        }
        
        Debug.Log("Sistema de pedidos reiniciado");
    }
}