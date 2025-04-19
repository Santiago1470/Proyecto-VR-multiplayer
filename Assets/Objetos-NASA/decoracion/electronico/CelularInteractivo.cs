using UnityEngine;

public class CelularInteractivo : MonoBehaviour
{
    public GameObject uiCamara;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Asegúrate de que el jugador tenga ese tag
        {
            uiCamara.SetActive(true); // Muestra el UI del celular
        }
    }
}