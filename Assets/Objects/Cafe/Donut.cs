using UnityEngine;


// Este script se agrega a las donas para identificarlas
public class Donut : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    public DonutType type = DonutType.Regular;
    
    public enum DonutType
    {
        Regular,
        Chocolate,
        Glazed,
        Sprinkles,
        Strawberry
    }
    
    void Start()
    {
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }
    }
    
    // Puedes agregar más funcionalidad específica para las donas aquí
}