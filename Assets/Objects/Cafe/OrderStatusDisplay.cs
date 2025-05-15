using UnityEngine;
using TMPro;
using Unity.Netcode;

[RequireComponent(typeof(OrderCompletionSystem))]
public class OrderStatusDisplay : NetworkBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Referencia al TextMeshProUGUI para mostrar el estado del pedido")]
    public TextMeshProUGUI statusText;

    [Header("Formato")]
    [Tooltip("Formato del texto. {0}=tazas completadas, {1}=total tazas, {2}=donuts completados, {3}=total donuts")]
    public string textFormat = "Pedido actual:\n<color=#C87B30>Tazas: {0}/{1}</color>\n<color=#FF69B4>Donuts: {2}/{3}</color>";
    
    private OrderCompletionSystem orderSystem;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Obtener referencia al sistema de pedidos
        orderSystem = GetComponent<OrderCompletionSystem>();
        
        if (orderSystem == null)
        {
            Debug.LogError("No se encontró el componente OrderCompletionSystem");
            enabled = false;
            return;
        }

        if (statusText == null)
        {
            Debug.LogError("No se ha asignado un TextMeshProUGUI para mostrar el estado");
            enabled = false;
            return;
        }

        // Actualizar la UI inicial
        UpdateStatusDisplay();
    }

    private void Update()
    {
        // Actualizar la UI en cada frame para reflejar cambios
        // Esto funciona para todos los clientes ya que ahora los datos están sincronizados en red
        UpdateStatusDisplay();
    }

    private void UpdateStatusDisplay()
    {
        if (orderSystem == null || statusText == null)
            return;
            
        // Formatear y mostrar la información de pedidos
        statusText.text = string.Format(textFormat, 
            orderSystem.completedCups, 
            orderSystem.totalCups, 
            orderSystem.completedDonuts, 
            orderSystem.totalDonuts);
        
        // Comprobar si el pedido está completo y mostrar mensaje adicional
        if (orderSystem.completedCups == orderSystem.totalCups && 
            orderSystem.completedDonuts == orderSystem.totalDonuts)
        {
            statusText.text += "\n\n<color=#00FF00>¡PEDIDO COMPLETO!</color>";
        }
    }

    // Método opcional para inicializar las referencias si se agrega el script en tiempo de ejecución
    public void Initialize(TextMeshProUGUI textComponent, OrderCompletionSystem orderCompletionSystem)
    {
        statusText = textComponent;
        orderSystem = orderCompletionSystem;
        UpdateStatusDisplay();
    }
    
    // Este método se puede usar para crear una UI que siga a cada jugador
    public void SetupPlayerSpecificUI(TextMeshProUGUI playerSpecificText)
    {
        statusText = playerSpecificText;
        UpdateStatusDisplay();
    }
}