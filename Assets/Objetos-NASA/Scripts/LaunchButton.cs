
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

public class LaunchButton : NetworkBehaviour
{
    [Header("References")]
    public RocketLaunch rocket;
    public TechoMover techo;

    [Header("Visual Settings")]
    public float pressDepth = 0.02f;

    private Vector3 originalPosition;
    private XRBaseInteractable interactable;

    // Network variable para controlar el estado del botón
    private NetworkVariable<bool> isButtonActive = new NetworkVariable<bool>(true);

    // Propiedad para detectar si estamos en multijugador
    private bool IsMultiplayer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        interactable = GetComponent<XRBaseInteractable>();
    }

    void Start()
    {
        // En singleplayer, habilitar interacción inmediatamente
        if (!IsMultiplayer)
        {
            EnableInteraction();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse a cambios en el estado del botón
        if (IsMultiplayer)
        {
            isButtonActive.OnValueChanged += OnButtonActiveChanged;

            // Aplicar el estado actual
            OnButtonActiveChanged(false, isButtonActive.Value);

            // En multijugador, habilitar solo en clientes
            if (IsClient)
            {
                EnableInteraction();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsMultiplayer)
        {
            if (isButtonActive != null)
                isButtonActive.OnValueChanged -= OnButtonActiveChanged;

            if (IsClient)
            {
                DisableInteraction();
            }
        }

        base.OnNetworkDespawn();
    }

    void OnDestroy()
    {
        // Cleanup para singleplayer
        if (!IsMultiplayer)
        {
            DisableInteraction();
        }
    }

    private void OnButtonActiveChanged(bool previousValue, bool newValue)
    {
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
        }
    }

    private void DisableInteraction()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnButtonPressed);
            interactable.selectExited.RemoveListener(OnButtonReleased);
        }
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        // Efectos visuales locales inmediatos
        transform.localPosition = originalPosition - new Vector3(0, pressDepth, 0);

        if (IsMultiplayer)
        {
            // Modo multijugador
            OnButtonPressedServerRpc();
        }
        else
        {
            // Modo singleplayer
            OnButtonPressedSingleplayer();
        }
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        // Restaurar posición local inmediatamente
        transform.localPosition = originalPosition;
    }

    // ==================== MULTIJUGADOR ====================
    [ServerRpc(RequireOwnership = false)]
    private void OnButtonPressedServerRpc()
    {
        if (!IsServer) return;

        // Verificaciones adicionales
        if (rocket != null && !rocket.IsSpawned)
        {
            Debug.LogError("LaunchButton: RocketLaunch no está spawneado correctamente!");
            return;
        }

        // Abrir el techo
        if (techo != null)
            techo.AbrirTecho();

        // Iniciar la cuenta regresiva del cohete
        if (rocket != null)
            rocket.StartLaunch();

        // Desactivar el botón usando NetworkVariable
        isButtonActive.Value = false;
    }

    // Método para reactivar el botón desde el servidor
    [ServerRpc(RequireOwnership = false)]
    public void ReactivateButtonServerRpc()
    {
        if (!IsServer) return;

        isButtonActive.Value = true;
    }

    // ==================== SINGLEPLAYER ====================
    private void OnButtonPressedSingleplayer()
    {
        // Abrir el techo
        if (techo != null)
            techo.AbrirTecho();

        // Iniciar la cuenta regresiva del cohete
        if (rocket != null)
            rocket.StartLaunch();

        // Desactivar el botón
        gameObject.SetActive(false);
    }

    // Método público para reactivar en singleplayer
    public void ReactivateButton()
    {
        gameObject.SetActive(true);
        transform.localPosition = originalPosition;
    }
}
