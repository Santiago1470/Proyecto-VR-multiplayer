using UnityEngine;


public class XRSocketTagFilter : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
{
    [Tooltip("Tag que debe tener el objeto para ser aceptado en este socket.")]
    public string tagAceptado = "ParteB";

    public override bool CanSelect(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        // Asegura que se cumpla la l�gica base y adem�s que el tag coincida
        return base.CanSelect(interactable) && interactable.transform.CompareTag(tagAceptado);
    }
}
