using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class VRSheetsCollectorScript : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor> sockets = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnHeight = 5f;
    [SerializeField] private float fallSpeed = 2f;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip socketFillSound;
    [SerializeField] private ParticleSystem completionEffect;
    [SerializeField] private TextMeshProUGUI progressText; // Referencia al TextMeshProUGUI

    [Header("Configuración")]
    [SerializeField] private string targetTag = "sheets";
    [SerializeField] private int requiredObjects = 10;
    
    [Header("Mensajes de Ánimo")]
    [SerializeField] private List<string> encouragingMessages = new List<string>() {
        "¡Sigue así!",
        "¡Ya falta menos!",
        "¡Vas por buen camino!",
        "¡Casi lo tienes!",
        "¡Tu puedes lograrlo!"
    };

    private int currentFilledSockets = 1;
    private bool rewardDelivered = false;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Verifica que tengamos suficientes sockets
        if (sockets.Count < requiredObjects)
        {
            Debug.LogWarning("¡Advertencia! Se requieren " + requiredObjects + " sockets, pero solo hay " + sockets.Count + " configurados.");
        }
    }

    private void Start()
    {
        // Suscribirse a los eventos de los sockets
        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i] != null)
            {
                int socketIndex = i; // Para usar dentro del lambda
                sockets[i].selectEntered.AddListener((args) => OnSocketFilled(socketIndex, args));
                sockets[i].selectExited.AddListener((args) => OnSocketEmptied(socketIndex, args));
            }
            else
            {
                Debug.LogError("El socket " + i + " es nulo. Verifique las referencias en el inspector.");
            }
        }
        
        // Inicializar el texto de progreso
        UpdateProgressText();
    }

    private void OnSocketFilled(int socketIndex, SelectEnterEventArgs args)
    {
        // Verifica si el objeto tiene el tag correcto
        if (args.interactableObject.transform.CompareTag(targetTag))
        {
            currentFilledSockets++;
            
            // Reproducir sonido cuando se coloca un objeto
            if (socketFillSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(socketFillSound);
            }
            
            Debug.Log("Socket " + socketIndex + " llenado. Total de sockets llenos: " + currentFilledSockets + "/" + requiredObjects);
            
            // Mostrar mensaje de éxito por encontrar una hoja
            if (progressText != null)
            {
                progressText.text = "¡Bien hecho, has puesto la hoja de la investigación!";
                progressText.color = Color.green; // Cambiar a color verde para éxito
                // Usar una corrutina para mostrar el mensaje de éxito brevemente
                StartCoroutine(ShowMessageThenUpdateProgress(1.5f));
            }
            else
            {
                // Actualizar el texto de progreso directamente si no tenemos un TextMeshPro
                UpdateProgressText();
            }
            
            // Verifica si todos los objetos requeridos están en su lugar
            CheckCompletion();
        }
        else
        {
            // El objeto NO tiene el tag correcto
            if (progressText != null)
            {
                progressText.text = "Esa no es una hoja de investigación";
                progressText.color = Color.red; // Cambiar a color rojo para error
                // Mostrar mensaje de error temporalmente
                StartCoroutine(ShowMessageThenUpdateProgress(1.5f));
            }
            Debug.Log("Se colocó un objeto incorrecto en el socket " + socketIndex);
        }
    }

    private void OnSocketEmptied(int socketIndex, SelectExitEventArgs args)
    {
        // Verifica si el objeto tenía el tag correcto
        if (args.interactableObject.transform.CompareTag(targetTag))
        {
            currentFilledSockets--;
            Debug.Log("Socket " + socketIndex + " vaciado. Total de sockets llenos: " + currentFilledSockets + "/" + requiredObjects);
            
            // Actualizar el texto de progreso y restaurar el color
            UpdateProgressText();
            if (progressText != null)
            {
                progressText.color = Color.white;
            }
        }
        else
        {
            // Cuando se quita un objeto incorrecto, restaurar el mensaje normal y el color
            UpdateProgressText();
            if (progressText != null)
            {
                progressText.color = Color.white;
            }
            Debug.Log("Se quitó un objeto incorrecto del socket " + socketIndex);
        }
    }

    // Corrutina que muestra un mensaje temporal y luego actualiza el texto de progreso
    private IEnumerator ShowMessageThenUpdateProgress(float delay)
    {
        // Esperar el tiempo especificado
        yield return new WaitForSeconds(delay);
        
        // Actualizar el texto de progreso después del retraso y restaurar el color
        UpdateProgressText();
        
        // Restaurar el color del texto a blanco (o el color predeterminado)
        if (progressText != null)
        {
            progressText.color = Color.white;
        }
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            if (currentFilledSockets >= requiredObjects)
            {
                // Mensaje de finalización
                progressText.text = "¡Completado!\n" + currentFilledSockets + "/" + requiredObjects + " hojas recolectadas";
            }
            else
            {
                // Calcular cuántas hojas faltan
                int remaining = requiredObjects - currentFilledSockets;
                string message;
                
                if (remaining == 1)
                {
                    message = "¡Solo falta 1 hoja!";
                }
                else if (remaining <= 3)
                {
                    message = "¡Ya solo faltan " + remaining + " hojas!\n¡Tu puedes!";
                }
                else
                {
                    // Obtener un mensaje aleatorio de ánimo
                    string encouragement = encouragingMessages[Random.Range(0, encouragingMessages.Count)];
                    message = "Hojas: " + currentFilledSockets + "/" + requiredObjects + "\n" + encouragement;
                }
                
                progressText.text = message;
            }
        }
    }

    private void CheckCompletion()
    {
        if (currentFilledSockets >= requiredObjects && !rewardDelivered)
        {
            rewardDelivered = true;
            Debug.Log("¡Todos los objetos con tag 'sheets' han sido colocados! Entregando recompensa...");
            
            // Activar efectos de partículas si están disponibles
            if (completionEffect != null)
            {
                completionEffect.Play();
            }
            
            // Reproducir sonido de éxito
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound);
            }
            
            // Entregar el objeto desde el cielo
            StartCoroutine(DeliverReward());
        }
    }

    private IEnumerator DeliverReward()
    {
        // Esperar un momento para darle dramatismo
        yield return new WaitForSeconds(1.5f);
        
        // Posición desde la que caerá el objeto (encima del punto de spawn)
        Vector3 spawnPosition = spawnPoint != null 
            ? new Vector3(spawnPoint.position.x, spawnPoint.position.y + spawnHeight, spawnPoint.position.z) 
            : new Vector3(transform.position.x, transform.position.y + spawnHeight, transform.position.z);
        
        // Instanciar el objeto
        GameObject reward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity);
        
        // Si el objeto tiene un Rigidbody, aplicarle una velocidad inicial
        Rigidbody rb = reward.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * fallSpeed;
        }
        else
        {
            // Si no tiene Rigidbody, simular la caída manualmente
            StartCoroutine(AnimateRewardFall(reward, spawnPosition));
        }
    }

    private IEnumerator AnimateRewardFall(GameObject reward, Vector3 startPosition)
    {
        Vector3 targetPosition = spawnPoint != null 
            ? spawnPoint.position 
            : new Vector3(transform.position.x, transform.position.y, transform.position.z);
        
        float distance = Vector3.Distance(new Vector3(startPosition.x, targetPosition.y, startPosition.z), targetPosition);
        float duration = distance / fallSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float y = Mathf.Lerp(startPosition.y, targetPosition.y, t);
            reward.transform.position = new Vector3(startPosition.x, y, startPosition.z);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Asegurarse de que el objeto esté exactamente en la posición objetivo al final
        reward.transform.position = targetPosition;
    }

    private void OnDestroy()
    {
        // Desuscribirse de los eventos para evitar memory leaks
        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i] != null)
            {
                sockets[i].selectEntered.RemoveAllListeners();
                sockets[i].selectExited.RemoveAllListeners();
            }
        }
    }
}