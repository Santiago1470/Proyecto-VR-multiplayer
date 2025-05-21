using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

public class DespegueButton : NetworkBehaviour
{
    [Header("References")]
    public RocketLaunch rocket;

    [Header("Visual Settings")]
    public float pressDepth = 0.02f; // Profundidad del hundimiento visual

    private XRBaseInteractable interactable;
    private Vector3 originalPosition;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        originalPosition = transform.localPosition;
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
        Debug.Log("DespegueButton: Botón presionado!"); // Para debug

        // Efecto visual inmediato
        transform.localPosition = originalPosition - new Vector3(0, pressDepth, 0);

        // Enviar comando al servidor para lanzar el cohete
        OnButtonPressedServerRpc();
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        // Restaurar la posición original del botón
        transform.localPosition = originalPosition;
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnButtonPressedServerRpc()
    {
        Debug.Log("DespegueButton: ServerRPC llamado!"); // Para debug

        // Solo el servidor ejecuta la lógica del juego
        if (!IsServer) return;

        Debug.Log("DespegueButton: Ejecutando en servidor!"); // Para debug

        // Lanzar el cohete
        if (rocket != null)
        {
            Debug.Log("DespegueButton: Llamando LaunchRocketServerRpc!"); // Para debug
            rocket.LaunchRocketServerRpc();
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
        Debug.Log("DespegueButton: Desactivando botón!"); // Para debug
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