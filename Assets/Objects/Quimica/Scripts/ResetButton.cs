using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public class ResetButton : NetworkBehaviour
{
    [Header("Referencias")]
    public ResetManager resetManager;
    
    [Header("Configuración")]
    [SerializeField] private bool onlyHostCanUse = false; // Si solo el host puede usar el botón
    
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        
        // Buscar ResetManager si no está asignado
        if (resetManager == null)
        {
            resetManager = FindObjectOfType<ResetManager>();
        }
    }

    public override void OnNetworkSpawn()
    {
        // Asegurar que el botón esté activo cuando se spawne
        gameObject.SetActive(true);
        
        // Buscar ResetManager nuevamente por si no se había spawneado antes
        if (resetManager == null)
        {
            resetManager = FindObjectOfType<ResetManager>();
        }
    }

    void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnPressed);
        }
    }

    void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnPressed);
        }
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        // Verificar si solo el host puede usar el botón
        if (onlyHostCanUse && !IsHost)
        {
            Debug.Log("Solo el host puede usar el botón de reset");
            return;
        }

        // Verificar que estemos en una sesión de red
        if (!IsSpawned)
        {
            Debug.LogWarning("ResetButton: No estamos en una sesión de red activa");
            return;
        }

        // Verificar que tengamos referencia al ResetManager
        if (resetManager == null)
        {
            Debug.LogError("ResetButton: No se encontró ResetManager");
            return;
        }

        // Solicitar el reset
        resetManager.RequestResetAllTubes();
        
        Debug.Log($"Reset solicitado por {(IsHost ? "Host" : "Cliente")}");
    }
}