using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MartilloGolpe : MonoBehaviour
{
    private bool estaEnMano = false;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnAgarrado);
            grabInteractable.selectExited.AddListener(OnSoltado);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnAgarrado);
            grabInteractable.selectExited.RemoveListener(OnSoltado);
        }
    }

    private void OnAgarrado(SelectEnterEventArgs args)
    {
        estaEnMano = true;
        Debug.Log("Martillo agarrado");
    }

    private void OnSoltado(SelectExitEventArgs args)
    {
        estaEnMano = false;
        Debug.Log("Martillo soltado");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Solo activar si el martillo está en mano
        if (estaEnMano && collision.gameObject.CompareTag("Carro"))
        {
            CarroRepair reparador = collision.gameObject.GetComponent<CarroRepair>();
            if (reparador != null)
            {
                reparador.Reparar();
            }
        }
    }

    // Método manual si algún día quieres cambiar esto desde otro script
    public void SetMartilloEnMano(bool enMano)
    {
        estaEnMano = enMano;
    }
}