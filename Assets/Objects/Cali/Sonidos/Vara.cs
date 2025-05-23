using UnityEngine;

public class VRVelocityTracker : MonoBehaviour
{
    [Header("Configuración de Tracking")]
    public int muestrasVelocidad = 5; // Número de muestras para promediar
    public float multiplicadorSensibilidad = 1.0f; // Multiplicador de sensibilidad
    
    // Variables privadas para tracking
    private Vector3[] historialPosiciones;
    private float[] historialTiempos;
    private int indiceActual = 0;
    private Vector3 velocidadActual = Vector3.zero;
    private Vector3 posicionAnterior;
    
    void Start()
    {
        // Inicializar arrays de historial
        historialPosiciones = new Vector3[muestrasVelocidad];
        historialTiempos = new float[muestrasVelocidad];
        
        // Llenar con posición inicial
        for (int i = 0; i < muestrasVelocidad; i++)
        {
            historialPosiciones[i] = transform.position;
            historialTiempos[i] = Time.time;
        }
        
        posicionAnterior = transform.position;
    }
    
    void Update()
    {
        // Actualizar historial de posiciones
        ActualizarHistorial();
        
        // Calcular velocidad promedio
        CalcularVelocidad();
    }
    
    void ActualizarHistorial()
    {
        // Guardar posición y tiempo actuales
        historialPosiciones[indiceActual] = transform.position;
        historialTiempos[indiceActual] = Time.time;
        
        // Avanzar índice circular
        indiceActual = (indiceActual + 1) % muestrasVelocidad;
    }
    
    void CalcularVelocidad()
    {
        // Calcular velocidad usando método de diferencias finitas
        Vector3 sumaVelocidades = Vector3.zero;
        int muestrasValidas = 0;
        
        for (int i = 0; i < muestrasVelocidad - 1; i++)
        {
            int indice1 = (indiceActual + i) % muestrasVelocidad;
            int indice2 = (indiceActual + i + 1) % muestrasVelocidad;
            
            float deltaTime = historialTiempos[indice2] - historialTiempos[indice1];
            
            if (deltaTime > 0)
            {
                Vector3 deltaPos = historialPosiciones[indice2] - historialPosiciones[indice1];
                sumaVelocidades += deltaPos / deltaTime;
                muestrasValidas++;
            }
        }
        
        // Promediar y aplicar multiplicador
        if (muestrasValidas > 0)
        {
            velocidadActual = (sumaVelocidades / muestrasValidas) * multiplicadorSensibilidad;
        }
    }
    
    // Método público para obtener la velocidad
    public Vector3 GetVelocidad()
    {
        return velocidadActual;
    }
    
    // Método público para obtener la magnitud de velocidad
    public float GetMagnitudVelocidad()
    {
        return velocidadActual.magnitude;
    }
    
    // Método para obtener solo velocidad en cierta dirección
    public float GetVelocidadEnDireccion(Vector3 direccion)
    {
        return Vector3.Dot(velocidadActual, direccion.normalized);
    }
    
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // Dibujar vector de velocidad
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, velocidadActual * 0.1f);
            
            // Dibujar magnitud como esfera
            float magnitud = velocidadActual.magnitude;
            Gizmos.color = Color.Lerp(Color.green, Color.red, magnitud / 10f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, magnitud * 0.02f);
        }
    }
}