using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public bool playOnce = true;
    public float fadeInTime = 1.0f;
    public float fadeOutTime = 1.0f;
    
    [Header("Debug")]
    public bool showDebugGizmo = true;
    public Color gizmoColor = Color.green;
    
    private bool hasPlayed = false;
    private bool isInside = false;
    private float currentVolume = 0f;
    private float targetVolume = 0f;
    private float initialVolume;

    private void Start()
    {
        // Verifica si hay un AudioSource adjunto
        if (audioSource == null)
        {
            // Intenta obtener el componente del mismo objeto
            audioSource = GetComponent<AudioSource>();
            
            // Si todavía no hay AudioSource, crea uno
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("AudioTrigger: Se ha creado un AudioSource automáticamente. Por favor, configura el clip de audio.");
            }
        }
        
        // Guarda el volumen inicial y apaga el audio
        initialVolume = audioSource.volume;
        audioSource.volume = 0;
        
        // Configura el AudioSource para que no se reproduzca automáticamente
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        // Si estamos dentro del trigger, aumentamos el volumen gradualmente
        if (isInside)
        {
            targetVolume = initialVolume;
        }
        else
        {
            targetVolume = 0;
        }
        
        // Ajustar el volumen gradualmente
        float fadeSpeed = isInside ? fadeInTime : fadeOutTime;
        fadeSpeed = fadeSpeed <= 0 ? 0.01f : fadeSpeed;
        
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime / fadeSpeed);
        audioSource.volume = currentVolume;
        
        // Si el volumen es casi cero y no estamos en el trigger, detenemos el audio
        if (!isInside && currentVolume < 0.01f && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que entró es el jugador
        if (other.CompareTag("Player"))
        {
            isInside = true;
            
            // Si el audio debe reproducirse solo una vez y ya se ha reproducido, salimos
            if (playOnce && hasPlayed)
                return;
            
            // Reproducir audio si no está ya sonando
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                hasPlayed = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Verifica si el objeto que salió es el jugador
        if (other.CompareTag("Player"))
        {
            isInside = false;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (showDebugGizmo)
        {
            // Dibuja un wireframe del collider para visualizar el área del trigger
            Gizmos.color = gizmoColor;
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                if (collider is BoxCollider)
                {
                    BoxCollider boxCollider = (BoxCollider)collider;
                    Gizmos.DrawWireCube(transform.position + boxCollider.center, Vector3.Scale(boxCollider.size, transform.localScale));
                }
                else if (collider is SphereCollider)
                {
                    SphereCollider sphereCollider = (SphereCollider)collider;
                    Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z));
                }
            }
        }
    }
    
    // Método para reiniciar el trigger (para reutilizarlo)
    public void Reset()
    {
        hasPlayed = false;
    }
}