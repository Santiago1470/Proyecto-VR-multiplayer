using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

public class DespegueButton : NetworkBehaviour
{
    [Header("References")]
    public RocketLaunch rocket;

    private XRBaseInteractable interactable;

    private void Awake()
    {
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
        }
    }

    private void DisableInteraction()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnButtonPressed);
        }
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        // Enviar comando al servidor para lanzar el cohete
        OnButtonPressedServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnButtonPressedServerRpc()
    {
        // Solo el servidor ejecuta la lógica del juego
        if (!IsServer) return;

        // Lanzar el cohete
        if (rocket != null)
            rocket.LaunchRocketServerRpc();

        // Desactivar este botón en todos los clientes
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
    }
}
