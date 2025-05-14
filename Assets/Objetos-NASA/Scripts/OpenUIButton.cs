using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class OpenUIButton : MonoBehaviour
{
    public Canvas instruccionesCanvas; // Asigna el Canvas en el Inspector
    public float pressDepth = 0.02f;   // Efecto visual opcional de hundimiento

    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.AddListener(MostrarCanvas);
        interactable.selectExited.AddListener(RestaurarBoton);
    }

    private void OnDisable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.RemoveListener(MostrarCanvas);
        interactable.selectExited.RemoveListener(RestaurarBoton);
    }

    private void MostrarCanvas(SelectEnterEventArgs args)
    {
        // Efecto visual del botón presionado
        transform.localPosition = originalPosition - new Vector3(0, pressDepth, 0);

        // Mostrar Canvas si no está activo
        if (instruccionesCanvas != null && !instruccionesCanvas.gameObject.activeSelf)
        {
            instruccionesCanvas.gameObject.SetActive(true);
        }
    }

    private void RestaurarBoton(SelectExitEventArgs args)
    {
        // Restaurar posición del botón
        transform.localPosition = originalPosition;
    }
}
