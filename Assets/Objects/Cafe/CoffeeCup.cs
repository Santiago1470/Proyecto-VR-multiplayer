using UnityEngine;


// Este script se agrega a la taza de café para identificarla
public class CoffeeCup : MonoBehaviour
{
    public LiquidFiller liquidFiller;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    
    void Start()
    {
        if (liquidFiller == null)
        {
            liquidFiller = GetComponent<LiquidFiller>();
        }
        
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }
    }
    
    // Puedes agregar más funcionalidad específica para la taza aquí
}