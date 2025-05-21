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

    // Propiedad para detectar si estamos en multijugador
    private bool IsMultiplayer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        originalPosition = transform.localPosition;
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

        // En multijugador, habilitar solo en clientes
        if (IsMultiplayer && IsClient)
        {
            EnableInteraction();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsMultiplayer && IsClient)
        {
            DisableInteraction();
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
        Debug.Log("DespegueButton: Botón presionado!");

        // Efecto visual inmediato
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
        // Restaurar la posición original del botón
        transform.localPosition = originalPosition;
    }

    // ==================== MULTIJUGADOR ====================
    [ServerRpc(RequireOwnership = false)]
    private void OnButtonPressedServerRpc()
    {
        Debug.Log("DespegueButton: ServerRPC llamado!");

        if (!IsServer) return;

        Debug.Log("DespegueButton: Ejecutando en servidor!");

        // Lanzar el cohete
        if (rocket != null)
        {
            Debug.Log("DespegueButton: Llamando LaunchRocket!");
            rocket.LaunchRocket();
        }
        else
        {
            Debug.LogError("DespegueButton: RocketLaunch reference is null!");
        }

        // Desactivar este botón en todos los clientes
        DeactivateButtonClientRpc();
    }

    [ClientRpc]
    private void DeactivateButtonClientRpc()
    {
        Debug.Log("DespegueButton: Desactivando botón!");
        gameObject.SetActive(false);
    }

    [ClientRpc]
    public void ReactivateButtonClientRpc()
    {
        gameObject.SetActive(true);
        transform.localPosition = originalPosition;
    }

    // ==================== SINGLEPLAYER ====================
    private void OnButtonPressedSingleplayer()
    {
        Debug.Log("DespegueButton: Modo singleplayer - lanzando cohete!");

        // Lanzar el cohete
        if (rocket != null)
        {
            rocket.LaunchRocket();
        }
        else
        {
            Debug.LogError("DespegueButton: RocketLaunch reference is null!");
        }

        // Desactivar este botón
        gameObject.SetActive(false);
    }

    // Método público para reactivar en singleplayer
    public void ReactivateButton()
    {
        gameObject.SetActive(true);
        transform.localPosition = originalPosition;
    }
}