using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SocketSlot : MonoBehaviour
{
    [Tooltip("Referencia al GameManager que controla la lógica de fin de juego")]
    public GameCompletionManager gameManager;

    // Indica si este slot está actualmente ocupado por un objeto válido
    private bool isOccupied = false;

    void Reset()
    {
        // Si no se arrastra manualmente, intenta encontrar un GameManager en la escena
        if (gameManager == null)
            gameManager = FindObjectOfType<GameCompletionManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Sólo contar si el tag es "Cup" o "Donut" y aún no estaba ocupado
        if (!isOccupied && (other.CompareTag("Cup") || other.CompareTag("Donut")))
        {
            isOccupied = true;
            gameManager.NotifySlotOccupied();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Cuando salga el objeto con tag válido, liberar el slot
        if (isOccupied && (other.CompareTag("Cup") || other.CompareTag("Donut")))
        {
            isOccupied = false;
            gameManager.NotifySlotFreed();
        }
    }
}
