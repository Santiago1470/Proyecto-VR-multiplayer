using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRWindInstrument : MonoBehaviour
{
    [Header("Configuración de Audio")]
    public AudioClip[] sonidosInstrumento; // Array de clips de sonido para variedad
    public AudioSource audioSource;
    public float volumenReproduccion = 0.8f;
    
    [Header("Configuración de Reproducción")]
    public bool reproducirEnBucle = true; // Para instrumentos que suenan continuamente
    public float tiempoFadeIn = 0.2f; // Tiempo para hacer fade in del sonido
    public float tiempoFadeOut = 0.3f; // Tiempo para hacer fade out del sonido
    public bool sonidoAleatorio = false; // Si debe elegir sonidos al azar o secuencial
    
    [Header("Configuración Avanzada")]
    public float tiempoMinimoReproduccion = 0.5f; // Tiempo mínimo que debe sonar
    public bool permitirCambiarSonido = true; // Permitir cambiar sonido mientras está agarrado
    public float intervaloCambioSonido = 3f; // Intervalo para cambiar sonido automáticamente
    
    // Variables privadas
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool estaAgarrado = false;
    private bool estaReproduciendo = false;
    private Coroutine corrutinaSonido;
    private Coroutine corrutinaCambioSonido;
    private int indiceSonidoActual = 0;
    private float tiempoInicioReproduccion;
    
    void Start()
    {
        // Configurar componentes
        ConfigurarComponentes();
        
        // Configurar eventos de agarre
        ConfigurarEventosAgarre();
        
        // Configurar AudioSource
        ConfigurarAudioSource();
    }
    
    void ConfigurarComponentes()
    {
        // Obtener o crear XRGrabInteractable
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }
        
        // Obtener o crear AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Asegurar que hay un Collider para la interacción
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"El GameObject {gameObject.name} necesita un Collider para funcionar con XR Interaction Toolkit");
        }
    }
    
    void ConfigurarEventosAgarre()
    {
        // Suscribirse a eventos de agarre
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }
    
    void ConfigurarAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // Audio 3D completo
        audioSource.volume = 0f; // Empezar en silencio para el fade in
        audioSource.loop = reproducirEnBucle;
    }
    
    void OnGrab(SelectEnterEventArgs args)
    {
        estaAgarrado = true;
        IniciarReproduccion();
        
        Debug.Log($"Instrumento {gameObject.name} agarrado por {args.interactorObject.transform.name}");
    }
    
    void OnRelease(SelectExitEventArgs args)
    {
        estaAgarrado = false;
        DetenerReproduccion();
        
        Debug.Log($"Instrumento {gameObject.name} soltado");
    }
    
    void IniciarReproduccion()
    {
        if (sonidosInstrumento.Length == 0)
        {
            Debug.LogWarning("No hay clips de audio asignados al instrumento");
            return;
        }
        
        estaReproduciendo = true;
        tiempoInicioReproduccion = Time.time;
        
        // Detener corrutinas anteriores
        if (corrutinaSonido != null)
            StopCoroutine(corrutinaSonido);
        if (corrutinaCambioSonido != null)
            StopCoroutine(corrutinaCambioSonido);
        
        // Iniciar reproducción
        corrutinaSonido = StartCoroutine(ReproducirConFade());
        
        // Iniciar cambio de sonido automático si está habilitado
        if (permitirCambiarSonido && sonidosInstrumento.Length > 1)
        {
            corrutinaCambioSonido = StartCoroutine(CambiarSonidoPeriodicamente());
        }
    }
    
    void DetenerReproduccion()
    {
        if (!estaReproduciendo) return;
        
        // Verificar tiempo mínimo de reproducción
        float tiempoReproducido = Time.time - tiempoInicioReproduccion;
        
        if (tiempoReproducido < tiempoMinimoReproduccion)
        {
            // Esperar el tiempo mínimo antes de detener
            StartCoroutine(DetenerDespuesDeMinimo(tiempoMinimoReproduccion - tiempoReproducido));
        }
        else
        {
            // Detener inmediatamente con fade out
            if (corrutinaSonido != null)
                StopCoroutine(corrutinaSonido);
            if (corrutinaCambioSonido != null)
                StopCoroutine(corrutinaCambioSonido);
            
            corrutinaSonido = StartCoroutine(DetenerConFade());
        }
    }
    
    System.Collections.IEnumerator ReproducirConFade()
    {
        // Seleccionar sonido
        AudioClip clipSeleccionado = SeleccionarSonido();
        audioSource.clip = clipSeleccionado;
        
        // Reproducir
        audioSource.Play();
        
        // Fade in
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoFadeIn)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / tiempoFadeIn;
            audioSource.volume = Mathf.Lerp(0f, volumenReproduccion, progreso);
            yield return null;
        }
        
        audioSource.volume = volumenReproduccion;
    }
    
    System.Collections.IEnumerator DetenerConFade()
    {
        estaReproduciendo = false;
        float volumenInicial = audioSource.volume;
        
        // Fade out
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoFadeOut)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / tiempoFadeOut;
            audioSource.volume = Mathf.Lerp(volumenInicial, 0f, progreso);
            yield return null;
        }
        
        audioSource.volume = 0f;
        audioSource.Stop();
    }
    
    System.Collections.IEnumerator DetenerDespuesDeMinimo(float tiempoEspera)
    {
        yield return new WaitForSeconds(tiempoEspera);
        
        // Solo detener si ya no está agarrado
        if (!estaAgarrado)
        {
            if (corrutinaSonido != null)
                StopCoroutine(corrutinaSonido);
            if (corrutinaCambioSonido != null)
                StopCoroutine(corrutinaCambioSonido);
            
            corrutinaSonido = StartCoroutine(DetenerConFade());
        }
    }
    
    System.Collections.IEnumerator CambiarSonidoPeriodicamente()
    {
        while (estaAgarrado && estaReproduciendo)
        {
            yield return new WaitForSeconds(intervaloCambioSonido);
            
            if (estaAgarrado && estaReproduciendo)
            {
                CambiarSonido();
            }
        }
    }
    
    AudioClip SeleccionarSonido()
    {
        if (sonidoAleatorio)
        {
            indiceSonidoActual = Random.Range(0, sonidosInstrumento.Length);
        }
        else
        {
            indiceSonidoActual = (indiceSonidoActual + 1) % sonidosInstrumento.Length;
        }
        
        return sonidosInstrumento[indiceSonidoActual];
    }
    
    void CambiarSonido()
    {
        if (sonidosInstrumento.Length <= 1) return;
        
        // Cambiar a un nuevo sonido
        AudioClip nuevoClip = SeleccionarSonido();
        
        // Cambiar el clip sin interrumpir la reproducción
        if (reproducirEnBucle)
        {
            audioSource.clip = nuevoClip;
            // El loop continuará con el nuevo clip
        }
        else
        {
            // Para clips no looped, reproducir el nuevo clip
            audioSource.clip = nuevoClip;
            audioSource.Play();
        }
        
        Debug.Log($"Cambiado a sonido: {nuevoClip.name}");
    }
    
    // Métodos públicos para control externo
    public void CambiarSonidoManual()
    {
        if (estaReproduciendo)
        {
            CambiarSonido();
        }
    }
    
    public void AjustarVolumen(float nuevoVolumen)
    {
        volumenReproduccion = Mathf.Clamp01(nuevoVolumen);
        if (estaReproduciendo)
        {
            audioSource.volume = volumenReproduccion;
        }
    }
    
    public bool EstaReproduciendo()
    {
        return estaReproduciendo && estaAgarrado;
    }
    
    void OnDestroy()
    {
        // Desuscribirse de eventos para evitar errores
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualizar estado en el editor
        if (Application.isPlaying)
        {
            Gizmos.color = estaAgarrado ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            
            if (estaReproduciendo)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 0.3f, Vector3.one * 0.1f);
            }
        }
    }
}