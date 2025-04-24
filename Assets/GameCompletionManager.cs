using UnityEngine;
using UnityEngine.Events;

public class GameCompletionManager : MonoBehaviour
{
    [Tooltip("Número total de slots que deben llenarse para completar el minijuego")]
    public int totalSlots = 12;

    [Tooltip("Evento que se dispara cuando el minijuego se completa")]
    public UnityEvent onGameCompleted;

    private int occupiedSlots = 0;

    /// <summary>
    /// Llamar desde cada SocketSlot cuando se ocupa un slot válido.
    /// </summary>
    public void NotifySlotOccupied()
    {
        occupiedSlots++;
        CheckCompletion();
    }

    /// <summary>
    /// Llamar desde cada SocketSlot cuando un objeto sale del slot.
    /// </summary>
    public void NotifySlotFreed()
    {
        occupiedSlots = Mathf.Max(0, occupiedSlots - 1);
    }

    private void CheckCompletion()
    {
        if (occupiedSlots >= totalSlots)
        {
            Debug.Log("¡Minijuego completado! Todos los objetos están en su lugar.");
            onGameCompleted?.Invoke();
        }
    }
}
