using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SimpleXRAudioToggle : MonoBehaviour
{
    public AudioSource audioSource;
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.5f;
    
    private bool isPlaying = false;
    private float currentVolume = 0f;
    private float targetVolume = 0f;
    private float initialVolume;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    private void Start()
    {
        // Obtener el componente interactable
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        
        // Suscribirse al evento de selección
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
        }
        
        // Guardar el volumen inicial
        if (audioSource != null)
        {
            initialVolume = audioSource.volume;
            audioSource.volume = 0f;
        }
    }

    private void Update()
    {
        if (audioSource != null)
        {
            // Aplicar el efecto fade
            float fadeSpeed = isPlaying ? fadeInTime : fadeOutTime;
            fadeSpeed = fadeSpeed <= 0 ? 0.01f : fadeSpeed;
            
            currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime / fadeSpeed);
            audioSource.volume = currentVolume;
            
            // Detener el audio si el volumen es casi cero
            if (!isPlaying && currentVolume < 0.01f && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        ToggleAudio();
    }
    
    // Para pruebas sin XR
    private void OnMouseDown()
    {
        ToggleAudio();
    }
    
    public void ToggleAudio()
    {
        if (audioSource != null)
        {
            // Invertir el estado de reproducción
            isPlaying = !isPlaying;
            
            if (isPlaying)
            {
                // Comenzar la reproducción con fade in
                targetVolume = initialVolume;
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else
            {
                // Iniciar fade out
                targetVolume = 0f;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse del evento
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }
}