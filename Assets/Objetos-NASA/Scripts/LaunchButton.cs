using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class LaunchButton : MonoBehaviour
{
    public RocketLaunch rocket;
    public TechoMover techo;
    public float pressDepth = 0.02f; // Profundidad del hundimiento visual
    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.AddListener(OnButtonPressed);
        interactable.selectExited.AddListener(OnButtonReleased);
    }

    private void OnDisable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.RemoveListener(OnButtonPressed);
        interactable.selectExited.RemoveListener(OnButtonReleased);
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        // Hundimiento visual del botón
        transform.localPosition = originalPosition - new Vector3(0, pressDepth, 0);

        // Abrir el techo inmediatamente
        if (techo != null)
            techo.AbrirTecho();

        // Iniciar la cuenta regresiva del cohete
        if (rocket != null)
            rocket.StartLaunch();

        // Desactivar el primer botón
        gameObject.SetActive(false);
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        // Restaurar la posición original del botón
        transform.localPosition = originalPosition;
    }
}


