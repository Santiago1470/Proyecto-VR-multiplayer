using UnityEngine;
using UnityEngine.XR;

public class VRMaracas : MonoBehaviour
{
    [Header("Configuración de Audio")]
    public AudioClip[] sonidosMaracas; // Array de clips de sonido para variedad
    public AudioSource audioSource;
    public float volumenMinimo = 0.1f;
    public float volumenMaximo = 1.0f;
    
    [Header("Configuración de Movimiento")]
    public float umbralVelocidad = 2.0f; // Velocidad mínima para activar sonido
    public float umbralAceleracion = 5.0f; // Aceleración mínima para activar sonido
    public float tiempoEntreSonidos = 0.1f; // Tiempo mínimo entre sonidos
    
    // Variables privadas
    private Vector3 velocidadAnterior;
    private Vector3 posicionAnterior;
    private float ultimoTiempoSonido;
    
    // Variables para suavizado
    private Vector3[] historialVelocidades;
    private int indiceHistorial = 0;
    private const int tamañoHistorial = 5;
    
    void Start()
    {
        // Inicializar el AudioSource si no está asignado
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Configurar AudioSource
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // Audio 3D completo
        
        // Inicializar historial de velocidades
        historialVelocidades = new Vector3[tamañoHistorial];
        
        // Inicializar posición
        posicionAnterior = transform.position;
    }
    
    void Update()
    {
        // Calcular velocidad y aceleración
        Vector3 posicionActual = transform.position;
        Vector3 velocidadActual = (posicionActual - posicionAnterior) / Time.deltaTime;
        
        // Agregar al historial para suavizado
        historialVelocidades[indiceHistorial] = velocidadActual;
        indiceHistorial = (indiceHistorial + 1) % tamañoHistorial;
        
        // Calcular velocidad promedio
        Vector3 velocidadPromedio = Vector3.zero;
        for (int i = 0; i < tamañoHistorial; i++)
        {
            velocidadPromedio += historialVelocidades[i];
        }
        velocidadPromedio /= tamañoHistorial;
        
        // Calcular aceleración
        Vector3 aceleracion = (velocidadPromedio - velocidadAnterior) / Time.deltaTime;
        
        // Verificar si debe reproducir sonido
        float magnitudVelocidad = velocidadPromedio.magnitude;
        float magnitudAceleracion = aceleracion.magnitude;
        
        if (DebeReproducirSonido(magnitudVelocidad, magnitudAceleracion))
        {
            ReproducirSonidoMaracas(magnitudVelocidad, magnitudAceleracion);
        }
        
        // Actualizar valores anteriores
        velocidadAnterior = velocidadPromedio;
        posicionAnterior = posicionActual;
    }
    
    bool DebeReproducirSonido(float velocidad, float aceleracion)
    {
        // Verificar umbrales y tiempo transcurrido
        bool umbralAlcanzado = velocidad > umbralVelocidad || aceleracion > umbralAceleracion;
        bool tiempoSuficiente = Time.time - ultimoTiempoSonido > tiempoEntreSonidos;
        
        return umbralAlcanzado && tiempoSuficiente && sonidosMaracas.Length > 0;
    }
    
    void ReproducirSonidoMaracas(float velocidad, float aceleracion)
    {
        // Seleccionar sonido aleatorio
        AudioClip clipSeleccionado = sonidosMaracas[Random.Range(0, sonidosMaracas.Length)];
        
        // Calcular volumen basado en la intensidad del movimiento
        float intensidad = Mathf.Max(velocidad / (umbralVelocidad * 3), aceleracion / (umbralAceleracion * 3));
        float volumen = Mathf.Lerp(volumenMinimo, volumenMaximo, Mathf.Clamp01(intensidad));
        
        // Variar ligeramente el pitch para más realismo
        float pitch = Random.Range(0.9f, 1.1f);
        
        // Reproducir sonido
        audioSource.clip = clipSeleccionado;
        audioSource.volume = volumen;
        audioSource.pitch = pitch;
        audioSource.Play();
        
        // Actualizar tiempo del último sonido
        ultimoTiempoSonido = Time.time;
        
        Debug.Log($"Sonido reproducido - Velocidad: {velocidad:F2}, Aceleración: {aceleracion:F2}, Volumen: {volumen:F2}");
    }
    
    // Método público para ajustar sensibilidad en tiempo real
    public void AjustarSensibilidad(float nuevaVelocidad, float nuevaAceleracion)
    {
        umbralVelocidad = nuevaVelocidad;
        umbralAceleracion = nuevaAceleracion;
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualizar información de debug en el editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, velocidadAnterior * 0.1f);
        }
    }
}