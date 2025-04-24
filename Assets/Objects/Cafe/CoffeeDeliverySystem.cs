using UnityEngine;

using System.Collections.Generic;

public class CoffeeDeliverySystem : MonoBehaviour
{
    [Header("Referencias")]
    public LiquidFiller liquidFiller;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] deliveryLocations = new UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[7]; // 7 lugares
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] donutLocations = new UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[5];    // 5 donas

    [Header("Configuración")]
    public float requiredFillLevel = 0.28f; // Nivel considerado "lleno" (ajusta esto según tu maxFillHeight)
    
    [Header("Estado del Juego")]
    public List<bool> deliveriesCompleted = new List<bool>();
    public List<bool> donutsDelivered = new List<bool>();
    
    [Header("UI y Feedback")]
    public AudioClip deliverySuccessSound;
    public AudioClip gameCompletedSound;
    private AudioSource audioSource;
    
    private bool notifiedFull = false;
    private int totalDeliveriesCompleted = 0;
    private int totalDonutsDelivered = 0;
    
    void Start()
    {
        // Inicializar listas de seguimiento
        for (int i = 0; i < deliveryLocations.Length; i++)
        {
            deliveriesCompleted.Add(false);
            
            // Configurar eventos de socket para cafés
            if (deliveryLocations[i] != null)
            {
                int locationIndex = i; // Importante crear una variable local para la clausura
                deliveryLocations[i].selectEntered.AddListener((args) => {
                    HandleCoffeeDelivery(locationIndex, args.interactableObject);
                });
            }
        }
        
        // Inicializar listas para donas
        for (int i = 0; i < donutLocations.Length; i++)
        {
            donutsDelivered.Add(false);
            
            // Configurar eventos de socket para donas
            if (donutLocations[i] != null)
            {
                int locationIndex = i; // Importante crear una variable local para la clausura
                donutLocations[i].selectEntered.AddListener((args) => {
                    HandleDonutDelivery(locationIndex, args.interactableObject);
                });
            }
        }
        
        // Configurar audio source
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Update()
    {
        // Verificar si la taza está llena
        if (liquidFiller.currentFill >= requiredFillLevel && !notifiedFull)
        {
            Debug.Log("¡La taza está llena! ¡Lista para entregar!");
            notifiedFull = true;
            // Aquí puedes activar un efecto visual o sonido que indique que la taza está lista
        }
        else if (liquidFiller.currentFill < requiredFillLevel)
        {
            notifiedFull = false;
        }
        
        // Verificar si el juego está completo
        CheckGameCompletion();
    }
    
    void HandleCoffeeDelivery(int locationIndex, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable deliveredObject)
    {
        // Verificar si es una taza de café
        CoffeeCup coffeeCup = deliveredObject.transform.GetComponent<CoffeeCup>();
        if (coffeeCup == null) return;
        
        // Verificar si la taza está llena
        if (liquidFiller.currentFill >= requiredFillLevel)
        {
            // Marcar la entrega como completada si no se había completado antes
            if (!deliveriesCompleted[locationIndex])
            {
                deliveriesCompleted[locationIndex] = true;
                totalDeliveriesCompleted++;
                
                Debug.Log($"¡Café entregado en la ubicación {locationIndex + 1}!");
                
                // Reproducir sonido de éxito
                if (deliverySuccessSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(deliverySuccessSound);
                }
                
                // Otorgar puntos o actualizar UI aquí
            }
        }
    }
    
    void HandleDonutDelivery(int locationIndex, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable deliveredObject)
    {
        // Verificar si es una dona
        Donut donut = deliveredObject.transform.GetComponent<Donut>();
        if (donut == null) return;
        
        // Marcar la dona como entregada si no se había entregado antes
        if (!donutsDelivered[locationIndex])
        {
            donutsDelivered[locationIndex] = true;
            totalDonutsDelivered++;
            
            Debug.Log($"¡Dona entregada en la ubicación {locationIndex + 1}!");
            
            // Reproducir sonido de éxito
            if (deliverySuccessSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deliverySuccessSound);
            }
            
            // Otorgar puntos o actualizar UI aquí
        }
    }
    
    void CheckGameCompletion()
    {
        // Determinar si todos los cafés y donas han sido entregados
        if (totalDeliveriesCompleted == deliveryLocations.Length && 
            totalDonutsDelivered == donutLocations.Length)
        {
            Debug.Log("¡Juego completado! Todas las entregas realizadas.");
            
            // Reproducir sonido de juego completado
            if (gameCompletedSound != null && audioSource != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(gameCompletedSound);
            }
            
            // Aquí puedes activar alguna celebración o pantalla de victoria
        }
    }
    
    // Función para reiniciar el juego
    public void ResetGame()
    {
        // Reiniciar el estado de entregas
        for (int i = 0; i < deliveriesCompleted.Count; i++)
        {
            deliveriesCompleted[i] = false;
        }
        
        for (int i = 0; i < donutsDelivered.Count; i++)
        {
            donutsDelivered[i] = false;
        }
        
        totalDeliveriesCompleted = 0;
        totalDonutsDelivered = 0;
        
        // Reiniciar líquido
        liquidFiller.ResetLiquid();
        
        Debug.Log("Juego reiniciado");
    }
}