using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

public class DespegueButton : NetworkBehaviour
{
    [Header("References")]
    public RocketLaunch rocket;

    [Header("Visual Settings")]
    public float pressDepth = 0.02f;

    private XRBaseInteractable interactable;
    private Vector3 originalPosition;

    // Network variable para controlar el estado del botón
    private NetworkVariable<bool> isButtonActive = new NetworkVariable<bool>(true);

    // Propiedad para detectar si estamos en multijugador
    private bool IsMultiplayer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        originalPosition = transform.localPosition;

        // Debug para verificar configuración
        Debug.Log($"DespegueButton Awake: GameObject name = {gameObject.name}");
    }

    void Start()
    {
        // En singleplayer, habilitar interacción inmediatamente
        if (!IsMultiplayer)
        {
            Debug.Log("DespegueButton: Modo singleplayer detectado");
            EnableInteraction();
        }
        else
        {
            Debug.Log("DespegueButton: Modo multijugador detectado - esperando NetworkSpawn");
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"DespegueButton: OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");

        base.OnNetworkSpawn();

        // Suscribirse a cambios en el estado del botón
        if (IsMultiplayer)
        {
            isButtonActive.OnValueChanged += OnButtonActiveChanged;

            // Aplicar el estado actual
            OnButtonActiveChanged(false, isButtonActive.Value);

            // Habilitar interacción
            EnableInteraction();

            Debug.Log("DespegueButton: NetworkSpawn completado correctamente");
        }
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("DespegueButton: OnNetworkDespawn");

        if (IsMultiplayer)
        {
            if (isButtonActive != null)
                isButtonActive.OnValueChanged -= OnButtonActiveChanged;

            DisableInteraction();
        }

        base.OnNetworkDespawn();
    }

    void OnDestroy()
    {
        Debug.Log("DespegueButton: OnDestroy");

        // Cleanup para singleplayer
        if (!IsMultiplayer)
        {
            DisableInteraction();
        }
    }

    private void OnButtonActiveChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"DespegueButton: Estado del botón cambió de {previousValue} a {newValue}");
        gameObject.SetActive(newValue);
        if (newValue)
        {
            transform.localPosition = originalPosition;
        }
    }

    private void EnableInteraction()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnButtonPressed);
            interactable.selectExited.AddListener(OnButtonReleased);
            interactable.enabled = true;

            Debug.Log("DespegueButton: Interacción habilitada correctamente");
        }
        else
        {
            Debug.LogError("DespegueButton: XRBaseInteractable component not found!");
        }
    }

    private void DisableInteraction()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnButtonPressed);
            interactable.selectExited.RemoveListener(OnButtonReleased);
            Debug.Log("DespegueButton: Interacción deshabilitada");
        }
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        Debug.Log("DespegueButton: ¡Botón presionado!");

        // Verificar que el botón esté activo
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("DespegueButton: Botón presionado pero no está activo!");
            return;
        }

        // Efecto visual inmediato
        transform.localPosition = originalPosition - new Vector3(0, pressDepth, 0);

        if (IsMultiplayer)
        {
            // Verificaciones adicionales para multijugador
            if (!IsSpawned)
            {
                Debug.LogError("DespegueButton: No se puede llamar ServerRpc - objeto no spawneado");
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("DespegueButton: NetworkManager.Singleton es null");
                return;
            }

            if (!NetworkManager.Singleton.IsListening)
            {
                Debug.LogError("DespegueButton: NetworkManager no está escuchando");
                return;
            }

            Debug.Log("DespegueButton: Llamando OnButtonPressedServerRpc...");

            // Usar try-catch para capturar errores de RPC
            try
            {
                OnButtonPressedServerRpc();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DespegueButton: Error al llamar ServerRpc: {e.Message}");
                Debug.LogError($"DespegueButton: Stack trace: {e.StackTrace}");
            }
        }
        else
        {
            // Modo singleplayer
            OnButtonPressedSingleplayer();
        }
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        // Restaurar la posición original del botón
        transform.localPosition = originalPosition;
    }

    // ==================== MULTIJUGADOR ====================
    [ServerRpc(RequireOwnership = false)]
    private void OnButtonPressedServerRpc()
    {
        Debug.Log("DespegueButton: ServerRPC recibido!");

        if (!IsServer)
        {
            Debug.LogWarning("DespegueButton: ServerRpc llamado pero no somos servidor");
            return;
        }

        Debug.Log("DespegueButton: Ejecutando en servidor!");

        // Verificar que el rocket existe y es válido
        if (rocket != null)
        {
            if (rocket.IsSpawned)
            {
                Debug.Log("DespegueButton: Llamando LaunchRocket!");
                rocket.LaunchRocket();

                // Desactivar este botón usando NetworkVariable
                isButtonActive.Value = false;
                Debug.Log("DespegueButton: Botón desactivado");
            }
            else
            {
                Debug.LogError("DespegueButton: RocketLaunch no está spawneado correctamente!");
            }
        }
        else
        {
            Debug.LogError("DespegueButton: RocketLaunch reference is null!");
        }
    }

    // Método para reactivar el botón desde el servidor
    [ServerRpc(RequireOwnership = false)]
    public void ReactivateButtonServerRpc()
    {
        Debug.Log("DespegueButton: ReactivateButtonServerRpc llamado");

        if (!IsServer) return;

        isButtonActive.Value = true;
        Debug.Log("DespegueButton: Botón reactivado");
    }

    // ==================== SINGLEPLAYER ====================
    private void OnButtonPressedSingleplayer()
    {
        Debug.Log("DespegueButton: Modo singleplayer - lanzando cohete!");

        // Lanzar el cohete
        if (rocket != null)
        {
            rocket.LaunchRocket();

            // Desactivar este botón
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("DespegueButton: RocketLaunch reference is null!");
        }
    }

    // Método público para reactivar en singleplayer
    public void ReactivateButton()
    {
        Debug.Log("DespegueButton: Reactivando botón en singleplayer");
        gameObject.SetActive(true);
        transform.localPosition = originalPosition;
    }
}