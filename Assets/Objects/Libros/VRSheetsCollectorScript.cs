using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using Unity.Netcode;
using System.Linq;

public class NetworkedSheetsCollector : NetworkBehaviour
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
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Configuración")]
    [SerializeField] private string targetTag = "sheets";
    [SerializeField] private int sheetsPerPlayer = 3;
    [SerializeField] private int maxPlayers = 2;
    
    [Header("Mensajes de Ánimo")]
    [SerializeField] private List<string> encouragingMessages = new List<string>() {
        "¡Sigue así!",
        "¡Ya falta menos!",
        "¡Vas por buen camino!",
        "¡Casi lo tienes!",
        "¡Tu puedes lograrlo!"
    };

    // Variables de red
    private NetworkVariable<int> currentFilledSockets = new NetworkVariable<int>(1);
    private NetworkVariable<int> requiredObjects = new NetworkVariable<int>(3); // Valor predeterminado para 1 jugador
    private NetworkVariable<bool> rewardDelivered = new NetworkVariable<bool>(false);
    
    private AudioSource audioSource;
    private int previousSocketCount = 1; // Para detectar cambios en currentFilledSockets

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // Configurar los sockets y requerimientos basados en el número de jugadores
            UpdatePlayerRequirements();
            
            // Suscribirse al evento de cambio de jugadores
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerCountChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerCountChanged;
        }
        
        // Suscribirse a los eventos de los sockets
        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i] != null)
            {
                int socketIndex = i;
                sockets[i].selectEntered.AddListener((args) => OnSocketFilled(socketIndex, args));
                sockets[i].selectExited.AddListener((args) => OnSocketEmptied(socketIndex, args));
            }
            else
            {
                Debug.LogError("El socket " + i + " es nulo. Verifique las referencias en el inspector.");
            }
        }
        
        // Suscribirse a los cambios de la variable de red
        currentFilledSockets.OnValueChanged += OnFilledSocketsChanged;
        
        // Inicializar el texto de progreso
        UpdateProgressText();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerCountChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerCountChanged;
        }
        
        // Desuscribirse de los eventos de la variable de red
        currentFilledSockets.OnValueChanged -= OnFilledSocketsChanged;
        
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
    
    private void OnPlayerCountChanged(ulong clientId)
    {
        if (IsServer)
        {
            UpdatePlayerRequirements();
        }
    }
    
    private void UpdatePlayerRequirements()
    {
        if (!IsServer) return;
        
        // Obtener el número de jugadores conectados (incluyendo el host/servidor)
        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        playerCount = Mathf.Clamp(playerCount, 1, maxPlayers);
        
        // Calcular el número requerido de objetos (3 por jugador)
        int newRequiredObjects = playerCount * sheetsPerPlayer;
        requiredObjects.Value = newRequiredObjects;
        
        // Asegurarse de que tenemos suficientes sockets activados
        ActivateSocketsBasedOnPlayerCount(playerCount);
        
        Debug.Log($"Jugadores conectados: {playerCount}, Hojas requeridas: {requiredObjects.Value}");
    }
    
    private void ActivateSocketsBasedOnPlayerCount(int playerCount)
    {
        // Activar/desactivar sockets según el número de jugadores
        int socketsToActivate = playerCount * sheetsPerPlayer;
        
        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i] != null)
            {
                bool shouldBeActive = i < socketsToActivate;
                sockets[i].gameObject.SetActive(shouldBeActive);
            }
        }
    }

    private void OnFilledSocketsChanged(int previous, int current)
    {
        // Este método se llama en todos los clientes cuando currentFilledSockets cambia
        if (current > previousSocketCount)
        {
            // Se llenó un socket
            if (socketFillSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(socketFillSound);
            }
            
            ShowSuccessMessage();
        }
        
        previousSocketCount = current;
        UpdateProgressText();
        
        // Verificar si se completó la tarea (solo para UI y efectos, la lógica real está en el servidor)
        if (current >= requiredObjects.Value && !rewardDelivered.Value)
        {
            // Mostrar efectos de completado
            if (completionEffect != null)
            {
                completionEffect.Play();
            }
            
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound);
            }
        }
    }

    private void OnSocketFilled(int socketIndex, SelectEnterEventArgs args)
    {
        // Verifica si el objeto tiene el tag correcto
        if (args.interactableObject.transform.CompareTag(targetTag))
        {
            // Solo el servidor actualiza la variable de red
            if (IsServer)
            {
                currentFilledSockets.Value++;
                CheckCompletion();
            }
        }
        else
        {
            // El objeto NO tiene el tag correcto
            if (progressText != null)
            {
                progressText.text = "Esa no es una hoja de investigación";
                progressText.color = Color.red;
                StartCoroutine(ShowMessageThenUpdateProgress(1.5f));
            }
        }
    }

    private void OnSocketEmptied(int socketIndex, SelectExitEventArgs args)
    {
        // Verifica si el objeto tenía el tag correcto
        if (args.interactableObject.transform.CompareTag(targetTag))
        {
            // Solo el servidor actualiza la variable de red
            if (IsServer)
            {
                currentFilledSockets.Value--;
            }
            
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
        }
    }

    private void ShowSuccessMessage()
    {
        if (progressText != null)
        {
            progressText.text = "¡Bien hecho, has puesto la hoja de la investigación!";
            progressText.color = Color.green;
            StartCoroutine(ShowMessageThenUpdateProgress(1.5f));
        }
    }

    private IEnumerator ShowMessageThenUpdateProgress(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateProgressText();
        
        if (progressText != null)
        {
            progressText.color = Color.white;
        }
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            if (currentFilledSockets.Value >= requiredObjects.Value)
            {
                progressText.text = "¡Completado!\n" + currentFilledSockets.Value + "/" + requiredObjects.Value + " hojas recolectadas";
            }
            else
            {
                int remaining = requiredObjects.Value - currentFilledSockets.Value;
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
                    string encouragement = encouragingMessages[Random.Range(0, encouragingMessages.Count)];
                    message = "Hojas: " + currentFilledSockets.Value + "/" + requiredObjects.Value + "\n" + encouragement;
                }
                
                progressText.text = message;
            }
        }
    }

    private void CheckCompletion()
    {
        if (!IsServer) return;
        
        if (currentFilledSockets.Value >= requiredObjects.Value && !rewardDelivered.Value)
        {
            rewardDelivered.Value = true;
            Debug.Log("¡Todos los objetos con tag 'sheets' han sido colocados! Entregando recompensa...");
            
            // La recompensa solo se entrega desde el servidor
            DeliverRewardServerRpc();
        }
    }

    [ServerRpc]
    private void DeliverRewardServerRpc()
    {
        // Notificar a todos los clientes para reproducir efectos
        DeliverRewardClientRpc();
        
        // Esperar un momento y luego entregar el premio en el servidor
        StartCoroutine(DeliverReward());
    }

    [ClientRpc]
    private void DeliverRewardClientRpc()
    {
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
    }

    private IEnumerator DeliverReward()
    {
        // Esperar un momento para darle dramatismo
        yield return new WaitForSeconds(1.5f);
        
        // Posición desde la que caerá el objeto
        Vector3 spawnPosition = spawnPoint != null 
            ? new Vector3(spawnPoint.position.x, spawnPoint.position.y + spawnHeight, spawnPoint.position.z) 
            : new Vector3(transform.position.x, transform.position.y + spawnHeight, transform.position.z);
        
        // Instanciar el objeto como objeto en red
        GameObject reward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity);
        
        // Si estamos en el servidor, hacer que sea un objeto en red
        if (IsServer)
        {
            NetworkObject networkObject = reward.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            else
            {
                Debug.LogWarning("La recompensa no tiene un componente NetworkObject.");
            }
        }
        
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
}