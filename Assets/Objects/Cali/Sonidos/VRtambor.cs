using UnityEngine;

public class VRDrum : MonoBehaviour
{
    [Header("Configuración de Audio")]
    public AudioClip sonidoTambor; // El sonido único del tambor
    public AudioSource audioSource;
    public float volumenMinimo = 0.3f;
    public float volumenMaximo = 1.0f;
    
    [Header("Configuración de Detección")]
    public string tagObjetoGolpeador = "Baqueta"; // Tag del objeto que puede golpear
    public float fuerzaMinimaGolpe = 2.0f; // Fuerza mínima para activar el sonido
    public float tiempoEntreSonidos = 0.05f; // Tiempo mínimo entre golpes
    
    [Header("Configuración de Respuesta")]
    public bool variacionPitch = true; // Si debe variar ligeramente el pitch
    public float rangoVariacionPitch = 0.1f; // Rango de variación del pitch (±)
    public bool efectoVisual = true; // Si debe mostrar efecto visual al golpear
    public ParticleSystem particulasGolpe; // Sistema de partículas opcional
    
    // Variables privadas
    private float ultimoTiempoGolpe;
    private Vector3 escalaOriginal;
    private Coroutine corrutinaEfecto;
    
    void Start()
    {
        // Configurar AudioSource
        ConfigurarAudioSource();
        
        // Guardar escala original para efecto visual
        escalaOriginal = transform.localScale;
        
        // Verificar que hay un Collider configurado como Trigger
        VerificarCollider();
    }
    
    void ConfigurarAudioSource()
    {
        // Obtener o crear AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Configurar AudioSource
        audioSource.clip = sonidoTambor;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // Audio 3D completo
        audioSource.volume = volumenMaximo;
    }
    
    void VerificarCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"El tambor {gameObject.name} necesita un Collider para detectar golpes");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"Se recomienda que el Collider del tambor {gameObject.name} sea un Trigger para mejor detección");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Verificar si el objeto que golpea tiene el tag correcto
        if (!other.CompareTag(tagObjetoGolpeador)) return;
        
        // Verificar tiempo entre golpes
        if (Time.time - ultimoTiempoGolpe < tiempoEntreSonidos) return;
        
        // Calcular intensidad del golpe basado en la velocidad
        float intensidad = CalcularIntensidadGolpe(other);
        
        // Verificar si la intensidad es suficiente
        if (intensidad >= fuerzaMinimaGolpe)
        {
            TocarTambor(intensidad);
            ultimoTiempoGolpe = Time.time;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Alternativa usando OnCollisionEnter si no usas Trigger
        if (!collision.gameObject.CompareTag(tagObjetoGolpeador)) return;
        
        // Verificar tiempo entre golpes
        if (Time.time - ultimoTiempoGolpe < tiempoEntreSonidos) return;
        
        // Calcular intensidad basada en la fuerza del impacto
        float intensidad = collision.relativeVelocity.magnitude;
        
        if (intensidad >= fuerzaMinimaGolpe)
        {
            TocarTambor(intensidad);
            ultimoTiempoGolpe = Time.time;
        }
    }
    
    float CalcularIntensidadGolpe(Collider other)
    {
        // Intentar obtener la velocidad del Rigidbody
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            return rb.linearVelocity.magnitude;
        }
        
        // Si no hay Rigidbody, usar una intensidad base
        return fuerzaMinimaGolpe + 1f;
    }
    
    void TocarTambor(float intensidad)
    {
        if (sonidoTambor == null)
        {
            Debug.LogWarning("No hay sonido asignado al tambor");
            return;
        }
        
        // Calcular volumen basado en la intensidad
        float volumen = Mathf.Lerp(volumenMinimo, volumenMaximo, 
            Mathf.Clamp01(intensidad / (fuerzaMinimaGolpe * 3)));
        
        // Configurar pitch
        float pitch = 1.0f;
        if (variacionPitch)
        {
            pitch = 1.0f + Random.Range(-rangoVariacionPitch, rangoVariacionPitch);
        }
        
        // Reproducir sonido
        audioSource.volume = volumen;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(sonidoTambor);
        
        // Efectos visuales
        if (efectoVisual)
        {
            MostrarEfectoVisual(intensidad);
        }
        
        // Activar partículas si están asignadas
        if (particulasGolpe != null)
        {
            particulasGolpe.Play();
        }
        
        Debug.Log($"Tambor golpeado - Intensidad: {intensidad:F2}, Volumen: {volumen:F2}, Pitch: {pitch:F2}");
    }
    
    void MostrarEfectoVisual(float intensidad)
    {
        // Detener efecto anterior si existe
        if (corrutinaEfecto != null)
        {
            StopCoroutine(corrutinaEfecto);
        }
        
        // Iniciar nuevo efecto
        corrutinaEfecto = StartCoroutine(EfectoRebote(intensidad));
    }
    
    System.Collections.IEnumerator EfectoRebote(float intensidad)
    {
        // Calcular escala del efecto basada en la intensidad
        float factorEscala = 1.0f + (intensidad / (fuerzaMinimaGolpe * 10)) * 0.1f;
        Vector3 escalaGolpe = escalaOriginal * factorEscala;
        
        // Animación de "rebote"
        float duracion = 0.1f;
        float tiempo = 0f;
        
        // Expandir
        while (tiempo < duracion / 2)
        {
            tiempo += Time.deltaTime;
            float progreso = tiempo / (duracion / 2);
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaGolpe, progreso);
            yield return null;
        }
        
        // Contraer
        tiempo = 0f;
        while (tiempo < duracion / 2)
        {
            tiempo += Time.deltaTime;
            float progreso = tiempo / (duracion / 2);
            transform.localScale = Vector3.Lerp(escalaGolpe, escalaOriginal, progreso);
            yield return null;
        }
        
        // Asegurar que vuelve a la escala original
        transform.localScale = escalaOriginal;
        corrutinaEfecto = null;
    }
    
    // Métodos públicos para configuración externa
    public void CambiarSonido(AudioClip nuevoSonido)
    {
        sonidoTambor = nuevoSonido;
        audioSource.clip = nuevoSonido;
    }
    
    public void AjustarSensibilidad(float nuevaFuerza)
    {
        fuerzaMinimaGolpe = nuevaFuerza;
    }
    
    public void AjustarVolumen(float nuevoVolumenMin, float nuevoVolumenMax)
    {
        volumenMinimo = Mathf.Clamp01(nuevoVolumenMin);
        volumenMaximo = Mathf.Clamp01(nuevoVolumenMax);
    }
    
    // Método para testear el tambor desde el editor
    [ContextMenu("Probar Tambor")]
    void ProbarTambor()
    {
        TocarTambor(fuerzaMinimaGolpe * 2);
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualizar área de detección
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = col as SphereCollider;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
        
        // Indicar si está reproduciéndose
        if (Application.isPlaying && audioSource != null && audioSource.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}