using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DespegueButton : MonoBehaviour
{
    public RocketLaunch rocket;

    private void OnEnable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.AddListener(OnButtonPressed);
    }

    private void OnDisable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.RemoveListener(OnButtonPressed);
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        // Lanzar el cohete
        if (rocket != null)
            rocket.LaunchRocket();

        // Desactivar este botón después de ser presionado
        gameObject.SetActive(false);
    }
}


