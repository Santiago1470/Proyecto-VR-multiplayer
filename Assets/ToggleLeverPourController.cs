using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToggleLeverPourController : MonoBehaviour
{
    [Header("Sistema de Partículas (Café)")]
    public ParticleSystem coffeeStream;

    [Header("Palanca (XRGrabInteractable)")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable toggleLever;

    [Header("Parámetros de Emisión")]
    public float baseEmissionRate = 50f; // Tasa máxima de emisión para el chorro

    [Header("Taza y Llenado")]
    [Tooltip("Objeto que representa el líquido dentro de la taza")]
    public Transform liquidObject;
    public float fillSpeed = 0.2f;       // Velocidad de llenado (ajústalo según necesidad)
    public float maxFillScaleY = 1f;     // Escala máxima en Y (taza completamente llena)
    public float initialFillScaleY = 0f; // Escala inicial en Y (taza vacía)

    private ParticleSystem.EmissionModule emissionModule;
    private bool isPouring = false;

    void Start()
    {
        if (coffeeStream != null)
        {
            emissionModule = coffeeStream.emission;
        }

        // Suscribir al evento de selección de la palanca
        if (toggleLever != null)
        {
            toggleLever.selectEntered.AddListener(OnToggleLeverSelected);
        }

        // Inicializar el nivel del líquido en la taza
        if (liquidObject != null)
        {
            Vector3 scale = liquidObject.localScale;
            scale.y = initialFillScaleY;
            liquidObject.localScale = scale;
        }
    }

    void OnDestroy()
    {
        if (toggleLever != null)
        {
            toggleLever.selectEntered.RemoveListener(OnToggleLeverSelected);
        }
    }

    // Cada vez que se selecciona la palanca se alterna el estado del efecto
    private void OnToggleLeverSelected(SelectEnterEventArgs args)
    {
        TogglePour();
    }

    private void TogglePour()
    {
        if (isPouring)
        {
            DeactivatePour();
        }
        else
        {
            ActivatePour();
        }
    }

    // Activa el efecto de partículas y comienza a llenar la taza
    public void ActivatePour()
    {
        if (coffeeStream == null) return;

        // Configura la tasa de emisión al valor base y reproduce el Particle System
        var rate = emissionModule.rateOverTime;
        rate.constant = baseEmissionRate;
        emissionModule.rateOverTime = rate;

        if (!coffeeStream.isPlaying)
        {
            coffeeStream.Play();
        }
        isPouring = true;
    }

    // Detiene el efecto de partículas y deja de llenar la taza
    public void DeactivatePour()
    {
        if (coffeeStream == null) return;

        // Configura la tasa de emisión a 0 y detén el Particle System
        var rate = emissionModule.rateOverTime;
        rate.constant = 0;
        emissionModule.rateOverTime = rate;

        if (coffeeStream.isPlaying)
        {
            coffeeStream.Stop();
        }
        isPouring = false;
    }

    void Update()
    {
        if (liquidObject == null)
            return;

        // Mientras se esté vertiendo, aumenta progresivamente el nivel del líquido
        if (isPouring)
        {
            Vector3 scale = liquidObject.localScale;
            scale.y = Mathf.Clamp(scale.y + fillSpeed * Time.deltaTime, initialFillScaleY, maxFillScaleY);
            liquidObject.localScale = scale;
        }
    }
}

