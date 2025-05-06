using UnityEngine;

public class ActivarTextoVR : MonoBehaviour
{
    public GameObject textoCanvas;            // Objeto del texto en el Canvas
    public ItemDoor itemDoorScript;           // Referencia al script que maneja la puerta
    public SlidingDoorCar slidingDoorScript;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Solo mostrar el texto si las puertas están bloqueadas
            if (itemDoorScript != null && !itemDoorScript.CanOpenDoor())
            {
                textoCanvas.SetActive(true);
            }
            // Mostrar el texto solo si las puertas están cerradas
            if (slidingDoorScript != null && !slidingDoorScript.AreDoorsOpen())
            {
                textoCanvas.SetActive(true);
            }

            if (itemDoorScript == null && slidingDoorScript == null)
            {
                textoCanvas.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            textoCanvas.SetActive(false);
        }
    }
}
