using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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

    [Header("Configuración")]
    [SerializeField] private string targetTag = "sheets";
    [SerializeField] private int requiredObjects = 10;

    private int currentFilledSockets = 0;
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
            
            // Verifica si todos los objetos requeridos están en su lugar
            CheckCompletion();
        }
    }

    private void OnSocketEmptied(int socketIndex, SelectExitEventArgs args)
    {
        // Verifica si el objeto tenía el tag correcto
        if (args.interactableObject.transform.CompareTag(targetTag))
        {
            currentFilledSockets--;
            Debug.Log("Socket " + socketIndex + " vaciado. Total de sockets llenos: " + currentFilledSockets + "/" + requiredObjects);
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