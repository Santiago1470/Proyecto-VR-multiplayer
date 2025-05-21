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

    private void Awake()
    {
        originalPosition = transform.localPosition;
        interactable = GetComponent<XRBaseInteractable>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Solo habilitar interacción en clientes (no en servidor dedicado)
        if (IsClient)
        {
            EnableInteraction();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            DisableInteraction();
        }

        base.OnNetworkDespawn();
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

        // Enviar comando al servidor
        OnButtonPressedServerRpc();
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        // Restaurar posición local inmediatamente
        transform.localPosition = originalPosition;
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnButtonPressedServerRpc()
    {
        // Solo el servidor ejecuta la lógica del juego
        if (!IsServer) return;

        // Abrir el techo
        if (techo != null)
            techo.AbrirTecho();

        // Iniciar la cuenta regresiva del cohete
        if (rocket != null)
            rocket.StartLaunchServerRpc();

        // Desactivar el botón en todos los clientes
        DeactivateButtonClientRpc();
    }

    [ClientRpc]
    private void DeactivateButtonClientRpc()
    {
        gameObject.SetActive(false);
    }

    // Método público para reactivar el botón (llamado desde RocketLaunch)
    [ClientRpc]
    public void ReactivateButtonClientRpc()
    {
        gameObject.SetActive(true);
        transform.localPosition = originalPosition;
    }
}

