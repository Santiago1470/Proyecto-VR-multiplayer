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

    private void Start()
    {
        // Obtener referencia al sistema de pedidos
        orderSystem = GetComponent<OrderCompletionSystem>();
        
        // Suscribirse a cambios en NetworkVariables
        orderSystem.completedCups.OnValueChanged += OnCupsChanged;
        orderSystem.completedDonuts.OnValueChanged += OnDonutsChanged;

        // Actualizar la UI inicial
        UpdateStatusDisplay();
    }

    private void OnCupsChanged(int prev, int current)
    {
        UpdateStatusDisplay();
    }

    private void OnDonutsChanged(int prev, int current)
    {
        UpdateStatusDisplay();
    }
    private void Update()
    {
        // Actualizar la UI en cada frame para reflejar cambios
        UpdateStatusDisplay();
    }

     private void UpdateStatusDisplay()
    {
        if (statusText != null)
        {
            statusText.text = string.Format(textFormat,
                orderSystem.completedCups.Value,
                orderSystem.totalCups.Value,
                orderSystem.completedDonuts.Value,
                orderSystem.totalDonuts.Value);

            if (orderSystem.completedCups.Value == orderSystem.totalCups.Value &&
                orderSystem.completedDonuts.Value == orderSystem.totalDonuts.Value)
            {
                statusText.text += "\n\n<color=#00FF00>¡PEDIDO COMPLETO!</color>";
            }
        }
    }

    private void OnDestroy()
    {
        if (orderSystem != null)
        {
            orderSystem.completedCups.OnValueChanged -= OnCupsChanged;
            orderSystem.completedDonuts.OnValueChanged -= OnDonutsChanged;
        }
    }
}